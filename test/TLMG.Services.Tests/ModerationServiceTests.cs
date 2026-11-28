using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Moderation;
using TLMG.Services.Sessions;
using Xunit;

namespace TLMG.Services.Tests
{
    public sealed class ModerationServiceTests
    {
        private readonly SessionRegistry _registry = new SessionRegistry();

        [Fact]
        public void BlockPlayer_WithUnknownSession_ReturnsInvalidSession()
        {
            NoBlocksRepository repository = new NoBlocksRepository();
            ModerationService service = new ModerationService(repository, _registry, new BlockPolicy(repository));
            string token = "unknown";

            OperationResult result = service.BlockPlayer(token, 2);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.ErrorKey);
        }

        [Fact]
        public void BlockPlayer_WithGuestSession_ReturnsPermissionDenied()
        {
            NoBlocksRepository repository = new NoBlocksRepository();
            ModerationService service = new ModerationService(repository, _registry, new BlockPolicy(repository));
            string token = _registry.StartGuestSession().SessionToken;

            OperationResult result = service.BlockPlayer(token, 2);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.PermissionDenied, result.ErrorKey);
        }

        [Fact]
        public void BlockPlayer_WithInvalidRegisteredUserId_ReturnsInvalidSession()
        {
            NoBlocksRepository repository = new NoBlocksRepository();
            ModerationService service = new ModerationService(repository, _registry, new BlockPolicy(repository));
            string token = _registry.StartRegisteredSession(0, "Player", out _);

            OperationResult result = service.BlockPlayer(token, 2);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.ErrorKey);
        }

        [Fact]
        public void GetBlockedPlayers_WithBlockedPlayer_ReturnsThatPlayer()
        {
            BlockRepository repository = new BlockRepository();
            ModerationService service = new ModerationService(repository, _registry, new BlockPolicy(repository));
            string token = _registry.StartRegisteredSession(1, "Owner", out _);

            BlockedPlayerList result = service.GetBlockedPlayers(token);

            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.ErrorKey);
            BlockedPlayer player = Assert.Single(result.Players);
            Assert.Equal(2, player.UserId);
            Assert.Equal("Other", player.Nickname);
        }

        private sealed class BlockRepository : NoBlocksRepository
        {
            public override BlockListLookup FindBlocksInvolvingUser(int userId)
            {
                BlockRelationship block = new BlockRelationship
                {
                    BlockerUserId = userId,
                    BlockedUserId = 2,
                    BlockedNickname = "Other"
                };
                return new BlockListLookup { WasLookupSuccessful = true, Relationships = new[] { block } };
            }
        }
    }
}
