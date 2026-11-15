using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Tests
{
    /// <summary>Callback de lobby de prueba: cuenta las invocaciones y guarda la última sala avisada.</summary>
    internal sealed class LobbyCallbackStub : ILobbyManagerCallback
    {
        internal int CallCount { get; private set; }

        internal LobbyRoom LastLobbyRoom { get; private set; }

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
    }
}
