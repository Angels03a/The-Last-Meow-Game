using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Tests
{
    /// <summary>Lobby callback that counts calls and saves the last room received.</summary>
    internal sealed class LobbyCallbackStub : ILobbyManagerCallback
    {
        internal int CallCount { get; private set; }

        internal LobbyRoom LastLobbyRoom { get; private set; }

        internal int LastExpelledRoomId { get; private set; }

        public void OnPlayerExpelled(int roomId)
        {
            CallCount++;
            LastExpelledRoomId = roomId;
        }

        public void CreateRoomResult(OperationLobbyRoomResult operationCreateRoomResult)
        {
            CallCount++;
        }

        public void EnterLobbyResult(OperationLobbyRoomResult operationEnterLobbyResult)
        {
            CallCount++;
        }

        public void LeaveLobbyResult(OperationResult operationLeaveLobbyResult)
        {
            CallCount++;
        }

        public void OnLobbyRoomChanged(LobbyRoom lobbyRoom)
        {
            CallCount++;
            LastLobbyRoom = lobbyRoom;
        }

        public void ChangePlayerReadinessResult(OperationLobbyRoomResult result)
        {
            CallCount++;
        }

        public void ChangeCardsPerPlayerResult(OperationLobbyRoomResult result)
        {
            CallCount++;
        }

        public void RequestMatchStartResult(OperationRequestMatchStartResult result)
        {
            CallCount++;
        }
        public void LobbyHeartbeatEcho(int sequence, string errorKey) { }
    }
}
