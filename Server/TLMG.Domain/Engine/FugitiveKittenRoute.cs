using System;
using System.Collections.Generic;

namespace TLMG.Domain.Engine
{
    /// <summary>Catálogo autoritativo de la ruta del gatito fugitivo.</summary>
    public sealed class FugitiveKittenRoute
    {
        public const int WindowMilliseconds = 15000;
        public const int CatchToleranceMilliseconds = 150;
        private const int StartDurationMilliseconds = 900;
        private const int HalfLaneMilliseconds = 900;
        private const int FullLaneMilliseconds = 1800;
        private const int HiddenMilliseconds = 1000;
        private readonly IReadOnlyList<KittenRouteSegment> _segments;

        public FugitiveKittenRoute()
        {
            List<KittenRouteSegment> segments = new List<KittenRouteSegment>();
            AddSegment(segments, "Start", "P1", "P1", StartDurationMilliseconds, false);
            AddSegment(segments, "Run", "P1", "E1", HalfLaneMilliseconds, true);
            AddSegment(segments, "Hide", "E1", "E1", HiddenMilliseconds, false);
            AddSegment(segments, "Run", "E1", "P2", HalfLaneMilliseconds, true);
            AddSegment(segments, "Tunnel", "P2", "P3", HiddenMilliseconds, false);
            AddSegment(segments, "Run", "P3", "P4", FullLaneMilliseconds, true);
            AddSegment(segments, "Tunnel", "P4", "P2", HiddenMilliseconds, false);
            AddSegment(segments, "Run", "P2", "E1", HalfLaneMilliseconds, true);
            AddSegment(segments, "Hide", "E1", "E1", HiddenMilliseconds, false);
            AddSegment(segments, "Run", "E1", "P1", HalfLaneMilliseconds, true);
            AddSegment(segments, "Tunnel", "P1", "P4", HiddenMilliseconds, false);
            AddSegment(segments, "Run", "P4", "E2", HalfLaneMilliseconds, true);
            AddSegment(segments, "Hide", "E2", "E2", HiddenMilliseconds, false);
            AddSegment(segments, "Run", "E2", "P3", HalfLaneMilliseconds, true);
            int escapeMilliseconds = WindowMilliseconds - segments[segments.Count - 1].EndMilliseconds;
            AddSegment(segments, "Escape", "P3", "P3", escapeMilliseconds, false);
            _segments = segments.AsReadOnly();
        }

        public IReadOnlyList<KittenRouteSegment> Segments => _segments;

        /// <summary>Indica si la hora del servidor permite atrapar, incluida la tolerancia.</summary>
        public bool CanCatchAt(TimeSpan elapsed)
        {
            bool canCatch = false;
            double elapsedMilliseconds = elapsed.TotalMilliseconds;
            if (elapsedMilliseconds >= 0 && elapsedMilliseconds < WindowMilliseconds)
            {
                foreach (KittenRouteSegment segment in _segments)
                {
                    if (segment.IsCatchable && elapsedMilliseconds >= segment.StartMilliseconds
                        && elapsedMilliseconds <= segment.EndMilliseconds + CatchToleranceMilliseconds)
                    {
                        canCatch = true;
                        break;
                    }
                }
            }
            return canCatch;
        }

        private static void AddSegment(List<KittenRouteSegment> segments, string kind, string origin,
            string destination, int durationMilliseconds, bool isCatchable)
        {
            int start = segments.Count == 0 ? 0 : segments[segments.Count - 1].EndMilliseconds;
            segments.Add(new KittenRouteSegment(kind, origin, destination, start, start + durationMilliseconds,
                isCatchable));
        }
    }
}
