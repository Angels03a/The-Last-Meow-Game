using System.Collections.Generic;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Helpers
{
    /// <summary>
    /// Convierte las salas del registro en los DTO que viajan al cliente; no guarda estado.
    /// </summary>
    public static class LobbyRoomHelper
    {
        /// <summary>Convierte la copia de una sala en el DTO que viaja al cliente.</summary>
        /// <param name="lobbyRoomSnapshot">Copia de la sala tomada en el registro.</param>
        /// <returns>La sala con su id y el id y nickname de cada jugador, sin tokens; nunca es null.</returns>
        public static LobbyRoom ConvertToLobbyRoom(LobbyRoomSnapshot lobbyRoomSnapshot)
        {
            LobbyRoom lobbyRoom = new LobbyRoom
            {
                RoomId = lobbyRoomSnapshot.RoomId,
                Players = new List<LobbyPlayer>()
            };

            foreach (PlayerSession playerSession in lobbyRoomSnapshot.Players)
            {
                lobbyRoom.Players.Add(new LobbyPlayer
                {
                    UserId = playerSession.UserId,
                    Nickname = playerSession.Nickname
                });
            }

            return lobbyRoom;
        }
    }
}