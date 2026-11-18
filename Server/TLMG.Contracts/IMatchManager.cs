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

        /// <summary>
        /// Voltea la carta de arriba de la pila elegida durante el reto del jugador. Primero hay que voltear la pila
        /// propia y después se elige una pila ajena antes de cada volteo. La revelación se detiene al cumplir la
        /// apuesta o al voltear un Gato con Maullido Fatal.
        /// </summary>
        /// <param name="sessionToken">Token vigente del jugador registrado.</param>
        /// <param name="pileOwnerUserId">Cuenta del dueño de la pila de la que se voltea.</param>
        [OperationContract(IsOneWay = true)]
        void RevealCard(string sessionToken, int pileOwnerUserId);

        /// <summary>
        /// Elige la carta que pierde el jugador castigado. Si el castigo es a ciegas, el índice es el lugar que se
        /// muestra (de 0 a CardSlotCount - 1) y el servidor lo traduce; si el castigado elige viendo su mano, es la
        /// posición de la carta en OwnHandCards.
        /// </summary>
        /// <param name="sessionToken">Token vigente del jugador registrado.</param>
        /// <param name="cardIndex">Lugar o posición de la carta que se elimina.</param>
        [OperationContract(IsOneWay = true)]
        void ChoosePunishmentCard(string sessionToken, int cardIndex);
    }
}
