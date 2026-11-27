using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Identifies the target and reason for a report; the server resolves the reporter and room.
    /// </summary>
    [DataContract]
    public sealed class ReportSubmission
    {
        [DataMember]
        public int TargetUserId { get; set; }

        /// <summary>Identifies a supported report reason by its stable name, independent of the display language.</summary>
        [DataMember]
        public string ReasonCode { get; set; } = string.Empty;

        [DataMember]
        public string Comment { get; set; } = string.Empty;
    }
}
