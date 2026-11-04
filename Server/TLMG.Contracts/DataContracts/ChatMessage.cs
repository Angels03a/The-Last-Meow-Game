using System;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Mensaje aceptado; su retransmisión al emisor confirma la aceptación.</summary>
    [DataContract]
    public class ChatMessage
    {
        /// <summary>Sala destinataria.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Identificador recibido del emisor.</summary>
        [DataMember]
        public Guid ClientMessageId { get; set; }

        /// <summary>Cuenta emisora obtenida de la sesión vigente.</summary>
        [DataMember]
        public int SenderUserId { get; set; }

        /// <summary>Nickname obtenido de la sesión vigente.</summary>
        [DataMember]
        public string SenderNickname { get; set; }

        /// <summary>Texto validado y normalizado.</summary>
        [DataMember]
        public string Text { get; set; }

        /// <summary>Instante UTC reservado al aceptar el envío.</summary>
        [DataMember]
        public DateTime SentAtUtc { get; set; }
    }
}
