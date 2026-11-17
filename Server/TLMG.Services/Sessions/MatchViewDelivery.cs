using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Vista ya armada para un destinatario y el callback al que se entrega fuera del candado.</summary>
    public sealed class MatchViewDelivery
    {
        /// <summary>Crea la entrega.</summary>
        /// <param name="callback">Callback del destinatario, copiado dentro del candado.</param>
        /// <param name="matchView">Vista privada del destinatario.</param>
        public MatchViewDelivery(IMatchManagerCallback callback, MatchView matchView)
        {
            Callback = callback;
            MatchView = matchView;
        }

        /// <summary>Obtiene el callback del destinatario.</summary>
        public IMatchManagerCallback Callback { get; }

        /// <summary>Obtiene la vista privada del destinatario.</summary>
        public MatchView MatchView { get; }
    }
}
