namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado de una operación del registro sobre el canal de amigos y las invitaciones a sala.
    /// </summary>
    public enum FriendsChangeStatus
    {
        /// <summary>La operación se aplicó.</summary>
        Applied,

        /// <summary>El token no pertenece a una sesión vigente, o el canal ya pertenece a otra sesión.</summary>
        InvalidSession,

        /// <summary>El jugador que invita no está en una sala.</summary>
        NotInRoom,

        /// <summary>La sala del jugador que invita tiene el máximo de jugadores.</summary>
        RoomFull,

        /// <summary>
        /// El invitado no puede recibir la invitación: es el mismo jugador, no está en línea o ya está en una sala.
        /// </summary>
        CannotInvite,

        /// <summary>La invitación no existe, no es del jugador o su sala ya no existe.</summary>
        InvitationNotValid
    }
}