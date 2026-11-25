using Xunit;

using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Services.Helpers;
using TLMG.Services.Match;

namespace TLMG.Services.Tests
{
    /// <summary>Tests how engine challenge and punishment results map to ErrorKey.</summary>
    public class MatchChallengeErrorKeyTests
    {
        private const int RevealStatusCount = 9;
        private const int PunishmentStatusCount = 12;

        /// <summary>Checks ConvertToErrorKey_WithAcceptedRevealStatus_ReturnsNull.</summary>
        /// <param name="revealStatus">Engine result for an accepted reveal.</param>
        [Theory]
        [InlineData(ChallengeRevealStatus.Revealed)]
        [InlineData(ChallengeRevealStatus.ChallengeSucceeded)]
        [InlineData(ChallengeRevealStatus.ChallengeFailed)]
        public void ConvertToErrorKey_WithAcceptedRevealStatus_ReturnsNull(ChallengeRevealStatus revealStatus)
        {
            // Arrange: The status comes from the engine.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(revealStatus);

            // Assert
            Assert.Null(errorKey);
        }

        /// <summary>Checks ConvertToErrorKey_WithRejectedRevealStatus_ReturnsItsOwnKey.</summary>
        /// <param name="revealStatus">Engine rejection.</param>
        /// <param name="expectedErrorKey">Error key translated by the client.</param>
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
            // Arrange: The status comes from the engine.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(revealStatus);

            // Assert
            Assert.Equal(expectedErrorKey, errorKey);
        }

        /// <summary>Checks ConvertToErrorKey_WithCardRemovedStatus_ReturnsNull.</summary>
        [Fact]
        public void ConvertToErrorKey_WithCardRemovedStatus_ReturnsNull()
        {
            // Arrange & Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(PunishmentStatus.CardRemoved);

            // Assert
            Assert.Null(errorKey);
        }

        /// <summary>Checks ConvertToErrorKey_WithRejectedPunishmentStatus_ReturnsItsOwnKey.</summary>
        /// <param name="punishmentStatus">Engine rejection.</param>
        /// <param name="expectedErrorKey">Error key translated by the client.</param>
        [Theory]
        [InlineData(PunishmentStatus.PlayerNotInMatch, MatchErrorKeys.NotInMatch)]
        [InlineData(PunishmentStatus.NoPunishmentPending, MatchErrorKeys.NoPunishmentPending)]
        [InlineData(PunishmentStatus.NotTheChooser, MatchErrorKeys.NotTheChooser)]
        [InlineData(PunishmentStatus.CardIndexNotValid, MatchErrorKeys.CardIndexNotValid)]
        public void ConvertToErrorKey_WithRejectedPunishmentStatus_ReturnsItsOwnKey(
            PunishmentStatus punishmentStatus,
            string expectedErrorKey)
        {
            // Arrange: The status comes from the engine.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(punishmentStatus);

            // Assert
            Assert.Equal(expectedErrorKey, errorKey);
        }

        /// <summary>Checks ConvertToErrorKey_WithStatusesOfTheReconnectionWindow_ReturnsUnexpectedError.</summary>
        /// <param name="punishmentStatus">Status specific to the reconnection window (C2).</param>
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
            // Arrange: These statuses still need an error key even when clients cannot trigger them directly.

            // Act
            string errorKey = MatchViewHelper.ConvertToErrorKey(punishmentStatus);

            // Assert
            Assert.Equal(ErrorKeys.Common.Unexpected, errorKey);
        }

        /// <summary>Checks EngineStatuses_Members_KeepTheMapsInSync.</summary>
        [Fact]
        public void EngineStatuses_Members_KeepTheMapsInSync()
        {
            // Arrange & Act
            int revealCount = System.Enum.GetNames(typeof(ChallengeRevealStatus)).Length;
            int punishmentCount = System.Enum.GetNames(typeof(PunishmentStatus)).Length;

            // Assert: A new engine status should fail this test before a rejection can reach the client without a key.
            Assert.Equal(RevealStatusCount, revealCount);
            Assert.Equal(PunishmentStatusCount, punishmentCount);
        }
    }
}
