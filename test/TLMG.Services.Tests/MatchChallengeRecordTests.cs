using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Pruebas del registro del desenlace del último reto, que el motor no conserva.</summary>
    public class MatchChallengeRecordTests
    {
        private const int BettorUserId = 1;
        private const int OtherUserId = 2;
        private const int PromisedCount = 2;
        private const int SingleCard = 1;

        /// <summary>Verifica Begin_WithBettorAndPromise_StartsInProgressWithoutCards.</summary>
        [Fact]
        public void Begin_WithBettorAndPromise_StartsInProgressWithoutCards()
        {
            // Arrange & Act
            MatchChallengeRecord record = MatchChallengeRecord.Begin(BettorUserId, PromisedCount);

            // Assert
            Assert.Equal(MatchChallengeOutcome.InProgress, record.Outcome);
            Assert.Equal(BettorUserId, record.BettorUserId);
            Assert.Equal(PromisedCount, record.PromisedCount);
            Assert.Empty(record.RevealedCards);
            Assert.Equal(MatchPunishmentResolution.None, record.PunishmentResolution);
        }

        /// <summary>Verifica RecordReveal_WithSeveralCards_KeepsOrderAndOwners.</summary>
        [Fact]
        public void RecordReveal_WithSeveralCards_KeepsOrderAndOwners()
        {
            // Arrange
            MatchChallengeRecord record = MatchChallengeRecord.Begin(BettorUserId, PromisedCount);

            // Act
            record.RecordReveal(BettorUserId, MatchCardKind.Safe);
            record.RecordReveal(OtherUserId, MatchCardKind.FatalCat);

            // Assert
            int[] owners = record.RevealedCards.Select(card => card.PileOwnerUserId).ToArray();
            Assert.Equal(new[] { BettorUserId, OtherUserId }, owners);
            Assert.Equal(MatchCardKind.FatalCat, record.RevealedCards.Last().Card);
        }

        /// <summary>Verifica MarkSucceeded_AfterReveals_LeavesNoPunishment.</summary>
        [Fact]
        public void MarkSucceeded_AfterReveals_LeavesNoPunishment()
        {
            // Arrange
            MatchChallengeRecord record = MatchChallengeRecord.Begin(BettorUserId, PromisedCount);

            // Act
            record.MarkSucceeded();

            // Assert
            Assert.Equal(MatchChallengeOutcome.Succeeded, record.Outcome);
            Assert.Equal(MatchPunishmentResolution.None, record.PunishmentResolution);
        }

        /// <summary>Verifica MarkFailed_ThenResolvePunishment_KeepsWhoChoseAndHowItEnded.</summary>
        [Fact]
        public void MarkFailed_ThenResolvePunishment_KeepsWhoChoseAndHowItEnded()
        {
            // Arrange
            MatchChallengeRecord record = MatchChallengeRecord.Begin(BettorUserId, PromisedCount);
            record.MarkFailed(OtherUserId, BettorUserId, MatchPunishmentResolution.Pending);

            // Act
            record.ResolvePunishment(MatchPunishmentResolution.ChosenByChooser);

            // Assert
            Assert.Equal(MatchChallengeOutcome.Failed, record.Outcome);
            Assert.Equal(OtherUserId, record.PunishmentChooserUserId);
            Assert.Equal(BettorUserId, record.PunishedUserId);
            Assert.Equal(MatchPunishmentResolution.ChosenByChooser, record.PunishmentResolution);
        }

        /// <summary>Verifica MarkFailed_WithSingleLife_RecordsNoChoice.</summary>
        [Fact]
        public void MarkFailed_WithSingleLife_RecordsNoChoice()
        {
            // Arrange
            MatchChallengeRecord record = MatchChallengeRecord.Begin(BettorUserId, SingleCard);

            // Act
            record.MarkFailed(BettorUserId, BettorUserId, MatchPunishmentResolution.SingleLifeNoChoice);

            // Assert
            Assert.Equal(MatchPunishmentResolution.SingleLifeNoChoice, record.PunishmentResolution);
        }
    }
}
