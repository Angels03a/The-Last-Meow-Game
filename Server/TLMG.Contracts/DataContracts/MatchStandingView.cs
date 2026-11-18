using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Resultado público de un participante.</summary>
    [DataContract]
    public class MatchStandingView
    {
        /// <summary>Identidad pública del participante.</summary>
        [DataMember]
        public int UserId { get; set; }

        /// <summary>Nombre visible congelado al iniciar.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Posición final calculada por el motor.</summary>
        [DataMember]
        public int Position { get; set; }

        /// <summary>Puntos calculados por el motor.</summary>
        [DataMember]
        public int Points { get; set; }
    }
}
