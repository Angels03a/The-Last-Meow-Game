namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Acceso a la persistencia del chat; el historial sirve solo como evidencia para reportes.
    /// </summary>
    public interface IChatMessageRepository
    {
        /// <summary>Guarda un mensaje de chat ya aceptado por el servidor.</summary>
        /// <param name="chatMessageRecord">Datos del mensaje; no debe ser null.</param>
        /// <returns>
        /// Saved si quedó guardado, un rechazo esperado, o DataFailure si la base de datos falló; un fallo nunca
        /// debe impedir que el mensaje llegue a los jugadores.
        /// </returns>
        ChatMessageSaveStatus SaveChatMessage(ChatMessageRecord chatMessageRecord);
    }
}