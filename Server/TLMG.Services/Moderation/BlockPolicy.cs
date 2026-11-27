using System.Collections.Generic;

using log4net;

using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Moderation
{
    public sealed class BlockPolicy
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BlockPolicy));

        private readonly IBlockRepository _blockRepository;

        internal object SynchronizationRoot { get; } = new object();

        public BlockPolicy(IBlockRepository blockRepository)
        {
            _blockRepository = blockRepository;
        }

        internal BlockListLookup FindBlocks(int userId)
        {
            BlockListLookup blocks = new BlockListLookup();

            if (GuestIdentity.IsGuestUserId(userId))
            {
                blocks.WasLookupSuccessful = true;
            }
            else if (userId > ValidationRules.NoUserId)
            {
                BlockListLookup storedBlocks = _blockRepository.FindBlocksInvolvingUser(userId);

                if (storedBlocks == null)
                {
                    _log.ErrorFormat("La consulta de bloqueos no devolvió un resultado para el jugador {0}.", userId);
                }
                else if (storedBlocks.WasLookupSuccessful)
                {
                    if (storedBlocks.Relationships != null && AreValidRelationships(storedBlocks, userId))
                    {
                        blocks = storedBlocks;
                    }
                    else
                    {
                        _log.ErrorFormat("La consulta de bloqueos devolvió relaciones inválidas para el jugador {0}.",
                            userId);
                    }
                }
            }

            return blocks;
        }

        internal static bool CanInteract(BlockListLookup blocks, int actorUserId, int targetUserId)
        {
            bool canInteract = blocks.WasLookupSuccessful
                && !HasOwnedBlock(blocks, targetUserId, actorUserId);

            return canInteract;
        }

        internal static bool CanShareRoom(BlockListLookup blocks, int otherUserId)
        {
            bool canShareRoom = blocks.WasLookupSuccessful;

            for (int relationshipIndex = 0;
                canShareRoom && relationshipIndex < blocks.Relationships.Count;
                relationshipIndex++)
            {
                BlockRelationship relationship = blocks.Relationships[relationshipIndex];
                canShareRoom = relationship.BlockerUserId != otherUserId
                    && relationship.BlockedUserId != otherUserId;
            }

            return canShareRoom;
        }

        private static bool HasOwnedBlock(BlockListLookup blocks, int ownerUserId, int targetUserId)
        {
            bool hasBlock = false;

            for (int relationshipIndex = 0;
                !hasBlock && relationshipIndex < blocks.Relationships.Count;
                relationshipIndex++)
            {
                BlockRelationship relationship = blocks.Relationships[relationshipIndex];
                hasBlock = relationship.BlockerUserId == ownerUserId
                    && relationship.BlockedUserId == targetUserId;
            }

            return hasBlock;
        }

        internal static IReadOnlyList<AcceptedFriend> FilterVisibleFriends(
            BlockListLookup blocks, int ownerUserId, IEnumerable<AcceptedFriend> friends)
        {
            List<AcceptedFriend> visibleFriends = new List<AcceptedFriend>();

            if (blocks.WasLookupSuccessful)
            {
                foreach (AcceptedFriend friend in friends)
                {
                    if (!HasOwnedBlock(blocks, ownerUserId, friend.UserId))
                    {
                        visibleFriends.Add(friend);
                    }
                }
            }

            return visibleFriends;
        }

        private static bool AreValidRelationships(BlockListLookup blocks, int userId)
        {
            bool areValid = true;

            for (int relationshipIndex = 0;
                areValid && relationshipIndex < blocks.Relationships.Count;
                relationshipIndex++)
            {
                BlockRelationship relationship = blocks.Relationships[relationshipIndex];
                areValid = relationship != null
                    && relationship.BlockerUserId > ValidationRules.NoUserId
                    && relationship.BlockedUserId > ValidationRules.NoUserId
                    && relationship.BlockerUserId != relationship.BlockedUserId
                    && (relationship.BlockerUserId == userId || relationship.BlockedUserId == userId);
            }

            return areValid;
        }
    }
}