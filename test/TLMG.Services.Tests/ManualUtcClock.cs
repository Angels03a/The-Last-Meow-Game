using System;

namespace TLMG.Services.Tests
{
    /// <summary>Reloj UTC que la prueba avanza a mano, para vencer la cuenta regresiva sin esperar.</summary>
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
