using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Lobby;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Started room with an authorized match. The first player creates it, the others join in ID order and everyone is
    /// ready in four-card mode. Two scenarios can share a registry and clock.
    /// </summary>
    internal sealed class MatchPlayScenario
    {
        internal const int FirstUserId = 1;
        internal const int InitialVersion = 1;
        internal const int CardsPerPlayerCount = 4;
        internal const string LobbyChannelPrefix = "lobby";
        internal const string MatchChannelPrefix = "match";
        internal const string NicknamePrefix = "Player";
        private const int CountdownSeconds = 3;
        internal const string FriendsChannelPrefix = "friends";

        private readonly int _cardsPerPlayer;

        private readonly Dictionary<int, string> _tokensByUserId = new Dictionary<int, string>();
        private readonly Dictionary<int, MatchCallbackStub> _callbacksByUserId =
            new Dictionary<int, MatchCallbackStub>();
        private readonly Dictionary<int, LobbyCallbackStub> _lobbyCallbacksByUserId =
            new Dictionary<int, LobbyCallbackStub>();

        internal MatchPlayScenario(int playerCount)
            : this(playerCount, FirstUserId, new ManualUtcClock(), new ScriptedRandom(), null, CardsPerPlayerCount)
        {
        }

        internal MatchPlayScenario(int playerCount, int cardsPerPlayer)
            : this(playerCount, FirstUserId, new ManualUtcClock(), new ScriptedRandom(), null, cardsPerPlayer)
        {
        }

        private MatchPlayScenario(
            int playerCount,
            int firstUserId,
            ManualUtcClock clock,
            ScriptedRandom randomSource,
            MatchPlayScenario previousScenario,
            int cardsPerPlayer)
        {
            _cardsPerPlayer = cardsPerPlayer;
            Clock = clock;
            RandomSource = randomSource;
            if (previousScenario == null)
            {
                Registry = new SessionRegistry(clock.ReadUtcNow, randomSource.PickIndex);
                MatchStart = MatchHistoryStub.CreateCoordinator(Registry);
            }
            else
            {
                Registry = previousScenario.Registry;
                MatchStart = previousScenario.MatchStart;
            }
            UserIds = Enumerable.Range(firstUserId, playerCount).ToList().AsReadOnly();
            CreateRoomWithAllPlayers();
            StartMatch();
        }

        internal SessionRegistry Registry { get; }

        internal MatchStartCoordinator MatchStart { get; }

        internal ManualUtcClock Clock { get; }

        internal ScriptedRandom RandomSource { get; }

        internal IReadOnlyList<int> UserIds { get; }

        internal int RoomId { get; private set; }

        internal Guid MatchId { get; private set; }

        internal int CreatorUserId => UserIds[0];

        /// <summary>Creates another match in the same registry with different player IDs.</summary>
        internal MatchPlayScenario CreateAnotherMatch(int playerCount, int firstUserId)
        {
            return new MatchPlayScenario(playerCount, firstUserId, Clock, RandomSource, this, CardsPerPlayerCount);
        }

        internal string TokenOf(int userId)
        {
            return _tokensByUserId[userId];
        }

        internal MatchCallbackStub CallbackOf(int userId)
        {
            MatchCallbackStub callback;

            if (!_callbacksByUserId.TryGetValue(userId, out callback))
            {
                callback = new MatchCallbackStub();
                _callbacksByUserId[userId] = callback;
            }

            return callback;
        }

        internal LobbyCallbackStub LobbyCallbackOf(int userId)
        {
            LobbyCallbackStub callback;

            if (!_lobbyCallbacksByUserId.TryGetValue(userId, out callback))
            {
                callback = new LobbyCallbackStub();
                _lobbyCallbacksByUserId[userId] = callback;
            }

            return callback;
        }

        internal SessionChannelCaller MatchCallerOf(int userId)
        {
            return new SessionChannelCaller(TokenOf(userId), MatchChannelPrefix + userId);
        }

        internal MatchDelivery Subscribe(int userId)
        {
            return Registry.SubscribeToMatch(MatchCallerOf(userId), MatchId, CallbackOf(userId));
        }

        internal void SubscribeAll()
        {
            foreach (int userId in UserIds)
            {
                Subscribe(userId);
            }
        }

        internal MatchDelivery Place(int userId, MatchCardKind cardKind)
        {
            return Registry.PlaceCard(MatchCallerOf(userId), cardKind);
        }

        internal MatchDelivery PlaceBet(int userId, int betValue)
        {
            return Registry.PlaceBet(MatchCallerOf(userId), betValue);
        }

        internal MatchDelivery Withdraw(int userId)
        {
            return Registry.WithdrawFromBet(MatchCallerOf(userId));
        }

        internal MatchDelivery Reveal(int userId, int pileOwnerUserId)
        {
            return Registry.RevealCard(MatchCallerOf(userId), pileOwnerUserId);
        }

        internal MatchDelivery Choose(int userId, int cardIndex)
        {
            return Registry.ChoosePunishmentCard(MatchCallerOf(userId), cardIndex);
        }

        /// <summary>
        /// Prepares a challenge. The first and third players place safe cards and the second places the given card. The
        /// first player bids and both others fold.
        /// </summary>
        internal void ReachChallengeReady(MatchCardKind secondPlayerCard, int betValue)
        {
            Place(UserIds[0], MatchCardKind.Safe);
            Place(UserIds[1], secondPlayerCard);
            Place(UserIds[2], MatchCardKind.Safe);
            PlaceBet(UserIds[0], betValue);
            Withdraw(UserIds[1]);
            Withdraw(UserIds[2]);
        }

        /// <summary>The first player wins the round by revealing one card from their own pile.</summary>
        internal void WinRoundWithABetOfOne()
        {
            ReachChallengeReady(MatchCardKind.Safe, 1);
            Reveal(UserIds[0], UserIds[0]);
        }

        /// <summary>
        /// The first player fails on the second player's Fatal Cat. The second player chooses blindly.
        /// </summary>
        internal void ReachBlindPunishment()
        {
            ReachChallengeReady(MatchCardKind.FatalCat, 2);
            Reveal(UserIds[0], UserIds[0]);
            Reveal(UserIds[0], UserIds[1]);
        }

        /// <summary>The first player fails on their own Fatal Cat and chooses a card from their visible hand.</summary>
        internal void ReachOwnPunishment()
        {
            Place(UserIds[0], MatchCardKind.FatalCat);
            Place(UserIds[1], MatchCardKind.Safe);
            Place(UserIds[2], MatchCardKind.Safe);
            PlaceBet(UserIds[0], 1);
            Withdraw(UserIds[1]);
            Withdraw(UserIds[2]);
            Reveal(UserIds[0], UserIds[0]);
        }

        /// <summary>Each player places a safe card in turn, starting with the creator.</summary>
        internal void PlaceOneSafeCardEach()
        {
            foreach (int userId in UserIds)
            {
                Place(userId, MatchCardKind.Safe);
            }
        }

        /// <summary>Current player view obtained by subscribing again with the player's callback.</summary>
        internal MatchView ReadViewAs(int userId)
        {
            return Subscribe(userId).ViewDeliveries[0].MatchView;
        }

        internal int ReadVersionAs(int userId)
        {
            return ReadViewAs(userId).Version;
        }

        /// <summary>Tells the registry that the player's match channel closed with its current callback.</summary>
        internal bool CloseMatchChannelOf(int userId)
        {
            return Registry.RemoveMatchChannel(MatchChannelPrefix + userId, CallbackOf(userId));
        }

        /// <summary>Recipient callbacks in delivery order.</summary>
        internal static IEnumerable<IMatchManagerCallback> RecipientsOf(MatchDelivery delivery)
        {
            return delivery.ViewDeliveries.Select(viewDelivery => viewDelivery.Callback);
        }

        /// <summary>Caller with the first player's match channel and the supplied token.</summary>
        internal static SessionChannelCaller CallerWithToken(string sessionToken)
        {
            return new SessionChannelCaller(sessionToken, MatchChannelPrefix + FirstUserId);
        }

        /// <summary>All players leave the room in entry order.</summary>
        internal void LeaveAll()
        {
            foreach (int userId in UserIds)
            {
                Registry.LeaveRoom(TokenOf(userId), out _);
            }
        }

        private SessionChannelCaller LobbyCallerOf(int userId)
        {
            return new SessionChannelCaller(TokenOf(userId), LobbyChannelPrefix + userId);
        }

        private void CreateRoomWithAllPlayers()
        {
            _tokensByUserId[CreatorUserId] = StartSessionOf(CreatorUserId);
            RoomId = Registry.CreateRoom(LobbyCallerOf(CreatorUserId), LobbyCallbackOf(CreatorUserId)).RoomId;

            foreach (int userId in UserIds.Skip(1))
            {
                string token = StartSessionOf(userId);
                _tokensByUserId[userId] = token;
                Registry.SubscribeFriendsChannel(token, FriendsChannelPrefix + userId, null, Array.Empty<int>());
                RoomInvitationCreation invitation = Registry.CreateRoomInvitation(TokenOf(CreatorUserId), userId);
                Registry.AcceptRoomInvitation(token, invitation.InvitationId);
                Registry.EnterRoom(LobbyCallerOf(userId), RoomId, LobbyCallbackOf(userId));
            }
        }

        private string StartSessionOf(int userId)
        {
            SessionEnd replacedSession;

            return Registry.StartRegisteredSession(userId, NicknamePrefix + userId, out replacedSession);
        }

        private void StartMatch()
        {
            Registry.ChangeCardsPerPlayer(LobbyCallerOf(CreatorUserId), RoomId, _cardsPerPlayer);

            foreach (int userId in UserIds)
            {
                Registry.ChangePlayerReadiness(LobbyCallerOf(userId), RoomId, true);
            }

            Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));
            long countdownId = Registry.ChangePlayerReadiness(LobbyCallerOf(CreatorUserId), RoomId, true)
                .Room.Start.CountdownId;
            LobbyMembershipChange change =
                MatchStart.RequestMatchStart(LobbyCallerOf(CreatorUserId), RoomId, countdownId);
            MatchId = change.Room.Start.MatchId.Value;
        }
    }
}
