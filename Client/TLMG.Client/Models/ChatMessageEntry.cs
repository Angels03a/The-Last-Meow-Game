namespace TLMG.Client.Models
{
    /// <summary>
    /// Representa un mensaje de chat tal como se muestra en LobbyScreen.
    /// </summary>
    public class ChatMessageEntry
    {
        /// <summary>
        /// Obtiene o establece el nickname de quien envió el mensaje.
        /// </summary>
        public string SenderNickname { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el texto del mensaje.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece si el mensaje lo envió el jugador de este cliente. Solo cambia el color del nombre.
        /// </summary>
        public bool IsLocalSender { get; set; }
    }
}