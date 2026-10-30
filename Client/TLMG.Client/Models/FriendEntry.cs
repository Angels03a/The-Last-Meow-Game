namespace TLMG.Client.Models
{
    /// <summary>
    /// Representa un amigo tal como se muestra en FriendsScreen: su nombre, su presencia y si se le puede invitar.
    /// </summary>
    public class FriendEntry
    {
        /// <summary>
        /// Obtiene o establece el id de la cuenta del amigo, tal como llegó del servidor.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Obtiene o establece el nickname del amigo.
        /// </summary>
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece si el amigo está en línea.
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Obtiene o establece si el botón Invitar se habilita: el amigo está en línea y el jugador tiene sala. El
        /// servidor vuelve a validar todo al recibir la invitación.
        /// </summary>
        public bool CanBeInvited { get; set; }
    }
}