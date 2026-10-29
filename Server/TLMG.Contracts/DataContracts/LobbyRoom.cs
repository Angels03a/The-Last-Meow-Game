using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Estado de una sala: su identificador y los jugadores que están dentro.
    /// </summary>
    [DataContract]
    public class LobbyRoom
    {
        /// <summary>Identificador de la sala.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Jugadores dentro de la sala, en el orden en que entraron.</summary>
        [DataMember]
        public List<LobbyPlayer> Players { get; set; }
    }
}