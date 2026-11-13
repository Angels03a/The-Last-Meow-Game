namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Fila que devuelve el procedimiento InsertChatMessage; solo se usa para materializar su respuesta.
    /// </summary>
    public sealed class ChatInsertOutcome
    {
        /// <summary>Estado que devolvió el procedimiento: Saved, InvalidText, UserNotFound o InvalidContext.</summary>
        public string Status { get; set; }
    }
}