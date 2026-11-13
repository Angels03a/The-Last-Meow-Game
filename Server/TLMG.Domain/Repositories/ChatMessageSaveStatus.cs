namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de guardar un mensaje de chat; separa los rechazos esperados de un fallo de datos.
    /// </summary>
    public enum ChatMessageSaveStatus
    {
        /// <summary>El mensaje quedó guardado.</summary>
        Saved,

        /// <summary>El texto no cumple la longitud permitida.</summary>
        InvalidText,

        /// <summary>La cuenta emisora no existe.</summary>
        UserNotFound,

        /// <summary>El ámbito no tiene la partida o la sala que exige.</summary>
        InvalidContext,

        /// <summary>La base de datos falló; el detalle ya quedó en el log.</summary>
        DataFailure
    }
}