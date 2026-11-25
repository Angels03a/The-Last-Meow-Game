using System;

namespace TLMG.Services.Tests
{
    /// <summary>UTC clock advanced by the test to expire a countdown without waiting.</summary>
    internal sealed class ManualUtcClock
    {
        private static readonly DateTime _startUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        internal DateTime NowUtc { get; private set; } = _startUtc;

        internal void Advance(TimeSpan elapsed)
        {
            NowUtc = NowUtc.Add(elapsed);
        }

        internal DateTime ReadUtcNow()
        {
            return NowUtc;
        }
    }
}
