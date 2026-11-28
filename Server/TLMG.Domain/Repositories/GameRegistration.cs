using System;
using System.Collections.Generic;

namespace TLMG.Domain.Repositories
{
    public sealed class GameRegistration
    {
        public Guid MatchId { get; set; }
        public DateTime LobbyOpenedAtUtc { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public int CardsPerPlayerCount { get; set; }
        public IReadOnlyList<int> RegisteredUserIds { get; set; }
    }
}
