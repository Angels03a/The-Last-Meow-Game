using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Request to check whether a server countdown has expired.</summary>
    [DataContract]
    public class OperationMatchStartRequest
    {
        /// <summary>Room for this request.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Countdown generation seen by the client.</summary>
        [DataMember]
        public long CountdownId { get; set; }
    }
}
