using System;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests match authorization. Rejections leave the match, subscription and version unchanged.
    /// </summary>
    public class SessionRegistryMatchPlayAuthorizationTests
    {
        private const int MinimumPlayerCount = 3;
        private const int OutsiderUserId = 99;
        private const int OtherMatchFirstUserId = 11;
        private const string OutsiderChannelId = "outsider";
        private const string OtherChannelId = "other";
        private const string UnknownToken = "unknown-token";
        private const int UndefinedCardValue = 99;

        /// <summary>Checks SubscribeToMatch_WithInvalidToken_RejectsWithInvalidSession.</summary>
        /// <param name="sessionToken">Token received from the client.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(UnknownToken)]
        public void SubscribeToMatch_WithInvalidToken_RejectsWithInvalidSession(string sessionToken)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            SessionChannelCaller caller = MatchPlayScenario.CallerWithToken(sessionToken);

            // Act
            MatchDelivery delivery = scenario.Registry.SubscribeToMatch(
                caller, scenario.MatchId, scenario.CallbackOf(scenario.CreatorUserId));

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
        }

        /// <summary>Checks SubscribeToMatch_WithoutChannelId_RejectsWithInvalidSession.</summary>
        [Fact]
        public void SubscribeToMatch_WithoutChannelId_RejectsWithInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            SessionChannelCaller caller = new SessionChannelCaller(scenario.TokenOf(scenario.CreatorUserId), string.Empty);

            // Act
            MatchDelivery delivery = scenario.Registry.SubscribeToMatch(
                caller, scenario.MatchId, scenario.CallbackOf(scenario.CreatorUserId));

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, delivery.ErrorKey);
        }

        /// <summary>Checks SubscribeToMatch_WithNullCallback_RejectsWithInvalidSession.</summary>
        [Fact]
        public void SubscribeToMatch_WithNullCallback_RejectsWithInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);

            // Act
            MatchDelivery delivery = scenario.Registry.SubscribeToMatch(
                scenario.MatchCallerOf(scenario.CreatorUserId), scenario.MatchId, null);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, delivery.ErrorKey);
        }

        /// <summary>Checks SubscribeToMatch_WithChannelOfAnotherPlayer_RejectsWithInvalidSession.</summary>
        [Fact]
        public void SubscribeToMatch_WithChannelOfAnotherPlayer_RejectsWithInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            int guestUserId = scenario.UserIds[1];
            scenario.Subscribe(scenario.CreatorUserId);
            SessionChannelCaller stolenChannel = new SessionChannelCaller(
                scenario.TokenOf(guestUserId), MatchPlayScenario.MatchChannelPrefix + scenario.CreatorUserId);

            // Act
            MatchDelivery delivery = scenario.Registry.SubscribeToMatch(
                stolenChannel, scenario.MatchId, scenario.CallbackOf(guestUserId));

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, delivery.ErrorKey);
        }

        /// <summary>Checks SubscribeToMatch_WithUserOutsideMatch_RejectsWithNotInMatch.</summary>
        [Fact]
        public void SubscribeToMatch_WithUserOutsideMatch_RejectsWithNotInMatch()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            SessionEnd replacedSession;
            string outsiderToken = scenario.Registry.StartRegisteredSession(OutsiderUserId, "Outsider", out replacedSession);
            SessionChannelCaller caller = new SessionChannelCaller(outsiderToken, OutsiderChannelId);

            // Act
            MatchDelivery delivery = scenario.Registry.SubscribeToMatch(
                caller, scenario.MatchId, new MatchCallbackStub());

            // Assert
            Assert.Equal(MatchErrorKeys.NotInMatch, delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
        }

        /// <summary>Checks SubscribeToMatch_WithUnknownMatchId_RejectsWithNotInMatch.</summary>
        [Fact]
        public void SubscribeToMatch_WithUnknownMatchId_RejectsWithNotInMatch()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);

            // Act
            MatchDelivery delivery = scenario.Registry.SubscribeToMatch(
                scenario.MatchCallerOf(scenario.CreatorUserId), Guid.NewGuid(), new MatchCallbackStub());

            // Assert
            Assert.Equal(MatchErrorKeys.NotInMatch, delivery.ErrorKey);
        }

        /// <summary>Checks SubscribeToMatch_WithMatchIdOfAnotherMatch_RejectsWithNotInMatch.</summary>
        [Fact]
        public void SubscribeToMatch_WithMatchIdOfAnotherMatch_RejectsWithNotInMatch()
        {
            // Arrange: The other match exists, but this player does not belong to it.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchPlayScenario otherMatch = scenario.CreateAnotherMatch(MinimumPlayerCount, OtherMatchFirstUserId);

            // Act
            MatchDelivery delivery = scenario.Registry.SubscribeToMatch(
                scenario.MatchCallerOf(scenario.CreatorUserId), otherMatch.MatchId, new MatchCallbackStub());

            // Assert
            Assert.NotEqual(scenario.MatchId, otherMatch.MatchId);
            Assert.Equal(MatchErrorKeys.NotInMatch, delivery.ErrorKey);
        }

        /// <summary>Checks SubscribeToMatch_AfterPlayerLeftRoom_RejectsWithNotInMatch.</summary>
        [Fact]
        public void SubscribeToMatch_AfterPlayerLeftRoom_RejectsWithNotInMatch()
        {
            // Arrange: The player was an original participant but has since left the match's room.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            int leavingUserId = scenario.UserIds[1];
            scenario.Registry.LeaveRoom(scenario.TokenOf(leavingUserId), out _);

            // Act
            MatchDelivery delivery = scenario.Subscribe(leavingUserId);

            // Assert
            Assert.NotNull(scenario.Registry.FindMatchOfParticipant(scenario.MatchId, leavingUserId));
            Assert.Equal(MatchErrorKeys.NotInMatch, delivery.ErrorKey);
        }

        /// <summary>Checks SubscribeToMatch_AfterSessionEnded_RejectsWithInvalidSession.</summary>
        [Fact]
        public void SubscribeToMatch_AfterSessionEnded_RejectsWithInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            SessionEnd sessionEnd;
            scenario.Registry.EndSession(scenario.TokenOf(scenario.CreatorUserId), out sessionEnd);

            // Act
            MatchDelivery delivery = scenario.Subscribe(scenario.CreatorUserId);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceCard_WithoutSubscription_RejectsWithNotSubscribed.</summary>
        [Fact]
        public void PlaceCard_WithoutSubscription_RejectsWithNotSubscribed()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);

            // Act
            MatchDelivery delivery = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.Equal(MatchErrorKeys.NotSubscribed, delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
        }

        /// <summary>Checks PlaceCard_WithInvalidToken_RejectsWithInvalidSession.</summary>
        /// <param name="sessionToken">Token received from the client.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(UnknownToken)]
        public void PlaceCard_WithInvalidToken_RejectsWithInvalidSession(string sessionToken)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            SessionChannelCaller caller = MatchPlayScenario.CallerWithToken(sessionToken);

            // Act
            MatchDelivery delivery = scenario.Registry.PlaceCard(caller, MatchCardKind.Safe);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceCard_WithAnotherChannelThanSubscribed_RejectsWithInvalidSession.</summary>
        [Fact]
        public void PlaceCard_WithAnotherChannelThanSubscribed_RejectsWithInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            SessionChannelCaller caller = new SessionChannelCaller(scenario.TokenOf(scenario.CreatorUserId), OtherChannelId);

            // Act
            MatchDelivery delivery = scenario.Registry.PlaceCard(caller, MatchCardKind.Safe);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, delivery.ErrorKey);
            Assert.Equal(MatchPlayScenario.InitialVersion, scenario.ReadVersionAs(scenario.CreatorUserId));
        }

        /// <summary>Checks PlaceCard_WithChannelOfAnotherPlayer_RejectsWithInvalidSession.</summary>
        [Fact]
        public void PlaceCard_WithChannelOfAnotherPlayer_RejectsWithInvalidSession()
        {
            // Arrange: The second player uses the creator's subscribed channel during the creator's turn.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            SessionChannelCaller caller = new SessionChannelCaller(
                scenario.TokenOf(scenario.UserIds[1]), MatchPlayScenario.MatchChannelPrefix + scenario.CreatorUserId);

            // Act
            MatchDelivery delivery = scenario.Registry.PlaceCard(caller, MatchCardKind.Safe);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceCard_AfterPlayerLeftRoom_RejectsWithNotSubscribed.</summary>
        [Fact]
        public void PlaceCard_AfterPlayerLeftRoom_RejectsWithNotSubscribed()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.Registry.LeaveRoom(scenario.TokenOf(scenario.CreatorUserId), out _);

            // Act
            MatchDelivery delivery = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.Equal(MatchErrorKeys.NotSubscribed, delivery.ErrorKey);
        }

        /// <summary>Checks PlaceCard_WithUndefinedCardKind_RejectsWithCardNotValid.</summary>
        [Fact]
        public void PlaceCard_WithUndefinedCardKind_RejectsWithCardNotValid()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();

            // Act
            MatchDelivery delivery = scenario.Place(scenario.CreatorUserId, (MatchCardKind)UndefinedCardValue);

            // Assert
            Assert.Equal(MatchErrorKeys.CardNotValid, delivery.ErrorKey);
            Assert.Equal(MatchPlayScenario.InitialVersion, scenario.ReadVersionAs(scenario.CreatorUserId));
        }
    }
}
