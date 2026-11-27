using System;
using System.Collections.Generic;

using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    internal sealed class LobbyStartState
    {
        private const int DefaultCardsPerPlayerCount = ValidationRules.FourCardsPerPlayerCount;

        public Guid RoomScopeId { get; set; }

        public RoomKickVoting KickVoting { get; set; } = new RoomKickVoting();

        public HashSet<int> ReadyUserIds { get; } = new HashSet<int>();

        public DateTime LobbyOpenedAtUtc { get; set; }

        public long Version { get; set; }

        public long CountdownId { get; set; }

        public DateTime? CountdownEndsAtUtc { get; set; }

        public Guid? MatchId { get; set; }

        public DateTime? MatchStartedAtUtc { get; set; }

        public int CardsPerPlayerCount { get; set; } = DefaultCardsPerPlayerCount;
    }
}
