using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Estado y desenlace público del minijuego.</summary>
    [DataContract]
    public enum FugitiveKittenResult
    {
        [EnumMember] InProgress,
        [EnumMember] Caught,
        [EnumMember] Escaped
    }
}
