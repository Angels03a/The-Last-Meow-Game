using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Room ID and current players.
    /// </summary>
    [DataContract]
    public class LobbyRoom
    {
        /// <summary>Room ID.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Players in the order they joined the room.</summary>
        [DataMember]
        public List<LobbyPlayer> Players { get; set; }

        /// <summary>Increasing version used to discard late updates for this room.</summary>
        [DataMember]
        public long Version { get; set; }

        /// <summary>Latest countdown generation. Active only when CountdownEndsAtUtc has a value.</summary>
        [DataMember]
        public long CountdownId { get; set; }

        /// <summary>Server deadline in UTC, or null without an active countdown.</summary>
        [DataMember]
        public DateTime? CountdownEndsAtUtc { get; set; }

        /// <summary>Snapshot time in UTC. The server decides when the countdown expires.</summary>
        [DataMember]
        public DateTime ServerTimeUtc { get; set; }

        /// <summary>Whether the server authorized entry to the match.</summary>
        [DataMember]
        public bool InGame { get; set; }

        /// <summary>Stable ID of the authorized match, or null before the start.</summary>
        [DataMember]
        public Guid? MatchId { get; set; }

        /// <summary>Authorization time in UTC, or null before the start.</summary>
        [DataMember]
        public DateTime? MatchStartedAtUtc { get; set; }

        /// <summary>Cards per player for the authorized or upcoming match.</summary>
        [DataMember]
        public int CardsPerPlayerCount { get; set; }
    }
}