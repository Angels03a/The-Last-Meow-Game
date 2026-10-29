using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Canal de amigos vivo de un jugador, que lo deja en línea; es inmutable.
    /// </summary>
    internal sealed class FriendsChannel
    {
        /// <summary>Crea el canal de amigos de un jugador.</summary>
        /// <param name="userId">Identificador de la cuenta del jugador.</param>
        /// <param name="channelId">Identificador del canal de amigos.</param>
        /// <param name="callback">Callback del canal de amigos.</param>
        public FriendsChannel(int userId, string channelId, IFriendsManagerCallback callback)
        {
            UserId = userId;
            ChannelId = channelId;
            Callback = callback;
        }

        /// <summary>Obtiene el identificador de la cuenta del jugador.</summary>
        public int UserId { get; }

        /// <summary>Obtiene el identificador del canal de amigos.</summary>
        public string ChannelId { get; }

        /// <summary>Obtiene el callback del canal de amigos.</summary>
        public IFriendsManagerCallback Callback { get; }
    }
}