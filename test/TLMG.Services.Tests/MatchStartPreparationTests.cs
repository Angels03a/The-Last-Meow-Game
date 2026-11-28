using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;
using Xunit;

namespace TLMG.Services.Tests
{
    public sealed class MatchStartPreparationTests
    {
        [Fact]
        public void CompleteMatchStart_WithNullPreparation_RejectsBeforePublishing()
        {
            SessionRegistry registry = new SessionRegistry();
            MatchStartPreparation preparation = null;

            LobbyMembershipChange result = registry.CompleteMatchStart(null, preparation, null);

            Assert.Equal(LobbyChangeStatus.NotAuthorized, result.Status);
            Assert.Null(result.Room);
            Assert.Empty(registry.FindCancelledMatchIds());
        }

        [Fact]
        public void CompleteMatchStart_WithMissingRegistration_RejectsBeforePublishing()
        {
            SessionRegistry registry = new SessionRegistry();
            LobbyRoomSnapshot room = new LobbyRoomSnapshot(1, System.Array.Empty<PlayerSession>(), null);
            LobbyMembershipChange membership = LobbyMembershipChange.CreateApplied(1, new LobbyRoomUpdate(1, room));
            MatchStartPreparation preparation = new MatchStartPreparation(membership, null);

            LobbyMembershipChange result = registry.CompleteMatchStart(null, preparation, null);

            Assert.Equal(LobbyChangeStatus.NotAuthorized, result.Status);
            Assert.Null(result.Room);
            Assert.Empty(registry.FindCancelledMatchIds());
        }

        [Fact]
        public void CompleteMatchStart_WithMissingMembership_RejectsBeforePublishing()
        {
            SessionRegistry registry = new SessionRegistry();
            MatchStartPreparation preparation = new MatchStartPreparation(null, new GameRegistration());

            LobbyMembershipChange result = registry.CompleteMatchStart(null, preparation, null);

            Assert.Equal(LobbyChangeStatus.NotAuthorized, result.Status);
            Assert.Null(result.Room);
            Assert.Empty(registry.FindCancelledMatchIds());
        }

        [Fact]
        public void CompleteMatchStart_WithAppliedWithoutRoom_RejectsBeforePublishing()
        {
            SessionRegistry registry = new SessionRegistry();
            LobbyMembershipChange membership = LobbyMembershipChange.CreateApplied(1, new LobbyRoomUpdate(1, null));
            MatchStartPreparation preparation = new MatchStartPreparation(membership, new GameRegistration());

            LobbyMembershipChange result = registry.CompleteMatchStart(null, preparation, null);

            Assert.Equal(LobbyChangeStatus.NotAuthorized, result.Status);
            Assert.Null(result.Room);
            Assert.Empty(registry.FindCancelledMatchIds());
        }
    }
}
