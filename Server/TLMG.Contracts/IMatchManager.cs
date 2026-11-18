using System;
using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>Canal dúplex propio para seguir una partida y colocar cartas; el servidor decide.</summary>
    [ServiceContract(CallbackContract = typeof(IMatchManagerCallback))]
    public interface IMatchManager
    {
        /// <summary>
        /// Suscribe el canal a una partida a la que pertenece el jugador; la última suscripción del jugador gana. El
        /// resultado y la vista privada del jugador llegan por callback.
        /// </summary>
        /// <param name="sessionToken">Token vigente del jugador registrado.</param>
        /// <param name="matchId">Partida autorizada al iniciar la sala; se obtiene de la sala.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToMatch(string sessionToken, Guid matchId);

        /// <summary>
        /// Coloca una carta de la mano del jugador en su pila de la partida suscrita. El resultado solo informa la
        /// aceptación o el rechazo; el nuevo estado llega como vista privada a cada jugador suscrito.
        /// </summary>
        /// <param name="sessionToken">Token vigente del jugador registrado.</param>
        /// <param name="cardKind">Tipo de la carta que se coloca.</param>
        [OperationContract(IsOneWay = true)]
        void PlaceCard(string sessionToken, MatchCardKind cardKind);

        /// <summary>
        /// Abre una apuesta o sube la que está en curso, según la fase: el servidor decide cuál de las dos es. El
        /// valor debe cumplir el rango de la vista (MinimumBetValue y MaximumBetValue). El resultado solo informa la
        /// aceptación o el rechazo; el nuevo estado llega como vista privada a cada jugador suscrito.
        /// </summary>
        /// <param name="sessionToken">Token vigente del jugador registrado.</param>
        /// <param name="betValue">Cartas que el jugador declara poder voltear.</param>
        [OperationContract(IsOneWay = true)]
        void PlaceBet(string sessionToken, int betValue);

        /// <summary>
        /// Retira al jugador de la apuesta en curso; ya no puede volver a esa apuesta. Cuando queda un solo
        /// apostador, el reto queda listo para que lo cumpla.
        /// </summary>
        /// <param name="sessionToken">Token vigente del jugador registrado.</param>
        [OperationContract(IsOneWay = true)]
        void WithdrawFromBet(string sessionToken);
    }
}
