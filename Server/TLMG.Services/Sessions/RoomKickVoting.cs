using System;
using System.Collections.Generic;

using TLMG.Domain.Moderation;

namespace TLMG.Services.Sessions
{
    internal sealed class RoomKickVoting
    {
        private readonly Dictionary<int, KickVoting> _votesByTarget = new Dictionary<int, KickVoting>();
        private readonly HashSet<int> _excludedUserIds = new HashSet<int>();
        private readonly HashSet<Guid> _excludedGuestSessions = new HashSet<Guid>();

        public KickVoteStatus RegisterVote(int voterUserId, int targetUserId, IReadOnlyCollection<int> participants)
        {
            KickVoting voting;

            if (!_votesByTarget.TryGetValue(targetUserId, out voting) || !voting.HasSameParticipants(participants))
            {
                voting = new KickVoting(targetUserId, participants);

                if (voting.IsValid)
                {
                    _votesByTarget[targetUserId] = voting;
                }
            }

            KickVoteStatus status = voting.RegisterVote(voterUserId);

            return status;
        }

        public IReadOnlyList<int> CopyConsensusVoterIds(int targetUserId)
        {
            IReadOnlyList<int> voterUserIds = Array.Empty<int>();
            if (_votesByTarget.TryGetValue(targetUserId, out KickVoting voting) && voting.HasConsensus)
            {
                voterUserIds = voting.CopyVoterUserIds();
            }

            return voterUserIds;
        }

        public void ResetVotes()
        {
            _votesByTarget.Clear();
        }

        public void Exclude(PlayerSession session)
        {
            if (session.IsRegistered)
            {
                _excludedUserIds.Add(session.UserId);
            }
            else
            {
                _excludedGuestSessions.Add(session.DiagnosticId);
            }
            ResetVotes();
        }

        public bool IsExcluded(PlayerSession session)
        {
            bool isExcluded = _excludedUserIds.Contains(session.UserId)
                || _excludedGuestSessions.Contains(session.DiagnosticId);

            return isExcluded;
        }
    }
}