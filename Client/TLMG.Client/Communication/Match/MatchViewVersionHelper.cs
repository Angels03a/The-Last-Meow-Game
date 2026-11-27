using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>Checks whether an incoming match view is current enough to display.</summary>
    public static class MatchViewVersionHelper
    {
        /// <summary>
        /// Replaces the displayed view only with a strictly newer version, so late or duplicate views cannot roll back
        /// the board.
        /// </summary>
        /// <param name="displayedView">Displayed view, or null before the first view arrives.</param>
        /// <param name="incomingView">Incoming view, which may be null.</param>
        /// <returns>True if the incoming view should be displayed.</returns>
        public static bool ShouldDisplay(MatchView displayedView, MatchView incomingView)
        {
            bool shouldDisplay = false;

            if (incomingView != null)
            {
                shouldDisplay = displayedView == null || incomingView.Version > displayedView.Version;
            }

            return shouldDisplay;
        }
    }
}
