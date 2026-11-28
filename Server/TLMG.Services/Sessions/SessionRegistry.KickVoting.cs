using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Domain.Moderation;
using TLMG.Domain.Repositories;
using TLMG.Services.Helpers;
using TLMG.Services.Moderation;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry : IKickSessionAccess
    {
        public KickVoteChange RegisterKickVote(string sessionToken, KickVote vote)
        {
            KickVoteStatus status = KickVoteStatus.InvalidSession;
            List<Action> notifications = new List<Action>();

            lock (_sessionsLock)
            {
                DateTime nowUtc = _readUtcNow();
                PlayerSession voter;

                if (!string.IsNullOrEmpty(sessionToken) && _sessionsByToken.ContainsKey(sessionToken))
                {
                    EvaluateHeartbeatDeadlinesInLock(nowUtc, notifications, true);
                }

                if (!string.IsNullOrEmpty(sessionToken) && _sessionsByToken.TryGetValue(sessionToken, out voter))
                {
                    voter.LastActivityUtc = nowUtc;
                    KickVoteChange change = RegisterKickVoteInLock(voter, vote, nowUtc);
                    status = change.Status;
                    notifications.AddRange(change.Notifications);
                }
            }

            PersistPendingMatchFinalizations();
            PersistPendingKickVotings();
            KickVoteChange completion = new KickVoteChange(status, notifications.AsReadOnly());

            return completion;
        }

        private KickVoteChange RegisterKickVoteInLock(PlayerSession voter, KickVote vote, DateTime nowUtc)
        {
            KickVoteStatus status = KickVoteStatus.InvalidVoting;
            IReadOnlyList<Action> notifications = Array.Empty<Action>();
            int roomId;

            if (_kickVotingHistory == null)
            {
                status = KickVoteStatus.DataFailure;
                _log.Error("El historial de votaciones de expulsión no está configurado.");
            }
            else if (vote != null)
            {
                if (!_roomIdsByUserId.TryGetValue(voter.UserId, out roomId) || roomId != vote.RoomId)
                {
                    status = KickVoteStatus.NotInRoom;
                }
                else if (voter.UserId == vote.TargetUserId || vote.TargetUserId == ValidationRules.NoUserId)
                {
                    status = KickVoteStatus.InvalidVoter;
                }
                else
                {
                    IReadOnlyList<int> participants = CopyKickParticipantsInLock(roomId, vote.TargetUserId);
                    status = ApplyKickVoteInLock(voter.UserId, vote, participants);

                    if (status == KickVoteStatus.ConsensusReached)
                    {
                        KickVoteChange expulsion = ExpelVotedPlayerInLock(roomId, vote.TargetUserId, nowUtc);
                        status = expulsion.Status;
                        notifications = expulsion.Notifications;
                    }
                }
            }

            KickVoteChange change = new KickVoteChange(status, notifications);

            return change;
        }

        private IReadOnlyList<int> CopyKickParticipantsInLock(int roomId, int targetUserId)
        {
            List<int> participants = new List<int>();
            LobbyStartState roomState = _startsByRoomId[roomId];
            List<LobbyMember> members = _membersByRoomId[roomId];

            if (!roomState.MatchId.HasValue)
            {
                foreach (LobbyMember member in members)
                {
                    participants.Add(member.UserId);
                }
            }
            else if (HasActiveParticipationInLock(targetUserId)
                && _matchesById.TryGetValue(roomState.MatchId.Value, out MatchRecord match))
            {
                foreach (MatchPlayer player in match.MatchState.Players)
                {
                    if (!player.HasLeftMatch && _roomIdsByUserId.TryGetValue(player.PlayerId, out int playerRoomId)
                        && playerRoomId == roomId)
                    {
                        participants.Add(player.PlayerId);
                    }
                }
            }

            return participants.AsReadOnly();
        }

        private KickVoteStatus ApplyKickVoteInLock(int voterUserId, KickVote vote, IReadOnlyList<int> participants)
        {
            KickVoteStatus status = KickVoteStatus.TargetUnavailable;

            if (participants.Contains(vote.TargetUserId))
            {
                status = _startsByRoomId[vote.RoomId].KickVoting.RegisterVote(
                    voterUserId, vote.TargetUserId, participants);
            }

            return status;
        }

        private bool IsExcludedFromRoomInLock(int userId, int roomId)
        {
            bool isExcluded = _tokensByUserId.TryGetValue(userId, out string token)
                && _sessionsByToken.TryGetValue(token, out PlayerSession session)
                && _startsByRoomId[roomId].KickVoting.IsExcluded(session);

            return isExcluded;
        }

        private KickVoteChange ExpelVotedPlayerInLock(int roomId, int targetUserId, DateTime nowUtc)
        {
            KickVoteStatus status = KickVoteStatus.DataFailure;
            List<Action> notifications = new List<Action>();
            LobbyStartState roomState = _startsByRoomId[roomId];
            LobbyMember target = (from member in _membersByRoomId[roomId]
                                  where member.UserId == targetUserId
                                  select member).First();

            CompletedKickVoting voting = CaptureCompletedVotingInLock(roomState, targetUserId, nowUtc);

            if (voting.IsValid && _tokensByUserId.TryGetValue(targetUserId, out string targetToken)
                && _sessionsByToken.TryGetValue(targetToken, out PlayerSession targetSession))
            {
                LobbyDeparture departure = new LobbyDeparture(
                    targetUserId, RoomExitCause.VotedOut, roomState.MatchId.HasValue);
                LobbyDepartureDelivery delivery = RemoveUserFromRoom(departure, nowUtc);

                if (delivery.Membership.Status == LobbyChangeStatus.Applied)
                {
                    _kickVotingHistory.Enqueue(voting);
                    roomState.KickVoting.Exclude(targetSession);
                    AddExpulsionNotification(notifications, target.Callback, roomId);
                    notifications.AddRange(SessionNotificationHelper.CreateRoomNotifications(delivery.Membership));
                    notifications.AddRange(SessionNotificationHelper.CreateMatchViewNotifications(delivery.MatchViews));
                    status = KickVoteStatus.ConsensusReached;
                    _log.InfoFormat("Jugador {0} expulsado por votación de la sala {1}.", targetUserId, roomId);
                }
            }

            if (status != KickVoteStatus.ConsensusReached)
            {
                roomState.KickVoting.ResetVotes();
                _log.ErrorFormat("No se pudo aplicar la expulsión por votación del jugador {0}.", targetUserId);
            }

            KickVoteChange change = new KickVoteChange(status, notifications.AsReadOnly());

            return change;
        }

        private static void AddExpulsionNotification(List<Action> notifications, ILobbyManagerCallback callback,
            int roomId)
        {
            if (callback != null)
            {
                notifications.Add(NotifyExpulsion);
            }

            void NotifyExpulsion()
            {
                callback.OnPlayerExpelled(roomId);
            }
        }
    }
}