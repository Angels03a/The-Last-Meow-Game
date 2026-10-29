using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Datos de un jugador dentro de una sala.
    /// </summary>
    [DataContract]
    public class LobbyPlayer
    {
        /// <summary>Identificador de la cuenta del jugador (id de User).</summary>
        [DataMember]
        public int UserId { get; set; }

        /// <summary>Nickname del jugador.</summary>
        [DataMember]
        public string Nickname { get; set; }
    }
}