using System;
using System.Collections.Generic;

namespace TLMG.Domain.Repositories
{
    /// <summary>Contains the final match data captured before historical persistence.</summary>
    public sealed class GameFinalization
    {
        public Guid MatchId { get; set; }
        public DateTime EndedAtUtc { get; set; }
        public bool IsAbandoned { get; set; }
        public IReadOnlyList<GameParticipantRecord> Participants { get; set; }
    }
}
