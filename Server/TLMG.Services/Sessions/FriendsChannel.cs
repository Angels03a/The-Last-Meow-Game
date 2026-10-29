using System.Collections.Generic;

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
        /// <param name="friendUserIds">Ids de sus amigos aceptados al suscribirse; no debe modificarse.</param>
        public FriendsChannel(
            int userId,
            string channelId,
            IFriendsManagerCallback callback,
            IReadOnlyList<int> friendUserIds)
        {
            UserId = userId;
            ChannelId = channelId;
            Callback = callback;
            FriendUserIds = friendUserIds;
        }

        /// <summary>Obtiene el identificador de la cuenta del jugador.</summary>
        public int UserId { get; }

        /// <summary>Obtiene el identificador del canal de amigos.</summary>
        public string ChannelId { get; }

        /// <summary>Obtiene el callback del canal de amigos.</summary>
        public IFriendsManagerCallback Callback { get; }

        /// <summary>Obtiene los ids de sus amigos aceptados al suscribirse, para avisarles su presencia.</summary>
        public IReadOnlyList<int> FriendUserIds { get; }
    }
}