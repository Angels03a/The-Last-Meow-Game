namespace TLMG.Services.Sessions
{
    public sealed class LobbyDeparture
    {
        public LobbyDeparture(int userId, RoomExitCause exitCause, bool leftAuthorizedMatch)
        {
            UserId = userId;
            ExitCause = exitCause;
            LeftAuthorizedMatch = leftAuthorizedMatch;
        }

        public int UserId { get; }

        public RoomExitCause ExitCause { get; }

        public bool LeftAuthorizedMatch { get; }
    }
}
