using System.Collections.Generic;

namespace TLMG.Services.Lobby
{
    /// <summary>
    /// Lo que devuelve una operación de sala: la respuesta para quien la pidió y los avisos para los demás.
    /// </summary>
    /// <typeparam name="TypeOfResult">Tipo del resultado de la operación que se responde por callback.</typeparam>
    public sealed class LobbyOperationResultWithNotification<TypeOfResult>
    {
        /// <summary>Crea el resultado de una operación que no avisa a nadie más.</summary>
        /// <param name="operationResult">Resultado para quien pidió la operación.</param>
        public LobbyOperationResultWithNotification(TypeOfResult operationResult)
            : this(operationResult, LobbyChangeNotification.None)
        {
        }

        /// <summary>Crea el resultado de una operación que además avisa a otros jugadores.</summary>
        /// <param name="operationResult">Resultado para quien pidió la operación.</param>
        /// <param name="notifications">Avisos para los demás jugadores; no debe modificarse después.</param>
        public LobbyOperationResultWithNotification(TypeOfResult operationResult, IReadOnlyList<LobbyChangeNotification> notifications)
        {
            OperationResult = operationResult;
            Notifications = notifications;
        }

        /// <summary>Obtiene el resultado para quien pidió la operación.</summary>
        public TypeOfResult OperationResult { get; }

        /// <summary>Obtiene los avisos para los demás jugadores; nunca es null.</summary>
        public IReadOnlyList<LobbyChangeNotification> Notifications { get; }
    }
}