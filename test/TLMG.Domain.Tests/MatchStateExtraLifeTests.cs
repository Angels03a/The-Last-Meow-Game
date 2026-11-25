using System;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>Tests who can receive an extra life from MatchState.GrantExtraLife and how often.</summary>
    public class MatchStateExtraLifeTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int FirstPlayerId = 1;
        private const int SecondPlayerId = 2;
        private const int ThirdPlayerId = 3;
        private const int UnknownPlayerId = 99;
        private const int OneCardBet = 1;

        private static readonly DateTime _nowUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>Checks GrantExtraLife_WithEightCardMode_GrantsItToThePlayer.</summary>
        [Fact]
        public void GrantExtraLife_WithEightCardMode_GrantsItToThePlayer()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(FirstPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.Granted, status);
            Assert.True(match.Players[0].HasExtraLife);
            Assert.Equal(EightCardsPerPlayer, match.Players[0].Lives);
        }

        /// <summary>Checks GrantExtraLife_WithModeWithoutExtraLife_ReturnsModeDoesNotAllowExtraLife.</summary>
        [Theory]
        [InlineData(FourCardsPerPlayer)]
        [InlineData(SixCardsPerPlayer)]
        public void GrantExtraLife_WithModeWithoutExtraLife_ReturnsModeDoesNotAllowExtraLife(int cardsPerPlayerCount)
        {
            // Arrange
            MatchState match = CreateMatch(cardsPerPlayerCount);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(FirstPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.ModeDoesNotAllowExtraLife, status);
            Assert.False(match.Players[0].HasExtraLife);
        }

        /// <summary>Checks GrantExtraLife_WithAnotherPlayerAfterTheFirst_ReturnsExtraLifeAlreadyGranted.</summary>
        [Fact]
        public void GrantExtraLife_WithAnotherPlayerAfterTheFirst_ReturnsExtraLifeAlreadyGranted()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer);
            match.GrantExtraLife(FirstPlayerId);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(SecondPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.ExtraLifeAlreadyGranted, status);
            Assert.False(match.Players[1].HasExtraLife);
        }

        /// <summary>Checks GrantExtraLife_WithTheSamePlayerTwice_DoesNotAccumulateIt.</summary>
        [Fact]
        public void GrantExtraLife_WithTheSamePlayerTwice_DoesNotAccumulateIt()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer);
            match.GrantExtraLife(FirstPlayerId);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(FirstPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.ExtraLifeAlreadyGranted, status);
            Assert.True(match.Players[0].HasExtraLife);
        }

        /// <summary>Checks GrantExtraLife_AfterTheExtraLifeWasConsumed_ReturnsExtraLifeAlreadyGranted.</summary>
        [Fact]
        public void GrantExtraLife_AfterTheExtraLifeWasConsumed_ReturnsExtraLifeAlreadyGranted()
        {
            // Arrange: A[G] bids 1 with an extra life. The Fatal Cat uses it.
            MatchState match = CreateMatch(EightCardsPerPlayer);
            match.GrantExtraLife(FirstPlayerId);
            match.PlaceCard(FirstPlayerId, CardKind.FatalCat);
            match.PlaceCard(SecondPlayerId, CardKind.Safe);
            match.PlaceCard(ThirdPlayerId, CardKind.Safe);
            match.OpenBet(FirstPlayerId, OneCardBet);
            match.WithdrawFromBet(SecondPlayerId);
            match.WithdrawFromBet(ThirdPlayerId);
            match.RevealCard(FirstPlayerId, FirstPlayerId, _nowUtc);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(SecondPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.ExtraLifeAlreadyGranted, status);
            Assert.False(match.Players[0].HasExtraLife);
            Assert.False(match.Players[1].HasExtraLife);
        }

        /// <summary>Checks GrantExtraLife_WithUnknownPlayer_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void GrantExtraLife_WithUnknownPlayer_ReturnsPlayerNotInMatch()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(UnknownPlayerId);

            // Assert
            Assert.Equal(ExtraLifeStatus.PlayerNotInMatch, status);
        }

        private static MatchState CreateMatch(int cardsPerPlayerCount)
        {
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            MatchState match = new MatchState(modeRules, new[] { FirstPlayerId, SecondPlayerId, ThirdPlayerId });

            return match;
        }
    }
}
