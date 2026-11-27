using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    public sealed class LobbyMembershipChange
    {
        public const int NoRoomId = 0;
        public const int NoUserId = 0;

        private LobbyMembershipChange(LobbyChangeStatus status, int userId, LobbyRoomUpdate roomUpdate)
        {
            Status = status;
            UserId = userId;
            RoomId = roomUpdate.RoomId;
            Room = roomUpdate.Room;
            RecipientCallbacks = roomUpdate.RecipientCallbacks;
            ExitCause = RoomExitCause.None;
            LeftAuthorizedMatch = false;
        }

        private LobbyMembershipChange(LobbyDeparture lobbyDeparture, LobbyRoomUpdate roomUpdate)
            : this(LobbyChangeStatus.Applied, lobbyDeparture.UserId, roomUpdate)
        {
            ExitCause = lobbyDeparture.ExitCause;
            LeftAuthorizedMatch = lobbyDeparture.LeftAuthorizedMatch;
        }

        public LobbyChangeStatus Status { get; }

        public int UserId { get; }

        public int RoomId { get; }

        public LobbyRoomSnapshot Room { get; }

        public IReadOnlyList<ILobbyManagerCallback> RecipientCallbacks { get; }

        public RoomExitCause ExitCause { get; }

        public bool LeftAuthorizedMatch { get; }

        public static LobbyMembershipChange CreateRejected(LobbyChangeStatus status, int userId)
        {
            LobbyRoomUpdate roomUpdate = new LobbyRoomUpdate(NoRoomId, null);
            LobbyMembershipChange membershipChange = new LobbyMembershipChange(status, userId, roomUpdate);

            return membershipChange;
        }

        public static LobbyMembershipChange CreateApplied(int userId, LobbyRoomUpdate roomUpdate)
        {
            LobbyMembershipChange membershipChange =
                new LobbyMembershipChange(LobbyChangeStatus.Applied, userId, roomUpdate);

            return membershipChange;
        }

        public static LobbyMembershipChange CreateDeparture(LobbyDeparture departure, LobbyRoomUpdate roomUpdate)
        {
            LobbyMembershipChange membershipChange = new LobbyMembershipChange(departure, roomUpdate);

            return membershipChange;
        }
    }
}