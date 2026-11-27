namespace TLMG.Services.Sessions
{
    public enum LobbyChangeStatus
    {
        Applied,
        InvalidSession,
        AlreadyInRoom,
        NotAuthorized,
        RoomFull,
        NotInRoom,
        MatchAlreadyStarted,
        MatchStartPersistenceFailed,
        DataFailure
    }
}