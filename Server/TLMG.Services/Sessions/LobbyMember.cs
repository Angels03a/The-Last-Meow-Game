using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Immutable room member and lobby channel used to notify that player.
    /// </summary>
    internal sealed class LobbyMember
    {
        /// <summary>Creates a room member.</summary>
        /// <param name="userId">Player's account ID.</param>
        /// <param name="channelId">Player's lobby channel ID.</param>
        /// <param name="callback">Player's lobby channel callback.</param>
        public LobbyMember(int userId, string channelId, ILobbyManagerCallback callback)
        {
            UserId = userId;
            ChannelId = channelId;
            Callback = callback;
        }

        /// <summary>Player's account ID.</summary>
        public int UserId { get; }

        /// <summary>Player's lobby channel ID.</summary>
        public string ChannelId { get; }

        /// <summary>Player's lobby channel callback.</summary>
        public ILobbyManagerCallback Callback { get; }
    }
}