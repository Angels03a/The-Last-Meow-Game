using Xunit;

using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Services.Helpers;
using TLMG.Services.Match;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas del mapa de los estados de apuesta del motor a ErrorKey.</summary>
    public class MatchBetErrorKeyTests
    {
        private const int UndefinedStatusValue = 99;

        /// <summary>Verifica ConvertToErrorKey_WithAcceptedBetStatus_ReturnsNull.</summary>
        [Fact]
        public void ConvertToErrorKey_WithAcceptedBetStatus_ReturnsNull()
        {
            // Arrange & Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(BetActionStatus.Accepted);

            // Assert
            Assert.Null(errorKey);
        }

        /// <summary>Verifica ConvertToErrorKey_WithRejectedBetStatus_ReturnsItsOwnKey.</summary>
        /// <param name="betStatus">Rechazo del motor.</param>
        /// <param name="expectedErrorKey">Clave que el cliente traduce.</param>
        [Theory]
        [InlineData(BetActionStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch)]
        [InlineData(BetActionStatus.NotPlayersTurn, MatchErrorKeys.NotYourTurn)]
        [InlineData(BetActionStatus.BetInProgress, MatchErrorKeys.BetInProgress)]
        [InlineData(BetActionStatus.NoBetInProgress, MatchErrorKeys.NoBetInProgress)]
        [InlineData(BetActionStatus.TableEmpty, MatchErrorKeys.TableEmpty)]
        [InlineData(BetActionStatus.EmptyHandRequiresBetOfOne, MatchErrorKeys.EmptyHandBet)]
        [InlineData(BetActionStatus.BetValueNotValid, MatchErrorKeys.BetValueNotValid)]
        [InlineData(BetActionStatus.NoCardPlacedInRound, MatchErrorKeys.NoCardPlaced)]
        [InlineData(BetActionStatus.PunishmentPending, MatchErrorKeys.PunishmentPending)]
        [InlineData(BetActionStatus.MatchFinished, MatchErrorKeys.MatchFinished)]
        public void ConvertToErrorKey_WithRejectedBetStatus_ReturnsItsOwnKey(
            BetActionStatus betStatus,
            string expectedErrorKey)
        {
            // Arrange: el estado viene del motor.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(betStatus);

            // Assert
            Assert.Equal(expectedErrorKey, errorKey);
        }

        /// <summary>Verifica ConvertToErrorKey_WithUnknownBetStatus_ReturnsUnexpectedError.</summary>
        [Fact]
        public void ConvertToErrorKey_WithUnknownBetStatus_ReturnsUnexpectedError()
        {
            // Arrange
            BetActionStatus unknownStatus = (BetActionStatus)UndefinedStatusValue;

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(unknownStatus);

            // Assert
            Assert.Equal(ErrorKeys.Common.Unexpected, errorKey);
        }

        /// <summary>Verifica BetActionStatus_Members_AreElevenSoTheMapStaysInSync.</summary>
        [Fact]
        public void BetActionStatus_Members_AreElevenSoTheMapStaysInSync()
        {
            // Arrange: diez rechazos con clave más la aceptación; si el motor agrega uno, esta prueba avisa.
            const int EngineStatusCount = 11;

            // Act
            int statusCount = System.Enum.GetNames(typeof(BetActionStatus)).Length;

            // Assert
            Assert.Equal(EngineStatusCount, statusCount);
        }
    }
}
