namespace TLMG.Client.Models
{
    /// <summary>
    /// Player shown on LobbyScreen, with their nickname and whether they are the local player.
    /// </summary>
    public class LobbyPlayerEntry
    {
        /// <summary>
        /// Player's nickname as received from the server.
        /// </summary>
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// Whether this player owns the local session. This changes only how the player card looks and grants no
        /// permissions.
        /// </summary>
        public bool IsLocalPlayer { get; set; }

        /// <summary>Ready state received from the server.</summary>
        public bool IsReady { get; set; }
    }
}