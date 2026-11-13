using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Base response of every server operation, used directly by operations that only report success or an error
    /// key so no empty result class is needed per operation.
    /// </summary>
    [DataContract]
    public class OperationResult
    {
        /// <summary>Indicates whether the operation was accepted.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Resource key of the business error so the client translates it; null on success.</summary>
        [DataMember]
        public string ErrorKey { get; set; }
    }
}