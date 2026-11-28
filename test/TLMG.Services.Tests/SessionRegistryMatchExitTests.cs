using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Services.Lobby;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests immediate voluntary exits (C1). Only a voluntary exit removes the player immediately. Other exit causes
    /// are recorded separately and a lost match connection has a reconnection window (C2).
    /// </summary>
    public class SessionRegistryMatchExitTests
    {
        private const int MinimumPlayerCount = 3;
        private const int SecondRound = 2;
        private const int AfterOnePlacementAndExitVersion = 3;
        private const int CardsPerPlayer = MatchPlayScenario.CardsPerPlayerCount;
        private const int GraceSeconds = 15;
        private const int LateEvaluationSeconds = 25;
        private const int LossGapSeconds = 4;
        private const int FriendsHeartbeatCount = 9;
        private const int HeartbeatGapSeconds = 3;
        private const int OrphanLimitSeconds = 30;
        private const int OrphanUserId = 1;
        private const int UnrelatedUserId = 90;

        [Fact]
        public void LeaveRoom_VoluntaryDuringMatch_RemovesPlayerFromEngineAndRestartsRound()
        {
            // Arrange: The first player places a card and the second leaves voluntarily.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int creatorUserId = scenario.CreatorUserId;
            int leavingUserId = scenario.UserIds[1];
            scenario.Place(creatorUserId, MatchCardKind.Safe);

            // Act
            IReadOnlyList<MatchViewDelivery> deliveries = LeaveVoluntarily(scenario, leavingUserId);

            // Assert
            MatchView creatorView = deliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(creatorUserId))).MatchView;
            Assert.Equal(SecondRound, creatorView.RoundNumber);
            Assert.Equal(AfterOnePlacementAndExitVersion, creatorView.Version);
            Assert.Equal(creatorUserId, creatorView.PlayerInTurnUserId);
            Assert.True(creatorView.Players.Single(player => player.UserId == leavingUserId).HasLeftMatch);
            MatchPlayerView creatorPlayer = creatorView.Players.Single(player => player.UserId == creatorUserId);
            Assert.Equal(0, creatorPlayer.PileCardCount);
            Assert.Equal(CardsPerPlayer, creatorPlayer.HandCardCount);
        }

        [Fact]
        public void LeaveRoom_VoluntaryDuringMatch_NotifiesOnlyTheOtherSubscribedPlayers()
        {
            // Arrange: The departing player and only one of the other two subscribe.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            int leavingUserId = scenario.UserIds[1];
            int subscribedUserId = scenario.UserIds[2];
            scenario.Subscribe(leavingUserId);
            scenario.Subscribe(subscribedUserId);

            // Act
            IReadOnlyList<MatchViewDelivery> deliveries = LeaveVoluntarily(scenario, leavingUserId);

            // Assert
            MatchViewDelivery delivery = Assert.Single(deliveries);
            Assert.Same(scenario.CallbackOf(subscribedUserId), delivery.Callback);
        }

        [Fact]
        public void LeaveRoom_WhenTurnPlayerLeaves_GivesTurnToFirstActivePlayer()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();

            // Act
            IReadOnlyList<MatchViewDelivery> deliveries = LeaveVoluntarily(scenario, scenario.CreatorUserId);

            // Assert
            Assert.All(deliveries, item => Assert.Equal(scenario.UserIds[1], item.MatchView.PlayerInTurnUserId));
        }

        [Fact]
        public void LeaveRoom_WhenOnlyOneActivePlayerRemains_HeWinsAtOnce()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int survivorUserId = scenario.UserIds[2];
            LeaveVoluntarily(scenario, scenario.CreatorUserId);

            // Act
            IReadOnlyList<MatchViewDelivery> deliveries = LeaveVoluntarily(scenario, scenario.UserIds[1]);

            // Assert
            MatchView survivorView = Assert.Single(deliveries).MatchView;
            Assert.Equal(MatchRoundPhase.MatchFinished, survivorView.Phase);
            Assert.Equal(survivorUserId, survivorView.WinnerUserId);
            Assert.Equal(survivorUserId, survivorView.FinalStandings[0].UserId);
            Assert.All(survivorView.FinalStandings.Where(row => row.UserId != survivorUserId),
                row => Assert.Equal(0, row.Points));
            Assert.False(survivorView.CanPlaceCard);
        }

        [Fact]
        public void LeaveRoom_VoluntaryDuringMatch_ReportsCauseAndAuthorizedMatch()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            LobbyMembershipChange change = scenario.Registry.LeaveRoom(
                scenario.TokenOf(scenario.UserIds[1]), out deliveries);

            // Assert
            Assert.Equal(RoomExitCause.Voluntary, change.ExitCause);
            Assert.True(change.LeftAuthorizedMatch);
        }

        [Fact]
        public void LeaveRoom_BeforeTheMatch_DoesNotTouchAnyMatch()
        {
            // Arrange: A room in the lobby has no authorized match.
            LobbyScenario scenario = new LobbyScenario();
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            LobbyMembershipChange change = scenario.Registry.LeaveRoom(scenario.CreatorToken, out deliveries);

            // Assert
            Assert.False(change.LeftAuthorizedMatch);
            Assert.Empty(deliveries);
            Assert.Equal(RoomExitCause.Voluntary, change.ExitCause);
        }

        [Fact]
        public void LeaveRoom_TwiceInARow_SecondIsRejectedWithoutDeliveries()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int leavingUserId = scenario.UserIds[1];
            LeaveVoluntarily(scenario, leavingUserId);
            int versionBefore = scenario.ReadVersionAs(scenario.CreatorUserId);
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            LobbyMembershipChange secondChange = scenario.Registry.LeaveRoom(
                scenario.TokenOf(leavingUserId), out deliveries);

            // Assert
            Assert.Equal(LobbyChangeStatus.NotInRoom, secondChange.Status);
            Assert.Empty(deliveries);
            Assert.Equal(versionBefore, scenario.ReadVersionAs(scenario.CreatorUserId));
        }

        [Fact]
        public void LeaveRoom_WithInvalidToken_IsRejectedWithoutDeliveries()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int versionBefore = scenario.ReadVersionAs(scenario.CreatorUserId);
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            LobbyMembershipChange change = scenario.Registry.LeaveRoom("unknown-token", out deliveries);

            // Assert
            Assert.Equal(LobbyChangeStatus.InvalidSession, change.Status);
            Assert.Empty(deliveries);
            Assert.Equal(versionBefore, scenario.ReadVersionAs(scenario.CreatorUserId));
        }

        [Fact]
        public void EndSession_DuringMatch_ReportsTheCauseAndLeavesTheEngineUntouched()
        {
            // Arrange
            const RoomExitCause exitCause = RoomExitCause.SessionEnded;
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int creatorUserId = scenario.CreatorUserId;
            int leavingUserId = scenario.UserIds[1];
            MatchView before = scenario.ReadViewAs(creatorUserId);

            // Act
            LobbyMembershipChange change = LeaveByCause(scenario, leavingUserId, exitCause);

            // Assert
            MatchView after = scenario.ReadViewAs(creatorUserId);
            Assert.Equal(exitCause, change.ExitCause);
            Assert.True(change.LeftAuthorizedMatch);
            Assert.Equal(before.Version, after.Version);
            Assert.Equal(before.RoundNumber, after.RoundNumber);
            Assert.False(after.Players.Single(player => player.UserId == leavingUserId).HasLeftMatch);
        }

        /// <summary>When every player loses the match link and the grace ends, the match finishes.</summary>
        [Fact]
        public void SubscribeToMatch_AfterEveryLinkWasLostAndGraceEnded_RejectsEveryPlayerAndFinishesTheMatch()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            CloseEveryMatchChannel(scenario);
            scenario.Clock.Advance(TimeSpan.FromSeconds(GraceSeconds));

            // Act
            MatchDelivery recovery = scenario.Subscribe(scenario.CreatorUserId);

            // Assert
            MatchSnapshot match = scenario.Registry.FindMatchOfParticipant(scenario.MatchId, scenario.CreatorUserId);
            Assert.Equal(MatchErrorKeys.NotInMatch, recovery.ErrorKey);
            Assert.Empty(recovery.ViewDeliveries);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
            AssertEveryPlayerIsRejected(scenario);
        }

        /// <summary>Returning inside the grace keeps the player in the same round, with the same hand.</summary>
        [Fact]
        public void SubscribeToMatch_WithinGraceAfterLosingTheLink_RecoversTheSameRoundAndHand()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            MatchView before = scenario.ReadViewAs(userId);
            Assert.True(scenario.CloseMatchChannelOf(userId));
            scenario.Clock.Advance(TimeSpan.FromSeconds(GraceSeconds - 1));

            // Act
            MatchDelivery recovery = scenario.Subscribe(userId);

            // Assert
            MatchView recovered = recovery.ViewDeliveries.Single(item => item.Callback == scenario.CallbackOf(userId))
                .MatchView;
            Assert.Null(recovery.ErrorKey);
            Assert.Equal(before.RoundNumber, recovered.RoundNumber);
            Assert.Equal(before.OwnHandCards, recovered.OwnHandCards);
            Assert.False(recovered.Players.Single(player => player.UserId == userId).HasLeftMatch);
        }

        /// <summary>
        /// A player who never linked is rejected once the first-link deadline passed, even if the first operation
        /// that notices it arrives much later.
        /// </summary>
        [Fact]
        public void SubscribeToMatch_AfterFirstLinkDeadlineWithoutLinking_RejectsAndFinishesTheMatch()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.Subscribe(scenario.CreatorUserId);
            scenario.Clock.Advance(TimeSpan.FromSeconds(LateEvaluationSeconds));

            // Act
            MatchDelivery recovery = scenario.Subscribe(scenario.UserIds[1]);

            // Assert
            MatchSnapshot match = scenario.Registry.FindMatchOfParticipant(scenario.MatchId, scenario.CreatorUserId);
            Assert.Equal(MatchErrorKeys.NotInMatch, recovery.ErrorKey);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
        }

        /// <summary>Losing the lobby channel after the authorization keeps the match link; a rebind works.</summary>
        [Fact]
        public void LeaveRoomByChannel_AfterMatchAuthorization_KeepsMatchLinkAndAllowsLobbyRebind()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            SessionChannelCaller original = new SessionChannelCaller(
                scenario.TokenOf(userId), MatchPlayScenario.LobbyChannelPrefix + userId);
            SessionChannelCaller rebound = new SessionChannelCaller(scenario.TokenOf(userId), original.ChannelId + "-new");

            // Act
            scenario.Registry.LeaveRoomByChannel(original.ChannelId);
            LobbyMembershipChange rebind = scenario.Registry.EnterRoom(
                rebound, scenario.RoomId, scenario.LobbyCallbackOf(userId));
            scenario.Registry.LeaveRoomByChannel(original.ChannelId);

            // Assert
            Assert.Equal(LobbyChangeStatus.Applied, rebind.Status);
            Assert.Null(scenario.Place(userId, MatchCardKind.Safe).ErrorKey);
            Assert.Null(scenario.Subscribe(userId).ErrorKey);
        }

        /// <summary>
        /// A new login during the grace invalidates the old token, refuses its logout while it still plays, and lets
        /// the new session recover the same round.
        /// </summary>
        [Fact]
        public void StartRegisteredSession_DuringGrace_InvalidatesOldTokenAndPreservesTheRound()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            int round = scenario.ReadViewAs(userId).RoundNumber;
            Assert.True(scenario.CloseMatchChannelOf(userId));
            bool wasLoggedOut = scenario.Registry.TryLogOut(
                scenario.TokenOf(userId), out _, out bool hasParticipation);
            scenario.Clock.Advance(TimeSpan.FromSeconds(GraceSeconds - 1));
            MatchCallbackStub callback = new MatchCallbackStub();

            // Act
            string replacement = scenario.Registry.StartRegisteredSession(userId, "Replacement", out _);
            MatchDelivery recovered = scenario.Registry.SubscribeToMatch(
                new SessionChannelCaller(replacement, "replacement"), scenario.MatchId, callback);

            // Assert
            Assert.False(wasLoggedOut);
            Assert.True(hasParticipation);
            Assert.Equal(ErrorKeys.Common.InvalidSession, scenario.Subscribe(userId).ErrorKey);
            Assert.False(scenario.CloseMatchChannelOf(userId));
            Assert.Null(recovered.ErrorKey);
            Assert.Equal(round, recovered.ViewDeliveries.Single(item => item.Callback == callback)
                .MatchView.RoundNumber);
        }

        /// <summary>A healthy friends link does not extend the grace of a player whose match link is silent.</summary>
        [Fact]
        public void ProcessHeartbeat_OnFriendsLinkWhileMatchLinkIsSilent_DoesNotExtendTheMatchGrace()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            string token = scenario.TokenOf(userId);
            string friendsLink = MatchPlayScenario.FriendsChannelPrefix + userId;
            scenario.Registry.SubscribeFriendsChannel(
                token, friendsLink, new FriendsCallbackStub(), Array.Empty<int>());

            // Act
            int echoes = SendFriendsHeartbeats(scenario, token, friendsLink, FriendsHeartbeatCount);

            // Assert
            Assert.Equal(FriendsHeartbeatCount, echoes);
            Assert.Equal(MatchErrorKeys.NotInMatch, scenario.Subscribe(userId).ErrorKey);
            Assert.True(scenario.Registry.TryFindSessionByToken(token, out _));
            Assert.Equal(RoundPhase.MatchFinished,
                scenario.Registry.FindMatchOfParticipant(scenario.MatchId, userId).Phase);
        }

        /// <summary>A repeated or too frequent pulse is not echoed; the accepted one echoes its sequence.</summary>
        [Fact]
        public void ProcessHeartbeat_WithDuplicateOrTooFrequentPulse_DoesNotEchoNorChangeTheVersion()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            SessionChannelCaller caller = scenario.MatchCallerOf(scenario.CreatorUserId);
            int version = scenario.ReadVersionAs(scenario.CreatorUserId);
            List<int> echoedSequences = new List<int>();
            List<string> echoedErrors = new List<string>();
            Action<int, string> echo = (sequence, error) => RecordEcho(echoedSequences, echoedErrors, sequence, error);

            // Act
            IReadOnlyList<Action> first = scenario.Registry.ProcessHeartbeat(
                caller.SessionToken, caller.ChannelId, HeartbeatChannel.Match, 1, 0, echo);
            IReadOnlyList<Action> duplicate = scenario.Registry.ProcessHeartbeat(
                caller.SessionToken, caller.ChannelId, HeartbeatChannel.Match, 1, 0, echo);
            IReadOnlyList<Action> tooFrequent = scenario.Registry.ProcessHeartbeat(
                caller.SessionToken, caller.ChannelId, HeartbeatChannel.Match, 2, 1, echo);
            RunNotifications(first.Concat(duplicate).Concat(tooFrequent));

            // Assert
            Assert.Single(first);
            Assert.Empty(duplicate);
            Assert.Empty(tooFrequent);
            Assert.Equal(new[] { 1 }, echoedSequences);
            Assert.Equal(new[] { string.Empty }, echoedErrors);
            Assert.Equal(version, scenario.ReadVersionAs(scenario.CreatorUserId));
        }

        /// <summary>
        /// A session without links expires at 30 seconds of silence, not before. The session is looked up only once,
        /// at the end, because the lookup itself counts as activity and would postpone the expiry.
        /// </summary>
        /// <param name="seconds">Seconds without activity.</param>
        /// <param name="isStillValid">Whether the session is still registered.</param>
        [Theory]
        [InlineData(1, true)]
        [InlineData(OrphanLimitSeconds - 1, true)]
        [InlineData(OrphanLimitSeconds, false)]
        [InlineData(OrphanLimitSeconds + 1, false)]
        public void EvaluateConnectionDeadlines_OnSessionWithoutLinks_ExpiresItOnlyAtTheLimit(
            int seconds,
            bool isStillValid)
        {
            // Arrange
            ManualUtcClock clock = new ManualUtcClock();
            SessionRegistry registry = new SessionRegistry(clock.ReadUtcNow);
            string token = registry.StartRegisteredSession(OrphanUserId, "Orphan", out _);
            clock.Advance(TimeSpan.FromSeconds(seconds));

            // Act
            registry.EvaluateConnectionDeadlines();
            bool isValid = registry.TryFindSessionByToken(token, out _);

            // Assert
            Assert.Equal(isStillValid, isValid);
        }

        /// <summary>A pulse from a replaced friends link is not acknowledged and leaves the new link alone.</summary>
        [Fact]
        public void ProcessHeartbeat_FromReplacedFriendsLink_DoesNotAcknowledgeTheNewLink()
        {
            // Arrange
            ManualUtcClock clock = new ManualUtcClock();
            SessionRegistry registry = new SessionRegistry(clock.ReadUtcNow);
            string token = registry.StartRegisteredSession(OrphanUserId, "Player", out _);
            FriendsCallbackStub callback = new FriendsCallbackStub();
            List<int> echoedSequences = new List<int>();
            List<string> echoedErrors = new List<string>();
            Action<int, string> echo = (sequence, error) => RecordEcho(echoedSequences, echoedErrors, sequence, error);
            registry.SubscribeFriendsChannel(token, "previous", callback, Array.Empty<int>());
            IReadOnlyList<Action> acknowledged = registry.ProcessHeartbeat(
                token, "previous", HeartbeatChannel.Friends, 1, 0, echo);
            registry.SubscribeFriendsChannel(token, "current", callback, Array.Empty<int>());

            // Act
            IReadOnlyList<Action> fromReplaced = registry.ProcessHeartbeat(
                token, "previous", HeartbeatChannel.Friends, 2, 1, echo);
            IReadOnlyList<Action> outOfSequence = registry.ProcessHeartbeat(
                token, "current", HeartbeatChannel.Friends, 2, 1, echo);
            IReadOnlyList<Action> fromCurrent = registry.ProcessHeartbeat(
                token, "current", HeartbeatChannel.Friends, 1, 0, echo);
            RunNotifications(acknowledged.Concat(fromReplaced).Concat(outOfSequence).Concat(fromCurrent));

            // Assert
            Assert.Single(acknowledged);
            Assert.Empty(fromReplaced);
            Assert.Empty(outOfSequence);
            Assert.Single(fromCurrent);
            Assert.Equal(new[] { 1, 1 }, echoedSequences);
            Assert.Equal(new[] { string.Empty, string.Empty }, echoedErrors);
        }

        /// <summary>Replacing a session with a healthy match link pauses the match for the others only.</summary>
        [Fact]
        public void StartRegisteredSession_WithHealthyMatchLink_PublishesPauseOnlyToRemainingPlayers()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int replacedUserId = scenario.CreatorUserId;
            int previousVersion = scenario.ReadVersionAs(replacedUserId);

            // Act
            scenario.Registry.StartRegisteredSession(replacedUserId, "Replacement", out SessionEnd replaced);

            // Assert
            Assert.Equal(MinimumPlayerCount - 1, replaced.MatchViews.Count);
            Assert.DoesNotContain(replaced.MatchViews, item => item.Callback == scenario.CallbackOf(replacedUserId));
            Assert.All(replaced.MatchViews, item => Assert.Equal(previousVersion + 1, item.MatchView.Version));
            Assert.All(replaced.MatchViews, item => Assert.False(item.MatchView.CanPlaceCard));
        }

        /// <summary>A new login inside the original grace gets the room and the match to recover.</summary>
        [Fact]
        public void GetRecoveryContext_AfterLoginInsideTheGrace_ReturnsRoomAndMatch()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            string previous = scenario.TokenOf(userId);
            Assert.True(scenario.CloseMatchChannelOf(userId));
            scenario.Clock.Advance(TimeSpan.FromSeconds(GraceSeconds - 1));
            string token = scenario.Registry.StartRegisteredSession(userId, "Replacement", out _);

            // Act
            OperationRecoveryContextResult result = scenario.Registry.GetRecoveryContext(token).OperationResult;

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.ErrorKey);
            Assert.Equal(scenario.RoomId, result.RoomId);
            Assert.Equal(scenario.MatchId, result.MatchId);
            Assert.False(scenario.Registry.GetRecoveryContext(previous).OperationResult.IsSuccess);
        }

        /// <summary>A new login at or after the end of the original grace gets nothing to recover.</summary>
        /// <param name="seconds">Seconds since the match link was lost.</param>
        [Theory]
        [InlineData(GraceSeconds)]
        [InlineData(GraceSeconds + 1)]
        public void GetRecoveryContext_AfterLoginAtOrAfterTheGraceEnd_ReturnsNoReferences(int seconds)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            string previous = scenario.TokenOf(userId);
            Assert.True(scenario.CloseMatchChannelOf(userId));
            scenario.Clock.Advance(TimeSpan.FromSeconds(seconds));
            string token = scenario.Registry.StartRegisteredSession(userId, "Replacement", out _);

            // Act
            OperationRecoveryContextResult result = scenario.Registry.GetRecoveryContext(token).OperationResult;

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.RoomId);
            Assert.Null(result.MatchId);
            Assert.False(scenario.Registry.GetRecoveryContext(previous).OperationResult.IsSuccess);
        }

        /// <summary>The same token keeps its seat until the grace ends; asking again does not extend it.</summary>
        [Fact]
        public void GetRecoveryContext_WithSameTokenAcrossTheGraceEnd_KeepsTheSeatThenReleasesIt()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            string token = scenario.TokenOf(userId);
            scenario.CloseMatchChannelOf(userId);
            scenario.Clock.Advance(TimeSpan.FromSeconds(GraceSeconds - 1));

            // Act
            ResultWithNotification<OperationRecoveryContextResult> insideGrace =
                scenario.Registry.GetRecoveryContext(token);
            scenario.Clock.Advance(TimeSpan.FromSeconds(1));
            OperationRecoveryContextResult expired = scenario.Registry.GetRecoveryContext(token).OperationResult;

            // Assert
            Assert.Equal(scenario.MatchId, insideGrace.OperationResult.MatchId);
            Assert.Empty(insideGrace.Notifications);
            Assert.True(expired.IsSuccess);
            Assert.Null(expired.RoomId);
            Assert.Null(expired.MatchId);
            Assert.Equal(MatchErrorKeys.NotInMatch, scenario.Subscribe(userId).ErrorKey);
        }

        /// <summary>A session with no room or match receives no references to recover.</summary>
        [Fact]
        public void GetRecoveryContext_WithSessionOutsideAnyMatch_ReturnsSuccessWithoutReferences()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            string unrelated = scenario.Registry.StartRegisteredSession(UnrelatedUserId, "Unrelated", out _);

            // Act
            OperationRecoveryContextResult result = scenario.Registry.GetRecoveryContext(unrelated).OperationResult;

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.ErrorKey);
            Assert.Null(result.RoomId);
            Assert.Null(result.MatchId);
        }

        /// <summary>A token that the registry does not know is rejected and reveals nothing.</summary>
        [Fact]
        public void GetRecoveryContext_WithTokenUnknownToTheRegistry_ReturnsInvalidSessionWithoutReferences()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            SessionRegistry otherRegistry = new SessionRegistry();

            // Act
            OperationRecoveryContextResult result = otherRegistry
                .GetRecoveryContext(scenario.TokenOf(scenario.CreatorUserId)).OperationResult;

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.ErrorKey);
            Assert.Null(result.RoomId);
            Assert.Null(result.MatchId);
        }

        /// <summary>After the recovery context and the lobby rebind, the player gets the own current view.</summary>
        /// <param name="cards">Cards per player of the mode.</param>
        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        public void SubscribeToMatch_AfterRecoveryContextAndLobbyRebind_ReturnsTheOwnPrivateView(int cards)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount, cards);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            scenario.CloseMatchChannelOf(userId);
            string token = scenario.Registry.StartRegisteredSession(userId, "Replacement", out _);
            OperationRecoveryContextResult context = scenario.Registry.GetRecoveryContext(token).OperationResult;
            LobbyMembershipChange room = scenario.Registry.EnterRoom(
                new SessionChannelCaller(token, "recovered-lobby"), context.RoomId.Value, scenario.LobbyCallbackOf(userId));
            MatchCallbackStub callback = new MatchCallbackStub();

            // Act
            MatchDelivery delivery = scenario.Registry.SubscribeToMatch(
                new SessionChannelCaller(token, "recovered-match"), room.Room.Start.MatchId.Value, callback);

            // Assert
            MatchView view = delivery.ViewDeliveries.Single(item => item.Callback == callback).MatchView;
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(cards, view.OwnHandCards.Count);
            Assert.Empty(view.OwnPileCards);
            Assert.All(view.Players, player => Assert.Empty(player.RevealedPileCards));
        }

        /// <summary>If the match finishes before the new session subscribes, it receives the final view.</summary>
        [Fact]
        public void SubscribeToMatch_WhenTheMatchFinishedBeforeTheSubscription_ReturnsTheTerminalView()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            string token = scenario.Registry.StartRegisteredSession(userId, "Replacement", out _);
            OperationRecoveryContextResult context = scenario.Registry.GetRecoveryContext(token).OperationResult;
            LeaveRoomEveryPlayerExcept(scenario, userId);

            // Act
            MatchDelivery delivery = scenario.Registry.SubscribeToMatch(
                new SessionChannelCaller(token, "terminal-match"), context.MatchId.Value, new MatchCallbackStub());

            // Assert
            MatchView view = Assert.Single(delivery.ViewDeliveries).MatchView;
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(MatchRoundPhase.MatchFinished, view.Phase);
            Assert.Equal(userId, view.WinnerUserId);
        }

        /// <summary>A player who left a lobby-only room has nothing to recover after a new login.</summary>
        [Fact]
        public void GetRecoveryContext_AfterLeavingALobbyOnlyRoom_ReturnsNoReferences()
        {
            // Arrange
            SessionRegistry registry = new SessionRegistry();
            string token = registry.StartRegisteredSession(OrphanUserId, "Player", out _);
            registry.CreateRoom(new SessionChannelCaller(token, "initial-lobby"), new LobbyCallbackStub());
            registry.LeaveRoomByChannel("initial-lobby");
            string replacement = registry.StartRegisteredSession(OrphanUserId, "Player", out _);

            // Act
            OperationRecoveryContextResult result = registry.GetRecoveryContext(replacement).OperationResult;

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.RoomId);
            Assert.Null(result.MatchId);
        }

        /// <summary>The view projects one reconnection deadline per player in grace, with the server time.</summary>
        [Fact]
        public void SubscribeToMatch_WithTwoPlayersInGrace_ProjectsEachDeadlineAndTheServerTime()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            DateTime started = scenario.Clock.NowUtc;
            scenario.CloseMatchChannelOf(scenario.UserIds[1]);
            scenario.Clock.Advance(TimeSpan.FromSeconds(LossGapSeconds));
            scenario.CloseMatchChannelOf(scenario.UserIds[2]);

            // Act
            MatchView view = scenario.ReadViewAs(scenario.CreatorUserId);

            // Assert
            Assert.Equal(started.AddSeconds(LossGapSeconds), view.ServerTimeUtc);
            Assert.Equal(started.AddSeconds(GraceSeconds), view.ReconnectDeadlinesUtc[scenario.UserIds[1]]);
            Assert.Equal(started.AddSeconds(GraceSeconds + LossGapSeconds),
                view.ReconnectDeadlinesUtc[scenario.UserIds[2]]);
            Assert.DoesNotContain(scenario.CreatorUserId, view.ReconnectDeadlinesUtc.Keys);
            Assert.False(view.IsWaitingForInitialLinks);
            Assert.False(view.CanPlaceCard);
        }

        /// <summary>Time passing inside a grace does not change the version of the match.</summary>
        [Fact]
        public void SubscribeToMatch_AfterTimePassesInsideTheGrace_KeepsTheSameVersion()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.CloseMatchChannelOf(scenario.UserIds[1]);
            MatchView before = scenario.ReadViewAs(scenario.CreatorUserId);
            scenario.Clock.Advance(TimeSpan.FromSeconds(1));

            // Act
            MatchView after = scenario.ReadViewAs(scenario.CreatorUserId);

            // Assert
            Assert.Equal(before.Version, after.Version);
        }

        /// <summary>Leaving a lobby while in a match sends the lobby notice and the match notice separately.</summary>
        [Fact]
        public void LeaveLobby_WhileInMatch_DeliversLobbyAndMatchNoticesSeparately()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            LobbyService lobbyService = new LobbyService(scenario.Registry, scenario.Registry, scenario.MatchStart);
            int leavingUserId = scenario.UserIds[1];
            int remainingUserId = scenario.CreatorUserId;
            int lobbyCallsBefore = scenario.LobbyCallbackOf(remainingUserId).CallCount;
            int matchViewsBefore = scenario.CallbackOf(remainingUserId).Views.Count;

            // Act
            ResultWithNotification<OperationResult> result = lobbyService.LeaveLobby(scenario.TokenOf(leavingUserId));
            Array.ForEach(result.Notifications.ToArray(), notification => notification());

            // Assert
            Assert.True(result.OperationResult.IsSuccess);
            Assert.Equal(lobbyCallsBefore + 1, scenario.LobbyCallbackOf(remainingUserId).CallCount);
            Assert.Equal(matchViewsBefore + 1, scenario.CallbackOf(remainingUserId).Views.Count);
            Assert.Equal(SecondRound, scenario.CallbackOf(remainingUserId).LastView.RoundNumber);
        }

        /// <summary>Leaving after the victory changes neither the winner nor the points of the final table.</summary>
        [Fact]
        public void LeaveRoom_AfterMatchFinished_PreservesFinalStandings()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.WinRoundWithABetOfOne();
            scenario.WinRoundWithABetOfOne();
            MatchView before = scenario.ReadViewAs(scenario.UserIds[1]);

            // Act
            LobbyMembershipChange departure = scenario.Registry.LeaveRoom(
                scenario.TokenOf(scenario.UserIds[0]), out IReadOnlyList<MatchViewDelivery> deliveries);
            MatchView after = scenario.ReadViewAs(scenario.UserIds[1]);

            // Assert
            Assert.Equal(LobbyChangeStatus.Applied, departure.Status);
            Assert.Empty(deliveries);
            Assert.Equal(before.WinnerUserId, after.WinnerUserId);
            Assert.Equal(before.Version, after.Version);
            Assert.Equal(before.FinalStandings.Select(row => (row.UserId, row.Position, row.Points)),
                after.FinalStandings.Select(row => (row.UserId, row.Position, row.Points)));
        }

        private static void CloseEveryMatchChannel(MatchPlayScenario scenario)
        {
            foreach (int userId in scenario.UserIds)
            {
                Assert.True(scenario.CloseMatchChannelOf(userId));
            }
        }

        private static void AssertEveryPlayerIsRejected(MatchPlayScenario scenario)
        {
            foreach (int userId in scenario.UserIds)
            {
                Assert.Equal(MatchErrorKeys.NotInMatch, scenario.Subscribe(userId).ErrorKey);
            }
        }

        private static void LeaveRoomEveryPlayerExcept(MatchPlayScenario scenario, int stayingUserId)
        {
            foreach (int userId in scenario.UserIds.Where(id => id != stayingUserId))
            {
                scenario.Registry.LeaveRoom(scenario.TokenOf(userId), out _);
            }
        }

        private static int SendFriendsHeartbeats(
            MatchPlayScenario scenario,
            string token,
            string friendsLink,
            int count)
        {
            int echoes = 0;
            int sentSequence = 0;

            void CountEcho(int echoedSequence, string errorKey)
            {
                Assert.Equal(sentSequence, echoedSequence);
                Assert.Equal(string.Empty, errorKey);
                echoes++;
            }

            for (int sequence = 1; sequence <= count; sequence++)
            {
                sentSequence = sequence;
                IReadOnlyList<Action> notifications = scenario.Registry.ProcessHeartbeat(
                    token, friendsLink, HeartbeatChannel.Friends, sequence, sequence - 1, CountEcho);
                RunNotifications(notifications);

                scenario.Clock.Advance(TimeSpan.FromSeconds(HeartbeatGapSeconds));
            }

            return echoes;
        }

        private static void RecordEcho(List<int> sequences, List<string> errors, int sequence, string errorKey)
        {
            sequences.Add(sequence);
            errors.Add(errorKey);
        }

        private static void RunNotifications(IEnumerable<Action> notifications)
        {
            foreach (Action notification in notifications)
            {
                notification();
            }
        }

        private static IReadOnlyList<MatchViewDelivery> LeaveVoluntarily(MatchPlayScenario scenario, int userId)
        {
            IReadOnlyList<MatchViewDelivery> deliveries;
            scenario.Registry.LeaveRoom(scenario.TokenOf(userId), out deliveries);

            return deliveries;
        }

        private static LobbyMembershipChange LeaveByCause(MatchPlayScenario scenario, int userId, RoomExitCause cause)
        {
            LobbyMembershipChange change;
            SessionEnd sessionEnd;

            if (cause == RoomExitCause.LobbyChannelClosed)
            {
                change = scenario.Registry.LeaveRoomByChannel(MatchPlayScenario.LobbyChannelPrefix + userId);
            }
            else if (cause == RoomExitCause.SessionEnded)
            {
                scenario.Registry.EndSession(scenario.TokenOf(userId), out sessionEnd);
                change = sessionEnd.RoomDeparture;
            }
            else
            {
                scenario.Registry.StartRegisteredSession(
                    userId, MatchPlayScenario.NicknamePrefix + userId, out sessionEnd);
                change = sessionEnd.RoomDeparture;
            }

            return change;
        }
    }
}
