using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Player details within a room.
    /// </summary>
    [DataContract]
    public class LobbyPlayer
    {
        /// <summary>Player's account ID (User ID).</summary>
        [DataMember]
        public int UserId { get; set; }

        /// <summary>Player's nickname.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Whether the player is ready. Always false on entry.</summary>
        [DataMember]
        public bool IsReady { get; set; }
    }
}