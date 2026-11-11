using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Callback con el que el servidor responde las operaciones de sala y avisa sus cambios; lo implementa el cliente.
    /// </summary>
    [ServiceContract]
    public interface ILobbyManagerCallback
    {
        /// <summary>Entrega el resultado de crear una sala al cliente.</summary>
        /// <param name="operationCreateRoomResult">Resultado con la sala o la clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void CreateRoomResult(OperationCreateRoomResult operationCreateRoomResult);

        /// <summary>Entrega el resultado de entrar a una sala al cliente.</summary>
        /// <param name="operationEnterLobbyResult">Resultado con la sala o la clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void EnterLobbyResult(OperationEnterLobbyResult operationEnterLobbyResult);

        /// <summary>Entrega el resultado de salir de una sala al cliente.</summary>
        /// <param name="operationLeaveLobbyResult">Resultado con éxito o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void LeaveLobbyResult(OperationLeaveLobbyResult operationLeaveLobbyResult);

        /// <summary>Avisa cambios de miembros, estado Listo, cuenta o inicio autorizado de la partida.</summary>
        /// <param name="lobbyRoom">La sala con la lista actualizada de jugadores.</param>
        [OperationContract(IsOneWay = true)]
        void OnLobbyRoomChanged(LobbyRoom lobbyRoom);

        /// <summary>Entrega el resultado de ChangePlayerReadiness al solicitante.</summary>
        /// <param name="result">Resultado con el estado actual o la clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void ChangePlayerReadinessResult(OperationChangePlayerReadinessResult result);

        /// <summary>Entrega el resultado de ChangeCardsPerPlayer al solicitante.</summary>
        /// <param name="result">Resultado con el estado actual de la sala o la clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void ChangeCardsPerPlayerResult(OperationChangeCardsPerPlayerResult result);

        /// <summary>Entrega el resultado de RequestMatchStart al solicitante.</summary>
        /// <param name="result">Resultado con el estado actual o la clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void RequestMatchStartResult(OperationRequestMatchStartResult result);
    }
}