namespace TLMG.Client.Models
{
    /// <summary>
    /// Representa una solicitud de amistad pendiente tal como se muestra en FriendsScreen: de quién es, si el jugador
    /// la recibió o la envió, y su id para aceptarla o rechazarla.
    /// </summary>
    public class FriendRequestEntry
    {
        /// <summary>
        /// Obtiene o establece el id de la solicitud, tal como llegó del servidor.
        /// </summary>
        public int FriendRequestId { get; set; }

        /// <summary>
        /// Obtiene o establece el nickname del otro jugador.
        /// </summary>
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece si el jugador la recibió (puede aceptarla o rechazarla) o la envió (solo espera).
        /// </summary>
        public bool IsIncoming { get; set; }
    }
}
