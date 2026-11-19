using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Contract to create rooms, enter and leave them; the room state reaches clients through
    /// ILobbyManagerCallback.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(ILobbyManagerCallback))]
    public interface ILobbyManager
    {
        /// <summary>Requests a new room and stays inside it; the answer is CreateRoomResult.</summary>
        /// <param name="sessionToken">Session token of the player.</param>
        [OperationContract(IsOneWay = true)]
        void CreateRoom(string sessionToken);

        /// <summary>Requests to enter an authorized room; the answer is EnterLobbyResult.</summary>
        /// <param name="sessionToken">Session token of the player.</param>
        /// <param name="roomId">Identifier of the room to enter.</param>
        [OperationContract(IsOneWay = true)]
        void EnterLobby(string sessionToken, int roomId);

        /// <summary>Requests to leave the current room; the answer is LeaveLobbyResult.</summary>
        /// <param name="sessionToken">Session token of the player.</param>
        [OperationContract(IsOneWay = true)]
        void LeaveLobby(string sessionToken);

        /// <summary>Requests to change the own Ready state; the answer is ChangePlayerReadinessResult.</summary>
        /// <param name="sessionToken">Session token of the player.</param>
        /// <param name="request">Room and wanted state; null is rejected without revealing the room.</param>
        [OperationContract(IsOneWay = true)]
        void ChangePlayerReadiness(string sessionToken, OperationChangePlayerReadinessRequest request);

        /// <summary>
        /// Requests to change the cards per player of the room, which sets everyone to Not Ready when the value
        /// really changes; the answer is ChangeCardsPerPlayerResult.
        /// </summary>
        /// <param name="sessionToken">Session token of the player.</param>
        /// <param name="request">Room and wanted amount; null is rejected without revealing the room.</param>
        [OperationContract(IsOneWay = true)]
        void ChangeCardsPerPlayer(string sessionToken, OperationChangeCardsPerPlayerRequest request);

        /// <summary>Requests the server to validate the countdown and authorize the match start.</summary>
        /// <param name="sessionToken">Session token of the player.</param>
        /// <param name="request">Room and observed countdown generation; null is rejected.</param>
        [OperationContract(IsOneWay = true)]
        void RequestMatchStart(string sessionToken, OperationMatchStartRequest request);
        [OperationContract(IsOneWay = true)]
        void LobbyHeartbeat(string sessionToken, int sequence, int confirmedSequence);
    }
}