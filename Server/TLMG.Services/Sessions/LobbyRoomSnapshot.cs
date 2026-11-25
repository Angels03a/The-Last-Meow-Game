using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Immutable room snapshot with its ID and players in entry order.
    /// </summary>
    public sealed class LobbyRoomSnapshot
    {
        /// <summary>Creates a room snapshot.</summary>
        /// <param name="roomId">Room ID.</param>
        /// <param name="players">Room players. Leave the supplied list unchanged afterward.</param>
        /// <param name="start">Snapshot of room readiness and match start.</param>
        public LobbyRoomSnapshot(int roomId, IReadOnlyList<PlayerSession> players, LobbyStartSnapshot start)
        {
            RoomId = roomId;
            Players = players;
            Start = start;
        }

        /// <summary>Room ID.</summary>
        public int RoomId { get; }

        /// <summary>Players in room entry order.</summary>
        public IReadOnlyList<PlayerSession> Players { get; }

        /// <summary>Snapshot of room readiness and match start.</summary>
        public LobbyStartSnapshot Start { get; }
    }
}