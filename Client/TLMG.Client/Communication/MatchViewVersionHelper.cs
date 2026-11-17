using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>Decide si una vista de partida recibida debe mostrarse o ya quedó obsoleta.</summary>
    public static class MatchViewVersionHelper
    {
        /// <summary>
        /// Compara la vista recibida con la que ya se muestra: solo una versión estrictamente mayor la reemplaza, así
        /// una vista repetida o que llega tarde nunca regresa el estado.
        /// </summary>
        /// <param name="displayedView">Vista que ya se muestra; null si todavía no hay ninguna.</param>
        /// <param name="incomingView">Vista recién recibida; puede ser null.</param>
        /// <returns>true si la vista recibida debe mostrarse; en caso contrario, false.</returns>
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
