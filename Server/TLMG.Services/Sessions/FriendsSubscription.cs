using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado inmutable de registrar el canal de amigos de una sesión.
    /// </summary>
    public sealed class FriendsSubscription
    {
        private static readonly IReadOnlyList<int> _noUserIds = new List<int>().AsReadOnly();

        private FriendsSubscription(FriendsChangeStatus status, int userId, IReadOnlyList<int> onlineFriendUserIds)
        {
            Status = status;
            UserId = userId;
            OnlineFriendUserIds = onlineFriendUserIds;
        }

        /// <summary>Obtiene qué ocurrió con el intento de registro.</summary>
        public FriendsChangeStatus Status { get; }

        /// <summary>Obtiene el id de la cuenta del jugador; NoUserId si se rechazó.</summary>
        public int UserId { get; }

        /// <summary>Obtiene los ids de los amigos recibidos que están en línea; nunca es null.</summary>
        public IReadOnlyList<int> OnlineFriendUserIds { get; }

        /// <summary>Crea un resultado rechazado.</summary>
        /// <param name="status">Motivo del rechazo.</param>
        /// <returns>El resultado sin amigos en línea; nunca es null.</returns>
        public static FriendsSubscription CreateRejected(FriendsChangeStatus status)
        {
            FriendsSubscription friendsSubscription =
                new FriendsSubscription(status, LobbyMembershipChange.NoUserId, _noUserIds);

            return friendsSubscription;
        }

        /// <summary>Crea un resultado aplicado.</summary>
        /// <param name="userId">Id de la cuenta del jugador.</param>
        /// <param name="onlineFriendUserIds">Ids de los amigos en línea; no debe modificarse después.</param>
        /// <returns>El resultado aplicado; nunca es null.</returns>
        public static FriendsSubscription CreateApplied(int userId, IReadOnlyList<int> onlineFriendUserIds)
        {
            FriendsSubscription friendsSubscription =
                new FriendsSubscription(FriendsChangeStatus.Applied, userId, onlineFriendUserIds);

            return friendsSubscription;
        }
    }
}