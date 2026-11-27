using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>Delivers room operations and authoritative changes to the client.</summary>
    [ServiceContract]
    public interface ILobbyManagerCallback
    {
        /// <summary>Delivers the result of creating a room.</summary>
        [OperationContract(IsOneWay = true)]
        void CreateRoomResult(OperationLobbyRoomResult operationCreateRoomResult);

        /// <summary>Delivers the result of entering a room.</summary>
        [OperationContract(IsOneWay = true)]
        void EnterLobbyResult(OperationLobbyRoomResult operationEnterLobbyResult);

        /// <summary>Delivers the result of leaving a room.</summary>
        [OperationContract(IsOneWay = true)]
        void LeaveLobbyResult(OperationResult operationLeaveLobbyResult);

        /// <summary>Announces the authoritative room state.</summary>
        [OperationContract(IsOneWay = true)]
        void OnLobbyRoomChanged(LobbyRoom lobbyRoom);

        /// <summary>Delivers the result of changing readiness.</summary>
        [OperationContract(IsOneWay = true)]
        void ChangePlayerReadinessResult(OperationLobbyRoomResult result);

        /// <summary>Delivers the result of changing cards per player.</summary>
        [OperationContract(IsOneWay = true)]
        void ChangeCardsPerPlayerResult(OperationLobbyRoomResult result);

        /// <summary>Delivers the result of checking the match start.</summary>
        [OperationContract(IsOneWay = true)]
        void RequestMatchStartResult(OperationRequestMatchStartResult result);

        /// <summary>Confirms the lobby link heartbeat.</summary>
        [OperationContract(IsOneWay = true)]
        void LobbyHeartbeatEcho(int sequence, string errorKey);

        /// <summary>Notifies the affected player that unanimous voting removed them from the identified room.</summary>
        [OperationContract(IsOneWay = true)]
        void OnPlayerExpelled(int roomId);
    }
}
