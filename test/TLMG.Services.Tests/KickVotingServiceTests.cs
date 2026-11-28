using System;
using System.Collections.Generic;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Repositories;
using TLMG.Services.Moderation;
using TLMG.Services.Sessions;
using Xunit;

namespace TLMG.Services.Tests
{
    public class KickVotingServiceTests
    {
        private sealed class KickHistory : IKickVotingRepository
        {
            internal List<CompletedKickVoting> Records { get; } = new List<CompletedKickVoting>();

            public KickVotingWriteStatus RecordCompletedVoting(CompletedKickVoting voting)
            {
                Records.Add(voting);
                return KickVotingWriteStatus.Recorded;
            }
        }

        [Fact]
        public void VoteToKick_AfterMembershipChange_RequiresFreshVotesAndExcludesGuest()
        {
            LobbyScenario lobby = new LobbyScenario();
            KickHistory history = new KickHistory();
            lobby.Registry.ConfigureKickHistory(history);
            KickVotingService service = new KickVotingService(lobby.Registry, lobby.MatchStart);
            GuestSessionStart guest = lobby.Registry.StartGuestSession();
            lobby.Registry.SubscribeFriendsChannel(guest.SessionToken, "friends-guest", null, Array.Empty<int>());
            RoomInvitationCreation invitation = lobby.Registry.CreateRoomInvitation(lobby.CreatorToken, guest.UserId);
            Assert.Equal(FriendsChangeStatus.Applied, invitation.Status);
            lobby.Registry.AcceptRoomInvitation(guest.SessionToken, invitation.InvitationId);
            SessionChannelCaller caller = new SessionChannelCaller(guest.SessionToken, "guest");
            LobbyCallbackStub callback = new LobbyCallbackStub();
            Assert.Equal(LobbyChangeStatus.Applied, lobby.Registry.EnterRoom(caller, lobby.RoomId, callback).Status);
            string second = LobbyReadinessHelper.AddPlayer(lobby, 2, "second", null);
            KickVote vote = new KickVote { RoomId = lobby.RoomId, TargetUserId = guest.UserId };
            Assert.True(service.VoteToKick(lobby.CreatorToken, vote).OperationResult.IsSuccess);
            string third = LobbyReadinessHelper.AddPlayer(lobby, 3, "third", null);
            Assert.True(service.VoteToKick(second, vote).OperationResult.IsSuccess);
            Assert.True(service.VoteToKick(third, vote).OperationResult.IsSuccess);
            Assert.Empty(history.Records);

            ResultWithNotification<OperationResult> result = service.VoteToKick(lobby.CreatorToken, vote);

            Assert.True(result.OperationResult.IsSuccess);
            Assert.Equal(0, callback.LastExpelledRoomId);
            foreach (Action notify in result.Notifications)
            {
                notify();
            }
            Assert.Equal(lobby.RoomId, callback.LastExpelledRoomId);
            CompletedKickVoting record = Assert.Single(history.Records);
            Assert.Null(record.MatchId);
            Assert.True(record.Target.GuestSessionId.HasValue);
            Assert.Equal(3, record.Voters.Count);
            Assert.Equal(FriendsChangeStatus.CannotInvite,
                lobby.Registry.CreateRoomInvitation(lobby.CreatorToken, guest.UserId).Status);
            Assert.False(service.VoteToKick(second, vote).OperationResult.IsSuccess);
            Assert.Single(history.Records);
        }

        [Fact]
        public void VoteToKick_WithTemporarilyDisconnectedVoter_RequiresTheirVoteAfterReconnect()
        {
            MatchPlayScenario match = new MatchPlayScenario(3);
            match.SubscribeAll();
            KickHistory history = new KickHistory();
            match.Registry.ConfigureKickHistory(history);
            KickVotingService service = new KickVotingService(match.Registry, match.MatchStart);
            KickVote vote = new KickVote { RoomId = match.RoomId, TargetUserId = 3 };
            match.CloseMatchChannelOf(2);

            Assert.True(service.VoteToKick(match.TokenOf(1), vote).OperationResult.IsSuccess);
            Assert.Empty(history.Records);
            match.Subscribe(2);
            Assert.True(service.VoteToKick(match.TokenOf(2), vote).OperationResult.IsSuccess);

            CompletedKickVoting record = Assert.Single(history.Records);
            Assert.Equal(match.MatchId, record.MatchId);
            Assert.Equal(3, record.Target.UserId);
            Assert.Equal(2, record.Voters.Count);
        }
    }
}
