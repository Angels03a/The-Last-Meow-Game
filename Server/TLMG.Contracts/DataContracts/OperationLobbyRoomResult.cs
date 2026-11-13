using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Response of every room operation that answers with the current room, so create, enter and the room settings
    /// changes share one contract instead of one identical class each.
    /// </summary>
    [DataContract]
    public class OperationLobbyRoomResult : OperationResult
    {
        /// <summary>Current room after the operation; null when the operation failed.</summary>
        [DataMember]
        public LobbyRoom Room { get; set; }
    }
}