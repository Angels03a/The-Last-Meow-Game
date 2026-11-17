using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Engine;
using TLMG.Services.Helpers;
using TLMG.Services.Match;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Pruebas de las conversiones entre el motor y el contrato; cada valor del motor debe tener su equivalente.
    /// </summary>
    public class MatchViewHelperTests
    {
        private const int UndefinedCardValue = 99;

        /// <summary>Verifica ConvertToErrorKey_WithPlacedStatus_ReturnsNull.</summary>
        [Fact]
        public void ConvertToErrorKey_WithPlacedStatus_ReturnsNull()
        {
            // Arrange
            CardPlacementStatus placementStatus = CardPlacementStatus.Placed;

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(placementStatus);

            // Assert
            Assert.Null(errorKey);
        }

        /// <summary>Verifica ConvertToErrorKey_WithRejectedStatus_ReturnsItsOwnKey.</summary>
        /// <param name="placementStatus">Rechazo del motor.</param>
        /// <param name="expectedErrorKey">Clave que el cliente traduce.</param>
        [Theory]
        [InlineData(CardPlacementStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch)]
        [InlineData(CardPlacementStatus.BetInProgress, MatchErrorKeys.BetInProgress)]
        [InlineData(CardPlacementStatus.NotPlayersTurn, MatchErrorKeys.NotYourTurn)]
        [InlineData(CardPlacementStatus.HandEmpty, MatchErrorKeys.HandEmpty)]
        [InlineData(CardPlacementStatus.CardNotInHand, MatchErrorKeys.CardNotInHand)]
        [InlineData(CardPlacementStatus.PunishmentPending, MatchErrorKeys.PunishmentPending)]
        [InlineData(CardPlacementStatus.MatchFinished, MatchErrorKeys.MatchFinished)]
        public void ConvertToErrorKey_WithRejectedStatus_ReturnsItsOwnKey(
            CardPlacementStatus placementStatus,
            string expectedErrorKey)
        {
            // Arrange: el estado viene del motor.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(placementStatus);

            // Assert
            Assert.Equal(expectedErrorKey, errorKey);
        }

        /// <summary>Verifica ConvertToErrorKey_WithUnknownStatus_ReturnsNotInMatchWithoutThrowing.</summary>
        [Fact]
        public void ConvertToErrorKey_WithUnknownStatus_ReturnsNotInMatchWithoutThrowing()
        {
            // Arrange
            CardPlacementStatus unknownStatus = (CardPlacementStatus)UndefinedCardValue;

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(unknownStatus);

            // Assert
            Assert.Equal(MatchErrorKeys.NotInMatch, errorKey);
        }

        /// <summary>Verifica ConvertToContractPhase_WithEnginePhase_ReturnsSameNamedPhase.</summary>
        /// <param name="enginePhase">Fase del motor.</param>
        /// <param name="expectedPhase">Fase equivalente del contrato.</param>
        [Theory]
        [InlineData(RoundPhase.PlacingCards, MatchRoundPhase.PlacingCards)]
        [InlineData(RoundPhase.Betting, MatchRoundPhase.Betting)]
        [InlineData(RoundPhase.ChallengeReady, MatchRoundPhase.ChallengeReady)]
        [InlineData(RoundPhase.PunishmentPending, MatchRoundPhase.PunishmentPending)]
        [InlineData(RoundPhase.MatchFinished, MatchRoundPhase.MatchFinished)]
        public void ConvertToContractPhase_WithEnginePhase_ReturnsSameNamedPhase(
            RoundPhase enginePhase,
            MatchRoundPhase expectedPhase)
        {
            // Arrange: la fase viene del motor.

            // Act
            MatchRoundPhase contractPhase = MatchViewHelper.ConvertToContractPhase(enginePhase);

            // Assert
            Assert.Equal(expectedPhase, contractPhase);
        }

        /// <summary>Verifica ConvertToContractCard_WithEngineCard_ReturnsSameNamedCard.</summary>
        /// <param name="engineCard">Carta del motor.</param>
        /// <param name="expectedCard">Carta equivalente del contrato.</param>
        [Theory]
        [InlineData(CardKind.Safe, MatchCardKind.Safe)]
        [InlineData(CardKind.FatalCat, MatchCardKind.FatalCat)]
        public void ConvertToContractCard_WithEngineCard_ReturnsSameNamedCard(
            CardKind engineCard,
            MatchCardKind expectedCard)
        {
            // Arrange: la carta viene del motor.

            // Act
            MatchCardKind contractCard = MatchViewHelper.ConvertToContractCard(engineCard);

            // Assert
            Assert.Equal(expectedCard, contractCard);
        }

        /// <summary>Verifica TryConvertToEngineCard_WithDefinedCard_ReturnsEngineCard.</summary>
        /// <param name="contractCard">Carta recibida del cliente.</param>
        /// <param name="expectedCard">Carta equivalente del motor.</param>
        [Theory]
        [InlineData(MatchCardKind.Safe, CardKind.Safe)]
        [InlineData(MatchCardKind.FatalCat, CardKind.FatalCat)]
        public void TryConvertToEngineCard_WithDefinedCard_ReturnsEngineCard(
            MatchCardKind contractCard,
            CardKind expectedCard)
        {
            // Arrange: la carta viene del cliente.
            CardKind engineCard;

            // Act
            bool isValid = MatchViewHelper.TryConvertToEngineCard(contractCard, out engineCard);

            // Assert
            Assert.True(isValid);
            Assert.Equal(expectedCard, engineCard);
        }

        /// <summary>Verifica TryConvertToEngineCard_WithUndefinedCard_ReturnsFalse.</summary>
        [Fact]
        public void TryConvertToEngineCard_WithUndefinedCard_ReturnsFalse()
        {
            // Arrange
            CardKind engineCard;

            // Act
            bool isValid = MatchViewHelper.TryConvertToEngineCard((MatchCardKind)UndefinedCardValue, out engineCard);

            // Assert
            Assert.False(isValid);
        }

        /// <summary>Verifica ContractEnums_AgainstEngineEnums_HaveSameMemberCount.</summary>
        /// <param name="engineEnum">Enum del motor.</param>
        /// <param name="contractEnum">Enum equivalente del contrato.</param>
        [Theory]
        [InlineData(typeof(RoundPhase), typeof(MatchRoundPhase))]
        [InlineData(typeof(CardKind), typeof(MatchCardKind))]
        public void ContractEnums_AgainstEngineEnums_HaveSameMemberCount(Type engineEnum, Type contractEnum)
        {
            // Arrange: si el motor agrega un valor, esta prueba avisa antes de que falle una partida.

            // Act
            int engineCount = Enum.GetNames(engineEnum).Length;
            int contractCount = Enum.GetNames(contractEnum).Length;

            // Assert
            Assert.Equal(engineCount, contractCount);
        }

        /// <summary>Verifica CardPlacementStatus_Members_AreEightSoMappingStaysInSync.</summary>
        [Fact]
        public void CardPlacementStatus_Members_AreEightSoMappingStaysInSync()
        {
            // Arrange: ocho estados del motor, uno de ellos la aceptación.
            const int EngineStatusCount = 8;

            // Act
            int statusCount = Enum.GetNames(typeof(CardPlacementStatus)).Length;

            // Assert
            Assert.Equal(EngineStatusCount, statusCount);
        }
    }
}
