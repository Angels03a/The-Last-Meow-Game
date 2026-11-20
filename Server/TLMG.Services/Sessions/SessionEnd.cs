using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Immutable session end result listing who to notify of the room exit and presence change after releasing the
    /// lock.
    /// </summary>
    public sealed class SessionEnd
    {
        /// <summary>Crea el resultado de terminar una sesión.</summary>
        /// <param name="userId">Id de la cuenta del jugador cuya sesión terminó.</param>
        /// <param name="nickname">Player's nickname.</param>
        /// <param name="roomDeparture">Player's room change, or NotInRoom if outside a room.</param>
        /// <param name="friendsCallback">
        /// Callback del canal de amigos de la sesión que terminó, capturado antes de quitarlo; null si no tenía.
        /// </param>
        /// <param name="presenceRecipientCallbacks">
        /// Callbacks de los amigos en línea a quienes avisar que el jugador dejó de estarlo; no debe modificarse.
        /// </param>
        public SessionEnd(
            int userId,
            string nickname,
            LobbyMembershipChange roomDeparture,
            IFriendsManagerCallback friendsCallback,
            IReadOnlyList<IFriendsManagerCallback> presenceRecipientCallbacks)
        {
            UserId = userId;
            Nickname = nickname;
            RoomDeparture = roomDeparture;
            FriendsCallback = friendsCallback;
            PresenceRecipientCallbacks = presenceRecipientCallbacks;
        }

        /// <summary>Obtiene el id de la cuenta del jugador cuya sesión terminó.</summary>
        public int UserId { get; }

        internal IReadOnlyList<MatchViewDelivery> MatchViews { get; set; }

        /// <summary>Obtiene el nickname del jugador.</summary>
        public string Nickname { get; }

        /// <summary>Player's room change, always present.</summary>
        public LobbyMembershipChange RoomDeparture { get; }

        /// <summary>Obtiene el callback del canal de amigos de la sesión que terminó, o null si no tenía.</summary>
        public IFriendsManagerCallback FriendsCallback { get; }

        /// <summary>
        /// Obtiene los callbacks de los amigos en línea a quienes avisar que el jugador dejó de estarlo; vacía si no
        /// estaba en línea. Nunca es null.
        /// </summary>
        public IReadOnlyList<IFriendsManagerCallback> PresenceRecipientCallbacks { get; }
    }
}