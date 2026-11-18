using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado inmutable de un cambio de sala: qué pasó, a quién y a quién hay que avisar fuera del candado.
    /// </summary>
    public sealed class LobbyMembershipChange
    {
        /// <summary>Valor de RoomId cuando el resultado no se refiere a ninguna sala.</summary>
        public const int NoRoomId = 0;

        /// <summary>Valor de UserId cuando el token no pertenece a ninguna sesión.</summary>
        public const int NoUserId = 0;

        private static readonly IReadOnlyList<ILobbyManagerCallback> _noCallbacks =
            new List<ILobbyManagerCallback>().AsReadOnly();

        private LobbyMembershipChange(
            LobbyChangeStatus status,
            int userId,
            int roomId,
            LobbyRoomSnapshot room,
            IReadOnlyList<ILobbyManagerCallback> recipientCallbacks,
            RoomExitCause exitCause,
            bool leftAuthorizedMatch)
        {
            Status = status;
            UserId = userId;
            RoomId = roomId;
            Room = room;
            RecipientCallbacks = recipientCallbacks;
            ExitCause = exitCause;
            LeftAuthorizedMatch = leftAuthorizedMatch;
        }

        /// <summary>Obtiene qué ocurrió con el intento de cambio.</summary>
        public LobbyChangeStatus Status { get; }

        /// <summary>Obtiene el id de la cuenta del jugador; NoUserId si el token no era válido.</summary>
        public int UserId { get; }

        /// <summary>Obtiene el id de la sala afectada; NoRoomId si no hubo sala.</summary>
        public int RoomId { get; }

        /// <summary>Obtiene la sala tras el cambio; null si se rechazó o si la sala se borró por vacía.</summary>
        public LobbyRoomSnapshot Room { get; }

        /// <summary>Obtiene los callbacks destinatarios del cambio; nunca es null.</summary>
        public IReadOnlyList<ILobbyManagerCallback> RecipientCallbacks { get; }

        /// <summary>Obtiene la causa de la salida; None si el cambio no fue una salida.</summary>
        public RoomExitCause ExitCause { get; }

        /// <summary>Obtiene si el jugador dejó una sala que ya tenía una partida autorizada.</summary>
        public bool LeftAuthorizedMatch { get; }

        /// <summary>Crea un resultado de cambio rechazado.</summary>
        /// <param name="status">Motivo del rechazo.</param>
        /// <param name="userId">Id de la cuenta, o NoUserId si el token no era válido.</param>
        /// <returns>El resultado sin sala y sin jugadores a quienes avisar; nunca es null.</returns>
        public static LobbyMembershipChange CreateRejected(LobbyChangeStatus status, int userId)
        {
            LobbyMembershipChange lobbyMembershipChange =
                new LobbyMembershipChange(
                    status, userId, NoRoomId, null, _noCallbacks, RoomExitCause.None, false);

            return lobbyMembershipChange;
        }

        /// <summary>Crea un resultado aplicado sin jugadores a quienes avisar.</summary>
        /// <param name="userId">Id de la cuenta del jugador.</param>
        /// <param name="roomId">Id de la sala afectada.</param>
        /// <param name="room">La sala tras el cambio; null si la sala se eliminó.</param>
        /// <returns>El resultado aplicado; nunca es null.</returns>
        public static LobbyMembershipChange CreateApplied(int userId, int roomId, LobbyRoomSnapshot room)
        {
            LobbyMembershipChange lobbyMembershipChange = CreateApplied(userId, roomId, room, _noCallbacks);

            return lobbyMembershipChange;
        }

        /// <summary>Crea un resultado de cambio aplicado.</summary>
        /// <param name="userId">Id de la cuenta del jugador.</param>
        /// <param name="roomId">Id de la sala afectada.</param>
        /// <param name="room">La sala tras el cambio; null si la sala se eliminó.</param>
        /// <param name="recipientCallbacks">
        /// Callbacks destinatarios, incluido el solicitante cuando corresponda; no se modifica después.
        /// </param>
        /// <returns>El resultado aplicado; nunca es null.</returns>
        public static LobbyMembershipChange CreateApplied(
            int userId,
            int roomId,
            LobbyRoomSnapshot room,
            IReadOnlyList<ILobbyManagerCallback> recipientCallbacks)
        {
            LobbyMembershipChange lobbyMembershipChange =
                new LobbyMembershipChange(
                    LobbyChangeStatus.Applied, userId, roomId, room, recipientCallbacks, RoomExitCause.None, false);

            return lobbyMembershipChange;
        }

        /// <summary>Crea el cambio de una salida de sala, con su causa.</summary>
        /// <param name="userId">Jugador que salió.</param>
        /// <param name="roomId">Sala que dejó.</param>
        /// <param name="room">Sala que queda; null si quedó vacía.</param>
        /// <param name="recipientCallbacks">Callbacks de lobby de quienes siguen en la sala.</param>
        /// <param name="exitCause">Causa de la salida.</param>
        /// <param name="leftAuthorizedMatch">Indica si la sala ya tenía una partida autorizada.</param>
        /// <returns>El cambio aplicado.</returns>
        public static LobbyMembershipChange CreateDeparture(
            int userId,
            int roomId,
            LobbyRoomSnapshot room,
            IReadOnlyList<ILobbyManagerCallback> recipientCallbacks,
            RoomExitCause exitCause,
            bool leftAuthorizedMatch)
        {
            LobbyMembershipChange lobbyMembershipChange = new LobbyMembershipChange(
                LobbyChangeStatus.Applied, userId, roomId, room, recipientCallbacks, exitCause, leftAuthorizedMatch);

            return lobbyMembershipChange;
        }
    }
}