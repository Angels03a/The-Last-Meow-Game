using System;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Área de la tabla de posiciones de la conexión con el servidor. LeaderboardViewModel depende solo de esta
    /// interfaz (D-70); la implementa ServerConnectionManager (D-60). La consulta no necesita sesión: la hacen
    /// también los invitados. El resultado llega por evento, en el hilo de la interfaz, con la ErrorKey tal como la
    /// manda el servidor y sin textos; la traducción ocurre en pantalla.
    /// </summary>
    public interface ILeaderboardConnection
    {
        /// <summary>Se dispara con el resultado de la consulta: los jugadores o la clave de error.</summary>
        event EventHandler<ServerEventArgs<OperationGetLeaderboardResult>> LeaderboardResultReceived;

        /// <summary>
        /// Pide la tabla de posiciones al servidor; una consulta nueva sustituye a la anterior. La respuesta llega por
        /// LeaderboardResultReceived.
        /// </summary>
        /// <returns>
        /// true si se envió; false si no se pudo abrir el canal o enviar, caso en el que el detalle está en el log.
        /// </returns>
        bool RequestLeaderboard();

        /// <summary>Cierra el canal de la consulta si sigue abierto; es seguro llamarlo sin consulta.</summary>
        void CloseLeaderboardChannel();
    }
}
