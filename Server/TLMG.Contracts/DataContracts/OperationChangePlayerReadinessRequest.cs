using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Request to set the player's ready state.</summary>
    [DataContract]
    public class OperationChangePlayerReadinessRequest
    {
        /// <summary>Room for this request.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Requested ready state. Repeating it leaves the countdown unchanged.</summary>
        [DataMember]
        public bool IsReady { get; set; }
    }
}
