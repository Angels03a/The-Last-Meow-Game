using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Tests
{
    /// <summary>Callback de lobby de prueba que solo cuenta cuántas veces lo invocaron.</summary>
    internal sealed class LobbyCallbackStub : ILobbyManagerCallback
    {
        internal int CallCount { get; private set; }

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
