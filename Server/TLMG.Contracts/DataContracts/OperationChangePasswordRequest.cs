using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Groups the data of a password change so the contract stays under three parameters and the session, not a
    /// client-supplied user id, identifies the owner.
    /// </summary>
    [DataContract]
    public class OperationChangePasswordRequest
    {
        [DataMember]
        public string SessionToken { get; set; }

        [DataMember]
        public string CurrentPassword { get; set; }

        [DataMember]
        public string NewPassword { get; set; }
    }
}