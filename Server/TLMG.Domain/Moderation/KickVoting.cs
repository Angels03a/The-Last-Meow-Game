using System.Collections.Generic;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Moderation
{
    public sealed class KickVoting
    {
        private const int MinimumParticipants = 2;
        private const int ExcludedTargetCount = 1;
        private readonly HashSet<int> _participantUserIds = new HashSet<int>();
        private readonly HashSet<int> _voterUserIds = new HashSet<int>();

        public KickVoting(int targetUserId, IReadOnlyCollection<int> participantUserIds)
        {
            TargetUserId = targetUserId;
            IsValid = InitializeParticipants(participantUserIds);
        }

        public int TargetUserId { get; }

        public bool IsValid { get; }

        public bool HasConsensus { get; private set; }

        public int RequiredVoteCount { get; private set; }

        public int VoteCount => _voterUserIds.Count;

        public KickVoteStatus RegisterVote(int voterUserId)
        {
            KickVoteStatus status = KickVoteStatus.InvalidVoting;

            if (IsValid)
            {
                if (voterUserId == TargetUserId || !_participantUserIds.Contains(voterUserId))
                {
                    status = KickVoteStatus.InvalidVoter;
                }
                else if (HasConsensus)
                {
                    status = KickVoteStatus.VotingClosed;
                }
                else if (!_voterUserIds.Add(voterUserId))
                {
                    status = KickVoteStatus.AlreadyVoted;
                }
                else if (VoteCount == RequiredVoteCount)
                {
                    HasConsensus = true;
                    status = KickVoteStatus.ConsensusReached;
                }
                else
                {
                    status = KickVoteStatus.VoteRecorded;
                }
            }

            return status;
        }

        public bool HasSameParticipants(IReadOnlyCollection<int> participantUserIds)
        {
            bool hasSameParticipants = IsValid && participantUserIds != null
                && participantUserIds.Count == _participantUserIds.Count
                && _participantUserIds.SetEquals(participantUserIds);

            return hasSameParticipants;
        }

        public IReadOnlyList<int> CopyVoterUserIds()
        {
            List<int> voterUserIds = new List<int>(_voterUserIds);
            voterUserIds.Sort();
            IReadOnlyList<int> voters = voterUserIds.AsReadOnly();

            return voters;
        }

        private bool InitializeParticipants(IReadOnlyCollection<int> participantUserIds)
        {
            bool isValid = participantUserIds != null
                && participantUserIds.Count >= MinimumParticipants
                && participantUserIds.Count <= ValidationRules.MaximumPlayersPerMatchCount
                && TargetUserId != ValidationRules.NoUserId;

            if (isValid)
            {
                foreach (int participantUserId in participantUserIds)
                {
                    if (participantUserId == ValidationRules.NoUserId || !_participantUserIds.Add(participantUserId))
                    {
                        isValid = false;
                    }
                }

                isValid = isValid && _participantUserIds.Contains(TargetUserId);
            }

            if (isValid)
            {
                RequiredVoteCount = _participantUserIds.Count - ExcludedTargetCount;
            }
            else
            {
                _participantUserIds.Clear();
            }

            return isValid;
        }
    }
}
