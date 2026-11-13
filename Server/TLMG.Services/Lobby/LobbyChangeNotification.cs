using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Lobby
{
    public sealed class LobbyChangeNotification
    {
        public static readonly IReadOnlyList<LobbyChangeNotification> None = 
            new List<LobbyChangeNotification>().AsReadOnly();

        public LobbyChangeNotification(ILobbyManagerCallback callback, LobbyRoom lobbyRoom)
        {
            Callback = callback;
            LobbyRoom = lobbyRoom;
        }

        public ILobbyManagerCallback Callback { get; }

        public LobbyRoom LobbyRoom { get; }
    }
}