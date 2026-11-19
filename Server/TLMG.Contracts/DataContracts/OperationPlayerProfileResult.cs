using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Carries the editable profile data of the session owner back to the profile screen.
    /// </summary>
    [DataContract]
    public class OperationPlayerProfileResult : OperationResult
    {
        [DataMember]
        public string Nickname { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string AvatarKey { get; set; }
    }
}