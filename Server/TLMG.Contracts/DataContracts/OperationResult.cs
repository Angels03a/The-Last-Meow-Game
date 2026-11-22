using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Base response of every server operation, used directly by operations that only report success or an error
    /// key so no empty result class is needed per operation. Public contract: on success IsSuccess is true and
    /// ErrorKey is string.Empty; on failure IsSuccess is false and ErrorKey is a non-empty key that the client
    /// resources recognize.
    /// </summary>
    [DataContract]
    public class OperationResult
    {
        /// <summary>Indicates whether the operation was accepted.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Resource key of the business error so the client translates it; string.Empty on success, never null.
        /// </summary>
        [DataMember]
        public string ErrorKey { get; set; }
    }
}