using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests own and blind punishment choices, server slot mapping, last life, elimination and exits during punishment.
    /// </summary>
    public class SessionRegistryMatchPunishmentTests
    {
        private const int MinimumPlayerCount = 3;
        private const int CardsPerPlayer = MatchPlayScenario.CardsPerPlayerCount;
        private const int LivesAfterOnePunishment = CardsPerPlayer - 1;
        private const int SecondRound = 2;
        private const int NoUserId = 0;
        private const int OutOfRangeSlot = CardsPerPlayer;
        private const int FatalCatHandIndex = 0;
        private const int RoundsBeforeSingleLife = CardsPerPlayer - 1;

        /// <summary>Checks ChoosePunishmentCard_BlindWithAScrambledOrder_TranslatesTheSlotAndKeepsTheCat.</summary>
        [Fact]
        public void ChoosePunishmentCard_BlindWithAScrambledOrder_TranslatesTheSlotAndKeepsTheCat()
        {
            // Arrange: With no queued indices, the mapping is [1, 2, 3, 0]. Slot 0 means card 1, a safe card.
            MatchPlayScenario scenario = StartBlindPunishment();

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[1], 0);

            // Assert
            MatchView punishedView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(LivesAfterOnePunishment, punishedView.OwnHandCards.Count);
            Assert.Contains(MatchCardKind.FatalCat, punishedView.OwnHandCards);
            Assert.Equal(MatchPunishmentResolution.ChosenByChooser, punishedView.Challenge.PunishmentResolution);
        }

        /// <summary>Checks ChoosePunishmentCard_BlindWithTheIdentityOrder_RemovesTheFatalCatAtSlotZero.</summary>
        [Fact]
        public void ChoosePunishmentCard_BlindWithTheIdentityOrder_RemovesTheFatalCatAtSlotZero()
        {
            // Arrange: Indices 3, 2 and 1 keep the mapping unchanged. Slot 0 means card 0, the Fatal Cat.
            MatchPlayScenario scenario = StartBlindPunishment(3, 2, 1);

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[1], 0);

            // Assert
            MatchView punishedView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            Assert.DoesNotContain(MatchCardKind.FatalCat, punishedView.OwnHandCards);
            Assert.Equal(LivesAfterOnePunishment, punishedView.OwnHandCards.Count);
        }

        /// <summary>Checks ChoosePunishmentCard_Blind_AdvancesToTheNextRoundWithTheFirstActivePlayer.</summary>
        [Fact]
        public void ChoosePunishmentCard_Blind_AdvancesToTheNextRoundWithTheFirstActivePlayer()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[1], 0);

            // Assert
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(SecondRound, item.MatchView.RoundNumber));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(
                scenario.UserIds[0], item.MatchView.PlayerInTurnUserId));
            Assert.All(delivery.ViewDeliveries, item => Assert.Null(item.MatchView.Punishment));
        }

        /// <summary>Checks ChoosePunishmentCard_Blind_OthersSeeOnlyTheLivesGoingDown.</summary>
        [Fact]
        public void ChoosePunishmentCard_Blind_OthersSeeOnlyTheLivesGoingDown()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[1], 0);

            // Assert
            MatchView otherView = ViewOf(delivery, scenario, scenario.UserIds[2]);
            MatchPlayerView punished = otherView.Players.Single(player => player.UserId == scenario.UserIds[0]);
            Assert.Equal(LivesAfterOnePunishment, punished.HandCardCount);
            Assert.Equal(CardsPerPlayer, otherView.OwnHandCards.Count);
        }

        /// <summary>Checks ChoosePunishmentCard_OwnChoice_RemovesTheChosenHandCard.</summary>
        [Fact]
        public void ChoosePunishmentCard_OwnChoice_RemovesTheChosenHandCard()
        {
            // Arrange: After the failed challenge the bidder's hand is [Safe, Safe, Safe, FatalCat].
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.ReachOwnPunishment();

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[0], 0);

            // Assert
            MatchView bettorView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            Assert.Null(delivery.ErrorKey);
            Assert.Contains(MatchCardKind.FatalCat, bettorView.OwnHandCards);
            Assert.Equal(LivesAfterOnePunishment, bettorView.OwnHandCards.Count);
            Assert.Equal(MatchPunishmentResolution.ChosenByChooser, bettorView.Challenge.PunishmentResolution);
        }

        /// <summary>Checks ChoosePunishmentCard_ByAPlayerWhoIsNotTheChooser_IsRejectedWithoutChanges.</summary>
        [Fact]
        public void ChoosePunishmentCard_ByAPlayerWhoIsNotTheChooser_IsRejectedWithoutChanges()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[2]);

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[2], 0);

            // Assert
            Assert.Equal(MatchErrorKeys.NotTheChooser, delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
            Assert.Equal(versionBefore, scenario.ReadVersionAs(scenario.UserIds[2]));
        }

        /// <summary>Checks ChoosePunishmentCard_BlindWithASlotOutOfRange_IsRejected.</summary>
        /// <param name="slot">Slot outside the available card range.</param>
        [Theory]
        [InlineData(-1)]
        [InlineData(OutOfRangeSlot)]
        public void ChoosePunishmentCard_BlindWithASlotOutOfRange_IsRejected(int slot)
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[1], slot);

            // Assert
            Assert.Equal(MatchErrorKeys.CardIndexNotValid, delivery.ErrorKey);
        }

        /// <summary>Checks ChoosePunishmentCard_WithoutAPendingPunishment_IsRejected.</summary>
        [Fact]
        public void ChoosePunishmentCard_WithoutAPendingPunishment_IsRejected()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[0], 0);

            // Assert
            Assert.Equal(MatchErrorKeys.NoPunishmentPending, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceCardAndPlaceBet_DuringAPunishment_AreRejectedWithPunishmentPending.</summary>
        [Fact]
        public void PlaceCardAndPlaceBet_DuringAPunishment_AreRejectedWithPunishmentPending()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();

            // Act
            MatchDelivery cardDelivery = scenario.Place(scenario.UserIds[0], MatchCardKind.Safe);
            MatchDelivery betDelivery = scenario.PlaceBet(scenario.UserIds[0], 1);

            // Assert
            Assert.Equal(MatchErrorKeys.PunishmentPending, cardDelivery.ErrorKey);
            Assert.Equal(MatchErrorKeys.PunishmentPending, betDelivery.ErrorKey);
        }

        /// <summary>Checks RevealCard_FailingWithASingleLife_EliminatesWithoutAskingForAChoice.</summary>
        [Fact]
        public void RevealCard_FailingWithASingleLife_EliminatesWithoutAskingForAChoice()
        {
            // Arrange: After three blind punishments, the first player has only their Fatal Cat left.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            LoseSafeCardsToBlindPunishments(scenario);
            scenario.Place(scenario.UserIds[0], MatchCardKind.FatalCat);
            scenario.Place(scenario.UserIds[1], MatchCardKind.Safe);
            scenario.Place(scenario.UserIds[2], MatchCardKind.Safe);
            scenario.PlaceBet(scenario.UserIds[0], 1);
            scenario.Withdraw(scenario.UserIds[1]);
            scenario.Withdraw(scenario.UserIds[2]);

            // Act
            MatchDelivery delivery = scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            // Assert
            MatchView eliminatedView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            MatchView otherView = ViewOf(delivery, scenario, scenario.UserIds[2]);
            Assert.Equal(MatchPunishmentResolution.SingleLifeNoChoice, otherView.Challenge.PunishmentResolution);
            Assert.True(otherView.Players.Single(player => player.UserId == scenario.UserIds[0]).IsEliminated);
            Assert.Null(otherView.Punishment);
            Assert.Equal(MatchRoundPhase.PlacingCards, otherView.Phase);
            Assert.Equal(scenario.UserIds[1], otherView.PlayerInTurnUserId);
            Assert.False(eliminatedView.CanPlaceCard);
            Assert.Empty(eliminatedView.OwnHandCards);
        }

        /// <summary>Checks LeaveRoom_WhenAnEliminatedPlayerLeaves_ChangesNothing.</summary>
        [Fact]
        public void LeaveRoom_WhenAnEliminatedPlayerLeaves_ChangesNothing()
        {
            // Arrange
            MatchPlayScenario scenario = StartEliminationOfTheFirstPlayer();
            IReadOnlyList<MatchViewDelivery> deliveries;
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[1]);

            // Act
            scenario.Registry.LeaveRoom(scenario.TokenOf(scenario.UserIds[0]), out deliveries);

            // Assert
            Assert.Empty(deliveries);
            Assert.Equal(versionBefore, scenario.ReadVersionAs(scenario.UserIds[1]));
        }

        /// <summary>Checks LeaveRoom_AfterAnEliminationWhenAnotherLeaves_TheLastActivePlayerWins.</summary>
        [Fact]
        public void LeaveRoom_AfterAnEliminationWhenAnotherLeaves_TheLastActivePlayerWins()
        {
            // Arrange
            MatchPlayScenario scenario = StartEliminationOfTheFirstPlayer();
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            scenario.Registry.LeaveRoom(scenario.TokenOf(scenario.UserIds[1]), out deliveries);

            // Assert
            MatchView lastPlayerView = deliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(scenario.UserIds[2]))).MatchView;
            Assert.Equal(MatchRoundPhase.MatchFinished, lastPlayerView.Phase);
            Assert.Equal(scenario.UserIds[2], lastPlayerView.WinnerUserId);
        }

        /// <summary>Checks LeaveRoom_WhenThePunishedPlayerLeaves_CancelsThePunishmentAndRestartsTheRound.</summary>
        [Fact]
        public void LeaveRoom_WhenThePunishedPlayerLeaves_CancelsThePunishmentAndRestartsTheRound()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            scenario.Registry.LeaveRoom(scenario.TokenOf(scenario.UserIds[0]), out deliveries);

            // Assert
            MatchView chooserView = deliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(scenario.UserIds[1]))).MatchView;
            Assert.Null(chooserView.Punishment);
            Assert.False(chooserView.CanChoosePunishmentCard);
            Assert.Equal(MatchPunishmentResolution.Cancelled, chooserView.Challenge.PunishmentResolution);
            Assert.Equal(MatchRoundPhase.PlacingCards, chooserView.Phase);
            Assert.Equal(scenario.UserIds[1], chooserView.PlayerInTurnUserId);
        }

        /// <summary>Checks LeaveRoom_WhenTheChooserLeaves_ResolvesThePunishmentAtRandom.</summary>
        [Fact]
        public void LeaveRoom_WhenTheChooserLeaves_ResolvesThePunishmentAtRandom()
        {
            // Arrange: The random choice picks index 0, the Fatal Cat in the punished player's hand.
            MatchPlayScenario scenario = StartBlindPunishment();
            scenario.RandomSource.Enqueue(FatalCatHandIndex);
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            scenario.Registry.LeaveRoom(scenario.TokenOf(scenario.UserIds[1]), out deliveries);

            // Assert
            MatchView punishedView = deliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(scenario.UserIds[0]))).MatchView;
            Assert.Equal(MatchPunishmentResolution.ChosenAtRandom, punishedView.Challenge.PunishmentResolution);
            Assert.DoesNotContain(MatchCardKind.FatalCat, punishedView.OwnHandCards);
            Assert.Equal(LivesAfterOnePunishment, punishedView.OwnHandCards.Count);
            Assert.Null(punishedView.Punishment);
        }

        /// <summary>Checks LeaveRoom_WhenAnUninvolvedPlayerLeaves_KeepsThePunishmentPending.</summary>
        [Fact]
        public void LeaveRoom_WhenAnUninvolvedPlayerLeaves_KeepsThePunishmentPending()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            scenario.Registry.LeaveRoom(scenario.TokenOf(scenario.UserIds[2]), out deliveries);

            // Assert
            MatchView chooserView = deliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(scenario.UserIds[1]))).MatchView;
            Assert.Equal(MatchRoundPhase.PunishmentPending, chooserView.Phase);
            Assert.True(chooserView.CanChoosePunishmentCard);
            Assert.Equal(MatchPunishmentResolution.Pending, chooserView.Challenge.PunishmentResolution);
            Assert.True(chooserView.Players.Single(player => player.UserId == scenario.UserIds[2]).HasLeftMatch);
        }

        /// <summary>
        /// Losing the link of a player who is neither the punished one nor the chooser does not suspend the
        /// punishment, and the chooser is not blocked by that reconnection: the choice is accepted and the round
        /// advances while the third player's grace stays open.
        /// </summary>
        [Fact]
        public void ChoosePunishmentCard_WhenAnUninvolvedPlayerIsDisconnected_IsAcceptedForTheChooser()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            int punishedUserId = scenario.UserIds[0];
            int chooserUserId = scenario.UserIds[1];
            int bystanderUserId = scenario.UserIds[2];
            int versionBefore = scenario.ReadVersionAs(chooserUserId);
            IReadOnlyList<MatchViewDelivery> loss;
            bool wasRemoved = scenario.Registry.RemoveMatchChannel(
                MatchPlayScenario.MatchChannelPrefix + bystanderUserId, scenario.CallbackOf(bystanderUserId), out loss);
            Assert.True(wasRemoved);
            Assert.All(loss, item => Assert.Equal(MatchRoundPhase.PunishmentPending, item.MatchView.Phase));
            Assert.All(loss, item => Assert.Equal(MatchPunishmentResolution.Pending,
                item.MatchView.Challenge.PunishmentResolution));

            // Act
            MatchDelivery delivery = scenario.Choose(chooserUserId, 0);

            // Assert
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(2, delivery.ViewDeliveries.Count);
            Assert.DoesNotContain(scenario.CallbackOf(bystanderUserId), MatchPlayScenario.RecipientsOf(delivery));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(versionBefore + 2, item.MatchView.Version));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(SecondRound, item.MatchView.RoundNumber));
            Assert.All(delivery.ViewDeliveries, item => Assert.Null(item.MatchView.Punishment));
            MatchView punishedView = ViewOf(delivery, scenario, punishedUserId);
            Assert.Equal(LivesAfterOnePunishment, punishedView.OwnHandCards.Count);
            Assert.Equal(MatchPunishmentResolution.ChosenByChooser, punishedView.Challenge.PunishmentResolution);
        }

        /// <summary>
        /// The reconnection guard keeps blocking everyone who is not the chooser: while a third player is
        /// disconnected, the punished player cannot act and nothing changes.
        /// </summary>
        [Fact]
        public void PlaceCard_ByThePunishedPlayerWithAnUninvolvedOneDisconnected_IsRejectedWithReconnectionPending()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            Assert.True(scenario.CloseMatchChannelOf(scenario.UserIds[2]));

            // Act
            MatchDelivery attempt = scenario.Place(scenario.UserIds[0], MatchCardKind.Safe);

            // Assert
            Assert.Equal("Match_ReconnectionPending", attempt.ErrorKey);
            Assert.Empty(attempt.ViewDeliveries);
        }

        private static MatchPlayScenario StartBlindPunishment(params int[] shuffleIndexes)
        {
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.ReachChallengeReady(MatchCardKind.FatalCat, 2);
            scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            foreach (int shuffleIndex in shuffleIndexes)
            {
                scenario.RandomSource.Enqueue(shuffleIndex);
            }

            scenario.Reveal(scenario.UserIds[0], scenario.UserIds[1]);

            return scenario;
        }

        /// <summary>
        /// Three rounds in which the first player loses a safe card from slot 0 of the shuffled choices.
        /// </summary>
        private static void LoseSafeCardsToBlindPunishments(MatchPlayScenario scenario)
        {
            for (int roundNumber = 0; roundNumber < RoundsBeforeSingleLife; roundNumber++)
            {
                scenario.ReachChallengeReady(MatchCardKind.FatalCat, 2);
                scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);
                scenario.Reveal(scenario.UserIds[0], scenario.UserIds[1]);
                scenario.Choose(scenario.UserIds[1], 0);
            }
        }

        [Fact]
        public void GetRecoveryContext_EliminatedPlayerCanReturnAsSpectatorAfterLogin()
        {
            var scenario = StartEliminationOfTheFirstPlayer();
            int userId = scenario.CreatorUserId;
            Assert.True(scenario.CloseMatchChannelOf(userId));
            string replacement = scenario.Registry.StartRegisteredSession(userId, "Spectator", out _);
            var context = scenario.Registry.GetRecoveryContext(replacement).OperationResult;

            Assert.Equal(scenario.MatchId, context.MatchId);
            var delivery = scenario.Registry.SubscribeToMatch(new SessionChannelCaller(replacement, "spectator"),
                context.MatchId.Value, new MatchCallbackStub());
            var view = Assert.Single(delivery.ViewDeliveries).MatchView;
            Assert.True(view.Players.Single(player => player.UserId == userId).IsEliminated);
            Assert.Empty(view.OwnHandCards);
            Assert.Empty(view.OwnPileCards);
            Assert.False(view.CanPlaceCard);
        }

        private static MatchPlayScenario StartEliminationOfTheFirstPlayer()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            LoseSafeCardsToBlindPunishments(scenario);
            scenario.Place(scenario.UserIds[0], MatchCardKind.FatalCat);
            scenario.Place(scenario.UserIds[1], MatchCardKind.Safe);
            scenario.Place(scenario.UserIds[2], MatchCardKind.Safe);
            scenario.PlaceBet(scenario.UserIds[0], 1);
            scenario.Withdraw(scenario.UserIds[1]);
            scenario.Withdraw(scenario.UserIds[2]);
            scenario.Reveal(scenario.UserIds[0], scenario.UserIds[0]);

            return scenario;
        }

        private static MatchView ViewOf(MatchDelivery delivery, MatchPlayScenario scenario, int userId)
        {
            return delivery.ViewDeliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(userId))).MatchView;
        }
    }
}
