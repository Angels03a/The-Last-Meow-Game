using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization;
using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Match;
using TLMG.Services.Sessions;
using Xunit;

namespace TLMG.Services.Tests
{
    public class SessionRegistryFugitiveKittenTests
    {
        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        public void Start_WithFewerThanEightCards_CreatesNoFugitiveKitten(int cardsPerPlayerCount)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, cardsPerPlayerCount);

            // Act
            FugitiveKittenSnapshot kitten = scenario.Registry.FindFugitiveKitten(
                scenario.MatchId, scenario.CreatorUserId);

            // Assert
            Assert.Null(kitten);
        }

        [Fact]
        public void Start_WithEightCards_CreatesAFifteenSecondKittenThatLaterSubscriptionsDoNotRestart()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            DateTime creationTimeUtc = scenario.Clock.NowUtc;
            scenario.Subscribe(scenario.CreatorUserId);
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(100));

            // Act
            scenario.Subscribe(scenario.CreatorUserId);

            // Assert
            FugitiveKittenSnapshot kitten = scenario.Registry.FindFugitiveKitten(
                scenario.MatchId, scenario.CreatorUserId);
            Assert.Equal(creationTimeUtc, kitten.StartedAtUtc);
            Assert.Equal(TimeSpan.FromSeconds(15), kitten.ExpiresAtUtc - kitten.StartedAtUtc);
        }

        [Fact]
        public void TryCatchFugitiveKitten_WhenPaused_PreservesRunningViewAndVersion()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            int userId = scenario.CreatorUserId;
            MatchView initial = scenario.Subscribe(userId).ViewDeliveries[0].MatchView;
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(900));

            MatchDelivery delivery = scenario.Registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(userId));

            Assert.NotNull(initial.FugitiveKitten.PausedAtUtc);
            Assert.Equal(FugitiveKittenResult.InProgress, initial.FugitiveKitten.Result);
            Assert.Null(delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
            Assert.Equal(initial.Version, scenario.Registry.FindFugitiveKitten(scenario.MatchId, userId).Version);
        }

        [Fact]
        public void Pause_WaitsForInitialLinksAndFreezesElapsedTimeDuringOverlappingLosses()
        {
            var scenario = new MatchPlayScenario(3, 8);
            scenario.Subscribe(scenario.CreatorUserId);
            scenario.Clock.Advance(TimeSpan.FromSeconds(3));
            Assert.True(scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId)
                .PausedAtUtc.HasValue);

            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(900));
            Assert.True(scenario.CloseMatchChannelOf(scenario.UserIds[1]));
            scenario.Clock.Advance(TimeSpan.FromSeconds(2));
            Assert.True(scenario.CloseMatchChannelOf(scenario.UserIds[2]));
            scenario.Clock.Advance(TimeSpan.FromSeconds(2));
            scenario.Subscribe(scenario.UserIds[1]);
            Assert.True(scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId)
                .PausedAtUtc.HasValue);
            scenario.Subscribe(scenario.UserIds[2]);

            var resumed = scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId);
            Assert.Null(resumed.PausedAtUtc);
            Assert.Equal(TimeSpan.FromMilliseconds(900), scenario.Clock.NowUtc - resumed.StartedAtUtc);
        }

        [Fact]
        public void RecoveryContext_IsStableWhileKittenIsRunningPausedAndResolved()
        {
            var scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            string token = scenario.TokenOf(userId);
            var registry = scenario.Registry;
            Assert.Equal(scenario.MatchId, registry.GetRecoveryContext(token).OperationResult.MatchId);
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(900));
            scenario.CloseMatchChannelOf(userId);
            Assert.NotNull(registry.FindFugitiveKitten(scenario.MatchId, userId).PausedAtUtc);
            Assert.Equal(scenario.MatchId, registry.GetRecoveryContext(token).OperationResult.MatchId);
            scenario.Subscribe(userId);
            registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(userId));
            var caught = registry.FindFugitiveKitten(scenario.MatchId, userId);
            Assert.Equal(FugitiveKittenOutcome.Caught, caught.Outcome);
            Assert.Equal(scenario.MatchId, registry.GetRecoveryContext(token).OperationResult.MatchId);
            Assert.Equal(caught.Version, registry.FindFugitiveKitten(scenario.MatchId, userId).Version);
        }

        [Theory]
        [InlineData(899, false, null, 2, 0)]
        [InlineData(900, true, MatchPlayScenario.FirstUserId, 3, 3)]
        [InlineData(1950, true, MatchPlayScenario.FirstUserId, 3, 3)]
        [InlineData(1951, false, null, 2, 0)]
        [InlineData(14250, true, MatchPlayScenario.FirstUserId, 3, 3)]
        [InlineData(14251, false, null, 2, 0)]
        [InlineData(15000, false, null, 3, 3)]
        public void TryCatchFugitiveKitten_AtServerTime_CatchesOnlyInsideACatchableSegment(
            int milliseconds,
            bool expectedCaught,
            int? expectedWinnerUserId,
            int expectedVersion,
            int expectedViewCount)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(milliseconds));

            // Act
            MatchDelivery delivery = scenario.Registry.TryCatchFugitiveKitten(
                scenario.MatchCallerOf(scenario.CreatorUserId));

            // Assert
            FugitiveKittenSnapshot kitten = scenario.Registry.FindFugitiveKitten(
                scenario.MatchId, scenario.CreatorUserId);
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(expectedCaught, kitten.Outcome == FugitiveKittenOutcome.Caught);
            Assert.Equal(expectedWinnerUserId, kitten.WinnerUserId);
            Assert.Equal(expectedVersion, kitten.Version);
            Assert.Equal(expectedViewCount, delivery.ViewDeliveries.Count);
        }

        [Fact]
        public void TryCatchFugitiveKitten_WhenAnotherPlayerAlreadyCaughtIt_KeepsTheFirstWinnerAndChangesNothing()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(900));
            scenario.Registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(scenario.UserIds[0]));

            // Act
            MatchDelivery second = scenario.Registry.TryCatchFugitiveKitten(
                scenario.MatchCallerOf(scenario.UserIds[1]));

            // Assert
            FugitiveKittenSnapshot kitten = scenario.Registry.FindFugitiveKitten(
                scenario.MatchId, scenario.CreatorUserId);
            Assert.Equal(scenario.UserIds[0], kitten.WinnerUserId);
            Assert.Equal(3, kitten.Version);
            Assert.Null(second.ErrorKey);
            Assert.Empty(second.ViewDeliveries);
        }

        [Fact]
        public void TryCatchFugitiveKitten_WhenCaught_GrantsTheExtraLifeOnlyToTheWinner()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(900));

            // Act
            scenario.Registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(scenario.UserIds[0]));
            scenario.Registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(scenario.UserIds[1]));

            // Assert
            MatchState match = ReadMatchStateByReflection(scenario);
            Assert.True(match.Players[0].HasExtraLife);
            Assert.False(match.Players[1].HasExtraLife);
            Assert.False(match.Players[2].HasExtraLife);
        }

        [Fact]
        public void PlayerActions_WhileTheKittenIsRunning_AreRejectedWithFugitiveKittenInProgress()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;

            // Act
            MatchDelivery placement = scenario.Place(userId, MatchCardKind.Safe);
            MatchDelivery bet = scenario.PlaceBet(userId, 1);
            MatchDelivery withdrawal = scenario.Withdraw(userId);
            MatchDelivery reveal = scenario.Reveal(userId, 1);
            MatchDelivery choice = scenario.Choose(userId, 0);

            // Assert
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress, placement.ErrorKey);
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress, bet.ErrorKey);
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress, withdrawal.ErrorKey);
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress, reveal.ErrorKey);
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress, choice.ErrorKey);
        }

        /// <summary>
        /// A lost match link leaves a reconnection pending while the kitten, paused by that loss, is still
        /// unresolved. Both blocking conditions coexist, and the reconnection one prevails for every game action.
        /// </summary>
        [Fact]
        public void PlayerActions_WithPendingReconnectionAndUnresolvedKitten_AreRejectedWithReconnectionPending()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            int userId = scenario.CreatorUserId;
            Assert.True(scenario.CloseMatchChannelOf(scenario.UserIds[1]));
            FugitiveKittenSnapshot kittenBefore = scenario.Registry.FindFugitiveKitten(scenario.MatchId, userId);
            Assert.Equal(FugitiveKittenOutcome.InProgress, kittenBefore.Outcome);
            Assert.True(kittenBefore.PausedAtUtc.HasValue);

            // Act
            MatchDelivery placement = scenario.Place(userId, MatchCardKind.Safe);
            MatchDelivery bet = scenario.PlaceBet(userId, 1);
            MatchDelivery withdrawal = scenario.Withdraw(userId);
            MatchDelivery reveal = scenario.Reveal(userId, 1);
            MatchDelivery choice = scenario.Choose(userId, 0);

            // Assert
            MatchDelivery[] attempts = { placement, bet, withdrawal, reveal, choice };
            Assert.All(attempts, attempt => Assert.Equal("Match_ReconnectionPending", attempt.ErrorKey));
            Assert.All(attempts, attempt => Assert.Empty(attempt.ViewDeliveries));
            FugitiveKittenSnapshot kittenAfter = scenario.Registry.FindFugitiveKitten(scenario.MatchId, userId);
            Assert.Equal(FugitiveKittenOutcome.InProgress, kittenAfter.Outcome);
            Assert.Equal(kittenBefore.Version, kittenAfter.Version);
        }

        [Fact]
        public void PlaceCard_AfterTheKittenWindowEnds_ResolvesTheKittenAndNotifiesEveryoneEvenIfTheActionIsRejected()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromSeconds(15));

            // Act
            MatchDelivery rejected = scenario.Place(scenario.UserIds[1], MatchCardKind.Safe);

            // Assert
            Assert.Equal(MatchErrorKeys.NotYourTurn, rejected.ErrorKey);
            Assert.Equal(3, rejected.ViewDeliveries.Count);
            Assert.Equal(FugitiveKittenOutcome.Escaped,
                scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId).Outcome);
        }

        [Fact]
        public void CheckFugitiveKittenDeadline_AfterTheKittenEscaped_ReturnsOnlyTheRequesterViewWithoutNewVersion()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromSeconds(15));
            scenario.Place(scenario.UserIds[1], MatchCardKind.Safe);

            // Act
            MatchDelivery recovered = scenario.Registry.CheckFugitiveKittenDeadline(
                scenario.MatchCallerOf(scenario.CreatorUserId));

            // Assert
            MatchViewDelivery ownView = Assert.Single(recovered.ViewDeliveries);
            Assert.Same(scenario.CallbackOf(scenario.CreatorUserId), ownView.Callback);
            Assert.Equal(3, ownView.MatchView.Version);
            Assert.Equal(FugitiveKittenResult.Escaped, ownView.MatchView.FugitiveKitten.Result);
            Assert.Equal(3, scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId).Version);
        }

        [Fact]
        public void PlaceCard_AfterTheKittenEscaped_IsAcceptedAgain()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromSeconds(15));
            scenario.Registry.CheckFugitiveKittenDeadline(scenario.MatchCallerOf(scenario.CreatorUserId));

            // Act
            MatchDelivery placement = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.Null(placement.ErrorKey);
        }

        [Fact]
        public void Deadline_AfterCaught_ReturnsOnlyRequesterViewWithoutChangingVersion()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(900));
            scenario.Registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(scenario.CreatorUserId));
            int requester = scenario.UserIds[1];
            MatchDelivery recovered = scenario.Registry.CheckFugitiveKittenDeadline(scenario.MatchCallerOf(requester));
            MatchViewDelivery view = Assert.Single(recovered.ViewDeliveries);
            Assert.Same(scenario.CallbackOf(requester), view.Callback);
            Assert.Equal(3, view.MatchView.Version);
            Assert.Equal(FugitiveKittenResult.Caught, view.MatchView.FugitiveKitten.Result);
            Assert.Equal(scenario.CreatorUserId, view.MatchView.FugitiveKitten.WinnerUserId);
            Assert.Equal(3, scenario.Registry.FindFugitiveKitten(scenario.MatchId, requester).Version);
        }

        [Fact]
        public void PublicView_IsIndependentAndSerializable()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromSeconds(3));
            MatchView first = scenario.ReadViewAs(scenario.UserIds[0]);
            Assert.Equal(scenario.Clock.NowUtc, first.FugitiveKitten.ServerTimeUtc);
            Assert.Equal(TimeSpan.FromSeconds(3),
                first.FugitiveKitten.ServerTimeUtc - first.FugitiveKitten.StartedAtUtc);
            MatchView second = scenario.ReadViewAs(scenario.UserIds[1]);
            Assert.NotSame(first.FugitiveKitten, second.FugitiveKitten);
            Assert.NotSame(first.FugitiveKitten.Segments, second.FugitiveKitten.Segments);
            first.FugitiveKitten.Segments[0].Kind = "changed";
            Assert.Equal("Start", second.FugitiveKitten.Segments[0].Kind);
            scenario.Registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(scenario.CreatorUserId));
            MatchView caught = scenario.ReadViewAs(scenario.CreatorUserId);
            Assert.Equal(FugitiveKittenResult.Caught, caught.FugitiveKitten.Result);
            Assert.Equal(scenario.CreatorUserId, caught.FugitiveKitten.WinnerUserId);
            Assert.Equal("Player1", caught.FugitiveKitten.WinnerNickname);
            DataContractSerializer serializer = new DataContractSerializer(typeof(MatchView));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, caught);
                stream.Position = 0;
                MatchView copy = (MatchView)serializer.ReadObject(stream);
                Assert.Equal(caught.Version, copy.Version);
                Assert.Equal(caught.FugitiveKitten.ServerTimeUtc, copy.FugitiveKitten.ServerTimeUtc);
                Assert.Equal(FugitiveKittenResult.Caught, copy.FugitiveKitten.Result);
                Assert.Equal(15, copy.FugitiveKitten.Segments.Count);
            }
        }

        [Fact]
        public void TryCatchFugitiveKitten_WithInvalidSession_RejectsWithInvalidSessionAndDoesNotChangeState()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(900));

            // Act
            MatchDelivery result = scenario.Registry.TryCatchFugitiveKitten(
                MatchPlayScenario.CallerWithToken("invalid"));

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.ErrorKey);
            Assert.Empty(result.ViewDeliveries);
            Assert.Equal(2, scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId).Version);
        }

        /// <summary>
        /// The extra life is not exposed by any public view of the match, so this is the only place that reads the
        /// private match store. It is used by one test; if the match gets a public way to read it, replace this.
        /// </summary>
        private static MatchState ReadMatchStateByReflection(MatchPlayScenario scenario)
        {
            FieldInfo matchesField = typeof(SessionRegistry).GetField(
                "_matchesById", BindingFlags.Instance | BindingFlags.NonPublic);
            IDictionary matches = (IDictionary)matchesField.GetValue(scenario.Registry);
            object record = matches[scenario.MatchId];

            return (MatchState)record.GetType().GetProperty("MatchState").GetValue(record);
        }
    }
}
