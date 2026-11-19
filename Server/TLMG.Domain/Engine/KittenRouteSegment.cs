namespace TLMG.Domain.Engine
{
    /// <summary>Tramo inmutable de la ruta del gatito.</summary>
    public sealed class KittenRouteSegment
    {
        internal KittenRouteSegment(string kind, string origin, string destination, int startMilliseconds,
            int endMilliseconds, bool isCatchable)
        {
            Kind = kind;
            Origin = origin;
            Destination = destination;
            StartMilliseconds = startMilliseconds;
            EndMilliseconds = endMilliseconds;
            IsCatchable = isCatchable;
        }

        public string Kind { get; }
        public string Origin { get; }
        public string Destination { get; }
        public int StartMilliseconds { get; }
        public int EndMilliseconds { get; }
        public bool IsCatchable { get; }
    }
}
