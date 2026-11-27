using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IModerationManager
    {
        private const string ModerationChannelName = "Moderation";

        public void BlockPlayer(string sessionToken, int targetUserId)
        {
            LogChannelOperation(ModerationChannelName, nameof(BlockPlayer), sessionToken);
            IModerationManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IModerationManagerCallback>();
            OperationResult operation = _moderationService.BlockPlayer(sessionToken, targetUserId);

            SafeNotify(NotifyCompletion);

            void NotifyCompletion()
            {
                callback.BlockPlayerCompleted(operation);
            }
        }

        public void UnblockPlayer(string sessionToken, int targetUserId)
        {
            LogChannelOperation(ModerationChannelName, nameof(UnblockPlayer), sessionToken);
            IModerationManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IModerationManagerCallback>();
            OperationResult operation = _moderationService.UnblockPlayer(sessionToken, targetUserId);

            SafeNotify(NotifyCompletion);

            void NotifyCompletion()
            {
                callback.UnblockPlayerCompleted(operation);
            }
        }

        public void GetBlockedPlayers(string sessionToken)
        {
            LogChannelOperation(ModerationChannelName, nameof(GetBlockedPlayers), sessionToken);
            IModerationManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IModerationManagerCallback>();
            BlockedPlayerList blockedPlayers = _moderationService.GetBlockedPlayers(sessionToken);

            SafeNotify(NotifyBlockedPlayers);

            void NotifyBlockedPlayers()
            {
                callback.BlockedPlayersReceived(blockedPlayers);
            }
        }

        public void ReportPlayer(string sessionToken, ReportSubmission submission)
        {
            LogChannelOperation(ModerationChannelName, nameof(ReportPlayer), sessionToken);
            IModerationManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IModerationManagerCallback>();
            ResultWithNotification<OperationResult> completion = _reportService.ReportPlayer(sessionToken, submission);

            SafeNotify(NotifyCompletion);
            SafeNotifyAll(completion.Notifications);

            void NotifyCompletion()
            {
                callback.ReportPlayerCompleted(completion.OperationResult);
            }
        }
        public void VoteToKick(string sessionToken, KickVote vote)
        {
            LogChannelOperation(ModerationChannelName, nameof(VoteToKick), sessionToken);
            IModerationManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<IModerationManagerCallback>();
            ResultWithNotification<OperationResult> completion = _kickVotingService.VoteToKick(sessionToken, vote);

            SafeNotify(NotifyCompletion);
            SafeNotifyAll(completion.Notifications);

            void NotifyCompletion()
            {
                callback.KickVoteCompleted(completion.OperationResult);
            }
        }
    }
}
