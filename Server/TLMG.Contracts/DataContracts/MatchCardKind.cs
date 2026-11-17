using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Tipo de carta que viaja por la red; Services lo traduce desde y hacia el motor.</summary>
    [DataContract]
    public enum MatchCardKind
    {
        /// <summary>Carta segura.</summary>
        [EnumMember]
        Safe,

        /// <summary>Gato con Maullido Fatal.</summary>
        [EnumMember]
        FatalCat
    }
}
