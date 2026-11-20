using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests the in-memory match created by SessionRegistry at start authorization. Each match has one MatchState and
    /// participants fixed in entry order. Tests its lifetime separately from the room state.
    /// </summary>
    public class SessionRegistryMatchesTests
    {
        private const int CreatorUserId = 1;
        private const int FirstGuestUserId = 2;
        private const int MinimumPlayerCount = 3;
        private const int MaximumPlayerCount = 6;
        private const int TwoPlayers = 2;
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int CountdownSeconds = 3;
        private const int OneSecond = 1;
        private const int FirstRoundNumber = 1;
        private const int NonParticipantUserId = 99;
        private const int NewcomerUserId = 4;
        private const int UnknownRoomOffset = 100;
        private const string LobbyChannelPrefix = "lobby";

        private static readonly DateTime _startUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>Checks RequestMatchStart_WithEveryPlayerReady_CreatesMatchWithAllParticipants.</summary>
        /// <param name="playerCount">Players in the room.</param>
        [Theory]
        [InlineData(MinimumPlayerCount)]
        [InlineData(MaximumPlayerCount)]
        public void RequestMatchStart_WithEveryPlayerReady_CreatesMatchWithAllParticipants(int playerCount)
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(playerCount, FourCardsPerPlayer);

            // Act
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Assert
            MatchSnapshot match = start.FindMatch(change, CreatorUserId);
            Assert.NotNull(match);
            Assert.Equal(playerCount, match.ParticipantUserIds.Count);
            Assert.Equal(FirstRoundNumber, match.RoundNumber);
            Assert.Equal(CreatorUserId, match.PlayerInTurnUserId);
        }

        /// <summary>Checks RequestMatchStart_WithRoomMode_CreatesEngineWithSameCardsPerPlayer.</summary>
        /// <param name="cardsPerPlayerCount">Room mode.</param>
        [Theory]
        [InlineData(FourCardsPerPlayer)]
        [InlineData(SixCardsPerPlayer)]
        [InlineData(EightCardsPerPlayer)]
        public void RequestMatchStart_WithRoomMode_CreatesEngineWithSameCardsPerPlayer(int cardsPerPlayerCount)
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, cardsPerPlayerCount);

            // Act
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Assert
            Assert.Equal(cardsPerPlayerCount, start.FindMatch(change, CreatorUserId).CardsPerPlayerCount);
        }

        /// <summary>Checks RequestMatchStart_WithSeveralPlayers_KeepsJoinOrder.</summary>
        [Fact]
        public void RequestMatchStart_WithSeveralPlayers_KeepsJoinOrder()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MaximumPlayerCount, FourCardsPerPlayer);

            // Act
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Assert
            Assert.Equal(start.UserIdsInJoinOrder, start.FindMatch(change, CreatorUserId).ParticipantUserIds);
        }

        /// <summary>Checks RequestMatchStart_WhenAuthorized_LinksMatchIdAndRoomId.</summary>
        [Fact]
        public void RequestMatchStart_WhenAuthorized_LinksMatchIdAndRoomId()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);

            // Act
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Assert
            MatchSnapshot match = start.FindMatch(change, CreatorUserId);
            Assert.Equal(change.Room.Start.MatchId, match.MatchId);
            Assert.Equal(start.Scenario.RoomId, match.RoomId);
            Assert.Equal(start.Scenario.RoomId, change.RoomId);
        }

        /// <summary>Checks RequestMatchStart_SequentialReentryOfSameGeneration_KeepsMatchIdAndMatch.</summary>
        [Fact]
        public void RequestMatchStart_SequentialReentryOfSameGeneration_KeepsMatchIdAndMatch()
        {
            // Arrange: A second player requests a start for the same generation after the first request.
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange firstChange = start.RequestStartAfterCountdown();

            // Act
            LobbyMembershipChange secondChange = start.RequestStartAs(FirstGuestUserId);

            // Assert
            Assert.Equal(firstChange.Room.Start.MatchId, secondChange.Room.Start.MatchId);
            Assert.Empty(secondChange.RecipientCallbacks);
            Assert.Equal(firstChange.Room.Start.Version, secondChange.Room.Start.Version);
            Assert.Equal(start.UserIdsInJoinOrder, start.FindMatch(secondChange, CreatorUserId).ParticipantUserIds);
        }

        /// <summary>Checks RequestMatchStart_BeforeCountdownEnds_CreatesNoMatch.</summary>
        [Fact]
        public void RequestMatchStart_BeforeCountdownEnds_CreatesNoMatch()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            start.Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds - OneSecond));

            // Act
            LobbyMembershipChange change = start.RequestStartAs(CreatorUserId);

            // Assert
            Assert.False(change.Room.Start.MatchId.HasValue);
            Assert.Empty(change.RecipientCallbacks);
        }

        /// <summary>Checks RequestMatchStart_WhenOnePlayerIsNotReady_CreatesNoMatch.</summary>
        [Fact]
        public void RequestMatchStart_WhenOnePlayerIsNotReady_CreatesNoMatch()
        {
            // Arrange: The last player becomes not ready again, canceling the current countdown.
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            start.ChangeReadiness(start.UserIdsInJoinOrder.Last(), false);
            start.Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));

            // Act
            LobbyMembershipChange change = start.RequestStartAs(CreatorUserId);

            // Assert
            Assert.False(change.Room.Start.MatchId.HasValue);
            Assert.Empty(change.RecipientCallbacks);
        }

        /// <summary>Checks RequestMatchStart_WithFewerThanThreePlayers_CreatesNoMatch.</summary>
        [Fact]
        public void RequestMatchStart_WithFewerThanThreePlayers_CreatesNoMatch()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(TwoPlayers, FourCardsPerPlayer);
            start.Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));

            // Act
            LobbyMembershipChange change = start.RequestStartAs(CreatorUserId);

            // Assert
            Assert.False(change.Room.Start.MatchId.HasValue);
            Assert.False(change.Room.Start.CountdownEndsAtUtc.HasValue);
        }

        /// <summary>Checks LeaveRoom_AfterMatchStarted_KeepsMatchParticipants.</summary>
        [Fact]
        public void LeaveRoom_AfterMatchStarted_KeepsMatchParticipants()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Act
            start.Scenario.Registry.LeaveRoom(start.TokenOf(FirstGuestUserId), out _);

            // Assert
            Assert.Equal(start.UserIdsInJoinOrder, start.FindMatch(change, CreatorUserId).ParticipantUserIds);
        }

        /// <summary>Checks LeaveRoom_WhenRoomBecomesEmpty_ReleasesMatch.</summary>
        [Fact]
        public void LeaveRoom_WhenRoomBecomesEmpty_ReleasesMatch()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Act
            LobbyMembershipChange lastDeparture = start.LeaveAll();

            // Assert
            Assert.Null(lastDeparture.Room);
            Assert.Null(start.FindMatch(change, CreatorUserId));
        }

        [Fact]
        public void TryLogOut_WithActiveParticipation_PreservesSessionAndMatch()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange change = start.RequestStartAfterCountdown();
            string token = start.TokenOf(CreatorUserId);

            // Act
            bool loggedOut = start.Scenario.Registry.TryLogOut(token, out SessionEnd sessionEnd,
                out bool hasActiveParticipation);

            // Assert
            Assert.False(loggedOut);
            Assert.True(hasActiveParticipation);
            Assert.Null(sessionEnd);
            Assert.True(start.Scenario.Registry.TryFindSessionByToken(token, out _));
            Assert.NotNull(start.FindMatch(change, CreatorUserId));
        }

        [Fact]
        public void TryLogOut_WithoutMatch_RevokesSession()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(CreatorUserId, "Creator", out _);

            // Act
            bool loggedOut = registry.TryLogOut(token, out SessionEnd sessionEnd,
                out bool hasActiveParticipation);

            // Assert
            Assert.True(loggedOut);
            Assert.False(hasActiveParticipation);
            Assert.NotNull(sessionEnd);
            Assert.False(registry.TryFindSessionByToken(token, out _));
        }


        /// <summary>Checks EndSession_OfLastRoomMember_ReleasesMatch.</summary>
        [Fact]
        public void EndSession_OfLastRoomMember_ReleasesMatch()
        {
            // Arrange: Everyone leaves except the creator, who then ends their session.
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange change = start.RequestStartAfterCountdown();
            start.LeaveAllExceptCreator();

            // Act
            bool wasEnded = start.Scenario.Registry.EndSession(start.TokenOf(CreatorUserId), out SessionEnd sessionEnd);

            // Assert
            Assert.True(wasEnded);
            Assert.Null(sessionEnd.RoomDeparture.Room);
            Assert.Null(start.FindMatch(change, CreatorUserId));
        }

        /// <summary>Checks FindMatchOfParticipant_WithUserOutsideMatch_ReturnsNull.</summary>
        [Fact]
        public void FindMatchOfParticipant_WithUserOutsideMatch_ReturnsNull()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Act
            MatchSnapshot match = start.FindMatch(change, NonParticipantUserId);

            // Assert
            Assert.Null(match);
        }

        /// <summary>Checks FindMatchOfParticipant_WithUnknownMatchId_ReturnsNull.</summary>
        [Fact]
        public void FindMatchOfParticipant_WithUnknownMatchId_ReturnsNull()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            start.RequestStartAfterCountdown();

            // Act
            MatchSnapshot match = start.Scenario.Registry.FindMatchOfParticipant(Guid.NewGuid(), CreatorUserId);

            // Assert
            Assert.Null(match);
        }

        [Fact]
        public void ChangePlayerReadiness_WhenTheLastPlayerBecomesReady_ArmsANewCountdownOfThreeSeconds()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            long firstCountdownId = start.ArmedRoom.Room.Start.CountdownId;
            int lastUserId = start.UserIdsInJoinOrder.Last();
            start.ChangeReadiness(lastUserId, false);
            start.Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds + OneSecond));

            // Act
            LobbyMembershipChange change = start.ChangeReadiness(lastUserId, true);

            // Assert
            Assert.Equal(LobbyChangeStatus.Applied, change.Status);
            Assert.Equal(firstCountdownId + 1, change.Room.Start.CountdownId);
            Assert.Equal(start.Clock.NowUtc.AddSeconds(CountdownSeconds), change.Room.Start.CountdownEndsAtUtc);
            Assert.Equal(MinimumPlayerCount, change.Room.Start.ReadyUserIds.Count);
        }

        [Fact]
        public void ChangePlayerReadiness_WhenAReadyPlayerBecomesNotReady_CancelsTheCountdown()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange armed = start.ArmedRoom;
            int lastUserId = start.UserIdsInJoinOrder.Last();

            // Act
            LobbyMembershipChange change = start.ChangeReadiness(lastUserId, false);

            // Assert
            Assert.True(armed.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.Equal(LobbyChangeStatus.Applied, change.Status);
            Assert.False(change.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.DoesNotContain(lastUserId, change.Room.Start.ReadyUserIds);
            Assert.Equal(MinimumPlayerCount - 1, change.Room.Start.ReadyUserIds.Count);
            Assert.True(change.Room.Start.Version > armed.Room.Start.Version);
        }

        [Fact]
        public void ChangePlayerReadiness_ToTheStateThePlayerAlreadyHas_ChangesNothingAndNotifiesNobody()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange armed = start.ArmedRoom;

            // Act
            LobbyMembershipChange change = start.ChangeReadiness(CreatorUserId, true);

            // Assert
            Assert.Equal(LobbyChangeStatus.Applied, change.Status);
            Assert.Equal(armed.Room.Start.Version, change.Room.Start.Version);
            Assert.Equal(armed.Room.Start.CountdownId, change.Room.Start.CountdownId);
            Assert.Equal(armed.Room.Start.CountdownEndsAtUtc, change.Room.Start.CountdownEndsAtUtc);
            Assert.Empty(change.RecipientCallbacks);
        }

        [Fact]
        public void ChangePlayerReadiness_ToNotReadyWhenAlreadyNotReady_ChangesNothing()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            int lastUserId = start.UserIdsInJoinOrder.Last();
            LobbyMembershipChange firstChange = start.ChangeReadiness(lastUserId, false);

            // Act
            LobbyMembershipChange repeated = start.ChangeReadiness(lastUserId, false);

            // Assert
            Assert.Equal(firstChange.Room.Start.Version, repeated.Room.Start.Version);
            Assert.Equal(firstChange.Room.Start.ReadyUserIds, repeated.Room.Start.ReadyUserIds);
            Assert.Empty(repeated.RecipientCallbacks);
        }

        [Fact]
        public void ChangePlayerReadiness_WithOnlyTwoPlayersReady_DoesNotArmACountdown()
        {
            // Arrange: ready the creator and leave the second player (not yet ready) in a two-player room.
            MatchStartScenario start = new MatchStartScenario(TwoPlayers, FourCardsPerPlayer, readyPlayerCount: 1);

            // Act
            LobbyMembershipChange change = start.ChangeReadiness(start.UserIdsInJoinOrder.Last(), true);

            // Assert
            Assert.Equal(TwoPlayers, change.Room.Start.ReadyUserIds.Count);
            Assert.False(change.Room.Start.CountdownEndsAtUtc.HasValue);
        }

        [Fact]
        public void ChangePlayerReadiness_WhenApplied_ReturnsTheCallbacksOfTheRoomMembersWithoutInvokingThem()
        {
            // Arrange
            LobbyScenario scenario = new LobbyScenario();
            LobbyCallbackStub secondCallback = new LobbyCallbackStub();
            LobbyCallbackStub thirdCallback = new LobbyCallbackStub();
            LobbyReadinessHelper.AddPlayer(scenario, FirstGuestUserId, "lobby2", secondCallback);
            LobbyReadinessHelper.AddPlayer(scenario, FirstGuestUserId + 1, "lobby3", thirdCallback);

            // Act
            LobbyMembershipChange change = LobbyReadinessHelper.MarkReady(
                scenario, scenario.CreatorToken, LobbyReadinessHelper.CreatorChannelId);

            // Assert
            Assert.Equal(new[] { CreatorUserId }, change.Room.Start.ReadyUserIds);
            Assert.Equal(TwoPlayers, change.RecipientCallbacks.Count);
            Assert.Contains(secondCallback, change.RecipientCallbacks);
            Assert.Contains(thirdCallback, change.RecipientCallbacks);
            Assert.Equal(0, secondCallback.CallCount);
            Assert.Equal(0, thirdCallback.CallCount);
        }

        [Fact]
        public void EnterRoom_WhileTheCountdownRuns_CancelsItBecauseTheNewPlayerIsNotReady()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            long armedCountdownId = start.ArmedRoom.Room.Start.CountdownId;

            string newcomerToken = start.Scenario.Authorize(NewcomerUserId);

            // Act
            LobbyMembershipChange entry = start.Scenario.Registry.EnterRoom(
                new SessionChannelCaller(newcomerToken, "lobby" + NewcomerUserId), start.Scenario.RoomId, null);

            // Assert
            Assert.Equal(LobbyChangeStatus.Applied, entry.Status);
            Assert.False(entry.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.Equal(MinimumPlayerCount, entry.Room.Start.ReadyUserIds.Count);
            Assert.Equal(armedCountdownId, entry.Room.Start.CountdownId);
        }

        [Fact]
        public void LeaveRoom_OfTheOnlyPlayerWhoIsNotReady_ArmsTheCountdownForTheRest()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount + 1, FourCardsPerPlayer);
            long firstCountdownId = start.ArmedRoom.Room.Start.CountdownId;
            int notReadyUserId = start.UserIdsInJoinOrder.Last();
            LobbyMembershipChange cancelled = start.ChangeReadiness(notReadyUserId, false);

            // Act
            LobbyMembershipChange departure = start.Scenario.Registry.LeaveRoom(start.TokenOf(notReadyUserId), out _);

            // Assert
            Assert.False(cancelled.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.True(departure.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.Equal(firstCountdownId + 1, departure.Room.Start.CountdownId);
            Assert.Equal(MinimumPlayerCount, departure.Room.Start.ReadyUserIds.Count);
        }

        [Fact]
        public void LeaveRoom_WhenFewerThanThreePlayersWouldRemain_CancelsTheCountdown()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange armed = start.ArmedRoom;

            // Act
            LobbyMembershipChange departure = start.Scenario.Registry.LeaveRoom(
                start.TokenOf(start.UserIdsInJoinOrder.Last()), out _);

            // Assert
            Assert.True(armed.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.False(departure.Room.Start.CountdownEndsAtUtc.HasValue);
            Assert.Equal(TwoPlayers, departure.Room.Start.ReadyUserIds.Count);
        }

        [Fact]
        public void RequestMatchStart_WithStaleCountdownId_CreatesNoMatchUntilTheCurrentIdIsUsed()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            long staleCountdownId = start.ArmedRoom.Room.Start.CountdownId;
            int lastUserId = start.UserIdsInJoinOrder.Last();
            start.ChangeReadiness(lastUserId, false);
            long currentCountdownId = start.ChangeReadiness(lastUserId, true).Room.Start.CountdownId;
            start.Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));

            // Act
            LobbyMembershipChange withStaleId = start.RequestStartWithCountdownId(CreatorUserId, staleCountdownId);
            LobbyMembershipChange withCurrentId = start.RequestStartWithCountdownId(CreatorUserId, currentCountdownId);

            // Assert
            Assert.NotEqual(staleCountdownId, currentCountdownId);
            Assert.False(withStaleId.Room.Start.MatchId.HasValue);
            Assert.Empty(withStaleId.RecipientCallbacks);
            Assert.True(withCurrentId.Room.Start.MatchId.HasValue);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void ChangePlayerReadiness_WithInvalidSession_RejectsWithoutChangingTheRoom(string token)
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange before = start.ArmedRoom;
            SessionChannelCaller invalidCaller = new SessionChannelCaller(token, LobbyReadinessHelper.CreatorChannelId);

            // Act
            LobbyMembershipChange change = start.Scenario.Registry.ChangePlayerReadiness(
                invalidCaller, start.Scenario.RoomId, false);

            // Assert
            Assert.Equal(LobbyChangeStatus.InvalidSession, change.Status);
            Assert.Null(change.Room);
            // Same-state repeat by the creator: it changes nothing, so its snapshot shows the room kept its version.
            LobbyMembershipChange repeated = start.ChangeReadiness(CreatorUserId, true);
            Assert.Equal(before.Room.Start.Version, repeated.Room.Start.Version);
            Assert.Empty(repeated.RecipientCallbacks);
        }

        [Fact]
        public void ChangePlayerReadiness_ByPlayerOutsideTheRoom_RejectsWithNotInRoomWithoutChangingTheRoom()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange before = start.ArmedRoom;
            string outsiderToken = start.Scenario.Registry.StartRegisteredSession(
                NonParticipantUserId, "Outsider", out _);

            // Act
            LobbyMembershipChange change = start.Scenario.Registry.ChangePlayerReadiness(
                new SessionChannelCaller(outsiderToken, "outsider"), start.Scenario.RoomId, true);

            // Assert
            Assert.Equal(LobbyChangeStatus.NotInRoom, change.Status);
            Assert.Null(change.Room);
            // Same-state repeat by the creator: it changes nothing, so its snapshot shows the room kept its version.
            LobbyMembershipChange repeated = start.ChangeReadiness(CreatorUserId, true);
            Assert.Equal(before.Room.Start.Version, repeated.Room.Start.Version);
            Assert.Empty(repeated.RecipientCallbacks);
        }

        [Fact]
        public void ChangePlayerReadiness_ForAnotherRoomId_RejectsWithNotInRoomWithoutChangingTheRoom()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange before = start.ArmedRoom;

            // Act
            LobbyMembershipChange change = start.Scenario.Registry.ChangePlayerReadiness(
                new SessionChannelCaller(start.TokenOf(CreatorUserId), LobbyReadinessHelper.CreatorChannelId),
                start.Scenario.RoomId + UnknownRoomOffset,
                false);

            // Assert
            Assert.Equal(LobbyChangeStatus.NotInRoom, change.Status);
            Assert.Null(change.Room);
            // Same-state repeat by the creator: it changes nothing, so its snapshot shows the room kept its version.
            LobbyMembershipChange repeated = start.ChangeReadiness(CreatorUserId, true);
            Assert.Equal(before.Room.Start.Version, repeated.Room.Start.Version);
            Assert.Empty(repeated.RecipientCallbacks);
        }

        [Fact]
        public void RequestMatchStart_ByPlayerOutsideTheRoom_RejectsWithNotInRoomAndCreatesNoMatch()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange before = start.ArmedRoom;
            string outsiderToken = start.Scenario.Registry.StartRegisteredSession(
                NonParticipantUserId, "Outsider", out _);
            start.Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));

            // Act
            LobbyMembershipChange change = start.Scenario.Registry.RequestMatchStart(
                new SessionChannelCaller(outsiderToken, "outsider"), start.Scenario.RoomId, before.Room.Start.CountdownId);

            // Assert
            Assert.Equal(LobbyChangeStatus.NotInRoom, change.Status);
            Assert.Null(change.Room);
            Assert.Empty(change.RecipientCallbacks);
            // Same-state repeat by the creator: it changes nothing, so its snapshot shows the room as it was left.
            LobbyMembershipChange repeated = start.ChangeReadiness(CreatorUserId, true);
            Assert.False(repeated.Room.Start.MatchId.HasValue);
            Assert.Equal(before.Room.Start.Version, repeated.Room.Start.Version);
            Assert.Equal(before.Room.Start.CountdownId, repeated.Room.Start.CountdownId);
            Assert.Empty(repeated.RecipientCallbacks);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown")]
        public void RequestMatchStart_WithInvalidSession_RejectsAndCreatesNoMatch(string token)
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            long countdownId = start.ArmedRoom.Room.Start.CountdownId;
            start.Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));

            // Act
            LobbyMembershipChange change = start.Scenario.Registry.RequestMatchStart(
                new SessionChannelCaller(token, LobbyReadinessHelper.CreatorChannelId), start.Scenario.RoomId, countdownId);

            // Assert
            Assert.Equal(LobbyChangeStatus.InvalidSession, change.Status);
            Assert.Null(change.Room);
            // Same-state repeat by the creator: it changes nothing, so its snapshot shows that no match was created.
            LobbyMembershipChange repeated = start.ChangeReadiness(CreatorUserId, true);
            Assert.False(repeated.Room.Start.MatchId.HasValue);
            Assert.Empty(repeated.RecipientCallbacks);
        }

        [Fact]
        public void ChangePlayerReadiness_AfterTheMatchStarted_RejectsAChangeWithMatchAlreadyStarted()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange started = start.RequestStartAfterCountdown();

            // Act
            LobbyMembershipChange change = start.ChangeReadiness(start.UserIdsInJoinOrder.Last(), false);

            // Assert
            Assert.Equal(LobbyChangeStatus.MatchAlreadyStarted, change.Status);
            Assert.Null(change.Room);
            // Same-state repeat by the creator: it changes nothing, so its snapshot shows the match was left alone.
            LobbyMembershipChange repeated = start.ChangeReadiness(CreatorUserId, true);
            Assert.Equal(started.Room.Start.MatchId, repeated.Room.Start.MatchId);
            Assert.Equal(started.Room.Start.Version, repeated.Room.Start.Version);
            Assert.Empty(repeated.RecipientCallbacks);
        }

        [Fact]
        public void ChangePlayerReadiness_AfterTheMatchStartedToTheSameState_IsAppliedWithoutChanges()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange started = start.RequestStartAfterCountdown();

            // Act
            LobbyMembershipChange change = start.ChangeReadiness(CreatorUserId, true);

            // Assert
            Assert.Equal(LobbyChangeStatus.Applied, change.Status);
            Assert.Equal(started.Room.Start.Version, change.Room.Start.Version);
            Assert.Equal(started.Room.Start.MatchId, change.Room.Start.MatchId);
        }

        [Fact]
        public void ChangeCardsPerPlayer_AfterTheMatchStarted_RejectsAnotherModeAndKeepsTheMatchMode()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange started = start.RequestStartAfterCountdown();

            // Act
            LobbyMembershipChange change = start.ChangeCardsPerPlayer(CreatorUserId, EightCardsPerPlayer);

            // Assert
            Assert.Equal(LobbyChangeStatus.MatchAlreadyStarted, change.Status);
            Assert.Null(change.Room);
            Assert.Equal(FourCardsPerPlayer, start.FindMatch(started, CreatorUserId).CardsPerPlayerCount);
        }

        [Fact]
        public void ChangeCardsPerPlayer_AfterTheMatchStartedToTheSameMode_IsAppliedWithoutChanges()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange started = start.RequestStartAfterCountdown();

            // Act
            LobbyMembershipChange change = start.ChangeCardsPerPlayer(CreatorUserId, FourCardsPerPlayer);

            // Assert
            Assert.Equal(LobbyChangeStatus.Applied, change.Status);
            Assert.Equal(started.Room.Start.Version, change.Room.Start.Version);
            Assert.Equal(started.Room.Start.MatchId, change.Room.Start.MatchId);
        }

        /// <summary>UTC clock advanced by the test.</summary>
        private sealed class TestClock
        {
            internal DateTime NowUtc { get; private set; } = _startUtc;

            internal void Advance(TimeSpan elapsed)
            {
                NowUtc = NowUtc.Add(elapsed);
            }

            internal DateTime ReadUtcNow()
            {
                return NowUtc;
            }
        }

        /// <summary>
        /// Room with a controlled clock. The creator and others join in ID order, select the mode and all become ready
        /// with the countdown running.
        /// </summary>
        private sealed class MatchStartScenario
        {
            private readonly Dictionary<int, string> _tokensByUserId = new Dictionary<int, string>();

            internal MatchStartScenario(int playerCount, int cardsPerPlayerCount)
                : this(playerCount, cardsPerPlayerCount, playerCount)
            {
            }

            internal MatchStartScenario(int playerCount, int cardsPerPlayerCount, int readyPlayerCount)
            {
                Clock = new TestClock();
                Scenario = new LobbyScenario(new SessionRegistry(Clock.ReadUtcNow));
                _tokensByUserId[CreatorUserId] = Scenario.CreatorToken;
                UserIdsInJoinOrder = Enumerable.Range(CreatorUserId, playerCount).ToList().AsReadOnly();

                foreach (int userId in UserIdsInJoinOrder.Skip(1))
                {
                    _tokensByUserId[userId] = LobbyReadinessHelper.AddPlayer(
                        Scenario, userId, ChannelOf(userId), null);
                }

                Scenario.Registry.ChangeCardsPerPlayer(CallerOf(CreatorUserId), Scenario.RoomId, cardsPerPlayerCount);

                foreach (int userId in UserIdsInJoinOrder.Take(readyPlayerCount))
                {
                    ArmedRoom = ChangeReadiness(userId, true);
                }
            }

            internal TestClock Clock { get; }

            /// <summary>Snapshot returned when the last player became ready: the room as the tests start it.</summary>
            internal LobbyMembershipChange ArmedRoom { get; }

            internal LobbyScenario Scenario { get; }

            internal IReadOnlyList<int> UserIdsInJoinOrder { get; }

            internal string TokenOf(int userId)
            {
                return _tokensByUserId[userId];
            }

            internal LobbyMembershipChange ChangeReadiness(int userId, bool isReady)
            {
                return Scenario.Registry.ChangePlayerReadiness(CallerOf(userId), Scenario.RoomId, isReady);
            }

            /// <summary>Lets the current countdown expire and requests a start as the creator.</summary>
            internal LobbyMembershipChange ChangeCardsPerPlayer(int userId, int cardsPerPlayerCount)
            {
                return Scenario.Registry.ChangeCardsPerPlayer(CallerOf(userId), Scenario.RoomId, cardsPerPlayerCount);
            }

            internal LobbyMembershipChange RequestStartWithCountdownId(int userId, long countdownId)
            {
                return Scenario.Registry.RequestMatchStart(CallerOf(userId), Scenario.RoomId, countdownId);
            }

            internal LobbyMembershipChange RequestStartAfterCountdown()
            {
                Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));

                return RequestStartAs(CreatorUserId);
            }

            internal LobbyMembershipChange RequestStartAs(int userId)
            {
                long armedCountdownId = ArmedRoom.Room.Start.CountdownId;

                return Scenario.Registry.RequestMatchStart(CallerOf(userId), Scenario.RoomId, armedCountdownId);
            }

            internal MatchSnapshot FindMatch(LobbyMembershipChange change, int userId)
            {
                return Scenario.Registry.FindMatchOfParticipant(change.Room.Start.MatchId.Value, userId);
            }

            /// <summary>All players except the creator leave in entry order.</summary>
            internal void LeaveAllExceptCreator()
            {
                foreach (int userId in UserIdsInJoinOrder.Skip(1))
                {
                    Scenario.Registry.LeaveRoom(TokenOf(userId), out _);
                }
            }

            /// <summary>All players leave in order. Returns the final exit that empties the room.</summary>
            internal LobbyMembershipChange LeaveAll()
            {
                LobbyMembershipChange lastDeparture = null;

                foreach (int userId in UserIdsInJoinOrder)
                {
                    lastDeparture = Scenario.Registry.LeaveRoom(TokenOf(userId), out _);
                }

                return lastDeparture;
            }

            private SessionChannelCaller CallerOf(int userId)
            {
                string channelId = userId == CreatorUserId ? LobbyReadinessHelper.CreatorChannelId : ChannelOf(userId);

                return new SessionChannelCaller(TokenOf(userId), channelId);
            }

            private static string ChannelOf(int userId)
            {
                return LobbyChannelPrefix + userId;
            }
        }
    }
}
