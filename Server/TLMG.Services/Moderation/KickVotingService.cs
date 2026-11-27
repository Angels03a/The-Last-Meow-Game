using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Moderation;
using TLMG.Services.Lobby;
using TLMG.Services.Sessions;

namespace TLMG.Services.Moderation
{
    public sealed class KickVotingService
    {
        private const string InvalidVoteErrorKey = "Moderation_InvalidVoteError";
        private const string AlreadyVotedErrorKey = "Moderation_AlreadyVotedError";
        private const string TargetUnavailableErrorKey = "Moderation_KickTargetUnavailableError";
        private static readonly ILog _log = LogManager.GetLogger(typeof(KickVotingService));
        private readonly IKickSessionAccess _sessionAccess;
        private readonly IMatchStartCoordinator _matchStartCoordinator;

        public KickVotingService(IKickSessionAccess sessionAccess, IMatchStartCoordinator matchStartCoordinator)
        {
            _sessionAccess = sessionAccess;
            _matchStartCoordinator = matchStartCoordinator;
        }

        public ResultWithNotification<OperationResult> VoteToKick(string sessionToken, KickVote vote)
        {
            OperationResult operation = new OperationResult { ErrorKey = ErrorKeys.Common.Unexpected };
            IReadOnlyList<Action> notifications = Array.Empty<Action>();
            KickVoteChange change = _sessionAccess.RegisterKickVote(sessionToken, vote);

            if (change == null || change.Notifications == null)
            {
                _log.Error("No se obtuvo un resultado válido de la votación.");
            }
            else
            {
                notifications = change.Notifications;
                operation.IsSuccess = change.Status == KickVoteStatus.VoteRecorded
                    || change.Status == KickVoteStatus.ConsensusReached;

                if (operation.IsSuccess)
                {
                    operation.ErrorKey = string.Empty;
                    if (change.Status == KickVoteStatus.ConsensusReached)
                    {
                        _matchStartCoordinator.ReconcileCancelledStarts();
                    }
                }
                else
                {
                    operation.ErrorKey = FindVoteErrorKey(change.Status);
                }
            }

            ResultWithNotification<OperationResult> completion = new ResultWithNotification<OperationResult>(
                operation, notifications);

            return completion;
        }

        private static string FindVoteErrorKey(KickVoteStatus status)
        {
            string errorKey = ErrorKeys.Common.Unexpected;

            if (status == KickVoteStatus.InvalidSession)
            {
                errorKey = ErrorKeys.Common.InvalidSession;
            }
            else if (status == KickVoteStatus.NotInRoom)
            {
                errorKey = ErrorKeys.Lobby.NotInRoom;
            }
            else if (status == KickVoteStatus.InvalidVoting || status == KickVoteStatus.InvalidVoter)
            {
                errorKey = InvalidVoteErrorKey;
            }
            else if (status == KickVoteStatus.AlreadyVoted)
            {
                errorKey = AlreadyVotedErrorKey;
            }
            else if (status == KickVoteStatus.TargetUnavailable || status == KickVoteStatus.VotingClosed)
            {
                errorKey = TargetUnavailableErrorKey;
            }

            return errorKey;
        }
    }
}
