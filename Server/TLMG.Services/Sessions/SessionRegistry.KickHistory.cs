using System;
using System.Collections.Generic;

using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Moderation;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private KickVotingHistory _kickVotingHistory;

        public void ConfigureKickHistory(IKickVotingRepository repository)
        {
            if (repository != null)
            {
                _kickVotingHistory = new KickVotingHistory(repository);
            }
            else
            {
                _log.Error("El repositorio del historial de votaciones de expulsión no está configurado.");
            }
        }

        private CompletedKickVoting CaptureCompletedVotingInLock(LobbyStartState roomState,
            int targetUserId, DateTime nowUtc)
        {
            IReadOnlyList<int> voterUserIds = roomState.KickVoting.CopyConsensusVoterIds(targetUserId);
            List<KickParticipantIdentity> voters = new List<KickParticipantIdentity>();
            foreach (int voterUserId in voterUserIds)
            {
                voters.Add(FindKickIdentityInLock(voterUserId));
            }

            CompletedKickVoting voting = new CompletedKickVoting(Guid.NewGuid(), roomState.RoomScopeId,
                roomState.MatchId, nowUtc, FindKickIdentityInLock(targetUserId), voters);

            return voting;
        }

        private KickParticipantIdentity FindKickIdentityInLock(int userId)
        {
            int? registeredUserId = null;
            Guid? guestSessionId = null;
            if (_tokensByUserId.TryGetValue(userId, out string token)
                && _sessionsByToken.TryGetValue(token, out PlayerSession session))
            {
                if (session.IsRegistered && userId > ValidationRules.NoUserId)
                {
                    registeredUserId = userId;
                }
                else if (!session.IsRegistered && userId < ValidationRules.NoUserId)
                {
                    guestSessionId = session.DiagnosticId;
                }
            }

            KickParticipantIdentity identity = new KickParticipantIdentity(registeredUserId, guestSessionId);

            return identity;
        }

        private void PersistPendingKickVotings()
        {
            if (_kickVotingHistory != null)
            {
                _kickVotingHistory.PersistPending();
            }
        }
    }
}