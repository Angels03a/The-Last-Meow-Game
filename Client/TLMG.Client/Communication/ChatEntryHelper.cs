using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>Convierte los mensajes de chat recibidos en las filas que muestran las pantallas.</summary>
    internal static class ChatEntryHelper
    {
        /// <summary>Crea la fila de chat de un mensaje recibido.</summary>
        /// <param name="chatMessage">Mensaje recibido del servidor.</param>
        /// <param name="localUserId">Id del jugador de este cliente, para marcar sus propios mensajes.</param>
        /// <returns>La fila lista para la lista de mensajes de la pantalla.</returns>
        internal static ChatMessageEntry CreateEntry(ChatMessage chatMessage, int localUserId)
        {
            ChatMessageEntry chatMessageEntry = new ChatMessageEntry
            {
                SenderNickname = chatMessage.SenderNickname,
                Text = chatMessage.Text,
                IsLocalSender = chatMessage.SenderUserId == localUserId
            };

            return chatMessageEntry;
        }
    }
}