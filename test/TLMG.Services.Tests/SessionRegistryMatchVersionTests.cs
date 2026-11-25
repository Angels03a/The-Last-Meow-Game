using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Characterization tests for the match version: they record when MatchRecord.Version rises, how many views are
    /// published for each transition and who receives them. They describe the current behavior and do not impose a
    /// new rule: a transition does not always raise the version exactly once per operation (several lost links in a
    /// single heartbeat sweep raise it once, while the same losses reported one by one raise it once each).
    /// The clock is manual and nothing waits: the server has no timers (D-54).
    /// </summary>
    public class SessionRegistryMatchVersionTests
    {
        private const int MinimumPlayerCount = 3;
        private const int EightCardsPerPlayer = 8;
        private const int OneVersion = 1;
        private const int TwoVersions = 2;
        private const int ThreeVersions = 3;
        private const int TwoSubscribers = 2;
        private const int FirstPlayerIndex = 0;
        private const int SecondPlayerIndex = 1;
        private const int ThirdPlayerIndex = 2;
        private const int FirstCardIndex = 0;

        // Timings mirrored from SessionRegistry, where they are private: link silence (9 s), reconnection grace and
        // kitten limit (15 s each).
        private const int PulseGapSeconds = 3;
        private const int JustPastLinkSilenceMilliseconds = 3100;
        private const int ReconnectGraceSeconds = 15;
        private const int FugitiveKittenLimitSeconds = 15;
        private const int HeartbeatPeriodSeconds = 2;
        private const int HalfSecondMilliseconds = 500;
        private const int CatchableMilliseconds = 900;
        private const int SecondsBeforePunishedPlayerLeaves = 4;
        private const int SecondsUntilPunishedPlayerReturns = 8;
        private const int SecondsBeforeRemainingTimeEnds = 5;
        private const int SecondsPastRemainingTime = 2;
        private const string ReconnectionPendingErrorKey = "Match_ReconnectionPending";

        private static readonly TimeSpan _punishmentLimit =
            TimeSpan.FromSeconds(ValidationRules.PunishmentChoiceSeconds);

        /// <summary>Reading the view again without a state transition never raises the version.</summary>
        [Fact]
        public void SubscribeToMatch_RepeatedWithoutTransition_KeepsVersionAndDeliversOnlyOwnView()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.Place(scenario.UserIds[FirstPlayerIndex], MatchCardKind.Safe);
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[SecondPlayerIndex]);

            // Act
            MatchDelivery firstRead = scenario.Subscribe(scenario.UserIds[SecondPlayerIndex]);
            MatchDelivery secondRead = scenario.Subscribe(scenario.UserIds[SecondPlayerIndex]);

            // Assert
            Assert.Single(firstRead.ViewDeliveries);
            Assert.Single(secondRead.ViewDeliveries);
            Assert.Equal(versionBefore, firstRead.ViewDeliveries[0].MatchView.Version);
            Assert.Equal(versionBefore, secondRead.ViewDeliveries[0].MatchView.Version);
        }

        /// <summary>
        /// A rejected action still raises the version once, and publishes the views, when an expired punishment
        /// deadline changed the state before the action was evaluated.
        /// </summary>
        [Fact]
        public void ChoosePunishmentCard_RejectedAfterExpiry_RaisesVersionOnceAndPublishesViews()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[ThirdPlayerIndex]);
            scenario.RandomSource.Enqueue(FirstCardIndex);
            scenario.Clock.Advance(_punishmentLimit);

            // Act
            MatchDelivery delivery = scenario.Choose(scenario.UserIds[SecondPlayerIndex], FirstCardIndex);

            // Assert
            Assert.Equal(MatchErrorKeys.NoPunishmentPending, delivery.ErrorKey);
            Assert.Equal(MinimumPlayerCount, delivery.ViewDeliveries.Count);
            AssertVersionOfAll(delivery.ViewDeliveries, versionBefore + OneVersion);
        }

        /// <summary>A heartbeat sweep applies an expired punishment once and notifies every subscriber once.</summary>
        [Fact]
        public void EvaluateConnectionDeadlines_WithExpiredPunishment_RaisesVersionOnceAndNotifiesSubscribers()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[ThirdPlayerIndex]);
            scenario.RandomSource.Enqueue(FirstCardIndex);
            MatchLinkKeeper keeper = new MatchLinkKeeper(scenario);
            keeper.PulseEverySecondPair(ValidationRules.PunishmentChoiceSeconds, scenario.UserIds);
            Assert.All(scenario.UserIds, userId => Assert.Empty(scenario.CallbackOf(userId).Views));

            // Act
            IReadOnlyList<Action> notifications = scenario.Registry.EvaluateConnectionDeadlines();
            MatchLinkKeeper.RunAll(notifications);

            // Assert
            Assert.Equal(MinimumPlayerCount, notifications.Count);
            Assert.All(scenario.UserIds, userId => Assert.Single(scenario.CallbackOf(userId).Views));
            Assert.All(scenario.UserIds, userId => Assert.Equal(
                versionBefore + OneVersion, scenario.CallbackOf(userId).LastView.Version));
            Assert.NotEqual(MatchRoundPhase.PunishmentPending, scenario.CallbackOf(scenario.UserIds[FirstPlayerIndex])
                .LastView.Phase);
        }

        /// <summary>A sweep with every link alive and no deadline reached changes and publishes nothing.</summary>
        [Fact]
        public void EvaluateConnectionDeadlines_WithoutChanges_KeepsVersionAndPublishesNothing()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[FirstPlayerIndex]);
            MatchLinkKeeper keeper = new MatchLinkKeeper(scenario);
            keeper.PulseEverySecondPair(HeartbeatPeriodSeconds * PulseGapSeconds, scenario.UserIds);

            // Act
            IReadOnlyList<Action> notifications = scenario.Registry.EvaluateConnectionDeadlines();

            // Assert
            Assert.Empty(notifications);
            Assert.All(scenario.UserIds, userId => Assert.Empty(scenario.CallbackOf(userId).Views));
            Assert.Equal(versionBefore, scenario.ReadVersionAs(scenario.UserIds[FirstPlayerIndex]));
        }

        /// <summary>
        /// Two links that go silent in the same sweep open their graces together: the version rises once and the
        /// only remaining subscriber receives a single view.
        /// </summary>
        [Fact]
        public void EvaluateConnectionDeadlines_WithTwoLinksSilentInTheSameSweep_RaisesVersionOnce()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[FirstPlayerIndex]);
            MatchLinkKeeper keeper = new MatchLinkKeeper(scenario);
            scenario.Clock.Advance(TimeSpan.FromSeconds(PulseGapSeconds));
            keeper.Pulse(scenario.UserIds[FirstPlayerIndex]);
            scenario.Clock.Advance(TimeSpan.FromSeconds(PulseGapSeconds));
            keeper.Pulse(scenario.UserIds[FirstPlayerIndex]);
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(JustPastLinkSilenceMilliseconds));

            // Act
            IReadOnlyList<Action> notifications = scenario.Registry.EvaluateConnectionDeadlines();
            MatchLinkKeeper.RunAll(notifications);

            // Assert
            Assert.Single(notifications);
            MatchCallbackStub remaining = scenario.CallbackOf(scenario.UserIds[FirstPlayerIndex]);
            Assert.Single(remaining.Views);
            Assert.Equal(versionBefore + OneVersion, remaining.LastView.Version);
            Assert.Empty(scenario.CallbackOf(scenario.UserIds[SecondPlayerIndex]).Views);
            Assert.Empty(scenario.CallbackOf(scenario.UserIds[ThirdPlayerIndex]).Views);
        }

        /// <summary>The same two losses, reported one by one, raise the version once for each.</summary>
        [Fact]
        public void RemoveMatchChannel_ForTwoPlayersOneByOne_RaisesVersionOncePerLoss()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int versionBefore = scenario.ReadVersionAs(scenario.UserIds[FirstPlayerIndex]);

            // Act
            IReadOnlyList<MatchViewDelivery> firstLoss = CloseMatchChannel(scenario, SecondPlayerIndex);
            IReadOnlyList<MatchViewDelivery> secondLoss = CloseMatchChannel(scenario, ThirdPlayerIndex);

            // Assert
            Assert.Equal(TwoSubscribers, firstLoss.Count);
            AssertVersionOfAll(firstLoss, versionBefore + OneVersion);
            Assert.Single(secondLoss);
            AssertVersionOfAll(secondLoss, versionBefore + TwoVersions);
        }

        /// <summary>
        /// When a reconnection grace expires, the sweep removes the player once: one version, one view for each of
        /// the players whose links are alive, and later sweeps change nothing.
        /// </summary>
        [Fact]
        public void EvaluateConnectionDeadlines_WhenGraceExpires_RaisesVersionOnceAndMarksPlayerAsGone()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int lostUserId = scenario.UserIds[SecondPlayerIndex];
            MatchLinkKeeper keeper = new MatchLinkKeeper(scenario);
            keeper.Pulse(scenario.UserIds[FirstPlayerIndex], scenario.UserIds[ThirdPlayerIndex]);
            CloseMatchChannel(scenario, SecondPlayerIndex);
            int versionAfterLoss = scenario.ReadVersionAs(scenario.UserIds[FirstPlayerIndex]);
            keeper.PulseEverySecondPair(ReconnectGraceSeconds - 1,
                new[] { scenario.UserIds[FirstPlayerIndex], scenario.UserIds[ThirdPlayerIndex] });
            Assert.All(scenario.UserIds, userId => Assert.Empty(scenario.CallbackOf(userId).Views));
            scenario.Clock.Advance(TimeSpan.FromSeconds(1));

            // Act
            IReadOnlyList<Action> notifications = scenario.Registry.EvaluateConnectionDeadlines();
            MatchLinkKeeper.RunAll(notifications);
            IReadOnlyList<Action> repeatedSweep = scenario.Registry.EvaluateConnectionDeadlines();

            // Assert
            MatchView view = scenario.CallbackOf(scenario.UserIds[FirstPlayerIndex]).LastView;
            Assert.Equal(versionAfterLoss + OneVersion, view.Version);
            Assert.True(view.Players.Single(player => player.UserId == lostUserId).HasLeftMatch);
            Assert.Empty(repeatedSweep);
            Assert.Equal(versionAfterLoss + OneVersion, scenario.ReadVersionAs(scenario.UserIds[FirstPlayerIndex]));
        }

        /// <summary>
        /// Losing the punished player's link suspends the punishment deadline, and returning resumes it with the
        /// time that was left. Each real transition (loss, return, final expiry) raises the version once.
        /// </summary>
        [Fact]
        public void PunishedPlayerLink_LostAndRestored_SuspendsAndResumesTheDeadlineWithOneVersionPerTransition()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            MatchView before = scenario.ReadViewAs(scenario.UserIds[ThirdPlayerIndex]);
            int punishedUserId = before.Punishment.PunishedUserId;
            int chooserUserId = before.Punishment.ChooserUserId;
            int bystanderUserId = scenario.UserIds.Single(
                userId => userId != punishedUserId && userId != chooserUserId);
            scenario.Clock.Advance(TimeSpan.FromSeconds(SecondsBeforePunishedPlayerLeaves));

            // Act and assert: the loss suspends the deadline.
            IReadOnlyList<MatchViewDelivery> loss = CloseMatchChannel(
                scenario, scenario.UserIds.ToList().IndexOf(punishedUserId));
            Assert.Equal(TwoSubscribers, loss.Count);
            AssertVersionOfAll(loss, before.Version + OneVersion);
            scenario.Clock.Advance(TimeSpan.FromSeconds(SecondsUntilPunishedPlayerReturns));
            MatchDelivery chooserAttempt = scenario.Choose(chooserUserId, FirstCardIndex);
            Assert.NotNull(chooserAttempt.ErrorKey);
            Assert.Empty(chooserAttempt.ViewDeliveries);

            // Act and assert: returning resumes it with the remaining time (limit minus the time already elapsed).
            MatchDelivery returned = scenario.Subscribe(punishedUserId);
            Assert.Null(returned.ErrorKey);
            Assert.Equal(MinimumPlayerCount, returned.ViewDeliveries.Count);
            AssertVersionOfAll(returned.ViewDeliveries, before.Version + TwoVersions);
            AssertPunishmentPending(returned.ViewDeliveries, true);
            scenario.Clock.Advance(TimeSpan.FromSeconds(SecondsBeforeRemainingTimeEnds));
            MatchView stillPending = scenario.ReadViewAs(bystanderUserId);
            Assert.Equal(before.Version + TwoVersions, stillPending.Version);
            Assert.Equal(MatchRoundPhase.PunishmentPending, stillPending.Phase);

            // Act and assert: past the remaining time the deadline expires and the version rises once.
            scenario.RandomSource.Enqueue(FirstCardIndex);
            scenario.Clock.Advance(TimeSpan.FromSeconds(SecondsPastRemainingTime));
            MatchDelivery expired = scenario.Subscribe(bystanderUserId);
            Assert.Equal(MinimumPlayerCount, expired.ViewDeliveries.Count);
            AssertVersionOfAll(expired.ViewDeliveries, before.Version + ThreeVersions);
            AssertPunishmentPending(expired.ViewDeliveries, false);
        }

        /// <summary>
        /// Losing the link of someone who is not the punished player does not suspend the punishment: it expires at
        /// its original deadline, and an action evaluated afterwards is rejected by the pause yet still publishes the
        /// views that the expiry produced.
        /// </summary>
        [Fact]
        public void ChooserLink_Lost_DoesNotSuspendThePunishmentDeadline()
        {
            // Arrange
            MatchPlayScenario scenario = StartBlindPunishment();
            MatchView before = scenario.ReadViewAs(scenario.UserIds[ThirdPlayerIndex]);
            int chooserUserId = before.Punishment.ChooserUserId;
            int bystanderUserId = scenario.UserIds.Single(userId => userId != before.Punishment.PunishedUserId
                && userId != chooserUserId);
            scenario.RandomSource.Enqueue(FirstCardIndex);
            scenario.Clock.Advance(TimeSpan.FromSeconds(SecondsBeforePunishedPlayerLeaves));
            CloseMatchChannel(scenario, scenario.UserIds.ToList().IndexOf(chooserUserId));
            scenario.Clock.Advance(TimeSpan.FromSeconds(SecondsUntilPunishedPlayerReturns));

            // Act
            MatchDelivery attempt = scenario.Place(bystanderUserId, MatchCardKind.Safe);

            // Assert: loss (one version) plus expiry (one version); the action itself is rejected by the pause.
            Assert.Equal(ReconnectionPendingErrorKey, attempt.ErrorKey);
            Assert.Equal(TwoSubscribers, attempt.ViewDeliveries.Count);
            AssertVersionOfAll(attempt.ViewDeliveries, before.Version + TwoVersions);
            AssertPunishmentPending(attempt.ViewDeliveries, false);
        }

        /// <summary>FugitiveKitten: the version rises only when the last first link arrives.</summary>
        [Fact]
        public void SubscribeToMatch_WithFugitiveKitten_RaisesVersionOnlyWhenTheLastFirstLinkArrives()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount, EightCardsPerPlayer);

            // Act
            scenario.Subscribe(scenario.UserIds[FirstPlayerIndex]);
            int versionAfterFirstLink = KittenVersion(scenario);
            scenario.Subscribe(scenario.UserIds[SecondPlayerIndex]);
            int versionAfterSecondLink = KittenVersion(scenario);
            MatchDelivery lastLink = scenario.Subscribe(scenario.UserIds[ThirdPlayerIndex]);

            // Assert
            Assert.Equal(MatchPlayScenario.InitialVersion, versionAfterFirstLink);
            Assert.Equal(MatchPlayScenario.InitialVersion, versionAfterSecondLink);
            Assert.Equal(MinimumPlayerCount, lastLink.ViewDeliveries.Count);
            AssertVersionOfAll(lastLink.ViewDeliveries, MatchPlayScenario.InitialVersion + OneVersion);
            Assert.Equal(MatchPlayScenario.InitialVersion + OneVersion, KittenVersion(scenario));
        }

        /// <summary>FugitiveKitten: a valid catch publishes the caught result and a new version to all.</summary>
        [Fact]
        public void TryCatchFugitiveKitten_WithValidCatch_PublishesTheNewVersionToEverySubscriber()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount, EightCardsPerPlayer);
            scenario.SubscribeAll();
            int versionBefore = KittenVersion(scenario);
            int catcherUserId = scenario.UserIds[FirstPlayerIndex];
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(CatchableMilliseconds));

            // Act
            MatchDelivery caught = scenario.Registry.TryCatchFugitiveKitten(scenario.MatchCallerOf(catcherUserId));

            // Assert
            Assert.Null(caught.ErrorKey);
            Assert.Equal(
                scenario.UserIds.Select(userId => scenario.CallbackOf(userId)),
                caught.ViewDeliveries.Select(item => item.Callback));
            AssertVersionOfAll(caught.ViewDeliveries, versionBefore + OneVersion);
            Assert.All(caught.ViewDeliveries, item => Assert.Equal(
                FugitiveKittenResult.Caught, item.MatchView.FugitiveKitten.Result));
            Assert.All(caught.ViewDeliveries, item => Assert.Equal(
                catcherUserId, item.MatchView.FugitiveKitten.WinnerUserId));
            Assert.Equal(versionBefore + OneVersion, KittenVersion(scenario));
        }

        /// <summary>FugitiveKitten: reaching the limit raises the version once; a repeated check does not.</summary>
        [Fact]
        public void FugitiveKitten_DeadlineReached_RaisesVersionOnceAndRepeatedCheckOnlyReadsOwnView()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount, EightCardsPerPlayer);
            scenario.SubscribeAll();
            int versionBefore = KittenVersion(scenario);
            scenario.Clock.Advance(TimeSpan.FromSeconds(FugitiveKittenLimitSeconds));
            SessionChannelCaller caller = scenario.MatchCallerOf(scenario.UserIds[FirstPlayerIndex]);

            // Act
            MatchDelivery first = scenario.Registry.CheckFugitiveKittenDeadline(caller);
            MatchDelivery repeated = scenario.Registry.CheckFugitiveKittenDeadline(caller);

            // Assert
            Assert.Equal(MinimumPlayerCount, first.ViewDeliveries.Count);
            AssertVersionOfAll(first.ViewDeliveries, versionBefore + OneVersion);
            Assert.Single(repeated.ViewDeliveries);
            Assert.Equal(versionBefore + OneVersion, repeated.ViewDeliveries[0].MatchView.Version);
            Assert.Equal(versionBefore + OneVersion, KittenVersion(scenario));
        }

        /// <summary>FugitiveKitten: a lost and restored link pauses and resumes the clock.</summary>
        [Fact]
        public void FugitiveKitten_LinkLostAndRestored_RaisesVersionOncePerTransition()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount, EightCardsPerPlayer);
            scenario.SubscribeAll();
            int versionBefore = KittenVersion(scenario);
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(HalfSecondMilliseconds));

            // Act and assert: the loss pauses the clock.
            IReadOnlyList<MatchViewDelivery> loss = CloseMatchChannel(scenario, SecondPlayerIndex);
            Assert.Equal(TwoSubscribers, loss.Count);
            Assert.Equal(versionBefore + OneVersion, KittenVersion(scenario));

            // Act and assert: the return resumes it.
            scenario.Clock.Advance(TimeSpan.FromSeconds(PulseGapSeconds));
            MatchDelivery returned = scenario.Subscribe(scenario.UserIds[SecondPlayerIndex]);
            Assert.Equal(MinimumPlayerCount, returned.ViewDeliveries.Count);
            Assert.Equal(versionBefore + TwoVersions, KittenVersion(scenario));
        }

        /// <summary>An action while the kitten game is in progress is rejected without changing the version.</summary>
        [Fact]
        public void PlaceCard_WhileFugitiveKittenInProgress_IsRejectedWithoutVersionChange()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount, EightCardsPerPlayer);
            scenario.SubscribeAll();
            int versionBefore = KittenVersion(scenario);

            // Act
            MatchDelivery delivery = scenario.Place(scenario.UserIds[FirstPlayerIndex], MatchCardKind.Safe);

            // Assert
            Assert.Equal(MatchErrorKeys.FugitiveKittenInProgress, delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
            Assert.Equal(versionBefore, KittenVersion(scenario));
        }

        /// <summary>
        /// Replacing a session during a match raises the version once; asking for the recovery context raises
        /// nothing; the rebind inside the grace raises it once more, and repeating the rebind changes nothing.
        /// </summary>
        [Fact]
        public void SessionReplacement_RecoveryContextAndRebind_RaiseVersionOnlyOnReplacementAndFirstRebind()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int userId = scenario.UserIds[FirstPlayerIndex];
            int versionBefore = scenario.ReadVersionAs(userId);
            string newToken = scenario.Registry.StartRegisteredSession(userId, "Replacement", out SessionEnd replaced);
            MatchCallbackStub newCallback = new MatchCallbackStub();
            SessionChannelCaller newCaller = new SessionChannelCaller(newToken, "replacement-match");

            // Act
            bool hasContext = scenario.Registry.GetRecoveryContext(newToken).OperationResult.IsSuccess;
            scenario.Clock.Advance(TimeSpan.FromSeconds(PulseGapSeconds));
            MatchDelivery rebind = scenario.Registry.SubscribeToMatch(newCaller, scenario.MatchId, newCallback);
            MatchDelivery repeatedRebind = scenario.Registry.SubscribeToMatch(newCaller, scenario.MatchId, newCallback);

            // Assert
            Assert.Equal(TwoSubscribers, replaced.MatchViews.Count);
            AssertVersionOfAll(replaced.MatchViews, versionBefore + OneVersion);
            Assert.True(hasContext);
            Assert.Equal(MinimumPlayerCount, rebind.ViewDeliveries.Count);
            AssertVersionOfAll(rebind.ViewDeliveries, versionBefore + TwoVersions);
            Assert.Single(repeatedRebind.ViewDeliveries);
            Assert.Equal(versionBefore + TwoVersions, repeatedRebind.ViewDeliveries[0].MatchView.Version);
        }

        /// <summary>
        /// The client replaces the view it shows only with one of a strictly greater version
        /// (MatchViewVersionHelper.ShouldDisplay), so an equal or older view is ignored. Across a mixed sequence of
        /// transitions, the versions that each subscriber receives never go backwards. Repeated publications of the
        /// same version are allowed; this test does not require an increment on every delivery.
        /// </summary>
        [Fact]
        public void RegistryOperations_InAMixedSequence_DeliverVersionsThatNeverDecreaseForAnySubscriber()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            Dictionary<MatchCallbackStub, List<int>> versionsByCallback =
                new Dictionary<MatchCallbackStub, List<int>>();
            CollectSubscriptionOfEveryPlayer(scenario, versionsByCallback);

            // Act
            MatchDelivery accepted = PlaceSafeCard(scenario, FirstPlayerIndex);
            MatchDelivery rejected = PlaceSafeCard(scenario, FirstPlayerIndex);
            IReadOnlyList<MatchViewDelivery> loss = CloseMatchChannel(scenario, ThirdPlayerIndex);
            scenario.Clock.Advance(TimeSpan.FromSeconds(PulseGapSeconds));
            MatchDelivery returned = scenario.Subscribe(scenario.UserIds[ThirdPlayerIndex]);
            MatchDelivery repeated = scenario.Subscribe(scenario.UserIds[ThirdPlayerIndex]);
            MatchDelivery secondAccepted = PlaceSafeCard(scenario, SecondPlayerIndex);
            Collect(versionsByCallback, accepted.ViewDeliveries);
            Collect(versionsByCallback, rejected.ViewDeliveries);
            Collect(versionsByCallback, loss);
            Collect(versionsByCallback, returned.ViewDeliveries);
            Collect(versionsByCallback, repeated.ViewDeliveries);
            Collect(versionsByCallback, secondAccepted.ViewDeliveries);

            // Assert: the operations really did what the sequence assumes, so the invariant is not vacuous.
            Assert.Null(accepted.ErrorKey);
            Assert.Equal(MinimumPlayerCount, accepted.ViewDeliveries.Count);
            Assert.Equal(MatchErrorKeys.NotYourTurn, rejected.ErrorKey);
            Assert.Empty(rejected.ViewDeliveries);
            Assert.Equal(TwoSubscribers, loss.Count);
            Assert.Null(returned.ErrorKey);
            Assert.Equal(MinimumPlayerCount, returned.ViewDeliveries.Count);
            Assert.Null(repeated.ErrorKey);
            Assert.Single(repeated.ViewDeliveries);
            Assert.Null(secondAccepted.ErrorKey);
            Assert.Equal(MinimumPlayerCount, versionsByCallback.Count);
            AssertNeverDecreasing(versionsByCallback);
            Assert.True(versionsByCallback.Values.All(versions => versions.Count > TwoVersions));
        }

        private static MatchPlayScenario StartBlindPunishment()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.ReachBlindPunishment();

            return scenario;
        }

        private static IReadOnlyList<MatchViewDelivery> CloseMatchChannel(MatchPlayScenario scenario, int playerIndex)
        {
            int userId = scenario.UserIds[playerIndex];
            IReadOnlyList<MatchViewDelivery> deliveries;
            bool wasRemoved = scenario.Registry.RemoveMatchChannel(
                MatchPlayScenario.MatchChannelPrefix + userId, scenario.CallbackOf(userId), out deliveries);
            Assert.True(wasRemoved);

            return deliveries;
        }

        private static void AssertVersionOfAll(IEnumerable<MatchViewDelivery> deliveries, int expectedVersion)
        {
            Assert.All(deliveries, item => Assert.Equal(expectedVersion, item.MatchView.Version));
        }

        private static void AssertPunishmentPending(IEnumerable<MatchViewDelivery> deliveries, bool isPending)
        {
            Assert.All(deliveries, item => Assert.Equal(
                isPending, item.MatchView.Phase == MatchRoundPhase.PunishmentPending));
        }

        private static MatchDelivery PlaceSafeCard(MatchPlayScenario scenario, int playerIndex)
        {
            return scenario.Place(scenario.UserIds[playerIndex], MatchCardKind.Safe);
        }

        private static int KittenVersion(MatchPlayScenario scenario)
        {
            return scenario.Registry.FindFugitiveKitten(scenario.MatchId, scenario.CreatorUserId).Version;
        }

        private static void CollectSubscriptionOfEveryPlayer(
            MatchPlayScenario scenario,
            Dictionary<MatchCallbackStub, List<int>> versionsByCallback)
        {
            foreach (int userId in scenario.UserIds)
            {
                MatchDelivery subscription = scenario.Subscribe(userId);
                Assert.Null(subscription.ErrorKey);
                Collect(versionsByCallback, subscription.ViewDeliveries);
            }
        }

        private static void AssertNeverDecreasing(Dictionary<MatchCallbackStub, List<int>> versionsByCallback)
        {
            foreach (List<int> versions in versionsByCallback.Values)
            {
                Assert.Equal(versions.OrderBy(version => version).ToList(), versions);
            }
        }

        private static void Collect(
            Dictionary<MatchCallbackStub, List<int>> versionsByCallback,
            IEnumerable<MatchViewDelivery> deliveries)
        {
            foreach (MatchViewDelivery delivery in deliveries)
            {
                MatchCallbackStub callback = (MatchCallbackStub)delivery.Callback;

                if (!versionsByCallback.ContainsKey(callback))
                {
                    versionsByCallback[callback] = new List<int>();
                }

                versionsByCallback[callback].Add(delivery.MatchView.Version);
            }
        }

        /// <summary>Sends match heartbeats with increasing sequences and runs the notifications they produce.</summary>
        private sealed class MatchLinkKeeper
        {
            private readonly MatchPlayScenario _scenario;
            private readonly Dictionary<int, int> _sequencesByUserId = new Dictionary<int, int>();

            internal MatchLinkKeeper(MatchPlayScenario scenario)
            {
                _scenario = scenario;
            }

            internal static void RunAll(IReadOnlyList<Action> notifications)
            {
                foreach (Action notification in notifications)
                {
                    notification();
                }
            }

            internal void Pulse(params int[] userIds)
            {
                foreach (int userId in userIds)
                {
                    int sequence;
                    _sequencesByUserId.TryGetValue(userId, out sequence);
                    sequence++;
                    _sequencesByUserId[userId] = sequence;
                    SessionChannelCaller caller = _scenario.MatchCallerOf(userId);
                    RunAll(_scenario.Registry.ProcessHeartbeat(
                        caller.SessionToken, caller.ChannelId, HeartbeatChannel.Match, sequence, sequence - 1,
                        IgnoreEcho));
                }
            }

            /// <summary>Advances second by second and pulses on every odd second, so no link goes silent.</summary>
            internal void PulseEverySecondPair(int seconds, IEnumerable<int> userIds)
            {
                int[] pulsingUserIds = userIds.ToArray();

                for (int second = 1; second <= seconds; second++)
                {
                    _scenario.Clock.Advance(TimeSpan.FromSeconds(1));

                    if (second % HeartbeatPeriodSeconds == 1)
                    {
                        Pulse(pulsingUserIds);
                    }
                }
            }

            private static void IgnoreEcho(int sequence, string errorKey)
            {
            }
        }
    }
}
