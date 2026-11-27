using System;

namespace TLMG.Domain.Repositories
{
    public sealed class PlayerReport
    {
        public int ReporterUserId { get; set; }

        public int ReportedUserId { get; set; }

        public Guid RoomScopeId { get; set; }

        public Guid? MatchId { get; set; }

        public ReportReason Reason { get; set; }

        public string Comment { get; set; } = string.Empty;
    }
}
