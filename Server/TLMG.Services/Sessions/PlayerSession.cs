using System;
using System.Collections.Generic;

using TLMG.Domain.Authorization;

namespace TLMG.Services.Sessions
{
    public sealed class PlayerSession
    {
        public PlayerSession(int userId, string nickname)
        {
            UserId = userId;
            Nickname = nickname;
        }

        internal bool IsRegistered
        {
            get
            {
                bool isRegistered = Role == AccountRole.RegisteredPlayer;

                return isRegistered;
            }
        }

        internal AccountRole Role { get; private set; }

        internal DateTime LastActivityUtc { get; set; }

        internal Dictionary<HeartbeatChannel, LinkHealth> Links { get; } =
            new Dictionary<HeartbeatChannel, LinkHealth>();

        public Guid DiagnosticId { get; } = Guid.NewGuid();

        public int UserId { get; }

        public string Nickname { get; internal set; }

        internal static PlayerSession CreateRegistered(int userId, string nickname)
        {
            PlayerSession session = new PlayerSession(userId, nickname) { Role = AccountRole.RegisteredPlayer };

            return session;
        }

        internal static PlayerSession CreateGuest(int guestUserId, string nickname)
        {
            PlayerSession session = new PlayerSession(guestUserId, nickname) { Role = AccountRole.Guest };

            return session;
        }
    }
}
