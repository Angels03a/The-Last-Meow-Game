namespace TLMG.Client.Models
{
    /// <summary>
    /// Representa un jugador tal como se muestra en LobbyScreen: su nickname y si es el jugador de este cliente.
    /// </summary>
    public class LobbyPlayerEntry
    {
        /// <summary>
        /// Obtiene o establece el nickname del jugador, tal como llegó del servidor.
        /// </summary>
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece si el jugador es el que tiene la sesión en este cliente. Solo cambia cómo se muestra
        /// la tarjeta; no da ningún permiso.
        /// </summary>
        public bool IsLocalPlayer { get; set; }

        /// <summary>Obtiene o establece el estado Listo recibido del servidor.</summary>
        public bool IsReady { get; set; }
    }
}