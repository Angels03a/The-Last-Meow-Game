namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Result of trying to change a player's room membership.
    /// </summary>
    public enum LobbyChangeStatus
    {
        /// <summary>The change was applied.</summary>
        Applied,

        /// <summary>The token does not belong to a current session.</summary>
        InvalidSession,

        /// <summary>The player or calling channel is already in a room.</summary>
        AlreadyInRoom,

        /// <summary>The room is missing or the player lacks entry permission.</summary>
        NotAuthorized,

        /// <summary>The room is full. Only an already authorized player receives this result.</summary>
        RoomFull,

        /// <summary>The player was outside every room.</summary>
        NotInRoom,

        /// <summary>The match is already authorized and readiness can no longer change.</summary>
        MatchAlreadyStarted,

        /// <summary>The match could not be started because its persistence failed.</summary>
        MatchStartPersistenceFailed
    }
}
