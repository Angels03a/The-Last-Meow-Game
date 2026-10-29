namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado inmutable de aceptar una invitación a sala.
    /// </summary>
    public sealed class RoomInvitationAcceptance
    {
        private RoomInvitationAcceptance(FriendsChangeStatus status, int userId, int roomId)
        {
            Status = status;
            UserId = userId;
            RoomId = roomId;
        }

        /// <summary>Obtiene qué ocurrió con el intento de aceptar.</summary>
        public FriendsChangeStatus Status { get; }

        /// <summary>Obtiene el id de la cuenta del jugador; NoUserId si el token no era válido.</summary>
        public int UserId { get; }

        /// <summary>Obtiene el id de la sala a la que el jugador quedó autorizado; NoRoomId si se rechazó.</summary>
        public int RoomId { get; }

        /// <summary>Crea un resultado rechazado.</summary>
        /// <param name="status">Motivo del rechazo.</param>
        /// <param name="userId">Id de la cuenta, o NoUserId si el token no era válido.</param>
        /// <returns>El resultado sin sala; nunca es null.</returns>
        public static RoomInvitationAcceptance CreateRejected(FriendsChangeStatus status, int userId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance =
                new RoomInvitationAcceptance(status, userId, LobbyMembershipChange.NoRoomId);

            return roomInvitationAcceptance;
        }

        /// <summary>Crea un resultado aplicado.</summary>
        /// <param name="userId">Id de la cuenta del jugador.</param>
        /// <param name="roomId">Id de la sala a la que quedó autorizado.</param>
        /// <returns>El resultado aplicado; nunca es null.</returns>
        public static RoomInvitationAcceptance CreateApplied(int userId, int roomId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance =
                new RoomInvitationAcceptance(FriendsChangeStatus.Applied, userId, roomId);

            return roomInvitationAcceptance;
        }
    }
}