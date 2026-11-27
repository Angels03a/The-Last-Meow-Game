using System;
using System.Collections.Generic;

using TLMG.Domain.Moderation;

namespace TLMG.Services.Sessions
{
    public sealed class KickVoteChange
    {
        public KickVoteChange(KickVoteStatus status, IReadOnlyList<Action> notifications)
        {
            Status = status;
            Notifications = notifications;
        }

        public KickVoteStatus Status { get; }

        public IReadOnlyList<Action> Notifications { get; }
    }
}
