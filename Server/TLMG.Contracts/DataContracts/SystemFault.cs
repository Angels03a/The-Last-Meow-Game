using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// WCF fault contract used exclusively for catastrophic infrastructure 
    /// errors or network disconnections, never for business rules.
    /// </summary>

    [DataContract]
    public class SystemFault
    {
        [DataMember]
        public string ErrorKey { get; set; }
    }
}