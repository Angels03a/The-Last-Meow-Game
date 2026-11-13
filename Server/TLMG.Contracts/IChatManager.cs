using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>Own duplex channel to subscribe to the room chat and send room messages.</summary>
    [ServiceContract(CallbackContract = typeof(IChatManagerCallback))]
    public interface IChatManager
    {
        /// <summary>Subscribes the channel after entering a room; the last subscription wins.</summary>
        /// <param name="sessionToken">Valid token of the registered player.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToChat(string sessionToken);

        /// <summary>
        /// Requests a send; the echo confirms acceptance and the result only reports rejections, so the client
        /// never shows a message the server did not accept.
        /// </summary>
        /// <param name="sessionToken">Valid token of the registered player.</param>
        /// <param name="request">Room, correlation identifier and text.</param>
        [OperationContract(IsOneWay = true)]
        void SendChatMessage(string sessionToken, OperationSendChatMessageRequest request);
    }
}