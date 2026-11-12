using System;
using System.ServiceModel;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Parte del manager que consulta la tabla de posiciones. Usa un canal propio y de corta vida, que no depende de
    /// la sesión del jugador: un fallo aquí se informa como resultado de la consulta y no desconecta la sesión.
    /// </summary>
    internal sealed partial class ServerConnectionManager : ILeaderboardConnection
    {
        private LeaderboardManagerClient _leaderboardClient;
        private LeaderboardCallback _leaderboardCallback;

        /// <inheritdoc />
        public event EventHandler<ServerEventArgs<OperationGetLeaderboardResult>> LeaderboardResultReceived;

        /// <inheritdoc />
        /// <remarks>
        /// Abrir el canal bloquea el hilo de la interfaz hasta que el servidor responde o vencen los tiempos del
        /// binding (BL-079).
        /// </remarks>
        public bool RequestLeaderboard()
        {
            bool wasLeaderboardRequestSent = false;
            CloseLeaderboardChannel();

            try
            {
                _leaderboardCallback = new LeaderboardCallback(this);
                _leaderboardClient = new LeaderboardManagerClient(new InstanceContext(_leaderboardCallback));
                _leaderboardClient.GetLeaderboard();
                wasLeaderboardRequestSent = true;
            }
            catch (TimeoutException ex)
            {
                _log.Error("Se agotó el tiempo al consultar la tabla de posiciones.", ex);
                CloseLeaderboardChannel();
            }
            catch (CommunicationException ex)
            {
                _log.Error("Error de comunicación al consultar la tabla de posiciones.", ex);
                CloseLeaderboardChannel();
            }

            return wasLeaderboardRequestSent;
        }

        /// <inheritdoc />
        public void CloseLeaderboardChannel()
        {
            CloseClientSafely(_leaderboardClient);
            _leaderboardClient = null;
            _leaderboardCallback = null;
        }

        /// <summary>
        /// Cierra el canal de la consulta y dispara LeaderboardResultReceived, siempre que el resultado sea de la
        /// consulta vigente; el de una consulta anterior se ignora.
        /// </summary>
        /// <param name="callback">Receptor que entregó el resultado.</param>
        /// <param name="operationGetLeaderboardResult">Resultado que llegó del servidor.</param>
        internal void NotifyLeaderboardResultReceived(
            LeaderboardCallback callback,
            OperationGetLeaderboardResult operationGetLeaderboardResult)
        {
            bool isCurrentCallback = ReferenceEquals(callback, _leaderboardCallback);

            if (isCurrentCallback && operationGetLeaderboardResult != null)
            {
                CloseLeaderboardChannel();
                LeaderboardResultReceived?.Invoke(
                    this,
                    new ServerEventArgs<OperationGetLeaderboardResult>(operationGetLeaderboardResult));
            }
        }
    }
}
