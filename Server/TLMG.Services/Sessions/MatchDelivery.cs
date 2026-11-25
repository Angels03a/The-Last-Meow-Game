using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Immutable match operation result containing an error key for rejection and views to send if state changed. Uses
    /// no shared state after creation.
    /// </summary>
    public sealed class MatchDelivery
    {
        private static readonly IReadOnlyList<MatchViewDelivery> _noViewDeliveries =
            new List<MatchViewDelivery>().AsReadOnly();

        private MatchDelivery(string errorKey, IReadOnlyList<MatchViewDelivery> viewDeliveries)
        {
            ErrorKey = errorKey;
            ViewDeliveries = viewDeliveries;
        }

        /// <summary>Error key, or null on acceptance.</summary>
        public string ErrorKey { get; }

        /// <summary>Views to deliver, always present and empty for a rejection without a state change.</summary>
        public IReadOnlyList<MatchViewDelivery> ViewDeliveries { get; }

        /// <summary>Creates a rejected operation result.</summary>
        /// <param name="errorKey">Error key for the requester.</param>
        /// <returns>Result without views.</returns>
        public static MatchDelivery CreateRejected(string errorKey)
        {
            MatchDelivery matchDelivery = new MatchDelivery(errorKey, _noViewDeliveries);

            return matchDelivery;
        }

        /// <summary>
        /// Creates a rejection that still carries a match change caused by an earlier deadline.
        /// </summary>
        /// <param name="errorKey">Error key for the requester.</param>
        /// <param name="viewDeliveries">Views for the earlier change. Leave the list unchanged afterward.</param>
        /// <returns>Result with error key and views.</returns>
        public static MatchDelivery CreateRejected(string errorKey, IReadOnlyList<MatchViewDelivery> viewDeliveries)
        {
            MatchDelivery matchDelivery = new MatchDelivery(errorKey, viewDeliveries);

            return matchDelivery;
        }

        /// <summary>Creates an accepted operation result.</summary>
        /// <param name="viewDeliveries">Views to deliver. Leave the list unchanged afterward.</param>
        /// <returns>Result without an error key.</returns>
        public static MatchDelivery CreateAccepted(IReadOnlyList<MatchViewDelivery> viewDeliveries)
        {
            MatchDelivery matchDelivery = new MatchDelivery(null, viewDeliveries);

            return matchDelivery;
        }
    }
}
