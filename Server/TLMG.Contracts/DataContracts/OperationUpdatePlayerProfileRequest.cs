using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Groups the profile fields the player can change so the contract stays under three parameters and the
    /// session identifies the owner instead of a client-supplied user id.
    /// </summary>
    [DataContract]
    public class OperationUpdatePlayerProfileRequest
    {
        [DataMember]
        public string SessionToken { get; set; }

        [DataMember]
        public string Nickname { get; set; }

        [DataMember]
        public string AvatarKey { get; set; }
    }
}