using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Requested number of cards per player for the room.</summary>
    [DataContract]
    public class OperationChangeCardsPerPlayerRequest
    {
        /// <summary>Room for this request.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Requested cards per player (4, 6 or 8). Repeating the current value preserves readiness.</summary>
        [DataMember]
        public int CardsPerPlayerCount { get; set; }
    }
}
