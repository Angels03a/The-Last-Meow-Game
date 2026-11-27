using System;
using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    public sealed class LobbyRoomUpdate
    {
        public LobbyRoomUpdate(int roomId, LobbyRoomSnapshot room)
            : this(roomId, room, Array.Empty<ILobbyManagerCallback>())
        {
        }

        public LobbyRoomUpdate(
            int roomId,
            LobbyRoomSnapshot room,
            IReadOnlyList<ILobbyManagerCallback> recipientCallbacks)
        {
            RoomId = roomId;
            Room = room;
            RecipientCallbacks = recipientCallbacks;
        }

        public int RoomId { get; }

        public LobbyRoomSnapshot Room { get; }

        public IReadOnlyList<ILobbyManagerCallback> RecipientCallbacks { get; }
    }
}
