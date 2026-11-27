using System;
using System.Collections.Generic;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Repositories
{
    public sealed class CompletedKickVoting
    {
        private const int MinimumVoters = 1;
        private const int MaximumVoters = ValidationRules.MaximumPlayersPerMatchCount - 1;

        public CompletedKickVoting(Guid votingId, Guid roomScopeId, Guid? matchId,
            DateTime completedAtUtc, KickParticipantIdentity target, IReadOnlyList<KickParticipantIdentity> voters)
        {
            VotingId = votingId;
            RoomScopeId = roomScopeId;
            MatchId = matchId;
            CompletedAtUtc = completedAtUtc;
            Target = target;
            if (voters != null)
            {
                Voters = new List<KickParticipantIdentity>(voters).AsReadOnly();
            }
            IsValid = votingId != Guid.Empty && roomScopeId != Guid.Empty
                && (!matchId.HasValue || matchId.Value != Guid.Empty)
                && completedAtUtc.Kind == DateTimeKind.Utc && target != null && target.IsValid
                && Voters.Count >= MinimumVoters && Voters.Count <= MaximumVoters
                && HasValidVoters();
        }

        public Guid VotingId { get; }
        public Guid RoomScopeId { get; }
        public Guid? MatchId { get; }
        public DateTime CompletedAtUtc { get; }
        public KickParticipantIdentity Target { get; }
        public IReadOnlyList<KickParticipantIdentity> Voters { get; } = Array.Empty<KickParticipantIdentity>();
        public bool IsValid { get; }

        private bool HasValidVoters()
        {
            HashSet<int> userIds = new HashSet<int>();
            HashSet<Guid> guestIds = new HashSet<Guid>();
            if (Target.UserId.HasValue)
            {
                userIds.Add(Target.UserId.Value);
            }
            else
            {
                guestIds.Add(Target.GuestSessionId.Value);
            }

            bool isValid = true;
            for (int voterIndex = 0; isValid && voterIndex < Voters.Count; voterIndex++)
            {
                KickParticipantIdentity voter = Voters[voterIndex];
                isValid = voter != null && voter.IsValid;
                if (isValid && voter.UserId.HasValue)
                {
                    isValid = userIds.Add(voter.UserId.Value);
                }
                else if (isValid)
                {
                    isValid = guestIds.Add(voter.GuestSessionId.Value);
                }
            }

            return isValid;
        }
    }
}