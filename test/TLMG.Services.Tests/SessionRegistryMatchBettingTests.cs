using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests PlaceBet and WithdrawFromBet in the registry, including authorization, engine decisions, version and
    /// views.
    /// </summary>
    public class SessionRegistryMatchBettingTests
    {
        private const int MinimumPlayerCount = 3;
        private const int TwoCards = 2;
        private const int ThreeCards = 3;
        private const int OneCard = 1;
        private const int SecondRound = 2;
        private const int CardsPerPlayer = MatchPlayScenario.CardsPerPlayerCount;
        private const int OutOfRangeBet = 0;
        private const int FullLaps = CardsPerPlayer;

        /// <summary>Checks PlaceBet_ByThePlayerInTurnWithACardPlaced_OpensTheBetForEveryone.</summary>
        [Fact]
        public void PlaceBet_ByThePlayerInTurnWithACardPlaced_OpensTheBetForEveryone()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            int bettorUserId = scenario.UserIds[0];

            // Act
            MatchDelivery delivery = scenario.PlaceBet(bettorUserId, TwoCards);

            // Assert
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(MinimumPlayerCount, delivery.ViewDeliveries.Count);
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(MatchRoundPhase.Betting, item.MatchView.Phase));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(TwoCards, item.MatchView.CurrentBet));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(bettorUserId, item.MatchView.BetLeaderUserId));
            int nextBettorUserId = scenario.UserIds[1];
            Assert.All(
                delivery.ViewDeliveries,
                item => Assert.Equal(nextBettorUserId, item.MatchView.PlayerInTurnUserId));
        }

        /// <summary>Checks PlaceBet_ByTheNextPlayerWithAHigherValue_RaisesTheBet.</summary>
        [Fact]
        public void PlaceBet_ByTheNextPlayerWithAHigherValue_RaisesTheBet()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            scenario.PlaceBet(scenario.UserIds[0], TwoCards);

            // Act
            MatchDelivery delivery = scenario.PlaceBet(scenario.UserIds[1], ThreeCards);

            // Assert
            MatchView view = delivery.ViewDeliveries[0].MatchView;
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(ThreeCards, view.CurrentBet);
            Assert.Equal(scenario.UserIds[1], view.BetLeaderUserId);
            Assert.Equal(scenario.UserIds[2], view.PlayerInTurnUserId);
        }

        /// <summary>Checks WithdrawFromBet_ByEveryoneButTheBettor_LeavesTheChallengeReadyAndRecordsIt.</summary>
        [Fact]
        public void WithdrawFromBet_ByEveryoneButTheBettor_LeavesTheChallengeReadyAndRecordsIt()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            int bettorUserId = scenario.UserIds[0];
            scenario.PlaceBet(bettorUserId, TwoCards);
            scenario.Withdraw(scenario.UserIds[1]);

            // Act
            MatchDelivery delivery = scenario.Withdraw(scenario.UserIds[2]);

            // Assert
            MatchView bettorView = delivery.ViewDeliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(bettorUserId))).MatchView;
            Assert.Equal(MatchRoundPhase.ChallengeReady, bettorView.Phase);
            Assert.True(bettorView.CanRevealCard);
            Assert.Equal(bettorUserId, bettorView.PlayerInTurnUserId);
            Assert.Equal(MatchChallengeOutcome.InProgress, bettorView.Challenge.Outcome);
            Assert.Equal(bettorUserId, bettorView.Challenge.BettorUserId);
            Assert.Equal(TwoCards, bettorView.Challenge.PromisedCount);
            Assert.Empty(bettorView.Challenge.RevealedCards);
        }

        /// <summary>Checks WithdrawFromBet_WhileOthersStillBet_KeepsBettingAndMarksTheWithdrawal.</summary>
        [Fact]
        public void WithdrawFromBet_WhileOthersStillBet_KeepsBettingAndMarksTheWithdrawal()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            scenario.PlaceBet(scenario.UserIds[0], OneCard);

            // Act
            MatchDelivery delivery = scenario.Withdraw(scenario.UserIds[1]);

            // Assert
            MatchView view = delivery.ViewDeliveries[0].MatchView;
            Assert.Equal(MatchRoundPhase.Betting, view.Phase);
            Assert.Null(view.Challenge);
            Assert.True(view.Players.Single(player => player.UserId == scenario.UserIds[1]).HasWithdrawnFromBet);
            Assert.Equal(scenario.UserIds[2], view.PlayerInTurnUserId);
        }

        /// <summary>Checks PlaceBet_WithEmptyHand_AcceptsOnlyABetOfOne.</summary>
        [Fact]
        public void PlaceBet_WithEmptyHand_AcceptsOnlyABetOfOne()
        {
            // Arrange: Everyone places four cards, leaving the creator with an empty hand.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            PlaceAllCards(scenario);
            int bettorUserId = scenario.UserIds[0];

            // Act
            MatchDelivery rejected = scenario.PlaceBet(bettorUserId, TwoCards);
            MatchDelivery accepted = scenario.PlaceBet(bettorUserId, OneCard);

            // Assert
            Assert.Equal(MatchErrorKeys.EmptyHandBet, rejected.ErrorKey);
            Assert.Null(accepted.ErrorKey);
        }

        /// <summary>Checks PlaceBet_OutOfTurn_IsRejectedWithoutChangingTheVersion.</summary>
        [Fact]
        public void PlaceBet_OutOfTurn_IsRejectedWithoutChangingTheVersion()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[0]);

            // Act
            MatchDelivery delivery = scenario.PlaceBet(scenario.UserIds[1], OneCard);

            // Assert
            Assert.Equal(MatchErrorKeys.NotYourTurn, delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
            Assert.Equal(versionBefore, scenario.ReadVersionAs(scenario.UserIds[0]));
        }

        /// <summary>Checks PlaceBet_WithoutAPlacedCard_IsRejectedWithNoCardPlaced.</summary>
        [Fact]
        public void PlaceBet_WithoutAPlacedCard_IsRejectedWithNoCardPlaced()
        {
            // Arrange: The first player placed one card. The second player's turn begins before they have placed any.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.Place(scenario.UserIds[0], MatchCardKind.Safe);

            // Act
            MatchDelivery delivery = scenario.PlaceBet(scenario.UserIds[1], OneCard);

            // Assert
            Assert.Equal(MatchErrorKeys.NoCardPlaced, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceBet_OnAnEmptyTable_IsRejectedWithTableEmpty.</summary>
        [Fact]
        public void PlaceBet_OnAnEmptyTable_IsRejectedWithTableEmpty()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();

            // Act
            MatchDelivery delivery = scenario.PlaceBet(scenario.UserIds[0], OneCard);

            // Assert
            Assert.Equal(MatchErrorKeys.TableEmpty, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceBet_WithValueOutOfRange_IsRejectedWithBetValueNotValid.</summary>
        /// <param name="betValue">Value outside the table's bid range.</param>
        [Theory]
        [InlineData(OutOfRangeBet)]
        [InlineData(-1)]
        [InlineData(CardsPerPlayer + 1)]
        public void PlaceBet_WithValueOutOfRange_IsRejectedWithBetValueNotValid(int betValue)
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();

            // Act
            MatchDelivery delivery = scenario.PlaceBet(scenario.UserIds[0], betValue);

            // Assert
            Assert.Equal(MatchErrorKeys.BetValueNotValid, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceBet_RaisingWithoutExceedingTheCurrentBet_IsRejected.</summary>
        [Fact]
        public void PlaceBet_RaisingWithoutExceedingTheCurrentBet_IsRejected()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            scenario.PlaceBet(scenario.UserIds[0], TwoCards);

            // Act
            MatchDelivery delivery = scenario.PlaceBet(scenario.UserIds[1], TwoCards);

            // Assert
            Assert.Equal(MatchErrorKeys.BetValueNotValid, delivery.ErrorKey);
        }

        /// <summary>Checks WithdrawFromBet_WithoutABetInProgress_IsRejectedWithNoBetInProgress.</summary>
        [Fact]
        public void WithdrawFromBet_WithoutABetInProgress_IsRejectedWithNoBetInProgress()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();

            // Act
            MatchDelivery delivery = scenario.Withdraw(scenario.UserIds[0]);

            // Assert
            Assert.Equal(MatchErrorKeys.NoBetInProgress, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceCard_WhileTheBetIsOpen_IsRejectedWithBetInProgress.</summary>
        [Fact]
        public void PlaceCard_WhileTheBetIsOpen_IsRejectedWithBetInProgress()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            scenario.PlaceBet(scenario.UserIds[0], OneCard);

            // Act
            MatchDelivery delivery = scenario.Place(scenario.UserIds[1], MatchCardKind.Safe);

            // Assert
            Assert.Equal(MatchErrorKeys.BetInProgress, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceBet_WithoutSubscription_IsRejectedWithNotSubscribed.</summary>
        [Fact]
        public void PlaceBet_WithoutSubscription_IsRejectedWithNotSubscribed()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);

            // Act
            MatchDelivery delivery = scenario.PlaceBet(scenario.UserIds[0], OneCard);

            // Assert
            Assert.Equal(MatchErrorKeys.NotSubscribed, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceBet_WithInvalidToken_IsRejectedWithInvalidSession.</summary>
        [Fact]
        public void PlaceBet_WithInvalidToken_IsRejectedWithInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();

            // Act
            MatchDelivery delivery = scenario.Registry.PlaceBet(MatchPlayScenario.CallerWithToken("invalid"), OneCard);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceBet_AtTheMaximumOfTheViewRange_IsAcceptedAndOneMoreIsRejected.</summary>
        [Fact]
        public void PlaceBet_AtTheMaximumOfTheViewRange_IsAcceptedAndOneMoreIsRejected()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            int bettorUserId = scenario.UserIds[0];
            MatchView view = scenario.ReadViewAs(bettorUserId);

            // Act
            MatchDelivery overTheMaximum = scenario.PlaceBet(bettorUserId, view.MaximumBetValue + 1);
            MatchDelivery atTheMaximum = scenario.PlaceBet(bettorUserId, view.MaximumBetValue);

            // Assert
            Assert.True(view.CanPlaceBet);
            Assert.Equal(MatchErrorKeys.BetValueNotValid, overTheMaximum.ErrorKey);
            Assert.Null(atTheMaximum.ErrorKey);
        }

        /// <summary>Checks LeaveRoom_VoluntaryDuringABet_CancelsTheBetAndRestartsTheRound.</summary>
        [Fact]
        public void LeaveRoom_VoluntaryDuringABet_CancelsTheBetAndRestartsTheRound()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            int bettorUserId = scenario.UserIds[0];
            scenario.PlaceBet(bettorUserId, TwoCards);
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            scenario.Registry.LeaveRoom(scenario.TokenOf(scenario.UserIds[1]), out deliveries);

            // Assert
            MatchView bettorView = deliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(bettorUserId))).MatchView;
            Assert.Equal(MatchRoundPhase.PlacingCards, bettorView.Phase);
            Assert.Equal(0, bettorView.CurrentBet);
            Assert.Equal(0, bettorView.BetLeaderUserId);
            Assert.Equal(SecondRound, bettorView.RoundNumber);
            Assert.Equal(0, bettorView.CardsOnTable);
            Assert.Equal(bettorUserId, bettorView.PlayerInTurnUserId);
            Assert.Null(bettorView.Challenge);
        }

        /// <summary>Checks LeaveRoom_VoluntaryByTheBettorWithTheChallengeReady_DropsTheChallenge.</summary>
        [Fact]
        public void LeaveRoom_VoluntaryByTheBettorWithTheChallengeReady_DropsTheChallenge()
        {
            // Arrange
            MatchPlayScenario scenario = StartWithOneCardEach();
            int bettorUserId = scenario.UserIds[0];
            scenario.PlaceBet(bettorUserId, TwoCards);
            scenario.Withdraw(scenario.UserIds[1]);
            scenario.Withdraw(scenario.UserIds[2]);
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            scenario.Registry.LeaveRoom(scenario.TokenOf(bettorUserId), out deliveries);

            // Assert
            Assert.All(deliveries, item => Assert.Null(item.MatchView.Challenge));
            Assert.All(deliveries, item => Assert.Equal(MatchRoundPhase.PlacingCards, item.MatchView.Phase));
            Assert.All(deliveries, item => Assert.False(item.MatchView.CanRevealCard));
        }

        private static MatchPlayScenario StartWithOneCardEach()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.PlaceOneSafeCardEach();

            return scenario;
        }

        private static void PlaceAllCards(MatchPlayScenario scenario)
        {
            for (int lapNumber = 0; lapNumber < FullLaps; lapNumber++)
            {
                MatchCardKind card = lapNumber == FullLaps - 1 ? MatchCardKind.FatalCat : MatchCardKind.Safe;

                foreach (int userId in scenario.UserIds)
                {
                    scenario.Place(userId, card);
                }
            }
        }
    }
}
