using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Pruebas del castigo en el registro: elección propia y a ciegas con la permutación del servidor, una sola vida,
    /// eliminación y salida de los jugadores durante el castigo.
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

        /// <summary>Verifica ChoosePunishmentCard_BlindWithAScrambledOrder_TranslatesTheSlotAndKeepsTheCat.</summary>
        [Fact]
        public void ChoosePunishmentCard_BlindWithAScrambledOrder_TranslatesTheSlotAndKeepsTheCat()
        {
            // Arrange: sin índices encolados la permutación queda [1, 2, 3, 0]: el lugar 0 es la carta 1 (segura).
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

        /// <summary>Verifica ChoosePunishmentCard_BlindWithTheIdentityOrder_RemovesTheFatalCatAtSlotZero.</summary>
        [Fact]
        public void ChoosePunishmentCard_BlindWithTheIdentityOrder_RemovesTheFatalCatAtSlotZero()
        {
            // Arrange: los índices 3, 2 y 1 dejan la permutación sin cambios: el lugar 0 es la carta 0 (el Gato).
            MatchPlayScenario scenario = StartBlindPunishment(3, 2, 1);

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[1], 0);

            // Assert
            MatchView punishedView = ViewOf(delivery, scenario, scenario.UserIds[0]);
            Assert.DoesNotContain(MatchCardKind.FatalCat, punishedView.OwnHandCards);
            Assert.Equal(LivesAfterOnePunishment, punishedView.OwnHandCards.Count);
        }

        /// <summary>Verifica ChoosePunishmentCard_Blind_AdvancesToTheNextRoundWithTheFirstActivePlayer.</summary>
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

        /// <summary>Verifica ChoosePunishmentCard_Blind_OthersSeeOnlyTheLivesGoingDown.</summary>
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

        /// <summary>Verifica ChoosePunishmentCard_OwnChoice_RemovesTheChosenHandCard.</summary>
        [Fact]
        public void ChoosePunishmentCard_OwnChoice_RemovesTheChosenHandCard()
        {
            // Arrange: tras el reto fallido la mano del apostador es [Safe, Safe, Safe, Gato].
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

        /// <summary>Verifica ChoosePunishmentCard_ByAPlayerWhoIsNotTheChooser_IsRejectedWithoutChanges.</summary>
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

        /// <summary>Verifica ChoosePunishmentCard_BlindWithASlotOutOfRange_IsRejected.</summary>
        /// <param name="slot">Lugar fuera del rango de cartas.</param>
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

        /// <summary>Verifica ChoosePunishmentCard_WithoutAPendingPunishment_IsRejected.</summary>
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

        /// <summary>Verifica PlaceCardAndPlaceBet_DuringAPunishment_AreRejectedWithPunishmentPending.</summary>
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

        /// <summary>Verifica RevealCard_FailingWithASingleLife_EliminatesWithoutAskingForAChoice.</summary>
        [Fact]
        public void RevealCard_FailingWithASingleLife_EliminatesWithoutAskingForAChoice()
        {
            // Arrange: tras tres castigos a ciegas el primer jugador solo conserva su Gato.
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

        /// <summary>Verifica LeaveRoom_WhenAnEliminatedPlayerLeaves_ChangesNothing.</summary>
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

        /// <summary>Verifica LeaveRoom_AfterAnEliminationWhenAnotherLeaves_TheLastActivePlayerWins.</summary>
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

        /// <summary>Verifica LeaveRoom_WhenThePunishedPlayerLeaves_CancelsThePunishmentAndRestartsTheRound.</summary>
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

        /// <summary>Verifica LeaveRoom_WhenTheChooserLeaves_ResolvesThePunishmentAtRandom.</summary>
        [Fact]
        public void LeaveRoom_WhenTheChooserLeaves_ResolvesThePunishmentAtRandom()
        {
            // Arrange: el azar elige el índice 0 de la mano del castigado, que es su Gato.
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

        /// <summary>Verifica LeaveRoom_WhenAnUninvolvedPlayerLeaves_KeepsThePunishmentPending.</summary>
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

        /// <summary>Tres rondas en que el primer jugador pierde una carta segura (lugar 0 de la permutación).</summary>
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
            var delivery = scenario.Registry.SubscribeToMatch(new LobbyCaller(replacement, "spectator"),
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
