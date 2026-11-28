using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Manages chat subscriptions and message acceptance.</summary>
    public interface IChatRegistry
    {
        /// <summary>Subscribes a channel to the current room, replacing the previous chat subscription.</summary>
        /// <param name="caller">Registered account token and chat channel.</param>
        /// <param name="callback">Chat channel callback.</param>
        /// <returns>Null on success, otherwise the error key.</returns>
        string SubscribeToChat(SessionChannelCaller caller, IChatManagerCallback callback);

        /// <summary>Validates the caller, channel and room and reserves the sending interval atomically.</summary>
        /// <param name="caller">Current token and subscribed chat channel.</param>
        /// <param name="submission">Room, correlation identifier and validated text.</param>
        /// <param name="chatDelivery">Delivery snapshot, or null on rejection.</param>
        /// <returns>Null on acceptance, otherwise the error key.</returns>
        string AcceptChatMessage(SessionChannelCaller caller, ChatSubmission submission, out ChatDelivery chatDelivery);

        /// <summary>Removes only the matching chat channel while preserving the player's sending interval.</summary>
        /// <param name="channelId">Closed or failed channel.</param>
        void RemoveChatChannel(string channelId);
    }
}
