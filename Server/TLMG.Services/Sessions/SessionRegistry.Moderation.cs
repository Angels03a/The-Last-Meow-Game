using System.Collections.Generic;

using TLMG.Domain.Repositories;
using TLMG.Services.Moderation;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private BlockPolicy _blockPolicy;

        public void ConfigureBlockPolicy(BlockPolicy blockPolicy)
        {
            _blockPolicy = blockPolicy;
        }

        private BlockListLookup FindSessionBlocks(string sessionToken)
        {
            BlockListLookup blocks = new BlockListLookup();
            PlayerSession playerSession;

            if (TryFindSessionByToken(sessionToken, out playerSession))
            {
                blocks = _blockPolicy.FindBlocks(playerSession.UserId);
            }

            return blocks;
        }

        private LobbyChangeStatus ValidateRoomEntryBlocksInLock(
            SessionChannelCaller caller, int roomId, BlockListLookup blocks)
        {
            LobbyChangeStatus status = LobbyChangeStatus.Applied;
            PlayerSession playerSession;
            List<LobbyMember> members;

            if (_sessionsByToken.TryGetValue(caller.SessionToken, out playerSession)
                && !CanReconnectToLobbyInLock(playerSession.UserId, roomId, caller.ChannelId)
                && !IsUserOrChannelInRoom(playerSession.UserId, caller.ChannelId)
                && TryFindAuthorizedRoom(playerSession.UserId, roomId, out members))
            {
                if (!blocks.WasLookupSuccessful)
                {
                    status = LobbyChangeStatus.DataFailure;
                }
                else if (!IsRoomCompatibleWithBlocksInLock(roomId, blocks))
                {
                    status = LobbyChangeStatus.NotAuthorized;
                }
            }

            return status;
        }

        private bool IsRoomCompatibleWithBlocksInLock(int roomId, BlockListLookup blocks)
        {
            List<LobbyMember> members;
            bool isCompatible = _membersByRoomId.TryGetValue(roomId, out members) && blocks.WasLookupSuccessful;

            for (int memberIndex = 0; isCompatible && memberIndex < members.Count; memberIndex++)
            {
                isCompatible = BlockPolicy.CanShareRoom(blocks, members[memberIndex].UserId);
            }

            return isCompatible;
        }
    }
}