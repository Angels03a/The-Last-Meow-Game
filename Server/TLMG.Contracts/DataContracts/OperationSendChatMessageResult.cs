using System;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Rechazo de un envío; la aceptación se confirma mediante el eco.</summary>
    [DataContract]
    public class OperationSendChatMessageResult
    {
        /// <summary>Identificador recibido; vacío si no había petición.</summary>
        [DataMember]
        public Guid ClientMessageId { get; set; }

        /// <summary>Clave del motivo del rechazo, sin contenido del mensaje.</summary>
        [DataMember]
        public string ErrorKey { get; set; }
    }
}
