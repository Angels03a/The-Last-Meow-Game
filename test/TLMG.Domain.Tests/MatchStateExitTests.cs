using System;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests exits in MatchState under Rules 4.0.6, 2.0.2 and No. 9b, including round restart and count, last-player
    /// victory, unchanged state on rejection and actions by players who left. Scenario round numbers illustrate the
    /// sequence. The expected number is the previous round plus one.
    /// </summary>
    public class MatchStateExitTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int PlayerAId = 1;
        private const int PlayerBId = 2;
        private const int PlayerCId = 3;
        private const int PlayerDId = 4;
        private const int UnknownPlayerId = 99;
        private const int OneCardBet = 1;
        private const int TwoCardBet = 2;
        private const int ThreeCardBet = 3;
        private const int FirstCardIndex = 0;
        private const int FirstRoundNumber = 1;
        private const int OneRound = 1;
        private const int FourLives = 4;
        private const int EightLives = 8;
        private const int NoChallenges = 0;
        private const int NoBet = 0;
        private const int NoCardsOnTable = 0;
        private const CardKind SafeCard = CardKind.Safe;
        private const CardKind FatalCatCard = CardKind.FatalCat;

        private static readonly DateTime _startUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Func<int, int> _pickFirstCard = cardCount => FirstCardIndex;

        /// <summary>Checks RemovePlayer_ScenarioE61BetLeaderLeavesBeforeRevealing_RestartsRoundOpenedByA.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE61BetLeaderLeavesBeforeRevealing_RestartsRoundOpenedByA()
        {
            // Arrange: In E6-1, A opens at 2 and B raises to 3. C, D and A fold, leaving B ready for the challenge.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            PlaceAllInTurnOrder(match);
            match.OpenBet(PlayerAId, TwoCardBet);
            match.RaiseBet(PlayerBId, ThreeCardBet);
            match.WithdrawFromBet(PlayerCId);
            match.WithdrawFromBet(PlayerDId);
            match.WithdrawFromBet(PlayerAId);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerBId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            AssertRestartedRound(match, PlayerAId);
            AssertFullHandsWithoutChallenges(match, PlayerAId, PlayerCId, PlayerDId);
        }

        /// <summary>Checks RemovePlayer_ScenarioE61VariantDuringPlacement_RestartsRoundOpenedByA.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE61VariantDuringPlacement_RestartsRoundOpenedByA()
        {
            // Arrange: E6-1 variant. B leaves during placement before bidding opens.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.PlaceCard(PlayerAId, SafeCard);
            match.PlaceCard(PlayerBId, SafeCard);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerBId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            AssertRestartedRound(match, PlayerAId);
            AssertFullHandsWithoutChallenges(match, PlayerAId, PlayerCId, PlayerDId);
        }

        /// <summary>Checks RemovePlayer_ScenarioE63FirstPlayerLeavesInTurn_RestartsRoundOpenedByB.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE63FirstPlayerLeavesInTurn_RestartsRoundOpenedByB()
        {
            // Arrange: In E6-3, A, B, C and D place safe cards. A leaves on their turn.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            PlaceAllInTurnOrder(match);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerAId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            AssertRestartedRound(match, PlayerBId);
            AssertFullHandsWithoutChallenges(match, PlayerBId, PlayerCId, PlayerDId);
            Assert.All(new[] { PlayerBId, PlayerCId, PlayerDId },
                playerId => Assert.Equal(FourLives, GetPlayer(match, playerId).HandCards.Count));
        }

        /// <summary>Checks PlaceCard_AfterFirstPlayerLeft_SkipsHimInTurnOrder.</summary>
        [Fact]
        public void PlaceCard_AfterFirstPlayerLeft_SkipsHimInTurnOrder()
        {
            // Arrange: E6-3 continues with B, C, D in turn order.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            PlaceAllInTurnOrder(match);
            LeaveNow(match, PlayerAId);
            match.PlaceCard(PlayerBId, SafeCard);
            match.PlaceCard(PlayerCId, SafeCard);

            // Act
            match.PlaceCard(PlayerDId, SafeCard);

            // Assert
            Assert.Equal(PlayerBId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Checks WithdrawFromBet_AfterFirstPlayerLeft_SkipsHimAmongBettors.</summary>
        [Fact]
        public void WithdrawFromBet_AfterFirstPlayerLeft_SkipsHimAmongBettors()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerAId);
            PlaceAllInTurnOrder(match);
            match.OpenBet(PlayerBId, OneCardBet);
            match.WithdrawFromBet(PlayerCId);

            // Act
            BetActionStatus status = match.WithdrawFromBet(PlayerDId);

            // Assert
            Assert.Equal(BetActionStatus.Accepted, status);
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
            Assert.Equal(PlayerBId, match.BetLeader.PlayerId);
        }

        /// <summary>Checks RemovePlayer_DuringHalfRevealedChallenge_DiscardsRevealedCardsAndRestarts.</summary>
        [Fact]
        public void RemovePlayer_DuringHalfRevealedChallenge_DiscardsRevealedCardsAndRestarts()
        {
            // Arrange: A bids 2 and reveals their card. One reveal remains when C leaves.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            PlaceAllInTurnOrder(match);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _startUtc);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerCId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            Assert.Null(match.Resolution);
            Assert.All(match.Players, player => Assert.Equal(NoCardsOnTable, player.RevealedCardCount));
            AssertRestartedRound(match, PlayerAId);
        }

        /// <summary>Checks RemovePlayer_AtRoundStartWithEmptyTable_RestartsAndCountsOneRound.</summary>
        [Fact]
        public void RemovePlayer_AtRoundStartWithEmptyTable_RestartsAndCountsOneRound()
        {
            // Arrange: In H-17 the round is active before anyone places a card.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerDId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.Equal(FirstRoundNumber + OneRound, match.RoundNumber);
            AssertRestartedRound(match, PlayerAId);
        }

        /// <summary>Checks RemovePlayer_ScenarioE64SecondLeaveLeavesOnePlayer_FinishesWithoutNewRound.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE64SecondLeaveLeavesOnePlayer_FinishesWithoutNewRound()
        {
            // Arrange: In E6-4, B leaves and the round restarts. A places a card and C leaves during that round.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LeaveNow(match, PlayerBId);
            match.PlaceCard(PlayerAId, SafeCard);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerCId);

            // Assert
            Assert.Equal(PlayerExitStatus.MatchFinished, status);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
            Assert.Equal(PlayerAId, match.Winner.PlayerId);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
        }

        /// <summary>Checks RemovePlayer_LastLeaveWithOpenBet_FinishesWithCleanRoundState.</summary>
        [Fact]
        public void RemovePlayer_LastLeaveWithOpenBet_FinishesWithCleanRoundState()
        {
            // Arrange: In F-01, C leaves, A and B place cards, A opens and B leaves during bidding.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LeaveNow(match, PlayerCId);
            PlaceAllInTurnOrder(match);
            match.OpenBet(PlayerAId, TwoCardBet);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerBId);

            // Assert
            Assert.Equal(PlayerExitStatus.MatchFinished, status);
            Assert.Equal(PlayerAId, match.Winner.PlayerId);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
            Assert.Equal(NoCardsOnTable, match.CardsOnTable);
            Assert.All(match.Players, player => Assert.Equal(FourLives, player.HandCards.Count));
            Assert.Equal(NoBet, match.CurrentBet);
            Assert.Null(match.BetLeader);
            Assert.All(match.Players, player => Assert.False(player.HasWithdrawnFromBet));
            Assert.Null(match.Resolution);
        }

        /// <summary>Checks Actions_AfterMatchFinishedByLeave_AreRejectedWithExistingStatuses.</summary>
        [Fact]
        public void Actions_AfterMatchFinishedByLeave_AreRejectedWithExistingStatuses()
        {
            // Arrange: Exits ended the match. PlayerInTurn no longer identifies an active turn.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LeaveNow(match, PlayerCId);
            Assert.Equal(PlayerExitStatus.MatchFinished, LeaveNow(match, PlayerBId));
            string matchBeforeActions = DescribeMatch(match);

            // Act
            CardPlacementStatus placementStatus = match.PlaceCard(PlayerAId, SafeCard);
            BetActionStatus openStatus = match.OpenBet(PlayerAId, OneCardBet);
            BetActionStatus withdrawStatus = match.WithdrawFromBet(PlayerAId);
            ChallengeRevealStatus revealStatus = match.RevealCard(PlayerAId, PlayerAId, _startUtc);
            PunishmentStatus punishmentStatus = match.ChoosePunishmentCard(PlayerAId, FirstCardIndex, _startUtc);

            // Assert
            Assert.Equal(CardPlacementStatus.MatchFinished, placementStatus);
            Assert.Equal(BetActionStatus.MatchFinished, openStatus);
            Assert.Equal(BetActionStatus.MatchFinished, withdrawStatus);
            Assert.Equal(ChallengeRevealStatus.NoChallengeInProgress, revealStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, punishmentStatus);
            Assert.Equal(matchBeforeActions, DescribeMatch(match));
        }

        /// <summary>Checks RemovePlayer_WhenExtraLifeWasConsumedInRound_KeepsItConsumed.</summary>
        [Fact]
        public void RemovePlayer_WhenExtraLifeWasConsumedInRound_KeepsItConsumed()
        {
            // Arrange: In H-15, A's extra life absorbs their own revealed Fatal Cat, then B leaves.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.GrantExtraLife(PlayerAId);
            match.PlaceCard(PlayerAId, FatalCatCard);
            PlaceSafeCardsUntilTurnOf(match, PlayerAId);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _startUtc);

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerBId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.False(GetPlayer(match, PlayerAId).HasExtraLife);
            Assert.Equal(EightLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>
        /// Checks RemovePlayer_ScenarioE55cEliminatedPlayerLeaves_ReturnsEliminatedPlayerLeftWithoutChange.
        /// </summary>
        [Fact]
        public void RemovePlayer_ScenarioE55cEliminatedPlayerLeaves_ReturnsEliminatedPlayerLeftWithoutChange()
        {
            // Arrange: In E5-5c, C is eliminated and chooses to leave in a later round with cards on the table.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            EliminateOnOwnCat(match, PlayerCId);
            match.PlaceCard(PlayerAId, SafeCard);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            PlayerExitStatus firstStatus = LeaveNow(match, PlayerCId);
            PlayerExitStatus secondStatus = LeaveNow(match, PlayerCId);

            // Assert
            Assert.Equal(PlayerExitStatus.EliminatedPlayerLeft, firstStatus);
            Assert.Equal(PlayerExitStatus.EliminatedPlayerLeft, secondStatus);
            Assert.False(GetPlayer(match, PlayerCId).HasLeftMatch);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Checks RemovePlayer_WhenPlayerAlreadyLeft_ReturnsPlayerAlreadyLeftWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_WhenPlayerAlreadyLeft_ReturnsPlayerAlreadyLeftWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerBId);
            match.PlaceCard(PlayerAId, SafeCard);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerBId);

            // Assert
            Assert.Equal(PlayerExitStatus.PlayerAlreadyLeft, status);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Checks RemovePlayer_WithUnknownPlayer_ReturnsPlayerNotInMatchWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_WithUnknownPlayer_ReturnsPlayerNotInMatchWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.PlaceCard(PlayerAId, SafeCard);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            PlayerExitStatus status = LeaveNow(match, UnknownPlayerId);

            // Assert
            Assert.Equal(PlayerExitStatus.PlayerNotInMatch, status);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Checks RemovePlayer_AfterMatchFinished_ReturnsMatchAlreadyFinishedWithoutChange.</summary>
        /// <param name="playerId">Player trying to leave, whether active, already gone or outside the match.</param>
        [Theory]
        [InlineData(PlayerAId)]
        [InlineData(PlayerCId)]
        [InlineData(UnknownPlayerId)]
        public void RemovePlayer_AfterMatchFinished_ReturnsMatchAlreadyFinishedWithoutChange(int playerId)
        {
            // Arrange: B and C left and A won.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LeaveNow(match, PlayerBId);
            LeaveNow(match, PlayerCId);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            PlayerExitStatus status = LeaveNow(match, playerId);

            // Assert
            Assert.Equal(PlayerExitStatus.MatchAlreadyFinished, status);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Checks RemovePlayer_WithNullExit_ThrowsArgumentNullExceptionWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_WithNullExit_ThrowsArgumentNullExceptionWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            Action removePlayer = () => match.RemovePlayer(PlayerBId, null, _pickFirstCard);

            // Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(removePlayer);
            Assert.Equal("exit", exception.ParamName);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Checks RemovePlayer_WithNullRandomSelector_ThrowsArgumentNullExceptionWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_WithNullRandomSelector_ThrowsArgumentNullExceptionWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            Action removePlayer = () => match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_startUtc), null);

            // Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(removePlayer);
            Assert.Equal("chooseRandomCardIndex", exception.ParamName);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Checks ActionsOfPlayerWhoLeft_InPlacementAndBetting_ReturnPlayerNotInMatch.</summary>
        [Fact]
        public void ActionsOfPlayerWhoLeft_InPlacementAndBetting_ReturnPlayerNotInMatch()
        {
            // Arrange: B left. A opened bidding in the restarted round.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerBId);
            CardPlacementStatus placementStatus = match.PlaceCard(PlayerBId, SafeCard);
            PlaceAllInTurnOrder(match);
            BetActionStatus openStatus = match.OpenBet(PlayerBId, OneCardBet);
            match.OpenBet(PlayerAId, OneCardBet);
            string matchBeforeActions = DescribeMatch(match);

            // Act
            BetActionStatus raiseStatus = match.RaiseBet(PlayerBId, TwoCardBet);
            BetActionStatus withdrawStatus = match.WithdrawFromBet(PlayerBId);

            // Assert
            Assert.Equal(CardPlacementStatus.PlayerNotInMatch, placementStatus);
            Assert.Equal(BetActionStatus.PlayerNotInMatch, openStatus);
            Assert.Equal(BetActionStatus.PlayerNotInMatch, raiseStatus);
            Assert.Equal(BetActionStatus.PlayerNotInMatch, withdrawStatus);
            Assert.Equal(matchBeforeActions, DescribeMatch(match));
        }

        /// <summary>Checks RevealCard_ByOrOnPlayerWhoLeft_ReturnsNotInMatchStatuses.</summary>
        [Fact]
        public void RevealCard_ByOrOnPlayerWhoLeft_ReturnsNotInMatchStatuses()
        {
            // Arrange: D left. A bid 2 and the challenge is ready.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerDId);
            PlaceAllInTurnOrder(match);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _startUtc);
            string matchBeforeReveals = DescribeMatch(match);

            // Act
            ChallengeRevealStatus byLeaverStatus = match.RevealCard(PlayerDId, PlayerBId, _startUtc);
            ChallengeRevealStatus onLeaverStatus = match.RevealCard(PlayerAId, PlayerDId, _startUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.PlayerNotInMatch, byLeaverStatus);
            Assert.Equal(ChallengeRevealStatus.PileOwnerNotInMatch, onLeaverStatus);
            Assert.Equal(matchBeforeReveals, DescribeMatch(match));
        }

        /// <summary>Checks PunishmentActions_ByPlayerWhoLeft_ReturnPlayerNotInMatch.</summary>
        [Fact]
        public void PunishmentActions_ByPlayerWhoLeft_ReturnPlayerNotInMatch()
        {
            // Arrange: D left earlier. A then failed on B's Fatal Cat.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerDId);
            FailOnCatOfB(match);
            string matchBeforeActions = DescribeMatch(match);

            // Act
            PunishmentStatus chooseStatus = match.ChoosePunishmentCard(PlayerDId, FirstCardIndex, _startUtc);
            PunishmentStatus suspendStatus = match.SuspendPunishment(PlayerDId, _startUtc);
            PunishmentStatus resumeStatus = match.ResumePunishment(PlayerDId, _startUtc, _pickFirstCard);

            // Assert
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, chooseStatus);
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, suspendStatus);
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, resumeStatus);
            Assert.Equal(matchBeforeActions, DescribeMatch(match));
        }

        /// <summary>Checks GrantExtraLife_ToPlayerWhoLeftBeforeAnyCard_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void GrantExtraLife_ToPlayerWhoLeftBeforeAnyCard_ReturnsPlayerNotInMatch()
        {
            // Arrange: In eight-card mode, B leaves before anyone places a card.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerBId);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(PlayerBId);

            // Assert
            Assert.Equal(ExtraLifeStatus.PlayerNotInMatch, status);
            Assert.False(GetPlayer(match, PlayerBId).HasExtraLife);
        }

        /// <summary>Checks RoundNumber_WhenMatchStarts_IsOne.</summary>
        [Fact]
        public void RoundNumber_WhenMatchStarts_IsOne()
        {
            // Arrange
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer);

            // Act
            MatchState match = new MatchState(modeRules, new[] { PlayerAId, PlayerBId, PlayerCId });

            // Assert
            Assert.Equal(FirstRoundNumber, match.RoundNumber);
        }

        /// <summary>Checks RevealCard_WhenChallengeSucceeds_CountsOneRound.</summary>
        [Fact]
        public void RevealCard_WhenChallengeSucceeds_CountsOneRound()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            WinChallenge(match, PlayerAId);

            // Assert
            Assert.Equal(FirstRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Checks ChoosePunishmentCard_AfterFailedChallenge_CountsOneRoundOnly.</summary>
        [Fact]
        public void ChoosePunishmentCard_AfterFailedChallenge_CountsOneRoundOnly()
        {
            // Arrange: The failed challenge leaves punishment pending and the round count unchanged for now.
            MatchState match = CreateMatchWithBlindPunishment();
            int roundNumberWithPunishment = match.RoundNumber;

            // Act
            match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _startUtc);

            // Assert
            Assert.Equal(FirstRoundNumber, roundNumberWithPunishment);
            Assert.Equal(FirstRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Checks RemovePlayer_FirstPlayerLeavesTwice_CountsOneRoundOnly.</summary>
        [Fact]
        public void RemovePlayer_FirstPlayerLeavesTwice_CountsOneRoundOnly()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerAId);

            // Act
            LeaveNow(match, PlayerAId);

            // Assert
            Assert.Equal(FirstRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Each active player places a safe card, starting with the current player.</summary>
        private static void PlaceAllInTurnOrder(MatchState match)
        {
            int firstPlayerId = match.PlayerInTurn.PlayerId;
            match.PlaceCard(firstPlayerId, SafeCard);
            PlaceSafeCardsUntilTurnOf(match, firstPlayerId);
        }

        /// <summary>
        /// A places a safe card, B their Fatal Cat and the others safe cards. A bids 2 and fails on B's cat.
        /// </summary>
        private static void FailOnCatOfB(MatchState match)
        {
            match.PlaceCard(PlayerAId, SafeCard);
            match.PlaceCard(PlayerBId, FatalCatCard);
            PlaceSafeCardsUntilTurnOf(match, PlayerAId);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _startUtc);
            match.RevealCard(PlayerAId, PlayerBId, _startUtc);
        }

        private static MatchState CreateMatchWithBlindPunishment()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            FailOnCatOfB(match);

            return match;
        }

        /// <summary>Checks the restarted round for placement, no bid, an empty table and the expected turn.</summary>
        private static void AssertRestartedRound(MatchState match, int openingPlayerId)
        {
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
            Assert.Equal(NoBet, match.CurrentBet);
            Assert.Null(match.BetLeader);
            Assert.Equal(NoCardsOnTable, match.CardsOnTable);
            Assert.Equal(openingPlayerId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Checks that the given players have four lives and no successful challenges.</summary>
        private static void AssertFullHandsWithoutChallenges(MatchState match, params int[] playerIds)
        {
            Assert.All(playerIds, playerId => Assert.Equal(FourLives, GetPlayer(match, playerId).Lives));
            Assert.All(playerIds,
                playerId => Assert.Equal(NoChallenges, GetPlayer(match, playerId).SuccessfulChallengeCount));
        }

        /// <summary>Summarizes visible match state as text for a before-and-after comparison.</summary>
        private static string DescribeMatch(MatchState match)
        {
            string players = string.Join(";", match.Players.Select(player =>
                $"{player.PlayerId}:{string.Join(",", player.HandCards)}|{string.Join(",", player.PileCards)}"
                + $"|{player.RevealedCardCount}|{player.SuccessfulChallengeCount}|{player.HasLeftMatch}"));
            string round = $"{match.RoundNumber}#{match.Phase}#{match.PlayerInTurn.PlayerId}#{match.CurrentBet}"
                + $"#{match.BetLeader?.PlayerId}#{match.Winner?.PlayerId}#{match.PendingPunishment?.DeadlineUtc}";

            return $"{players}#{round}";
        }

        private static MatchState CreateMatch(int cardsPerPlayerCount, params int[] playerIdsInOrder)
        {
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            MatchState match = new MatchState(modeRules, playerIdsInOrder);

            return match;
        }

        private static MatchPlayer GetPlayer(MatchState match, int playerId)
        {
            return match.Players.First(player => player.PlayerId == playerId);
        }

        /// <summary>The current player opens and all others fold, leaving the opener as the final bidder.</summary>
        private static void OpenAndConcede(MatchState match, int betValue)
        {
            match.OpenBet(match.PlayerInTurn.PlayerId, betValue);
            int maximumWithdrawals = match.Players.Count;

            for (int withdrawal = 0; withdrawal < maximumWithdrawals && match.Phase == RoundPhase.Betting;
                withdrawal++)
            {
                int blockingPlayerId = match.PlayerInTurn.PlayerId;
                BetActionStatus status = match.WithdrawFromBet(blockingPlayerId);
                Assert.True(status == BetActionStatus.Accepted,
                    $"El jugador {blockingPlayerId} no pudo retirarse en la ronda {match.RoundNumber}: {status}.");
            }

            Assert.True(match.Phase != RoundPhase.Betting,
                $"La apuesta no se cerró en la ronda {match.RoundNumber}; turno de {match.PlayerInTurn.PlayerId}.");
        }

        /// <summary>
        /// Players place a safe card in turn until the target player's turn. One full cycle is enough. A rejected
        /// placement or exhausted cycle reports the player and round that blocked progress.
        /// </summary>
        private static void PlaceSafeCardsUntilTurnOf(MatchState match, int playerId)
        {
            int maximumPlacements = match.Players.Count;

            for (int placement = 0; placement < maximumPlacements && match.PlayerInTurn.PlayerId != playerId;
                placement++)
            {
                int blockingPlayerId = match.PlayerInTurn.PlayerId;
                CardPlacementStatus status = match.PlaceCard(blockingPlayerId, SafeCard);
                Assert.True(status == CardPlacementStatus.Placed,
                    $"El jugador {blockingPlayerId} no pudo colocar en la ronda {match.RoundNumber}: {status}.");
            }

            Assert.True(match.PlayerInTurn.PlayerId == playerId,
                $"El turno quedó en {match.PlayerInTurn.PlayerId} en la ronda {match.RoundNumber}, no en {playerId}.");
        }

        /// <summary>
        /// Others place safe cards until the player's turn. That player places the given card, waits another cycle and
        /// bids 1 on their own pile. A safe card wins and a Fatal Cat fails.
        /// </summary>
        private static void BetOneOnOwnTopCard(MatchState match, int playerId, CardKind card)
        {
            PlaceSafeCardsUntilTurnOf(match, playerId);
            match.PlaceCard(playerId, card);
            PlaceSafeCardsUntilTurnOf(match, playerId);
            OpenAndConcede(match, OneCardBet);
            match.RevealCard(playerId, playerId, _startUtc);
        }

        private static void WinChallenge(MatchState match, int playerId)
        {
            BetOneOnOwnTopCard(match, playerId, SafeCard);
        }

        private static void WinChallenges(MatchState match, int playerId, int challengeCount)
        {
            for (int attempt = 0; attempt < challengeCount
                && GetPlayer(match, playerId).SuccessfulChallengeCount < challengeCount; attempt++)
            {
                WinChallenge(match, playerId);
            }

            Assert.True(GetPlayer(match, playerId).SuccessfulChallengeCount >= challengeCount,
                $"El jugador {playerId} no llegó a {challengeCount} retos en la ronda {match.RoundNumber}.");
        }

        /// <summary>
        /// Fails on the player's own Fatal Cat and loses a safe card, or the first card if no safe card remains.
        /// </summary>
        private static void LoseCardOnOwnCat(MatchState match, int playerId)
        {
            BetOneOnOwnTopCard(match, playerId, FatalCatCard);
            int safeCardIndex = GetPlayer(match, playerId).HandCards.ToList().IndexOf(SafeCard);
            match.ChoosePunishmentCard(playerId, Math.Max(safeCardIndex, FirstCardIndex), _startUtc);
        }

        private static void EliminateOnOwnCat(MatchState match, int playerId)
        {
            int maximumLosses = match.ModeRules.CardsPerPlayerCount;

            for (int loss = 0; loss < maximumLosses && !GetPlayer(match, playerId).IsEliminated; loss++)
            {
                LoseCardOnOwnCat(match, playerId);
            }

            Assert.True(GetPlayer(match, playerId).IsEliminated,
                $"El jugador {playerId} no quedó eliminado en la ronda {match.RoundNumber}.");
        }

        private static PlayerExitStatus LeaveNow(MatchState match, int playerId)
        {
            return match.RemovePlayer(playerId, PlayerExit.CreateVoluntary(_startUtc), _pickFirstCard);
        }
    }
}
