using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Engine;
using TLMG.Domain.Constants;
using TLMG.Services.Helpers;
using TLMG.Services.Match;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests engine-to-contract conversions. Every engine value needs a matching contract value.
    /// </summary>
    public class MatchViewHelperTests
    {
        private const int UndefinedCardValue = 99;

        /// <summary>Checks ConvertToErrorKey_WithPlacedStatus_ReturnsNull.</summary>
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

        /// <summary>Checks ConvertToErrorKey_WithRejectedStatus_ReturnsItsOwnKey.</summary>
        /// <param name="placementStatus">Engine rejection.</param>
        /// <param name="expectedErrorKey">Error key translated by the client.</param>
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
            // Arrange: The status comes from the engine.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(placementStatus);

            // Assert
            Assert.Equal(expectedErrorKey, errorKey);
        }

        /// <summary>Checks that an unknown status returns the generic error.</summary>
        [Fact]
        public void ConvertToErrorKey_WithUnknownStatus_ReturnsUnexpectedError()
        {
            // Arrange
            CardPlacementStatus unknownStatus = (CardPlacementStatus)UndefinedCardValue;

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(unknownStatus);

            // Assert
            Assert.Equal(ErrorKeys.Common.Unexpected, errorKey);
        }

        /// <summary>Checks ConvertToContractPhase_WithEnginePhase_ReturnsSameNamedPhase.</summary>
        /// <param name="enginePhase">Engine phase.</param>
        /// <param name="expectedPhase">Equivalent contract phase.</param>
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
            // Arrange: The phase comes from the engine.

            // Act
            MatchRoundPhase contractPhase = MatchViewHelper.ConvertToContractPhase(enginePhase);

            // Assert
            Assert.Equal(expectedPhase, contractPhase);
        }

        /// <summary>Checks ConvertToContractCard_WithEngineCard_ReturnsSameNamedCard.</summary>
        /// <param name="engineCard">Engine card.</param>
        /// <param name="expectedCard">Equivalent contract card.</param>
        [Theory]
        [InlineData(CardKind.Safe, MatchCardKind.Safe)]
        [InlineData(CardKind.FatalCat, MatchCardKind.FatalCat)]
        public void ConvertToContractCard_WithEngineCard_ReturnsSameNamedCard(
            CardKind engineCard,
            MatchCardKind expectedCard)
        {
            // Arrange: The card comes from the engine.

            // Act
            MatchCardKind contractCard = MatchViewHelper.ConvertToContractCard(engineCard);

            // Assert
            Assert.Equal(expectedCard, contractCard);
        }

        /// <summary>Checks TryConvertToEngineCard_WithDefinedCard_ReturnsEngineCard.</summary>
        /// <param name="contractCard">Card received from the client.</param>
        /// <param name="expectedCard">Equivalent engine card.</param>
        [Theory]
        [InlineData(MatchCardKind.Safe, CardKind.Safe)]
        [InlineData(MatchCardKind.FatalCat, CardKind.FatalCat)]
        public void TryConvertToEngineCard_WithDefinedCard_ReturnsEngineCard(
            MatchCardKind contractCard,
            CardKind expectedCard)
        {
            // Arrange: The card comes from the client.
            CardKind engineCard;

            // Act
            bool isValid = MatchViewHelper.TryConvertToEngineCard(contractCard, out engineCard);

            // Assert
            Assert.True(isValid);
            Assert.Equal(expectedCard, engineCard);
        }

        /// <summary>Checks TryConvertToEngineCard_WithUndefinedCard_ReturnsFalse.</summary>
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

        /// <summary>Checks ContractEnums_AgainstEngineEnums_HaveSameMemberCount.</summary>
        /// <param name="engineEnum">Engine enum.</param>
        /// <param name="contractEnum">Equivalent contract enum.</param>
        [Theory]
        [InlineData(typeof(RoundPhase), typeof(MatchRoundPhase))]
        [InlineData(typeof(CardKind), typeof(MatchCardKind))]
        public void ContractEnums_AgainstEngineEnums_HaveSameMemberCount(Type engineEnum, Type contractEnum)
        {
            // Arrange: A new engine value should fail this test before it can break a match.

            // Act
            int engineCount = Enum.GetNames(engineEnum).Length;
            int contractCount = Enum.GetNames(contractEnum).Length;

            // Assert
            Assert.Equal(engineCount, contractCount);
        }

        /// <summary>Checks CardPlacementStatus_Members_AreEightSoMappingStaysInSync.</summary>
        [Fact]
        public void CardPlacementStatus_Members_AreEightSoMappingStaysInSync()
        {
            // Arrange: Eight engine statuses, including one accepted status.
            const int EngineStatusCount = 8;

            // Act
            int statusCount = Enum.GetNames(typeof(CardPlacementStatus)).Length;

            // Assert
            Assert.Equal(EngineStatusCount, statusCount);
        }
    }
}
