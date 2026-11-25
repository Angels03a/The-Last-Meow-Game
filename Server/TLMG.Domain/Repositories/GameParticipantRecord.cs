using System;

namespace TLMG.Domain.Repositories
{
    /// <summary>Contains the final position, points and participation status of a registered player.</summary>
    public sealed class GameParticipantRecord
    {
        public int UserId { get; set; }
        public int? Position { get; set; }
        public int Points { get; set; }
        public GameParticipantFinalStatus FinalStatus { get; set; }
        public DateTime? LeftAtUtc { get; set; }
    }
}
