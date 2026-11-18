using Xunit;

using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Services.Helpers;
using TLMG.Services.Match;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas del mapa de los estados de reto y de castigo del motor a ErrorKey.</summary>
    public class MatchChallengeErrorKeyTests
    {
        private const int RevealStatusCount = 9;
        private const int PunishmentStatusCount = 12;

        /// <summary>Verifica ConvertToErrorKey_WithAcceptedRevealStatus_ReturnsNull.</summary>
        /// <param name="revealStatus">Resultado del motor que acepta el volteo.</param>
        [Theory]
        [InlineData(ChallengeRevealStatus.Revealed)]
        [InlineData(ChallengeRevealStatus.ChallengeSucceeded)]
        [InlineData(ChallengeRevealStatus.ChallengeFailed)]
        public void ConvertToErrorKey_WithAcceptedRevealStatus_ReturnsNull(ChallengeRevealStatus revealStatus)
        {
            // Arrange: el estado viene del motor.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(revealStatus);

            // Assert
            Assert.Null(errorKey);
        }

        /// <summary>Verifica ConvertToErrorKey_WithRejectedRevealStatus_ReturnsItsOwnKey.</summary>
        /// <param name="revealStatus">Rechazo del motor.</param>
        /// <param name="expectedErrorKey">Clave que el cliente traduce.</param>
        [Theory]
        [InlineData(ChallengeRevealStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch)]
        [InlineData(ChallengeRevealStatus.NoChallengeInProgress, MatchErrorKeys.NoChallenge)]
        [InlineData(ChallengeRevealStatus.NotTheBettor, MatchErrorKeys.NotTheBettor)]
        [InlineData(ChallengeRevealStatus.PileOwnerNotInMatch, MatchErrorKeys.PileOwnerNotInMatch)]
        [InlineData(ChallengeRevealStatus.OwnPileFirst, MatchErrorKeys.OwnPileFirst)]
        [InlineData(ChallengeRevealStatus.PileFullyRevealed, MatchErrorKeys.PileEmpty)]
        public void ConvertToErrorKey_WithRejectedRevealStatus_ReturnsItsOwnKey(
            ChallengeRevealStatus revealStatus,
            string expectedErrorKey)
        {
            // Arrange: el estado viene del motor.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(revealStatus);

            // Assert
            Assert.Equal(expectedErrorKey, errorKey);
        }

        /// <summary>Verifica ConvertToErrorKey_WithCardRemovedStatus_ReturnsNull.</summary>
        [Fact]
        public void ConvertToErrorKey_WithCardRemovedStatus_ReturnsNull()
        {
            // Arrange & Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(PunishmentStatus.CardRemoved);

            // Assert
            Assert.Null(errorKey);
        }

        /// <summary>Verifica ConvertToErrorKey_WithRejectedPunishmentStatus_ReturnsItsOwnKey.</summary>
        /// <param name="punishmentStatus">Rechazo del motor.</param>
        /// <param name="expectedErrorKey">Clave que el cliente traduce.</param>
        [Theory]
        [InlineData(PunishmentStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch)]
        [InlineData(PunishmentStatus.NoPunishmentPending, MatchErrorKeys.NoPunishmentPending)]
        [InlineData(PunishmentStatus.NotTheChooser, MatchErrorKeys.NotTheChooser)]
        [InlineData(PunishmentStatus.CardIndexNotValid, MatchErrorKeys.CardIndexNotValid)]
        public void ConvertToErrorKey_WithRejectedPunishmentStatus_ReturnsItsOwnKey(
            PunishmentStatus punishmentStatus,
            string expectedErrorKey)
        {
            // Arrange: el estado viene del motor.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(punishmentStatus);

            // Assert
            Assert.Equal(expectedErrorKey, errorKey);
        }

        /// <summary>Verifica ConvertToErrorKey_WithStatusesOfTheReconnectionWindow_ReturnsUnexpectedError.</summary>
        /// <param name="punishmentStatus">Estado que solo existe con la ventana de reconexión (C2).</param>
        [Theory]
        [InlineData(PunishmentStatus.Suspended)]
        [InlineData(PunishmentStatus.Resumed)]
        [InlineData(PunishmentStatus.DeadlineExpired)]
        [InlineData(PunishmentStatus.DeadlineNotReached)]
        [InlineData(PunishmentStatus.PunishmentSuspended)]
        [InlineData(PunishmentStatus.PunishmentNotSuspended)]
        [InlineData(PunishmentStatus.NotThePunished)]
        public void ConvertToErrorKey_WithStatusesOfTheReconnectionWindow_ReturnsUnexpectedError(
            PunishmentStatus punishmentStatus)
        {
            // Arrange: ningún cliente puede provocarlos hoy, pero el mapa no debe dejar pasar una clave vacía.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(punishmentStatus);

            // Assert
            Assert.Equal(ErrorKeys.Common.Unexpected, errorKey);
        }

        /// <summary>Verifica EngineStatuses_Members_KeepTheMapsInSync.</summary>
        [Fact]
        public void EngineStatuses_Members_KeepTheMapsInSync()
        {
            // Arrange & Act
            int revealCount = System.Enum.GetNames(typeof(ChallengeRevealStatus)).Length;
            int punishmentCount = System.Enum.GetNames(typeof(PunishmentStatus)).Length;

            // Assert: si el motor agrega un estado, esta prueba avisa antes de que llegue un rechazo sin clave.
            Assert.Equal(RevealStatusCount, revealCount);
            Assert.Equal(PunishmentStatusCount, punishmentCount);
        }
    }
}
