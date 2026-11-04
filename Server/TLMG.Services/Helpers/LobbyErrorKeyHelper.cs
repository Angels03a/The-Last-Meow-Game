namespace TLMG.Services.Helpers
{
    /// <summary>Claves de sala compartidas por lobby y chat.</summary>
    public static class LobbyErrorKeyHelper
    {
        /// <summary>El jugador no pertenece a la sala solicitada.</summary>
        public const string NotInRoomErrorKey = "Lobby_NotInRoomError";

        /// <summary>La partida ya comenzó y no admite cambiar la preparación.</summary>
        public const string MatchAlreadyStartedErrorKey = "Lobby_MatchAlreadyStartedError";
    }
}
