using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Recipient's prepared view and callback for delivery outside the lock.</summary>
    public sealed class MatchViewDelivery
    {
        /// <summary>Creates the delivery.</summary>
        /// <param name="callback">Recipient callback copied under the lock.</param>
        /// <param name="matchView">Recipient's private view.</param>
        public MatchViewDelivery(IMatchManagerCallback callback, MatchView matchView)
        {
            Callback = callback;
            MatchView = matchView;
        }

        /// <summary>Recipient callback.</summary>
        public IMatchManagerCallback Callback { get; }

        /// <summary>Recipient's private view.</summary>
        public MatchView MatchView { get; }
    }
}
