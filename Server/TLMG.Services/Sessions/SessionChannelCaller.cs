namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Identity and channel for one lobby, chat or match operation. Created once per call.
    /// </summary>
    public sealed class SessionChannelCaller
    {
        /// <summary>Copies the supplied subscription data.</summary>
        /// <param name="sessionToken">Session token from the client, possibly null or empty.</param>
        /// <param name="channelId">ID of the channel that sent the request.</param>
        public SessionChannelCaller(string sessionToken, string channelId)
        {
            SessionToken = sessionToken;
            ChannelId = channelId;
        }

        /// <summary>Session token received from the client.</summary>
        public string SessionToken { get; }

        /// <summary>ID of the channel that sent the request.</summary>
        public string ChannelId { get; }

        /// <summary>Whether both token and channel are present.</summary>
        public bool HasSessionAndChannel => !string.IsNullOrEmpty(SessionToken) && !string.IsNullOrEmpty(ChannelId);
    }
}