using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado inmutable de registrar el canal de amigos de una sesión.
    /// </summary>
    public sealed class FriendsSubscription
    {
        private static readonly IReadOnlyList<int> _noUserIds = new List<int>().AsReadOnly();
        private static readonly IReadOnlyList<IFriendsManagerCallback> _noCallbacks =
            new List<IFriendsManagerCallback>().AsReadOnly();

        private FriendsSubscription(
            FriendsChangeStatus status,
            int userId,
            string nickname,
            IReadOnlyList<int> onlineFriendUserIds,
            IReadOnlyList<IFriendsManagerCallback> presenceRecipientCallbacks)
        {
            Status = status;
            UserId = userId;
            Nickname = nickname;
            OnlineFriendUserIds = onlineFriendUserIds;
            PresenceRecipientCallbacks = presenceRecipientCallbacks;
        }

        /// <summary>Obtiene qué ocurrió con el intento de registro.</summary>
        public FriendsChangeStatus Status { get; }

        /// <summary>Obtiene el id de la cuenta del jugador; NoUserId si se rechazó.</summary>
        public int UserId { get; }

        /// <summary>Obtiene el nickname del jugador; null si se rechazó.</summary>
        public string Nickname { get; }

        /// <summary>Obtiene los ids de los amigos recibidos que están en línea; nunca es null.</summary>
        public IReadOnlyList<int> OnlineFriendUserIds { get; }

        /// <summary>
        /// Obtiene los callbacks de los amigos en línea a quienes avisar que el jugador pasó a estar en línea;
        /// vacía si ya lo estaba. Nunca es null.
        /// </summary>
        public IReadOnlyList<IFriendsManagerCallback> PresenceRecipientCallbacks { get; }

        /// <summary>Crea un resultado rechazado.</summary>
        /// <param name="status">Motivo del rechazo.</param>
        /// <returns>El resultado sin amigos en línea; nunca es null.</returns>
        public static FriendsSubscription CreateRejected(FriendsChangeStatus status)
        {
            FriendsSubscription friendsSubscription = new FriendsSubscription(
                status,
                LobbyMembershipChange.NoUserId,
                null,
                _noUserIds,
                _noCallbacks);

            return friendsSubscription;
        }

        /// <summary>Crea un resultado aplicado.</summary>
        /// <param name="userId">Id de la cuenta del jugador.</param>
        /// <param name="nickname">Nickname del jugador.</param>
        /// <param name="onlineFriendUserIds">Ids de los amigos en línea; no debe modificarse después.</param>
        /// <param name="presenceRecipientCallbacks">Callbacks a quienes avisar; no debe modificarse después.</param>
        /// <returns>El resultado aplicado; nunca es null.</returns>
        public static FriendsSubscription CreateApplied(
            int userId,
            string nickname,
            IReadOnlyList<int> onlineFriendUserIds,
            IReadOnlyList<IFriendsManagerCallback> presenceRecipientCallbacks)
        {
            FriendsSubscription friendsSubscription = new FriendsSubscription(
                FriendsChangeStatus.Applied,
                userId,
                nickname,
                onlineFriendUserIds,
                presenceRecipientCallbacks);

            return friendsSubscription;
        }
    }
}