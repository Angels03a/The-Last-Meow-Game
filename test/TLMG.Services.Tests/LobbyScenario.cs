using System;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    internal sealed class LobbyScenario
    {
        internal SessionRegistry Registry { get; } = new SessionRegistry();

        internal string CreatorToken { get; }

        internal int RoomId { get; }

        internal LobbyScenario()
        {
            CreatorToken = Registry.StartSession(1, "Creator", out SessionEnd replaced);
            LobbyCaller creatorCaller = new LobbyCaller(CreatorToken, "creator");
            RoomId = Registry.CreateRoom(creatorCaller, null).RoomId;
        }

        internal string Authorize(int userId)
        {
            string token = Registry.StartSession(userId, "Guest", out SessionEnd replaced);
            Registry.SubscribeFriendsChannel(token, "friends" + userId, null, Array.Empty<int>());
            RoomInvitationCreation invitation = Registry.CreateRoomInvitation(CreatorToken, userId);
            Registry.AcceptRoomInvitation(token, invitation.InvitationId);

            return token;
        }

        internal void FillRoom()
        {
            for (int userId = 2; userId <= 6; userId++)
            {
                string token = Authorize(userId);
                LobbyCaller caller = new LobbyCaller(token, "lobby" + userId);
                Registry.EnterRoom(caller, RoomId, null);
            }
        }
    }
}