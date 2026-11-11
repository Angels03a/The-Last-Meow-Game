using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Tests
{
    /// <summary>Callback de lobby de prueba que solo cuenta cuántas veces lo invocaron.</summary>
    internal sealed class LobbyCallbackStub : ILobbyManagerCallback
    {
        internal int CallCount { get; private set; }

        public void CreateRoomResult(OperationCreateRoomResult operationCreateRoomResult)
        {
            CallCount++;
        }

        public void EnterLobbyResult(OperationEnterLobbyResult operationEnterLobbyResult)
        {
            CallCount++;
        }

        public void LeaveLobbyResult(OperationLeaveLobbyResult operationLeaveLobbyResult)
        {
            CallCount++;
        }

        public void OnLobbyRoomChanged(LobbyRoom lobbyRoom)
        {
            CallCount++;
        }

        public void ChangePlayerReadinessResult(OperationChangePlayerReadinessResult result)
        {
            CallCount++;
        }

        public void ChangeCardsPerPlayerResult(OperationChangeCardsPerPlayerResult result)
        {
            CallCount++;
        }

        public void RequestMatchStartResult(OperationRequestMatchStartResult result)
        {
            CallCount++;
        }
    }
}
