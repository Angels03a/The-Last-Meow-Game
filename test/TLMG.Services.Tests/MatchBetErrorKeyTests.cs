using Xunit;

using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Services.Match;

namespace TLMG.Services.Tests
{
    /// <summary>Tests how engine bidding results map to ErrorKey.</summary>
    public class MatchBetErrorKeyTests
    {
        private const int UndefinedStatusValue = 99;

        /// <summary>Checks ConvertToErrorKey_WithAcceptedBetStatus_ReturnsNull.</summary>
        [Fact]
        public void ConvertToErrorKey_WithAcceptedBetStatus_ReturnsNull()
        {
            // Arrange & Act
            string errorKey = MatchErrorKeys.FindErrorKey(BetActionStatus.Accepted);

            // Assert
            Assert.Null(errorKey);
        }

        /// <summary>Checks ConvertToErrorKey_WithRejectedBetStatus_ReturnsItsOwnKey.</summary>
        /// <param name="betStatus">Engine rejection.</param>
        /// <param name="expectedErrorKey">Error key translated by the client.</param>
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
            // Arrange: The status comes from the engine.

            // Act
            string errorKey = MatchErrorKeys.FindErrorKey(betStatus);

            // Assert
            Assert.Equal(expectedErrorKey, errorKey);
        }

        /// <summary>Checks ConvertToErrorKey_WithUnknownBetStatus_ReturnsUnexpectedError.</summary>
        [Fact]
        public void ConvertToErrorKey_WithUnknownBetStatus_ReturnsUnexpectedError()
        {
            // Arrange
            BetActionStatus unknownStatus = (BetActionStatus)UndefinedStatusValue;

            // Act
            string errorKey = MatchErrorKeys.FindErrorKey(unknownStatus);

            // Assert
            Assert.Equal(ErrorKeys.Common.Unexpected, errorKey);
        }

        /// <summary>Checks BetActionStatus_Members_AreElevenSoTheMapStaysInSync.</summary>
        [Fact]
        public void BetActionStatus_Members_AreElevenSoTheMapStaysInSync()
        {
            // Arrange: Ten rejections have error keys, plus one accepted result. A new engine status should fail this
            // test.
            const int EngineStatusCount = 11;

            // Act
            int statusCount = System.Enum.GetNames(typeof(BetActionStatus)).Length;

            // Assert
            Assert.Equal(EngineStatusCount, statusCount);
        }
    }
}
