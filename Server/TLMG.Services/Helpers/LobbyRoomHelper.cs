using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Helpers
{
    public static class LobbyRoomHelper
    {
        public static LobbyRoom ConvertToLobbyRoom(LobbyRoomSnapshot lobbyRoomSnapshot)
        {
            LobbyRoom lobbyRoom = new LobbyRoom
            {
                RoomId = lobbyRoomSnapshot.RoomId,
                Players = new List<LobbyPlayer>(),
                Version = lobbyRoomSnapshot.Start.Version,
                CountdownId = lobbyRoomSnapshot.Start.CountdownId,
                CountdownEndsAtUtc = lobbyRoomSnapshot.Start.CountdownEndsAtUtc,
                ServerTimeUtc = lobbyRoomSnapshot.Start.ServerTimeUtc,
                InGame = lobbyRoomSnapshot.Start.MatchId.HasValue,
                MatchId = lobbyRoomSnapshot.Start.MatchId,
                MatchStartedAtUtc = lobbyRoomSnapshot.Start.MatchStartedAtUtc,
                CardsPerPlayerCount = lobbyRoomSnapshot.Start.CardsPerPlayerCount
            };

            foreach (PlayerSession playerSession in lobbyRoomSnapshot.Players)
            {
                lobbyRoom.Players.Add(new LobbyPlayer
                {
                    UserId = playerSession.UserId,
                    Nickname = playerSession.Nickname,
                    IsReady = lobbyRoomSnapshot.Start.ReadyUserIds.Contains(playerSession.UserId)
                });
            }

            return lobbyRoom;
        }
    }
}