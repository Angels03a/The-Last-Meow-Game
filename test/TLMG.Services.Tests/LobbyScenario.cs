using System;
using Xunit;
using TLMG.Services.Lobby;
using TLMG.Services.Moderation;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    internal sealed class LobbyScenario
    {
        internal SessionRegistry Registry { get; }

        internal MatchStartCoordinator MatchStart { get; }

        internal LobbyService Service { get; }

        internal string CreatorToken { get; }

        internal int RoomId { get; }

        internal LobbyScenario() : this(new SessionRegistry())
        {
        }

        /// <summary>Creates a room using the supplied registry, which can use a test clock.</summary>
        internal LobbyScenario(SessionRegistry registry)
        {
            Registry = registry;
            registry.ConfigureBlockPolicy(new BlockPolicy(new NoBlocksRepository()));
            MatchStart = MatchHistoryStub.CreateCoordinator(registry);
            Service = new LobbyService(registry, registry, MatchStart);
            CreatorToken = Registry.StartRegisteredSession(1, "Creator", out SessionEnd replaced);
            SessionChannelCaller creatorCaller = new SessionChannelCaller(CreatorToken, "creator");
            LobbyMembershipChange created = Registry.CreateRoom(creatorCaller, null);
            Assert.Equal(LobbyChangeStatus.Applied, created.Status);
            RoomId = created.RoomId;
        }

        internal string Authorize(int userId)
        {
            string token = Registry.StartRegisteredSession(userId, "Guest", out SessionEnd replaced);
            Registry.SubscribeFriendsChannel(token, "friends" + userId, null, Array.Empty<int>());
            RoomInvitationCreation invitation = Registry.CreateRoomInvitation(CreatorToken, userId);
            Assert.Equal(FriendsChangeStatus.Applied, invitation.Status);
            RoomInvitationAcceptance acceptance = Registry.AcceptRoomInvitation(token, invitation.InvitationId);
            Assert.Equal(FriendsChangeStatus.Applied, acceptance.Status);

            return token;
        }

        internal void FillRoom()
        {
            for (int userId = 2; userId <= 6; userId++)
            {
                string token = Authorize(userId);
                SessionChannelCaller caller = new SessionChannelCaller(token, "lobby" + userId);
                LobbyMembershipChange entry = Registry.EnterRoom(caller, RoomId, null);
                Assert.Equal(LobbyChangeStatus.Applied, entry.Status);
            }
        }
    }
}