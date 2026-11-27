using System;
using System.Collections.Concurrent;

using log4net;

using TLMG.Domain.Repositories;

namespace TLMG.Services.Moderation
{
    internal sealed class KickVotingHistory
    {
        private const int RetryDelaySeconds = 5;
        private static readonly ILog _log = LogManager.GetLogger(typeof(KickVotingHistory));
        private readonly IKickVotingRepository _repository;
        private readonly ConcurrentQueue<CompletedKickVoting> _pending = new ConcurrentQueue<CompletedKickVoting>();
        private readonly object _persistenceLock = new object();
        private DateTime _nextAttemptUtc;

        internal KickVotingHistory(IKickVotingRepository repository)
        {
            _repository = repository;
        }

        internal void Enqueue(CompletedKickVoting voting)
        {
            _pending.Enqueue(voting);
        }

        internal void PersistPending()
        {
            lock (_persistenceLock)
            {
                if (!_pending.IsEmpty && DateTime.UtcNow >= _nextAttemptUtc)
                {
                    int pendingCount = _pending.Count;
                    bool hasFailure = false;
                    for (int votingIndex = 0; votingIndex < pendingCount; votingIndex++)
                    {
                        if (_pending.TryDequeue(out CompletedKickVoting voting))
                        {
                            bool wasSaved = PersistVoting(voting);
                            if (!wasSaved)
                            {
                                hasFailure = true;
                            }
                        }
                    }
                    if (hasFailure)
                    {
                        _nextAttemptUtc = DateTime.UtcNow.AddSeconds(RetryDelaySeconds);
                    }
                }
            }
        }

        private bool PersistVoting(CompletedKickVoting voting)
        {
            KickVotingWriteStatus status = _repository.RecordCompletedVoting(voting);
            bool wasSaved = status == KickVotingWriteStatus.Recorded
                || status == KickVotingWriteStatus.AlreadyRecorded;
            if (!wasSaved)
            {
                _pending.Enqueue(voting);
                _log.ErrorFormat("Completed kick voting {0} is pending persistence: {1}.", voting.VotingId, status);
            }

            return wasSaved;
        }
    }
}