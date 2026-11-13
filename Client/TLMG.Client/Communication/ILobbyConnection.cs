using System;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Área de sala (lobby) de la conexión con el servidor: crear y dejar la sala, el estado Listo, las cartas
    /// por jugador y el chat de la sala. LobbyViewModel depende solo de esta interfaz (D-70); la implementa
    /// ServerConnectionManager, que conserva la sala aunque ninguna pantalla esté abierta (D-60). Los eventos se
    /// disparan en el hilo de la interfaz y llevan la ErrorKey tal como la manda el servidor, sin textos. Las
    /// operaciones solo envían: el resultado llega por el evento correspondiente, y toda decisión (listo, inicio
    /// de partida, cartas por jugador) es del servidor.
    /// </summary>
    public interface ILobbyConnection
    {
        /// <summary>Se dispara con el resultado de crear una sala.</summary>
        event EventHandler<ServerEventArgs<OperationLobbyRoomResult>> CreateRoomResultReceived;

        /// <summary>Se dispara cuando cambia la sala del jugador (jugadores, listos o cartas por jugador).</summary>
        event EventHandler<ServerEventArgs<LobbyRoom>> LobbyRoomChanged;

        /// <summary>Se dispara con el resultado de cambiar el estado Listo.</summary>
        event EventHandler<ServerEventArgs<OperationLobbyRoomResult>> ReadinessResultReceived;

        /// <summary>Se dispara con el resultado de comprobar el inicio de la partida.</summary>
        event EventHandler<ServerEventArgs<OperationRequestMatchStartResult>> MatchStartResultReceived;

        /// <summary>Se dispara con el resultado de cambiar las cartas por jugador.</summary>
        event EventHandler<ServerEventArgs<OperationLobbyRoomResult>> ChangeCardsPerPlayerResultReceived;

        /// <summary>Se dispara con cada mensaje de chat que el servidor confirma.</summary>
        event EventHandler<ServerEventArgs<ChatMessage>> ChatMessageReceived;

        /// <summary>Se dispara cuando el chat pasa a estar disponible o deja de estarlo.</summary>
        event EventHandler ChatAvailabilityChanged;

        /// <summary>Se dispara cuando cambia la cuenta regresiva de inicio.</summary>
        event EventHandler CountdownChanged;

        /// <summary>Obtiene si hay una sesión de jugador registrado; un invitado no la tiene.</summary>
        bool HasRegisteredSession { get; }

        /// <summary>Obtiene la última foto de la sala del jugador, o null si no está en una sala.</summary>
        LobbyRoom CurrentRoom { get; }

        /// <summary>Obtiene si se pidió crear una sala y su resultado todavía no llega.</summary>
        bool IsRoomCreationPending { get; }

        /// <summary>Obtiene el id de la cuenta del jugador en sesión, o 0 si no hay sesión registrada.</summary>
        int PlayerUserId { get; }

        /// <summary>Obtiene si el servidor aceptó la suscripción al chat de la sala.</summary>
        bool IsChatSubscribed { get; }

        /// <summary>Obtiene si la foto del servidor mantiene una cuenta regresiva activa.</summary>
        bool IsCountdownVisible { get; }

        /// <summary>Obtiene los segundos que faltan de la cuenta regresiva, o 0 si no la confirma.</summary>
        int CountdownSecondsRemaining { get; }

        /// <summary>Crea una sala y deja al jugador dentro; el resultado llega por CreateRoomResultReceived.</summary>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el canal o el envío.</returns>
        bool CreateRoom();

        /// <summary>Sale de la sala actual y la libera en el cliente sin esperar la respuesta.</summary>
        /// <returns>true si se envió; false si no hay sesión registrada o falló el canal o el envío.</returns>
        bool LeaveLobby();

        /// <summary>Pide el estado Listo deseado; el resultado llega por ReadinessResultReceived.</summary>
        /// <param name="isReadyToStartMatch">Estado Listo deseado.</param>
        /// <returns>true si se envió; false si no hay sala o falló el envío.</returns>
        bool ChangePlayerReadiness(bool isReadyToStartMatch);

        /// <summary>
        /// Pide cambiar las cartas por jugador de la sala (4, 6 u 8); el servidor valida el valor y reinicia el
        /// estado Listo de todos. El resultado llega por ChangeCardsPerPlayerResultReceived.
        /// </summary>
        /// <param name="cardsPerPlayerCount">Cartas por jugador elegidas.</param>
        /// <returns>true si se envió; false si no hay sala o falló el envío.</returns>
        bool ChangeCardsPerPlayer(int cardsPerPlayerCount);

        /// <summary>Envía un mensaje de chat; su eco por ChatMessageReceived es la única confirmación.</summary>
        /// <param name="messageText">Texto capturado por el jugador.</param>
        /// <returns>true si se envió; false si no hay sala, chat o falló el envío.</returns>
        bool SendChatMessage(string messageText);
    }
}