using System;

namespace TLMG.Services.Sessions
{
    /// <summary>Petición de envío de chat con el texto ya normalizado; solo transporta datos.</summary>
    public sealed class ChatSubmission
    {
        /// <summary>Sala indicada por el emisor.</summary>
        public int RoomId { get; set; }

        /// <summary>Correlación proporcionada por el emisor.</summary>
        public Guid ClientMessageId { get; set; }

        /// <summary>Texto recortado y normalizado; no se guarda en el registro.</summary>
        public string Text { get; set; }

        /// <summary>Resultado de validar el texto; null si es válido.</summary>
        public string ValidationErrorKey { get; set; }
    }
}