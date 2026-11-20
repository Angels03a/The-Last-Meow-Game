using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Card type sent over the network. Services maps it to and from the engine type.</summary>
    [DataContract]
    public enum MatchCardKind
    {
        /// <summary>Safe card.</summary>
        [EnumMember]
        Safe,

        /// <summary>Fatal Cat card.</summary>
        [EnumMember]
        FatalCat
    }
}
