using TLMG.Contracts.DataContracts;
using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Moderation
{
    public sealed class ModerationService
    {
        private const string InvalidTargetErrorKey = "Moderation_InvalidTargetError";
        private const string UserNotFoundErrorKey = "Moderation_UserNotFoundError";

        private readonly IBlockRepository _blockRepository;
        private readonly ISessionAuthorizer _sessionAuthorizer;
        private readonly BlockPolicy _blockPolicy;

        public ModerationService(IBlockRepository blockRepository, ISessionAuthorizer sessionAuthorizer,
            BlockPolicy blockPolicy)
        {
            _blockRepository = blockRepository;
            _sessionAuthorizer = sessionAuthorizer;
            _blockPolicy = blockPolicy;
        }

        public OperationResult BlockPlayer(string sessionToken, int targetUserId)
        {
            OperationResult operationBlockPlayer = ChangeBlock(sessionToken, targetUserId, true);

            return operationBlockPlayer;
        }

        public OperationResult UnblockPlayer(string sessionToken, int targetUserId)
        {
            OperationResult operationUnblockPlayer = ChangeBlock(sessionToken, targetUserId, false);

            return operationUnblockPlayer;
        }

        public BlockedPlayerList GetBlockedPlayers(string sessionToken)
        {
            BlockedPlayerList blockedPlayers = new BlockedPlayerList
            {
                ErrorKey = ErrorKeys.Common.Unexpected
            };

            lock (_blockPolicy.SynchronizationRoot)
            {
                SessionAuthorization sessionAuthorization = _sessionAuthorizer.Authorize(
                    sessionToken, SystemPermission.ManageBlocks);

                if (IsAuthorized(sessionAuthorization))
                {
                    BlockListLookup blocks = _blockPolicy.FindBlocks(sessionAuthorization.UserId);

                    if (blocks.WasLookupSuccessful && TryFillBlockedPlayers(blocks, sessionAuthorization.UserId, blockedPlayers))
                    {
                        blockedPlayers.IsSuccess = true;
                        blockedPlayers.ErrorKey = string.Empty;
                    }
                    else
                    {
                        blockedPlayers.Players.Clear();
                    }
                }
                else
                {
                    blockedPlayers.ErrorKey = FindAuthorizationErrorKey(sessionAuthorization);
                }
            }

            return blockedPlayers;
        }

        private OperationResult ChangeBlock(string sessionToken, int targetUserId, bool shouldBlock)
        {
            OperationResult operationChangeBlock = new OperationResult { ErrorKey = ErrorKeys.Common.Unexpected };

            lock (_blockPolicy.SynchronizationRoot)
            {
                SessionAuthorization sessionAuthorization = _sessionAuthorizer.Authorize(
                    sessionToken, SystemPermission.ManageBlocks);

                if (!IsAuthorized(sessionAuthorization))
                {
                    operationChangeBlock.ErrorKey = FindAuthorizationErrorKey(sessionAuthorization);
                }
                else if (targetUserId <= ValidationRules.NoUserId || targetUserId == sessionAuthorization.UserId)
                {
                    operationChangeBlock.ErrorKey = InvalidTargetErrorKey;
                }
                else
                {
                    BlockWriteStatus blockStatus;

                    if (shouldBlock)
                    {
                        blockStatus = _blockRepository.BlockPlayer(sessionAuthorization.UserId, targetUserId);
                    }
                    else
                    {
                        blockStatus = _blockRepository.UnblockPlayer(sessionAuthorization.UserId, targetUserId);
                    }
                    operationChangeBlock = CreateBlockOperation(blockStatus, shouldBlock);
                }
            }

            return operationChangeBlock;
        }

        private static bool IsAuthorized(SessionAuthorization sessionAuthorization)
        {
            bool isAuthorized = sessionAuthorization != null && sessionAuthorization.Status == AuthorizationStatus.Granted
                && sessionAuthorization.UserId > ValidationRules.NoUserId;

            return isAuthorized;
        }

        private static string FindAuthorizationErrorKey(SessionAuthorization sessionAuthorization)
        {
            string errorKey = ErrorKeys.Common.Unexpected;

            if (sessionAuthorization != null)
            {
                if (sessionAuthorization.Status == AuthorizationStatus.InvalidSession)
                {
                    errorKey = ErrorKeys.Common.InvalidSession;
                }
                else if (sessionAuthorization.Status == AuthorizationStatus.PermissionDenied)
                {
                    errorKey = ErrorKeys.Common.PermissionDenied;
                }
            }

            return errorKey;
        }

        private static OperationResult CreateBlockOperation(BlockWriteStatus blockStatus, bool shouldBlock)
        {
            bool isSuccess;

            if (shouldBlock)
            {
                isSuccess = blockStatus == BlockWriteStatus.Blocked || blockStatus == BlockWriteStatus.AlreadyBlocked;
            }
            else
            {
                isSuccess = blockStatus == BlockWriteStatus.Unblocked || blockStatus == BlockWriteStatus.NotBlocked;
            }
            OperationResult operationBlock = new OperationResult
            {
                IsSuccess = isSuccess,
                ErrorKey = ErrorKeys.Common.Unexpected
            };

            if (isSuccess)
            {
                operationBlock.ErrorKey = string.Empty;
            }
            else if (blockStatus == BlockWriteStatus.InvalidTarget)
            {
                operationBlock.ErrorKey = InvalidTargetErrorKey;
            }
            else if (blockStatus == BlockWriteStatus.UserNotFound)
            {
                operationBlock.ErrorKey = UserNotFoundErrorKey;
            }

            return operationBlock;
        }

        private static bool TryFillBlockedPlayers(BlockListLookup lookup, int userId, BlockedPlayerList result)
        {
            bool isValid = lookup != null && lookup.WasLookupSuccessful && lookup.Relationships != null;

            for (int relationshipIndex = 0;
                isValid && relationshipIndex < lookup.Relationships.Count;
                relationshipIndex++)
            {
                BlockRelationship relationship = lookup.Relationships[relationshipIndex];

                if (relationship == null)
                {
                    isValid = false;
                }
                else if (relationship.BlockerUserId == userId)
                {
                    isValid = relationship.BlockedUserId > ValidationRules.NoUserId
                        && relationship.BlockedUserId != userId
                        && !string.IsNullOrWhiteSpace(relationship.BlockedNickname);

                    if (isValid)
                    {
                        result.Players.Add(new BlockedPlayer
                        {
                            UserId = relationship.BlockedUserId,
                            Nickname = relationship.BlockedNickname
                        });
                    }
                }
            }

            return isValid;
        }
    }
}