using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    internal sealed class LobbyDepartureDelivery
    {
        public LobbyDepartureDelivery(LobbyMembershipChange membership, IReadOnlyList<MatchViewDelivery> matchViews)
        {
            Membership = membership;
            MatchViews = matchViews;
        }

        public LobbyMembershipChange Membership { get; }

        public IReadOnlyList<MatchViewDelivery> MatchViews { get; }
    }
}
