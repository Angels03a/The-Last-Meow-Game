using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Callback with which the server answers room operations and announces room changes; the client implements
    /// it.
    /// </summary>
    [ServiceContract]
    public interface ILobbyManagerCallback
    {
        /// <summary>Delivers the result of creating a room.</summary>
        /// <param name="operationCreateRoomResult">The room or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void CreateRoomResult(OperationLobbyRoomResult operationCreateRoomResult);

        /// <summary>Delivers the result of entering a room.</summary>
        /// <param name="operationEnterLobbyResult">The room or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void EnterLobbyResult(OperationLobbyRoomResult operationEnterLobbyResult);

        /// <summary>Delivers the result of leaving a room.</summary>
        /// <param name="operationLeaveLobbyResult">Success or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void LeaveLobbyResult(OperationResult operationLeaveLobbyResult);

        /// <summary>
        /// Announces member changes, Ready state, countdown or the authorized match start so every client keeps the
        /// same room without asking for it.
        /// </summary>
        /// <param name="lobbyRoom">The room with the updated list of players.</param>
        [OperationContract(IsOneWay = true)]
        void OnLobbyRoomChanged(LobbyRoom lobbyRoom);

        /// <summary>Delivers the result of changing the Ready state.</summary>
        /// <param name="result">The current room or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void ChangePlayerReadinessResult(OperationLobbyRoomResult result);

        /// <summary>Delivers the result of changing the cards per player.</summary>
        /// <param name="result">The current room or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void ChangeCardsPerPlayerResult(OperationLobbyRoomResult result);

        /// <summary>Delivers the result of the match start check.</summary>
        /// <param name="result">The current room, the retry delay or the error key.</param>
        [OperationContract(IsOneWay = true)]
        void RequestMatchStartResult(OperationRequestMatchStartResult result);
    }
}