using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Copia inmutable de una sala en un instante: su identificador y sus jugadores en orden de entrada.
    /// </summary>
    public sealed class LobbyRoomSnapshot
    {
        /// <summary>Crea la copia de una sala.</summary>
        /// <param name="roomId">Identificador de la sala.</param>
        /// <param name="players">Jugadores de la sala; la lista recibida no debe modificarse después.</param>
        /// <param name="start">Copia de preparación e inicio de la sala.</param>
        public LobbyRoomSnapshot(int roomId, IReadOnlyList<PlayerSession> players, LobbyStartSnapshot start)
        {
            RoomId = roomId;
            Players = players;
            Start = start;
        }

        /// <summary>Obtiene el identificador de la sala.</summary>
        public int RoomId { get; }

        /// <summary>Obtiene los jugadores de la sala, en el orden en que entraron.</summary>
        public IReadOnlyList<PlayerSession> Players { get; }

        /// <summary>Obtiene la copia de preparación e inicio de la sala.</summary>
        public LobbyStartSnapshot Start { get; }
    }
}