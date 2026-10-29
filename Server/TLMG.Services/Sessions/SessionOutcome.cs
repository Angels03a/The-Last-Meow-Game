using System;
using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Lo que devuelve una operación: la respuesta para quien la pidió y los avisos que hay que entregar a otros
    /// jugadores, ya listos para invocarse fuera de cualquier candado.
    /// </summary>
    /// <typeparam name="TResult">Tipo del resultado de la operación que se responde por callback.</typeparam>
    public sealed class SessionOutcome<TResult>
    {
        private static readonly IReadOnlyList<Action> _noNotifications = new List<Action>().AsReadOnly();

        /// <summary>Crea el resultado de una operación que no avisa a nadie más.</summary>
        /// <param name="result">Resultado para quien pidió la operación.</param>
        public SessionOutcome(TResult result)
            : this(result, _noNotifications)
        {
        }

        /// <summary>Crea el resultado de una operación que además avisa a otros jugadores.</summary>
        /// <param name="result">Resultado para quien pidió la operación.</param>
        /// <param name="notifications">Avisos para los demás jugadores; la lista no debe modificarse después.</param>
        public SessionOutcome(TResult result, IReadOnlyList<Action> notifications)
        {
            Result = result;
            Notifications = notifications;
        }

        /// <summary>Obtiene el resultado para quien pidió la operación.</summary>
        public TResult Result { get; }

        /// <summary>Obtiene los avisos para los demás jugadores; nunca es null.</summary>
        public IReadOnlyList<Action> Notifications { get; }
    }
}