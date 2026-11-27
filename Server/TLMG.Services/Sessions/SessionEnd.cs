using System;
using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    public sealed class SessionEnd
    {
        public SessionEnd(int userId, string nickname, LobbyMembershipChange roomDeparture,
            IFriendsManagerCallback friendsCallback, IReadOnlyList<IFriendsManagerCallback> presenceRecipientCallbacks)
        {
            UserId = userId;
            Nickname = nickname;
            RoomDeparture = roomDeparture;
            FriendsCallback = friendsCallback;
            PresenceRecipientCallbacks = presenceRecipientCallbacks;
        }

        public int UserId { get; }

        internal IReadOnlyList<MatchViewDelivery> MatchViews { get; set; } = Array.Empty<MatchViewDelivery>();

        public string Nickname { get; }

        public LobbyMembershipChange RoomDeparture { get; }

        public IFriendsManagerCallback FriendsCallback { get; }

        public IReadOnlyList<IFriendsManagerCallback> PresenceRecipientCallbacks { get; }
    }
}
