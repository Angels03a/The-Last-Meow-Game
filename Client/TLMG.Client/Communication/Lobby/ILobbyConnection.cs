using System;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Provides room creation, exit, readiness, cards per player and room chat for LobbyViewModel (D-70).
    /// ServerConnectionManager keeps the room even when no screen is open (D-60). Events run on the UI thread and carry
    /// the server's ErrorKey without translated text. Each request has a result event. The server decides readiness,
    /// match start and cards per player.
    /// </summary>
    public interface ILobbyConnection
    {
        /// <summary>Raised when the server responds to a request to create a room.</summary>
        event EventHandler<ServerEventArgs<OperationLobbyRoomResult>> CreateRoomResultReceived;

        /// <summary>Raised when the server responds to a request to leave after the match ends.</summary>
        event EventHandler<ServerEventArgs<OperationResult>> FinishedMatchLeaveResultReceived;

        /// <summary>Raised when room membership, readiness or cards per player change.</summary>
        event EventHandler<ServerEventArgs<LobbyRoom>> LobbyRoomChanged;

        /// <summary>Raised when the server responds to a readiness change.</summary>
        event EventHandler<ServerEventArgs<OperationLobbyRoomResult>> ReadinessResultReceived;

        /// <summary>Raised when the server returns the match start check.</summary>
        event EventHandler<ServerEventArgs<OperationRequestMatchStartResult>> MatchStartResultReceived;

        /// <summary>Raised when the server responds to a change in cards per player.</summary>
        event EventHandler<ServerEventArgs<OperationLobbyRoomResult>> ChangeCardsPerPlayerResultReceived;

        /// <summary>Se dispara con cada mensaje de chat que el servidor confirma.</summary>
        event EventHandler<ServerEventArgs<ChatMessage>> ChatMessageReceived;

        /// <summary>Se dispara cuando el chat pasa a estar disponible o deja de estarlo.</summary>
        event EventHandler ChatAvailabilityChanged;

        /// <summary>Raised when the start countdown changes.</summary>
        event EventHandler CountdownChanged;

        /// <summary>Whether the current player has a registered session. Guests have none.</summary>
        bool HasRegisteredSession { get; }

        /// <summary>The latest room snapshot, or null if the player is outside a room.</summary>
        LobbyRoom CurrentRoom { get; }

        /// <summary>Whether a request to create a room is waiting for a response.</summary>
        bool IsRoomCreationPending { get; }

        /// <summary>The current player's account ID, or 0 without a registered session.</summary>
        int PlayerUserId { get; }

        /// <summary>Obtiene si el servidor aceptó la suscripción al chat de la sala.</summary>
        bool IsChatSubscribed { get; }

        void RetryChatConnection();

        /// <summary>Whether the server snapshot shows an active countdown.</summary>
        bool IsCountdownVisible { get; }

        /// <summary>Seconds left in the countdown, or 0 if the server has not confirmed it.</summary>
        int CountdownSecondsRemaining { get; }

        /// <summary>Creates a room and joins it. CreateRoomResultReceived delivers the result.</summary>
        /// <returns>True if sent. False without a registered session or if the channel or send fails.</returns>
        bool CreateRoom();

        /// <summary>Leaves the room and releases it on the client without waiting for a response.</summary>
        /// <returns>True if sent. False without a registered session or if the channel or send fails.</returns>
        bool LeaveLobby();

        /// <summary>Requests to leave the room after the match ends and waits for the server's decision.</summary>
        /// <param name="matchId">Finished match associated with the room.</param>
        /// <returns>True if the request was sent.</returns>
        bool LeaveFinishedMatch(Guid matchId);

        /// <summary>Requests the desired ready state. ReadinessResultReceived delivers the result.</summary>
        /// <param name="isReadyToStartMatch">Requested ready state.</param>
        /// <returns>True if sent. False if there is no room or the send fails.</returns>
        bool ChangePlayerReadiness(bool isReadyToStartMatch);

        /// <summary>
        /// Requests 4, 6 or 8 cards per player. The server checks the value and resets everyone's ready state on a
        /// change. ChangeCardsPerPlayerResultReceived delivers the result.
        /// </summary>
        /// <param name="cardsPerPlayerCount">Selected number of cards per player.</param>
        /// <returns>True if sent. False if there is no room or the send fails.</returns>
        bool ChangeCardsPerPlayer(int cardsPerPlayerCount);

        /// <summary>Envía un mensaje de chat; su eco por ChatMessageReceived es la única confirmación.</summary>
        /// <param name="messageText">Texto capturado por el jugador.</param>
        /// <returns>true si se envió; false si no hay sala, chat o falló el envío.</returns>
        bool SendChatMessage(string messageText);
    }
}