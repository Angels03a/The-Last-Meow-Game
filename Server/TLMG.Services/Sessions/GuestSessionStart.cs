using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    public sealed class GuestSessionStart
    {
        private GuestSessionStart(string sessionToken, int userId, string nickname)
        {
            SessionToken = sessionToken;
            UserId = userId;
            Nickname = nickname;
        }

        public bool IsStarted => SessionToken != null;

        public string SessionToken { get; }

        public int UserId { get; }

        public string Nickname { get; }

        internal static GuestSessionStart CreateStarted(string sessionToken, PlayerSession guestSession)
        {
            return new GuestSessionStart(sessionToken, guestSession.UserId, guestSession.Nickname);
        }

        internal static GuestSessionStart CreateRejected()
        {
            return new GuestSessionStart(null, ValidationRules.NoUserId, null);
        }
    }
}