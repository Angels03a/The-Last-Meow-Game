using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Invitación a una sala recibida de un amigo; no incluye la sala, que el servidor revela al aceptarla.
    /// </summary>
    [DataContract]
    public class RoomInvitation
    {
        /// <summary>Identificador de la invitación, que el jugador usa para aceptarla.</summary>
        [DataMember]
        public int InvitationId { get; set; }

        /// <summary>Nickname del amigo que invita.</summary>
        [DataMember]
        public string InviterNickname { get; set; }
    }
}