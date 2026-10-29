using System.Collections.Generic;

namespace TLMG.Services.Lobby
{
    /// <summary>
    /// Lo que devuelve una operación de sala: la respuesta para quien la pidió y los avisos para los demás.
    /// </summary>
    /// <typeparam name="TResult">Tipo del resultado de la operación que se responde por callback.</typeparam>
    public sealed class LobbyOutcome<TResult>
    {
        /// <summary>Crea el resultado de una operación que no avisa a nadie más.</summary>
        /// <param name="result">Resultado para quien pidió la operación.</param>
        public LobbyOutcome(TResult result)
            : this(result, LobbyNotification.None)
        {
        }

        /// <summary>Crea el resultado de una operación que además avisa a otros jugadores.</summary>
        /// <param name="result">Resultado para quien pidió la operación.</param>
        /// <param name="notifications">Avisos para los demás jugadores; no debe modificarse después.</param>
        public LobbyOutcome(TResult result, IReadOnlyList<LobbyNotification> notifications)
        {
            Result = result;
            Notifications = notifications;
        }

        /// <summary>Obtiene el resultado para quien pidió la operación.</summary>
        public TResult Result { get; }

        /// <summary>Obtiene los avisos para los demás jugadores; nunca es null.</summary>
        public IReadOnlyList<LobbyNotification> Notifications { get; }
    }
}