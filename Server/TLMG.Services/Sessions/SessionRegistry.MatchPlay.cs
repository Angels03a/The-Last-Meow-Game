using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Services.Helpers;
using TLMG.Services.Match;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private readonly Dictionary<int, MatchChannel> _matchChannelsByUserId = new Dictionary<int, MatchChannel>();
        private readonly Dictionary<string, int> _matchUserIdsByChannelId = new Dictionary<string, int>();

        private delegate string MatchAction(MatchRecord matchRecord, int userId, DateTime nowUtc);

        public MatchDelivery SubscribeToMatch(LobbyCaller caller, Guid matchId, IMatchManagerCallback callback)
        {
            MatchDelivery matchDelivery;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                MatchRecord matchRecord;
                string errorKey = ValidateMatchSubscriptionInLock(
                    caller, matchId, callback, out playerSession, out matchRecord);

                if (errorKey == null)
                {
                    matchDelivery = SubscribeInLock(caller, playerSession.UserId, matchRecord, callback);
                }
                else
                {
                    matchDelivery = MatchDelivery.CreateRejected(errorKey);
                }
            }

            PersistPendingMatchFinalizations();
            return matchDelivery;
        }

        public MatchDelivery PlaceCard(LobbyCaller caller, MatchCardKind card)
        {
            MatchAction placeCardAction =
                (matchRecord, userId, nowUtc) => PlaceCardInEngineInLock(matchRecord, userId, card);
            MatchDelivery matchDelivery = RunMatchAction(caller, placeCardAction);

            return matchDelivery;
        }

        public MatchDelivery PlaceBet(LobbyCaller caller, int betValue)
        {
            MatchAction placeBetAction =
                (matchRecord, userId, nowUtc) => PlaceBetInEngineInLock(matchRecord, userId, betValue);
            MatchDelivery matchDelivery = RunMatchAction(caller, placeBetAction);

            return matchDelivery;
        }

        public MatchDelivery WithdrawFromBet(LobbyCaller caller)
        {
            MatchAction withdrawAction =
                (matchRecord, userId, nowUtc) => WithdrawFromBetInEngineInLock(matchRecord, userId);
            MatchDelivery matchDelivery = RunMatchAction(caller, withdrawAction);

            return matchDelivery;
        }

        public MatchDelivery RevealCard(LobbyCaller caller, int pileOwnerUserId)
        {
            MatchAction revealAction = (matchRecord, userId, nowUtc) =>
                RevealCardInEngineInLock(matchRecord, userId, pileOwnerUserId, nowUtc);
            MatchDelivery matchDelivery = RunMatchAction(caller, revealAction);

            return matchDelivery;
        }

        public MatchDelivery ChoosePunishmentCard(LobbyCaller caller, int cardIndex)
        {
            MatchAction chooseAction = (matchRecord, userId, nowUtc) =>
                ChoosePunishmentCardInEngineInLock(matchRecord, userId, cardIndex, nowUtc);
            MatchDelivery matchDelivery = RunMatchAction(caller, chooseAction);

            return matchDelivery;
        }

        public bool RemoveMatchChannel(string channelId, IMatchManagerCallback closedCallback)
        {
            bool wasRemoved = RemoveMatchChannel(channelId, closedCallback, out _);

            return wasRemoved;
        }

        public bool RemoveMatchChannel(string channelId, IMatchManagerCallback closedCallback,
            out IReadOnlyList<MatchViewDelivery> deliveries)
        {
            bool wasRemoved = false;
            deliveries = _noMatchViewDeliveries;

            lock (_sessionsLock)
            {
                int userId = 0;

                if (!string.IsNullOrEmpty(channelId) && _matchUserIdsByChannelId.TryGetValue(channelId, out userId))
                {
                    wasRemoved = ReferenceEquals(_matchChannelsByUserId[userId].Callback, closedCallback);
                }

                if (wasRemoved)
                {
                    MatchChannel channel = _matchChannelsByUserId[userId];
                    RemoveMatchChannelOfUserInLock(userId);
                    if (_matchesById.TryGetValue(channel.MatchId, out MatchRecord match))
                    {
                        DateTime nowUtc = _readUtcNow();
                        bool changed = ExpireMatchDeadlinesInLock(match, nowUtc);
                        changed |= OpenMatchGraceInLock(match, userId, nowUtc);
                        if (changed)
                        {
                            match.Version++;
                            deliveries = CreateViewDeliveriesInLock(match);
                        }
                    }
                }
            }

            PersistPendingMatchFinalizations();
            return wasRemoved;
        }

        private static bool IsMatchChannelOfCaller(MatchChannel matchChannel, LobbyCaller caller)
        {
            bool isSameSession = matchChannel.SessionToken == caller.SessionToken;
            bool isSameChannel = matchChannel.ChannelId == caller.ChannelId;

            bool isMatchChannel = isSameSession && isSameChannel;

            return isMatchChannel;
        }

        private static int FindHandCardCount(MatchState matchState, int playerId)
        {
            MatchPlayer player = (from matchPlayer in matchState.Players
                                  where matchPlayer.PlayerId == playerId
                                  select matchPlayer).First();
            int handCardCount = player.HandCards.Count;

            return handCardCount;
        }

        private static bool IsBlindChooser(MatchRecord matchRecord, int userId)
        {
            PendingPunishment pendingPunishment = matchRecord.MatchState.PendingPunishment;
            bool hasBlindSlots = pendingPunishment != null && matchRecord.BlindSlotOrder != null;

            bool isBlindChooser = hasBlindSlots && pendingPunishment.ChooserPlayerId == userId;

            return isBlindChooser;
        }

        private static string TranslateBlindSlot(int[] blindSlotOrder, int slot, out int engineIndex)
        {
            string errorKey = null;
            engineIndex = slot;

            if (slot < 0 || slot >= blindSlotOrder.Length)
            {
                errorKey = MatchErrorKeys.CardIndexNotValid;
            }
            else
            {
                engineIndex = blindSlotOrder[slot];
            }

            return errorKey;
        }

        private MatchDelivery RunMatchAction(LobbyCaller caller, MatchAction matchAction)
        {
            MatchDelivery matchDelivery;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                MatchRecord matchRecord;
                string errorKey = ValidateMatchActionInLock(caller, out playerSession, out matchRecord);

                if (errorKey == null)
                {
                    matchDelivery = ApplyMatchActionInLock(matchRecord, playerSession.UserId, matchAction);
                }
                else
                {
                    matchDelivery = MatchDelivery.CreateRejected(errorKey);
                }
            }

            PersistPendingMatchFinalizations();
            return matchDelivery;
        }

        private MatchDelivery ApplyMatchActionInLock(MatchRecord matchRecord, int userId, MatchAction matchAction)
        {
            DateTime nowUtc = _readUtcNow();
            bool hasExpired = ExpireMatchDeadlinesInLock(matchRecord, nowUtc);
            string errorKey = matchRecord.Disconnections.Count != 0
                && matchRecord.MatchState.Phase != RoundPhase.MatchFinished
                && matchRecord.MatchState.PendingPunishment?.ChooserPlayerId != userId
                ? "Match_ReconnectionPending"
                : matchRecord.Kitten != null && !matchRecord.Kitten.IsResolved
                ? MatchErrorKeys.FugitiveKittenInProgress
                : matchAction(matchRecord, userId, nowUtc);
            IReadOnlyList<MatchViewDelivery> viewDeliveries = _noMatchViewDeliveries;

            if (hasExpired || errorKey == null)
            {
                matchRecord.Version++;
                viewDeliveries = CreateViewDeliveriesInLock(matchRecord);
            }

            MatchDelivery matchDelivery;

            if (errorKey == null)
            {
                matchDelivery = MatchDelivery.CreateAccepted(viewDeliveries);
            }
            else
            {
                matchDelivery = MatchDelivery.CreateRejected(errorKey, viewDeliveries);
            }

            return matchDelivery;
        }

        private MatchDelivery SubscribeInLock(
            LobbyCaller caller,
            int userId,
            MatchRecord matchRecord,
            IMatchManagerCallback callback)
        {
            MatchDelivery matchDelivery;
            DateTime nowUtc = _readUtcNow();
            bool hasExpired = ExpireMatchDeadlinesInLock(matchRecord, nowUtc);
            if ((from player in matchRecord.MatchState.Players
                 where player.PlayerId == userId && player.HasLeftMatch
                 select player).Any())
            {
                if (hasExpired)
                {
                    matchRecord.Version++;
                }
                matchDelivery = MatchDelivery.CreateRejected(MatchErrorKeys.NotInMatch,
                    hasExpired ? CreateViewDeliveriesInLock(matchRecord) : _noMatchViewDeliveries);
            }
            else
            {
                bool hasResumed = ResumeMatchSubscriptionInLock(matchRecord, userId, nowUtc);
                hasExpired |= hasResumed;

                if (hasExpired)
                {
                    matchRecord.Version++;
                }

                if (UpdateKittenClockInLock(matchRecord, nowUtc) && !hasExpired)
                {
                    matchRecord.Version++;
                    hasExpired = true;
                }

                MatchViewDelivery ownViewDelivery = CreateViewDeliveryInLock(matchRecord, userId, callback);
                RemoveMatchChannelOfUserInLock(userId);
                _matchChannelsByUserId[userId] =
                    new MatchChannel(caller.SessionToken, caller.ChannelId, matchRecord.MatchId, callback);
                _matchUserIdsByChannelId[caller.ChannelId] = userId;
                TrackLinkInLock(_sessionsByToken[caller.SessionToken], HeartbeatChannel.Match, caller.ChannelId);
                IReadOnlyList<MatchViewDelivery> viewDeliveries = new List<MatchViewDelivery> { ownViewDelivery }
                    .AsReadOnly();

                if (hasExpired)
                {
                    viewDeliveries = CreateViewDeliveriesInLock(matchRecord);
                }

                matchDelivery = MatchDelivery.CreateAccepted(viewDeliveries);
            }

            return matchDelivery;
        }

        private bool ResumeMatchSubscriptionInLock(MatchRecord matchRecord, int userId, DateTime nowUtc)
        {
            matchRecord.AwaitingFirstLink.Remove(userId);
            bool hasResumed = matchRecord.Disconnections.Remove(userId);

            if (hasResumed && matchRecord.MatchState.Phase != RoundPhase.MatchFinished
                && matchRecord.MatchState.ResumePunishment(userId, nowUtc, _pickRandomIndex)
                    == PunishmentStatus.CardRemoved)
            {
                matchRecord.LastChallenge?.ResolvePunishment(MatchPunishmentResolution.ChosenAtRandom);
                DiscardEndedBlindSlots(matchRecord);
            }

            return hasResumed;
        }

        private string ValidateMatchSubscriptionInLock(
            LobbyCaller caller,
            Guid matchId,
            IMatchManagerCallback callback,
            out PlayerSession playerSession,
            out MatchRecord matchRecord)
        {
            string errorKey = ValidateMatchIdentityInLock(caller, out playerSession);
            matchRecord = null;

            if (errorKey == null && callback == null)
            {
                errorKey = ErrorKeys.Common.InvalidSession;
            }

            if (errorKey == null && !_matchesById.TryGetValue(matchId, out matchRecord))
            {
                errorKey = MatchErrorKeys.NotInMatch;
            }

            if (errorKey == null)
            {
                errorKey = ValidateMatchParticipationInLock(playerSession.UserId, matchRecord);
            }

            return errorKey;
        }

        private string ValidateMatchActionInLock(
            LobbyCaller caller,
            out PlayerSession playerSession,
            out MatchRecord matchRecord)
        {
            string errorKey = ValidateMatchIdentityInLock(caller, out playerSession);
            matchRecord = null;
            MatchChannel matchChannel = null;

            if (errorKey == null && !_matchChannelsByUserId.TryGetValue(playerSession.UserId, out matchChannel))
            {
                errorKey = MatchErrorKeys.NotSubscribed;
            }

            if (errorKey == null && !IsMatchChannelOfCaller(matchChannel, caller))
            {
                errorKey = ErrorKeys.Common.InvalidSession;
            }

            if (errorKey == null && !_matchesById.TryGetValue(matchChannel.MatchId, out matchRecord))
            {
                errorKey = MatchErrorKeys.NotInMatch;
            }

            if (errorKey == null)
            {
                errorKey = ValidateMatchParticipationInLock(playerSession.UserId, matchRecord);
            }

            return errorKey;
        }

        private string ValidateMatchIdentityInLock(LobbyCaller caller, out PlayerSession playerSession)
        {
            string errorKey = ErrorKeys.Common.InvalidSession;
            playerSession = null;
            int channelUserId;

            if (caller.HasSessionAndChannel && _sessionsByToken.TryGetValue(caller.SessionToken, out playerSession))
            {
                bool hasAccountIdentity = playerSession.UserId != LobbyMembershipChange.NoUserId;
                bool isChannelOfOtherUser = _matchUserIdsByChannelId.TryGetValue(caller.ChannelId, out channelUserId)
                    && channelUserId != playerSession.UserId;

                if (hasAccountIdentity && !isChannelOfOtherUser)
                {
                    errorKey = null;
                }
            }

            return errorKey;
        }

        private string ValidateMatchParticipationInLock(int userId, MatchRecord matchRecord)
        {
            string errorKey = MatchErrorKeys.NotInMatch;
            int currentRoomId;

            bool isParticipant = matchRecord.ParticipantUserIds.Contains(userId);

            if (isParticipant && _roomIdsByUserId.TryGetValue(userId, out currentRoomId)
                && currentRoomId == matchRecord.RoomId)
            {
                bool isRoomOfMatch = _startsByRoomId[currentRoomId].MatchId == matchRecord.MatchId;

                if (isRoomOfMatch)
                {
                    errorKey = null;
                }
            }

            return errorKey;
        }

        private string PlaceCardInEngineInLock(MatchRecord matchRecord, int userId, MatchCardKind card)
        {
            string errorKey = MatchErrorKeys.CardNotValid;
            CardKind engineCard;

            if (MatchViewHelper.TryConvertToEngineCard(card, out engineCard))
            {
                CardPlacementStatus placementStatus = matchRecord.MatchState.PlaceCard(userId, engineCard);
                errorKey = MatchViewHelper.ConvertToErrorKey(placementStatus);

                if (errorKey == null)
                {
                    matchRecord.LastChallenge = null;
                }
            }

            return errorKey;
        }

        private string PlaceBetInEngineInLock(MatchRecord matchRecord, int userId, int betValue)
        {
            MatchState matchState = matchRecord.MatchState;
            bool isOpening = matchState.Phase != RoundPhase.Betting;
            BetActionStatus betStatus;

            if (isOpening)
            {
                betStatus = matchState.OpenBet(userId, betValue);
            }
            else
            {
                betStatus = matchState.RaiseBet(userId, betValue);
            }

            string errorKey = MatchViewHelper.ConvertToErrorKey(betStatus);

            if (errorKey == null && isOpening)
            {
                matchRecord.LastChallenge = null;
            }

            return errorKey;
        }

        private string WithdrawFromBetInEngineInLock(MatchRecord matchRecord, int userId)
        {
            MatchState matchState = matchRecord.MatchState;
            string errorKey = MatchViewHelper.ConvertToErrorKey(matchState.WithdrawFromBet(userId));

            if (errorKey == null && matchState.Phase == RoundPhase.ChallengeReady)
            {
                matchRecord.LastChallenge =
                    MatchChallengeRecord.Begin(matchState.BetLeader.PlayerId, matchState.CurrentBet);
            }

            return errorKey;
        }

        private string RevealCardInEngineInLock(
            MatchRecord matchRecord,
            int userId,
            int pileOwnerUserId,
            DateTime nowUtc)
        {
            ChallengeRevealStatus revealStatus = matchRecord.MatchState.RevealCard(userId, pileOwnerUserId, nowUtc);
            string errorKey = MatchViewHelper.ConvertToErrorKey(revealStatus);

            if (errorKey == null && matchRecord.LastChallenge != null)
            {
                RecordRevealInLock(matchRecord, revealStatus);
            }

            return errorKey;
        }

        private void RecordRevealInLock(MatchRecord matchRecord, ChallengeRevealStatus revealStatus)
        {
            ChallengeResolution resolution = matchRecord.MatchState.Resolution;
            MatchChallengeRecord challenge = matchRecord.LastChallenge;
            challenge.RecordReveal(
                resolution.LastRevealedPlayerId.Value,
                MatchViewHelper.ConvertToContractCard(resolution.LastRevealedCard.Value));

            if (revealStatus == ChallengeRevealStatus.ChallengeSucceeded)
            {
                challenge.MarkSucceeded();
            }
            else if (revealStatus == ChallengeRevealStatus.ChallengeFailed)
            {
                RecordFailedChallengeInLock(matchRecord, challenge);
            }
        }

        private void RecordFailedChallengeInLock(MatchRecord matchRecord, MatchChallengeRecord challenge)
        {
            MatchState matchState = matchRecord.MatchState;
            PendingPunishment pendingPunishment = matchState.PendingPunishment;

            if (pendingPunishment != null)
            {
                challenge.MarkFailed(
                    pendingPunishment.ChooserPlayerId,
                    pendingPunishment.PunishedPlayerId,
                    MatchPunishmentResolution.Pending);
                matchRecord.BlindSlotOrder = null;

                if (pendingPunishment.IsBlindChoice)
                {
                    int punishedCardCount = FindHandCardCount(matchState, pendingPunishment.PunishedPlayerId);
                    matchRecord.BlindSlotOrder = CreateBlindSlotOrder(punishedCardCount);
                }
            }
            else
            {
                PendingPunishment appliedPunishment = matchState.LastPunishment;
                challenge.MarkFailed(
                    appliedPunishment.ChooserPlayerId,
                    appliedPunishment.PunishedPlayerId,
                    MatchPunishmentResolution.SingleLifeNoChoice);
                matchRecord.BlindSlotOrder = null;
            }
        }

        private int[] CreateBlindSlotOrder(int cardCount)
        {
            int[] slotOrder = Enumerable.Range(0, cardCount).ToArray();

            for (int slotNumber = cardCount - 1; slotNumber > 0; slotNumber--)
            {
                int swapNumber = _pickRandomIndex(slotNumber + 1);
                int swappedIndex = slotOrder[slotNumber];
                slotOrder[slotNumber] = slotOrder[swapNumber];
                slotOrder[swapNumber] = swappedIndex;
            }

            return slotOrder;
        }

        private string ChoosePunishmentCardInEngineInLock(
            MatchRecord matchRecord,
            int userId,
            int cardIndex,
            DateTime nowUtc)
        {
            int engineIndex = cardIndex;
            string errorKey = null;

            if (IsBlindChooser(matchRecord, userId))
            {
                errorKey = TranslateBlindSlot(matchRecord.BlindSlotOrder, cardIndex, out engineIndex);
            }

            if (errorKey == null)
            {
                PunishmentStatus punishmentStatus =
                    matchRecord.MatchState.ChoosePunishmentCard(userId, engineIndex, nowUtc);
                errorKey = MatchViewHelper.ConvertToErrorKey(punishmentStatus);
            }

            if (errorKey == null)
            {
                matchRecord.LastChallenge?.ResolvePunishment(MatchPunishmentResolution.ChosenByChooser);
                DiscardEndedBlindSlots(matchRecord);
            }

            return errorKey;
        }

        private IReadOnlyList<MatchViewDelivery> CreateViewDeliveriesInLock(MatchRecord matchRecord)
        {
            QueueMatchFinalizationInLock(matchRecord);
            List<MatchViewDelivery> viewDeliveries = new List<MatchViewDelivery>();

            foreach (int userId in matchRecord.ParticipantUserIds)
            {
                MatchChannel matchChannel;

                if (_matchChannelsByUserId.TryGetValue(userId, out matchChannel)
                    && matchChannel.MatchId == matchRecord.MatchId)
                {
                    viewDeliveries.Add(CreateViewDeliveryInLock(matchRecord, userId, matchChannel.Callback));
                }
            }

            IReadOnlyList<MatchViewDelivery> readOnlyViewDeliveries = viewDeliveries.AsReadOnly();

            return readOnlyViewDeliveries;
        }

        private MatchViewDelivery CreateViewDeliveryInLock(
            MatchRecord matchRecord,
            int userId,
            IMatchManagerCallback callback)
        {
            MatchViewContext viewContext = new MatchViewContext(
                matchRecord.MatchId,
                matchRecord.Version,
                matchRecord.MatchState,
                matchRecord.ParticipantNicknames,
                matchRecord.LastChallenge,
                matchRecord.GuestUserIds);
            MatchView matchView = MatchViewHelper.CreateMatchView(viewContext, userId);
            DateTime nowUtc = _readUtcNow();
            matchView.ServerTimeUtc = nowUtc;
            if (matchView.Phase != MatchRoundPhase.MatchFinished)
            {
                Dictionary<int, DateTime> reconnectDeadlinesUtc = new Dictionary<int, DateTime>();
                foreach (KeyValuePair<int, PlayerExit> disconnection in matchRecord.Disconnections)
                {
                    reconnectDeadlinesUtc.Add(disconnection.Key, disconnection.Value.EffectiveAtUtc);
                }
                matchView.ReconnectDeadlinesUtc = reconnectDeadlinesUtc;
                matchView.IsWaitingForInitialLinks = matchRecord.Kitten != null
                    && !matchRecord.Kitten.IsResolved && matchRecord.AwaitingFirstLink.Count != 0
                    && matchRecord.Disconnections.Count == 0;
            }

            if (matchRecord.Disconnections.Count != 0)
            {
                matchView.CanPlaceCard = false;
                matchView.CanPlaceBet = false;
                matchView.CanWithdrawFromBet = false;
                matchView.CanRevealCard = false;
            }
            matchView.FugitiveKitten = CreateFugitiveKittenViewInLock(matchRecord, nowUtc);
            MatchViewDelivery viewDelivery = new MatchViewDelivery(callback, matchView);

            return viewDelivery;
        }

        private void RemoveMatchChannelOfUserInLock(int userId)
        {
            MatchChannel matchChannel;

            if (_matchChannelsByUserId.TryGetValue(userId, out matchChannel))
            {
                _matchUserIdsByChannelId.Remove(matchChannel.ChannelId);
                _matchChannelsByUserId.Remove(userId);
            }
        }
    }
}