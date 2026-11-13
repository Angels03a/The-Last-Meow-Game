using System.Collections.Generic;

namespace TLMG.Services.Lobby
{
    public sealed class LobbyOperationResultWithNotification<TypeOfResult>
    {
        public LobbyOperationResultWithNotification(TypeOfResult operationResult)
            : this(operationResult, LobbyChangeNotification.None)
        {
        }

        public LobbyOperationResultWithNotification(TypeOfResult operationResult, 
            IReadOnlyList<LobbyChangeNotification> notifications)
        {
            OperationResult = operationResult;
            Notifications = notifications;
        }

        public TypeOfResult OperationResult { get; }

        public IReadOnlyList<LobbyChangeNotification> Notifications { get; }
    }
}