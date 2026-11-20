using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Match;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Pruebas de suscripción, colocación de cartas y vistas privadas de una partida autorizada.
    /// </summary>
    public class SessionRegistryMatchPlayTests
    {
        private const int MinimumPlayerCount = 3;
        private const int SecondVersion = 2;
        private const int ThirdVersion = 3;
        private const int FatalCatsInHand = 1;
        private const int SafeCardsInHand = 3;
        private const int FirstRoundNumber = 1;
        private const int NoCards = 0;
        private const int OneCard = 1;
        private const int FullHandCount = MatchPlayScenario.CardsPerPlayerCount;
        private const int AfterFirstLapVersion = MatchPlayScenario.InitialVersion + MinimumPlayerCount;
        private const int SurvivorFirstUserId = 6;
        private const string SecondChannelId = "second";

        [Fact]
        public void SubscribeToMatch_WithParticipant_AcceptsAndDeliversOwnView()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);

            // Act
            MatchDelivery delivery = scenario.Subscribe(scenario.CreatorUserId);

            // Assert
            Assert.Null(delivery.ErrorKey);
            MatchViewDelivery viewDelivery = Assert.Single(delivery.ViewDeliveries);
            Assert.Same(scenario.CallbackOf(scenario.CreatorUserId), viewDelivery.Callback);
            MatchView view = viewDelivery.MatchView;
            Assert.Equal(scenario.MatchId, view.MatchId);
            Assert.Equal(MatchPlayScenario.InitialVersion, view.Version);
            Assert.Equal(FirstRoundNumber, view.RoundNumber);
            Assert.Equal(MatchRoundPhase.PlacingCards, view.Phase);
            Assert.Equal(scenario.CreatorUserId, view.PlayerInTurnUserId);
        }

        [Fact]
        public void SubscribeToMatch_WithParticipant_ViewListsPlayersWithCountsInTurnOrder()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);

            // Act
            MatchView view = scenario.Subscribe(scenario.UserIds[1]).ViewDeliveries[0].MatchView;

            // Assert
            Assert.Equal(scenario.UserIds, view.Players.Select(player => player.UserId));
            Assert.All(view.Players, player => Assert.Equal(FullHandCount, player.HandCardCount));
            Assert.All(view.Players, player => Assert.Equal(NoCards, player.PileCardCount));
            Assert.All(view.Players, player => Assert.False(player.IsEliminated));
            Assert.Equal(
                scenario.UserIds.Select(userId => MatchPlayScenario.NicknamePrefix + userId),
                view.Players.Select(player => player.Nickname));
        }

        [Fact]
        public void SubscribeToMatch_WithParticipant_ViewContainsOwnHandInOrder()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);

            // Act
            MatchView view = scenario.Subscribe(scenario.CreatorUserId).ViewDeliveries[0].MatchView;

            // Assert
            Assert.Equal(FatalCatsInHand, view.OwnHandCards.Count(card => card == MatchCardKind.FatalCat));
            Assert.Equal(SafeCardsInHand, view.OwnHandCards.Count(card => card == MatchCardKind.Safe));
            Assert.Empty(view.OwnPileCards);
        }

        [Fact]
        public void SubscribeToMatch_WithSeveralRecipients_EachViewBelongsToItsRecipient()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Act
            MatchView creatorView = scenario.Subscribe(scenario.CreatorUserId).ViewDeliveries[0].MatchView;
            MatchView guestView = scenario.Subscribe(scenario.UserIds[1]).ViewDeliveries[0].MatchView;

            // Assert
            Assert.Equal(new[] { MatchCardKind.Safe }, creatorView.OwnPileCards);
            Assert.Equal(MatchPlayScenario.CardsPerPlayerCount - OneCard, creatorView.OwnHandCards.Count);
            Assert.Empty(guestView.OwnPileCards);
            Assert.Equal(MatchPlayScenario.CardsPerPlayerCount, guestView.OwnHandCards.Count);
        }

        [Fact]
        public void MatchPlayerView_Properties_OnlyRevealedPileCardsCarryCardKinds()
        {
            // Arrange
            PropertyInfo[] properties = typeof(MatchPlayerView).GetProperties();

            // Act
            string[] propertiesWithCards = properties
                .Where(property => property.PropertyType.IsGenericType)
                .Select(property => property.Name)
                .ToArray();

            // Assert: las cartas volteadas son públicas; ninguna otra propiedad del jugador lleva tipos de carta.
            Assert.Equal(new[] { nameof(MatchPlayerView.RevealedPileCards) }, propertiesWithCards);
        }

        [Fact]
        public void SubscribeToMatch_TwiceSequentially_KeepsOneSubscriptionAndSameVersion()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.Subscribe(scenario.CreatorUserId);

            // Act
            MatchDelivery secondDelivery = scenario.Subscribe(scenario.CreatorUserId);
            MatchDelivery placement = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.Equal(MatchPlayScenario.InitialVersion, secondDelivery.ViewDeliveries[0].MatchView.Version);
            Assert.Single(placement.ViewDeliveries);
        }

        [Fact]
        public void SubscribeToMatch_WithNewCallback_ReplacesPreviousCallback()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchCallbackStub oldCallback = new MatchCallbackStub();
            MatchCallbackStub newCallback = new MatchCallbackStub();
            LobbyCaller oldCaller = scenario.MatchCallerOf(scenario.CreatorUserId);
            LobbyCaller newCaller = new LobbyCaller(scenario.TokenOf(scenario.CreatorUserId), SecondChannelId);
            scenario.Registry.SubscribeToMatch(oldCaller, scenario.MatchId, oldCallback);
            scenario.Registry.SubscribeToMatch(newCaller, scenario.MatchId, newCallback);

            // Act
            MatchDelivery placement = scenario.Registry.PlaceCard(newCaller, MatchCardKind.Safe);

            // Assert
            Assert.Null(placement.ErrorKey);
            MatchViewDelivery viewDelivery = Assert.Single(placement.ViewDeliveries);
            Assert.Same(newCallback, viewDelivery.Callback);
        }

        [Fact]
        public void PlaceCard_WithOldChannelAfterReplacement_RejectsWithInvalidSession()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.Subscribe(scenario.CreatorUserId);
            LobbyCaller newCaller = new LobbyCaller(scenario.TokenOf(scenario.CreatorUserId), SecondChannelId);
            scenario.Registry.SubscribeToMatch(newCaller, scenario.MatchId, new MatchCallbackStub());

            // Act
            MatchDelivery delivery = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.Equal(ErrorKeys.Common.InvalidSession, delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
        }

        [Fact]
        public void PlaceCard_OutOfTurn_RejectsWithoutChangingVersion()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int guestUserId = scenario.UserIds[1];

            // Act
            MatchDelivery delivery = scenario.Place(guestUserId, MatchCardKind.Safe);

            // Assert
            Assert.Equal(MatchErrorKeys.NotYourTurn, delivery.ErrorKey);
            Assert.Empty(delivery.ViewDeliveries);
            Assert.Equal(MatchPlayScenario.InitialVersion, scenario.ReadVersionAs(guestUserId));
            Assert.Equal(scenario.CreatorUserId, scenario.ReadViewAs(guestUserId).PlayerInTurnUserId);
        }

        [Fact]
        public void PlaceCard_WithCardAlreadyPlaced_RejectsWithCardNotInHand()
        {
            // Arrange: una vuelta completa; el creador ya colocó su único Gato con Maullido Fatal.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            PlaceInTurnOrder(scenario, MatchCardKind.FatalCat, MinimumPlayerCount);

            // Act
            MatchDelivery delivery = scenario.Place(scenario.CreatorUserId, MatchCardKind.FatalCat);

            // Assert
            Assert.Equal(MatchErrorKeys.CardNotInHand, delivery.ErrorKey);
            Assert.Equal(AfterFirstLapVersion, scenario.ReadVersionAs(scenario.CreatorUserId));
        }

        [Fact]
        public void PlaceCard_WithEmptyHand_RejectsWithHandEmpty()
        {
            // Arrange: todos colocan sus 4 cartas y al creador le vuelve a tocar con la mano vacía.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            PlaceInTurnOrder(scenario, MatchCardKind.FatalCat, MinimumPlayerCount);
            PlaceInTurnOrder(scenario, MatchCardKind.Safe, MinimumPlayerCount * SafeCardsInHand);

            // Act
            MatchDelivery delivery = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.Equal(MatchErrorKeys.HandEmpty, delivery.ErrorKey);
        }

        [Fact]
        public void PlaceCard_InTurn_RaisesVersionAndPassesTurn()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int guestUserId = scenario.UserIds[1];

            // Act
            MatchDelivery delivery = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.Null(delivery.ErrorKey);
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(SecondVersion, item.MatchView.Version));
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(guestUserId, item.MatchView.PlayerInTurnUserId));
        }

        [Fact]
        public void PlaceCard_InTurn_NotifiesEverySubscribedPlayerWithDifferentViews()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();

            // Act
            MatchDelivery delivery = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.Equal(
                scenario.UserIds.Select(userId => scenario.CallbackOf(userId)),
                delivery.ViewDeliveries.Select(item => item.Callback));
            MatchView creatorView = delivery.ViewDeliveries[0].MatchView;
            MatchView guestView = delivery.ViewDeliveries[1].MatchView;
            Assert.Equal(new[] { MatchCardKind.Safe }, creatorView.OwnPileCards);
            Assert.Empty(guestView.OwnPileCards);
            Assert.NotEqual(creatorView.OwnHandCards.Count, guestView.OwnHandCards.Count);
        }

        [Fact]
        public void PlaceCard_InTurn_PublicPartOfEveryViewIsTheSame()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();

            // Act
            MatchDelivery delivery = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            MatchPlayerView creatorEntry = delivery.ViewDeliveries[1].MatchView.Players[0];
            Assert.Equal(OneCard, creatorEntry.PileCardCount);
            Assert.Equal(MatchPlayScenario.CardsPerPlayerCount - OneCard, creatorEntry.HandCardCount);
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(OneCard, item.MatchView.Players[0].PileCardCount));
        }

        [Fact]
        public void PlaceCard_WithOnlySomePlayersSubscribed_NotifiesOnlySubscribed()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.Subscribe(scenario.CreatorUserId);
            scenario.Subscribe(scenario.UserIds[2]);

            // Act
            MatchDelivery delivery = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.Equal(
                new[] { scenario.CallbackOf(scenario.CreatorUserId), scenario.CallbackOf(scenario.UserIds[2]) },
                delivery.ViewDeliveries.Select(item => item.Callback));
        }

        [Fact]
        public void PlaceCard_RepeatedByTheSamePlayer_RejectsSecondAndKeepsVersion()
        {
            // Arrange: reentrada secuencial; la segunda llamada llega cuando el turno ya pasó.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Act
            MatchDelivery secondDelivery = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.Equal(MatchErrorKeys.NotYourTurn, secondDelivery.ErrorKey);
            Assert.Equal(SecondVersion, scenario.ReadVersionAs(scenario.CreatorUserId));
        }

        [Fact]
        public void PlaceCard_InConsecutiveTurns_RaisesVersionByOneEach()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Act
            MatchDelivery delivery = scenario.Place(scenario.UserIds[1], MatchCardKind.Safe);

            // Assert
            Assert.All(delivery.ViewDeliveries, item => Assert.Equal(ThirdVersion, item.MatchView.Version));
        }

        [Fact]
        public void RemoveMatchChannel_WithOldCallbackAfterReplacement_KeepsNewSubscription()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchCallbackStub oldCallback = new MatchCallbackStub();
            LobbyCaller oldCaller = scenario.MatchCallerOf(scenario.CreatorUserId);
            LobbyCaller newCaller = new LobbyCaller(scenario.TokenOf(scenario.CreatorUserId), SecondChannelId);
            scenario.Registry.SubscribeToMatch(oldCaller, scenario.MatchId, oldCallback);
            MatchCallbackStub newCallback = scenario.CallbackOf(scenario.CreatorUserId);
            scenario.Registry.SubscribeToMatch(newCaller, scenario.MatchId, newCallback);

            // Act
            bool wasRemoved = scenario.Registry.RemoveMatchChannel(oldCaller.ChannelId, oldCallback);

            // Assert
            Assert.False(wasRemoved);
            Assert.Null(scenario.Registry.PlaceCard(newCaller, MatchCardKind.Safe).ErrorKey);
        }

        [Fact]
        public void RemoveMatchChannel_WithOldCallbackOnSameChannel_KeepsNewCallback()
        {
            // Arrange: mismo canal, otro callback; comparar solo el canal borraría la suscripción nueva.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchCallbackStub oldCallback = new MatchCallbackStub();
            LobbyCaller caller = scenario.MatchCallerOf(scenario.CreatorUserId);
            scenario.Registry.SubscribeToMatch(caller, scenario.MatchId, oldCallback);
            scenario.Subscribe(scenario.CreatorUserId);

            // Act
            bool wasRemoved = scenario.Registry.RemoveMatchChannel(caller.ChannelId, oldCallback);

            // Assert
            MatchDelivery placement = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);
            MatchViewDelivery viewDelivery = Assert.Single(placement.ViewDeliveries);
            Assert.False(wasRemoved);
            Assert.Same(scenario.CallbackOf(scenario.CreatorUserId), viewDelivery.Callback);
        }

        [Fact]
        public void RemoveMatchChannel_AfterSameChannelResubscribed_LeavesRegistryClean()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.Subscribe(scenario.CreatorUserId);
            scenario.Subscribe(scenario.CreatorUserId);

            // Act
            bool wasRemoved = scenario.CloseMatchChannelOf(scenario.CreatorUserId);

            // Assert
            Assert.True(wasRemoved);
            Assert.False(scenario.CloseMatchChannelOf(scenario.CreatorUserId));
            Assert.Equal(MatchErrorKeys.NotSubscribed, PlaceAsCreator(scenario).ErrorKey);
        }

        [Fact]
        public void RemoveMatchChannel_AfterSameChannelNewCallback_LeavesRegistryClean()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchCallbackStub firstCallback = new MatchCallbackStub();
            LobbyCaller caller = scenario.MatchCallerOf(scenario.CreatorUserId);
            scenario.Registry.SubscribeToMatch(caller, scenario.MatchId, firstCallback);
            scenario.Subscribe(scenario.CreatorUserId);

            // Act
            bool wasOldRemoved = scenario.Registry.RemoveMatchChannel(caller.ChannelId, firstCallback);
            bool wasNewRemoved = scenario.CloseMatchChannelOf(scenario.CreatorUserId);

            // Assert
            Assert.False(wasOldRemoved);
            Assert.True(wasNewRemoved);
            Assert.Equal(MatchErrorKeys.NotSubscribed, PlaceAsCreator(scenario).ErrorKey);
        }

        [Fact]
        public void RemoveMatchChannel_WithCurrentCallback_OnlyRemovesThatSubscription()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int closingUserId = scenario.UserIds[2];
            LobbyCaller closingCaller = scenario.MatchCallerOf(closingUserId);

            // Act
            bool wasRemoved = scenario.CloseMatchChannelOf(closingUserId);

            // Assert
            Assert.True(wasRemoved);
            MatchDelivery placement = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);
            Assert.Equal("Match_ReconnectionPending", placement.ErrorKey);
            Assert.DoesNotContain(scenario.CallbackOf(closingUserId), MatchPlayScenario.RecipientsOf(placement));
            Assert.Equal(MatchErrorKeys.NotSubscribed, scenario.Place(closingUserId, MatchCardKind.Safe).ErrorKey);
        }

        [Fact]
        public void RemoveMatchChannel_WhenChannelCloses_KeepsTurnRoundAndParticipantsOfTheMatch()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);
            int closingUserId = scenario.UserIds[1];
            MatchSnapshot before = scenario.Registry.FindMatchOfParticipant(scenario.MatchId, closingUserId);

            // Act
            scenario.CloseMatchChannelOf(closingUserId);

            // Assert
            MatchSnapshot after = scenario.Registry.FindMatchOfParticipant(scenario.MatchId, closingUserId);
            Assert.Equal(before.PlayerInTurnUserId, after.PlayerInTurnUserId);
            Assert.Equal(before.RoundNumber, after.RoundNumber);
            Assert.Equal(before.ParticipantUserIds, after.ParticipantUserIds);
        }

        /// <summary>
        /// Closing the channel opens the grace and publishes one new version to the players who stay; subscribing
        /// again resumes the player and publishes one more. Subscribing is not a neutral read after a loss.
        /// </summary>
        [Fact]
        public void RemoveMatchChannel_ThenSubscribeAgain_RaisesTheVersionOncePerTransition()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);
            int closingUserId = scenario.UserIds[1];

            // Act
            scenario.Registry.RemoveMatchChannel(
                MatchPlayScenario.MatchChannelPrefix + closingUserId,
                scenario.CallbackOf(closingUserId),
                out IReadOnlyList<MatchViewDelivery> closeDeliveries);
            MatchDelivery returned = scenario.Subscribe(closingUserId);

            // Assert
            Assert.Equal(MinimumPlayerCount - 1, closeDeliveries.Count);
            Assert.All(closeDeliveries, item => Assert.Equal(SecondVersion + 1, item.MatchView.Version));
            Assert.Equal(MinimumPlayerCount, returned.ViewDeliveries.Count);
            Assert.All(returned.ViewDeliveries, item => Assert.Equal(SecondVersion + 2, item.MatchView.Version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("never-subscribed")]
        public void RemoveMatchChannel_WithUnknownOrEmptyChannel_ReturnsFalse(string channelId)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();

            // Act
            bool wasRemoved = scenario.Registry.RemoveMatchChannel(channelId, new MatchCallbackStub());

            // Assert
            Assert.False(wasRemoved);
        }

        [Fact]
        public void LeaveRoom_OfSubscribedPlayer_RemovesItsCallbackButKeepsMatch()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int leavingUserId = scenario.UserIds[1];
            scenario.Registry.LeaveRoom(scenario.TokenOf(leavingUserId), out _);

            // Act
            MatchDelivery placement = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.DoesNotContain(scenario.CallbackOf(leavingUserId), MatchPlayScenario.RecipientsOf(placement));
            Assert.Equal(MinimumPlayerCount - OneCard, placement.ViewDeliveries.Count);
        }

        [Fact]
        public void EndSession_OfSubscribedPlayer_RemovesItsCallback()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int endingUserId = scenario.UserIds[2];
            SessionEnd sessionEnd;
            scenario.Registry.EndSession(scenario.TokenOf(endingUserId), out sessionEnd);

            // Act
            MatchDelivery placement = scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);

            // Assert
            Assert.DoesNotContain(scenario.CallbackOf(endingUserId), MatchPlayScenario.RecipientsOf(placement));
        }

        [Fact]
        public void LeaveRoom_WhenRoomBecomesEmpty_ReleasesMatchForSubscription()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            MatchPlayScenario survivor = scenario.CreateAnotherMatch(MinimumPlayerCount, SurvivorFirstUserId);

            // Act
            scenario.LeaveAll();

            // Assert
            Assert.Null(scenario.Registry.FindMatchOfParticipant(scenario.MatchId, scenario.CreatorUserId));
            Assert.Null(survivor.Subscribe(survivor.CreatorUserId).ErrorKey);
            Assert.Equal(MatchErrorKeys.NotInMatch, scenario.Subscribe(scenario.CreatorUserId).ErrorKey);
        }

        [Fact]
        public void SubscribeToMatch_AfterMutatingDeliveredView_DoesNotAffectServerState()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            MatchView firstView = scenario.Subscribe(scenario.CreatorUserId).ViewDeliveries[0].MatchView;
            firstView.OwnHandCards.Clear();
            firstView.Players.Clear();

            // Act
            MatchView secondView = scenario.Subscribe(scenario.CreatorUserId).ViewDeliveries[0].MatchView;

            // Assert
            Assert.Equal(MatchPlayScenario.CardsPerPlayerCount, secondView.OwnHandCards.Count);
            Assert.Equal(MinimumPlayerCount, secondView.Players.Count);
        }

        private static MatchDelivery PlaceAsCreator(MatchPlayScenario scenario)
        {
            return scenario.Place(scenario.CreatorUserId, MatchCardKind.Safe);
        }

        private static void PlaceInTurnOrder(MatchPlayScenario scenario, MatchCardKind cardKind, int placementCount)
        {
            int currentIndex = 0;

            for (int placement = 0; placement < placementCount; placement++)
            {
                scenario.Place(scenario.UserIds[currentIndex % scenario.UserIds.Count], cardKind);
                currentIndex++;
            }
        }
    }
}
