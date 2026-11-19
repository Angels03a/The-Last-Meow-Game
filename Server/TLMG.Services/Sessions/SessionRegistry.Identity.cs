using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry : ISessionIdentityProvider
    {
        public bool TryGetUserId(string sessionToken, out int userId)
        {
            PlayerSession playerSession;
            bool wasFound = TryFindSessionByToken(sessionToken, out playerSession);
            userId = ValidationRules.NoUserId;

            if (wasFound)
            {
                userId = playerSession.UserId;
            }

            return wasFound;
        }

        public bool TryUpdateNickname(string sessionToken, string nickname)
        {
            bool wasUpdated = false;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                lock (_sessionsLock)
                {
                    PlayerSession playerSession;

                    if (_sessionsByToken.TryGetValue(sessionToken, out playerSession))
                    {
                        playerSession.Nickname = nickname;
                        wasUpdated = true;
                    }
                }
            }

            return wasUpdated;
        }
    }
}