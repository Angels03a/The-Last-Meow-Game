using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Solicitud de amistad pendiente, vista desde el jugador que la recibió o la envió.
    /// </summary>
    [DataContract]
    public class FriendRequest
    {
        /// <summary>Identificador de la solicitud (id de Friendship); se envía al aceptarla o rechazarla.</summary>
        [DataMember]
        public int FriendRequestId { get; set; }

        /// <summary>Identificador de la cuenta de la otra persona (id de User).</summary>
        [DataMember]
        public int UserId { get; set; }

        /// <summary>Nickname de la otra persona.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>true si la recibió el jugador y puede responderla; false si la envió él y espera.</summary>
        [DataMember]
        public bool IsIncoming { get; set; }
    }
}
