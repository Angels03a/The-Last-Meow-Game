using System.ServiceModel;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    internal sealed class ModerationCallback : IModerationManagerCallback
    {
        private readonly ModerationChannel _channel;

        internal ModerationCallback(ModerationChannel channel)
        {
            _channel = channel;
        }

        public void BlockPlayerCompleted(OperationResult operation)
        {
            _channel.QueueCompletion(this, ModerationOperation.Block, operation);
        }

        public void UnblockPlayerCompleted(OperationResult operation)
        {
            _channel.QueueCompletion(this, ModerationOperation.Unblock, operation);
        }

        public void BlockedPlayersReceived(BlockedPlayerList blockedPlayers)
        {
            _channel.QueueCompletion(this, ModerationOperation.GetBlockedPlayers, blockedPlayers);
        }

        public void ReportPlayerCompleted(OperationResult operation)
        {
            _channel.QueueCompletion(this, ModerationOperation.Report, operation);
        }

        public void KickVoteCompleted(OperationResult operation)
        {
            _channel.QueueCompletion(this, ModerationOperation.VoteToKick, operation);
        }
    }
}