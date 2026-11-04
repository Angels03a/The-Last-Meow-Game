using System;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Petición de envío de un mensaje al chat de la sala.</summary>
    [DataContract]
    public class OperationSendChatMessageRequest
    {
        /// <summary>Identificador del cliente para correlacionar el eco o el rechazo; no evita duplicados.</summary>
        [DataMember]
        public Guid ClientMessageId { get; set; }

        /// <summary>Sala a la que el emisor debe pertenecer.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Texto que el servidor recorta y normaliza antes de validar.</summary>
        [DataMember]
        public string Text { get; set; }
    }
}
