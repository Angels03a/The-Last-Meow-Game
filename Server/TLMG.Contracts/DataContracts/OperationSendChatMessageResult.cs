using System;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Rechazo de un envío; la aceptación se confirma mediante el eco.</summary>
    [DataContract]
    public class OperationSendChatMessageResult : OperationResult
    {
        /// <summary>Identificador recibido; vacío si no había petición.</summary>
        [DataMember]
        public Guid ClientMessageId { get; set; }
    }
}
