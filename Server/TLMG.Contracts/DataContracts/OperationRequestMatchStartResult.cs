using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Response of the match start check: the room plus how long the client must wait before asking again.
    /// </summary>
    [DataContract]
    public class OperationRequestMatchStartResult : OperationLobbyRoomResult
    {
        /// <summary>Seconds to wait before checking again; 0 when there is no active countdown.</summary>
        [DataMember]
        public int RetryAfterSeconds { get; set; }
    }
}