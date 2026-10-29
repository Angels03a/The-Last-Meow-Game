namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado de intentar cambiar la pertenencia de un jugador a una sala.
    /// </summary>
    public enum LobbyChangeStatus
    {
        /// <summary>El cambio se aplicó.</summary>
        Applied,

        /// <summary>El token no pertenece a una sesión vigente.</summary>
        InvalidSession,

        /// <summary>El jugador, o el canal desde el que llama, ya está en una sala.</summary>
        AlreadyInRoom,

        /// <summary>La sala no existe o el jugador no tiene autorización de entrada para ella.</summary>
        NotAuthorized,

        /// <summary>La sala tiene el máximo de jugadores; solo se informa a quien ya estaba autorizado.</summary>
        RoomFull,

        /// <summary>El jugador no estaba en ninguna sala.</summary>
        NotInRoom
    }
}