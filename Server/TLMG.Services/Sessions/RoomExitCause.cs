namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Causa por la que un jugador deja su sala. Solo la salida voluntaria saca al jugador del motor de partida; las
    /// demás quedan registradas y esperan la ventana de reconexión (C2).
    /// </summary>
    public enum RoomExitCause
    {
        /// <summary>No es una salida: crear, entrar o cambiar la sala.</summary>
        None,

        /// <summary>El jugador pulsó Salir (LeaveLobby).</summary>
        Voluntary,

        /// <summary>El canal de lobby del jugador se cerró o falló.</summary>
        LobbyChannelClosed,

        /// <summary>La sesión terminó: cierre de sesión o canal de amigos perdido.</summary>
        SessionEnded,

        /// <summary>La sesión se reemplazó por un nuevo inicio de sesión de la misma cuenta.</summary>
        SessionReplaced
    }
}
