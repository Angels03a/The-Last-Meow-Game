using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Carries the email, the already verified code and the new password; the database checks again that the
    /// code was verified, so a client cannot skip the verification step.
    /// </summary>
    [DataContract]
    public class OperationResetPasswordRequest
    {
        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string VerificationCode { get; set; }

        [DataMember]
        public string NewPassword { get; set; }
    }
}