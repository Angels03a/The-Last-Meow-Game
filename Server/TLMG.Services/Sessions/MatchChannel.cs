using System;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Immutable player subscription stored only in the match registry.</summary>
    internal sealed class MatchChannel
    {
        /// <summary>Copies the supplied subscription data.</summary>
        /// <param name="sessionToken">Token of the session that subscribed the channel.</param>
        /// <param name="channelId">Match channel ID.</param>
        /// <param name="matchId">Subscribed match.</param>
        /// <param name="callback">Delivery callback.</param>
        internal MatchChannel(string sessionToken, string channelId, Guid matchId, IMatchManagerCallback callback)
        {
            SessionToken = sessionToken;
            ChannelId = channelId;
            MatchId = matchId;
            Callback = callback;
        }

        /// <summary>Token of the session that subscribed the channel.</summary>
        internal string SessionToken { get; }

        /// <summary>Match channel ID.</summary>
        internal string ChannelId { get; }

        /// <summary>Subscribed match.</summary>
        internal Guid MatchId { get; }

        /// <summary>Delivery callback.</summary>
        internal IMatchManagerCallback Callback { get; }
    }
}
