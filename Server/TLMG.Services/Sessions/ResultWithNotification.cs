using System;
using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Lo que devuelve una operación: la respuesta para quien la pidió y los avisos que hay que entregar a otros
    /// jugadores, ya listos para invocarse fuera de cualquier candado.
    /// </summary>
    /// <typeparam name="TypeOfResult">Tipo del resultado de la operación que se responde por callback.</typeparam>
    public sealed class ResultWithNotification<TypeOfResult>
    {
        private static readonly IReadOnlyList<Action> _noNotifications = new List<Action>().AsReadOnly();

        /// <summary>Crea el resultado de una operación que no avisa a nadie más.</summary>
        /// <param name="operationResult">Resultado para quien pidió la operación.</param>
        public ResultWithNotification(TypeOfResult operationResult)
            : this(operationResult, _noNotifications)
        {
        }

        /// <summary>Crea el resultado de una operación que además avisa a otros jugadores.</summary>
        /// <param name="operationResult">Resultado para quien pidió la operación.</param>
        /// <param name="notifications">Avisos para los demás jugadores; la lista no debe modificarse después.</param>
        public ResultWithNotification(TypeOfResult operationResult, IReadOnlyList<Action> notifications)
        {
            OperationResult = operationResult;
            Notifications = notifications;
        }

        /// <summary>Obtiene el resultado para quien pidió la operación.</summary>
        public TypeOfResult OperationResult { get; }

        /// <summary>Obtiene los avisos para los demás jugadores; nunca es null.</summary>
        public IReadOnlyList<Action> Notifications { get; }
    }
}