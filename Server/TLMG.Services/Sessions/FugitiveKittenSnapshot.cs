using System;
using TLMG.Domain.Engine;

namespace TLMG.Services.Sessions
{
    internal sealed class FugitiveKittenSnapshot
    {
        internal DateTime StartedAtUtc { get; set; }
        internal DateTime ExpiresAtUtc { get; set; }
        internal DateTime? PausedAtUtc { get; set; }
        internal FugitiveKittenRoute Route { get; set; }
        internal string Color { get; set; }
        internal FugitiveKittenOutcome Outcome { get; set; }
        internal int? WinnerUserId { get; set; }
        internal int Version { get; set; }
    }
}
