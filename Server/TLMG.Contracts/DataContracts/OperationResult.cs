using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Base class for all server operation responses. 
    /// Standardizes the success state and business error key.
    /// </summary>
   
    [DataContract]
    public abstract class OperationResult
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorKey { get; set; }
    }
}