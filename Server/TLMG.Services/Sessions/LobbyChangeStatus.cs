namespace TLMG.Services.Sessions
{
    /// <summary>Describes the outcome of a room operation.</summary>
    public enum LobbyChangeStatus
    {
        Applied,
        InvalidSession,
        AlreadyInRoom,
        NotAuthorized,
        RoomFull,
        NotInRoom,
        MatchAlreadyStarted,
        MatchStartPersistenceFailed
    }
}
