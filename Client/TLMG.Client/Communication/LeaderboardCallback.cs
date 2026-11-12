using System.ServiceModel;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Recibe el callback de la tabla de posiciones en un hilo de WCF y lo pasa al hilo de la interfaz como evento del
    /// ServerConnectionManager que lo creó. No decide nada ni toca controles. UseSynchronizationContext = false evita
    /// el interbloqueo cuando la interfaz llama al servidor mientras espera un callback. Vive aparte de
    /// ServerCallback porque la consulta usa su propio canal y no necesita sesión.
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    internal sealed class LeaderboardCallback : ILeaderboardManagerCallback
    {
        private readonly ServerConnectionManager _serverConnection;

        /// <summary>Crea el receptor de callbacks de una consulta.</summary>
        /// <param name="serverConnection">Manager cuyo evento se dispara con lo que llega del servidor.</param>
        internal LeaderboardCallback(ServerConnectionManager serverConnection)
        {
            _serverConnection = serverConnection;
        }

        /// <summary>Entrega la tabla de posiciones mediante el dispatcher de la interfaz.</summary>
        /// <param name="operationGetLeaderboardResult">Resultado con los jugadores o la clave de error.</param>
        public void GetLeaderboardResult(OperationGetLeaderboardResult operationGetLeaderboardResult)
        {
            _serverConnection.RunOnInterface(
                () => _serverConnection.NotifyLeaderboardResultReceived(this, operationGetLeaderboardResult));
        }
    }
}
