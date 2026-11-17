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
    }
}
