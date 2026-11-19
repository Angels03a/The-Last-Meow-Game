using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization;
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
        [InlineData(4, false)]
        [InlineData(6, false)]
        [InlineData(8, true)]
        public void Start_InitializesOnlyEightCards(int cards, bool expected)
        {
            MatchPlayScenario scenario = new MatchPlayScenario(3, cards);
            FugitiveKittenSnapshot state = scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId);
            Assert.Equal(expected, state != null);
            if (expected)
            {
                Assert.Equal(scenario.Clock.NowUtc, state.StartedAtUtc);
                Assert.Equal(TimeSpan.FromSeconds(15), state.ExpiresAtUtc - state.StartedAtUtc);
                scenario.Subscribe(scenario.CreatorUserId);
                scenario.Clock.Advance(TimeSpan.FromMilliseconds(100));
                scenario.Subscribe(scenario.CreatorUserId);
                Assert.Equal(state.StartedAtUtc,
                    scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId).StartedAtUtc);
            }
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
        [InlineData(899, false)]
        [InlineData(900, true)]
        [InlineData(1950, true)]
        [InlineData(1951, false)]
        [InlineData(14250, true)]
        [InlineData(14251, false)]
        [InlineData(15000, false)]
        public void Attempt_UsesServerTime(int milliseconds, bool catches)
        {
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
            MatchDelivery delivery = scenario.Registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(scenario.CreatorUserId));
            FugitiveKittenSnapshot state = scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId);
            Assert.Null(delivery.ErrorKey);
            Assert.Equal(catches, state.Outcome == FugitiveKittenOutcome.Caught);
            Assert.Equal(catches ? scenario.CreatorUserId : (int?)null, state.WinnerUserId);
            Assert.Equal(catches || milliseconds >= 15000 ? 3 : 2, state.Version);
            Assert.Equal(catches || milliseconds >= 15000 ? 3 : 0, delivery.ViewDeliveries.Count);
        }

        [Fact]
        public void TwoAttempts_FirstWinsAndSecondDoesNothing()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(900));
            scenario.Registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(scenario.UserIds[0]));
            MatchDelivery second = scenario.Registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(scenario.UserIds[1]));
            FugitiveKittenSnapshot state = scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId);
            Assert.Equal(scenario.UserIds[0], state.WinnerUserId);
            Assert.Equal(3, state.Version);
            Assert.Empty(second.ViewDeliveries);
            FieldInfo matchesField = typeof(SessionRegistry).GetField("_matchesById",
                BindingFlags.Instance | BindingFlags.NonPublic);
            IDictionary matches = (IDictionary)matchesField.GetValue(scenario.Registry);
            object record = matches[scenario.MatchId];
            MatchState match = (MatchState)record.GetType().GetProperty("MatchState").GetValue(record);
            Assert.True(match.Players[0].HasExtraLife);
            Assert.False(match.Players[1].HasExtraLife);
            Assert.False(match.Players[2].HasExtraLife);
        }

        [Fact]
        public void Deadline_ResolvesOnceAndNotifiesEvenIfActionFails()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress,
                scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe).ErrorKey);
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress, scenario.PlaceBet(scenario.CreatorUserId, 1).ErrorKey);
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress, scenario.Withdraw(scenario.CreatorUserId).ErrorKey);
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress, scenario.Reveal(scenario.CreatorUserId, 1).ErrorKey);
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress, scenario.Choose(scenario.CreatorUserId, 0).ErrorKey);
            scenario.Clock.Advance(TimeSpan.FromSeconds(15));
            MatchDelivery rejected = scenario.Place(scenario.UserIds[1], MatchCardKind.Safe);
            Assert.NotNull(rejected.ErrorKey);
            Assert.Equal(3, rejected.ViewDeliveries.Count);
            Assert.Equal(FugitiveKittenOutcome.Escaped,
                scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId).Outcome);
            MatchDelivery recovered = scenario.Registry.CheckFugitiveKittenDeadline(
                scenario.MatchCallerOf(scenario.CreatorUserId));
            MatchViewDelivery ownView = Assert.Single(recovered.ViewDeliveries);
            Assert.Same(scenario.CallbackOf(scenario.CreatorUserId), ownView.Callback);
            Assert.Equal(3, ownView.MatchView.Version);
            Assert.Equal(FugitiveKittenResult.Escaped, ownView.MatchView.FugitiveKitten.Result);
            Assert.Null(scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe).ErrorKey);
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
        public void Attempt_InvalidSession_DoesNotChangeState()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(3, 8);
            scenario.SubscribeAll();
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(900));
            MatchDelivery result = scenario.Registry.TryCatchFugitiveKitten(MatchPlayScenario.CallerWithToken("invalid"));
            Assert.NotNull(result.ErrorKey);
            Assert.Equal(2, scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId).Version);
        }
    }
}
