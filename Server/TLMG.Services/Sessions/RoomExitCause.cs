namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Reason for leaving a room. A voluntary exit removes the player immediately from the match engine. A
    /// disconnection is handled through the reconnection window (C2).
    /// </summary>
    public enum RoomExitCause
    {
        /// <summary>Room change other than an exit, such as creating, entering or updating a room.</summary>
        None,

        /// <summary>The player chose to leave (LeaveLobby).</summary>
        Voluntary,

        /// <summary>The player's lobby channel closed or failed.</summary>
        LobbyChannelClosed,

        /// <summary>The session ended after logout or loss of the friends channel.</summary>
        SessionEnded,

        /// <summary>A new login to the same account replaced the session.</summary>
        SessionReplaced
    }
}
