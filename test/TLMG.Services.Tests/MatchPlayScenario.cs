using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Sala ya iniciada con una partida autorizada: el primer jugador la crea, los demás entran en orden de id y todos
    /// quedan Listo con la modalidad de 4 cartas. Dos escenarios pueden compartir registro y reloj.
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
        private const string FriendsChannelPrefix = "friends";

        private readonly Dictionary<int, string> _tokensByUserId = new Dictionary<int, string>();
        private readonly Dictionary<int, MatchCallbackStub> _callbacksByUserId =
            new Dictionary<int, MatchCallbackStub>();

        internal MatchPlayScenario(int playerCount)
            : this(playerCount, FirstUserId, new ManualUtcClock(), null)
        {
        }

        private MatchPlayScenario(int playerCount, int firstUserId, ManualUtcClock clock, SessionRegistry registry)
        {
            Clock = clock;
            Registry = registry ?? new SessionRegistry(clock.ReadUtcNow);
            UserIds = Enumerable.Range(firstUserId, playerCount).ToList().AsReadOnly();
            CreateRoomWithAllPlayers();
            StartMatch();
        }

        internal SessionRegistry Registry { get; }

        internal ManualUtcClock Clock { get; }

        internal IReadOnlyList<int> UserIds { get; }

        internal int RoomId { get; private set; }

        internal Guid MatchId { get; private set; }

        internal int CreatorUserId => UserIds[0];

        /// <summary>Crea otra partida en el mismo registro, con jugadores de otros ids.</summary>
        internal MatchPlayScenario CreateAnotherMatch(int playerCount, int firstUserId)
        {
            return new MatchPlayScenario(playerCount, firstUserId, Clock, Registry);
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

        internal LobbyCaller MatchCallerOf(int userId)
        {
            return new LobbyCaller(TokenOf(userId), MatchChannelPrefix + userId);
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

        /// <summary>Vista actual del jugador, leída al volver a suscribirlo con su callback.</summary>
        internal MatchView ReadViewAs(int userId)
        {
            return Subscribe(userId).ViewDeliveries[0].MatchView;
        }

        internal int ReadVersionAs(int userId)
        {
            return ReadViewAs(userId).Version;
        }

        /// <summary>Informa al registro que el canal de partida del jugador se cerró con su callback actual.</summary>
        internal bool CloseMatchChannelOf(int userId)
        {
            return Registry.RemoveMatchChannel(MatchChannelPrefix + userId, CallbackOf(userId));
        }

        /// <summary>Los callbacks destinatarios de una entrega, en el orden de la entrega.</summary>
        internal static IEnumerable<IMatchManagerCallback> RecipientsOf(MatchDelivery delivery)
        {
            return delivery.ViewDeliveries.Select(viewDelivery => viewDelivery.Callback);
        }

        /// <summary>Un llamador con el canal de partida del primer jugador y el token recibido.</summary>
        internal static LobbyCaller CallerWithToken(string sessionToken)
        {
            return new LobbyCaller(sessionToken, MatchChannelPrefix + FirstUserId);
        }

        /// <summary>Todos los jugadores salen de la sala, en orden de ingreso.</summary>
        internal void LeaveAll()
        {
            foreach (int userId in UserIds)
            {
                Registry.LeaveRoom(TokenOf(userId));
            }
        }

        private LobbyCaller LobbyCallerOf(int userId)
        {
            return new LobbyCaller(TokenOf(userId), LobbyChannelPrefix + userId);
        }

        private void CreateRoomWithAllPlayers()
        {
            _tokensByUserId[CreatorUserId] = StartSessionOf(CreatorUserId);
            RoomId = Registry.CreateRoom(LobbyCallerOf(CreatorUserId), null).RoomId;

            foreach (int userId in UserIds.Skip(1))
            {
                string token = StartSessionOf(userId);
                _tokensByUserId[userId] = token;
                Registry.SubscribeFriendsChannel(token, FriendsChannelPrefix + userId, null, Array.Empty<int>());
                RoomInvitationCreation invitation = Registry.CreateRoomInvitation(TokenOf(CreatorUserId), userId);
                Registry.AcceptRoomInvitation(token, invitation.InvitationId);
                Registry.EnterRoom(LobbyCallerOf(userId), RoomId, null);
            }
        }

        private string StartSessionOf(int userId)
        {
            SessionEnd replacedSession;

            return Registry.StartSession(userId, NicknamePrefix + userId, out replacedSession);
        }

        private void StartMatch()
        {
            Registry.ChangeCardsPerPlayer(LobbyCallerOf(CreatorUserId), RoomId, CardsPerPlayerCount);

            foreach (int userId in UserIds)
            {
                Registry.ChangePlayerReadiness(LobbyCallerOf(userId), RoomId, true);
            }

            Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));
            long countdownId = Registry.ChangePlayerReadiness(LobbyCallerOf(CreatorUserId), RoomId, true)
                .Room.Start.CountdownId;
            LobbyMembershipChange change =
                Registry.RequestMatchStart(LobbyCallerOf(CreatorUserId), RoomId, countdownId);
            MatchId = change.Room.Start.MatchId.Value;
        }
    }
}
