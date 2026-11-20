using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Public minigame state and outcome.</summary>
    [DataContract]
    public enum FugitiveKittenResult
    {
        [EnumMember] InProgress,
        [EnumMember] Caught,
        [EnumMember] Escaped
    }
}
