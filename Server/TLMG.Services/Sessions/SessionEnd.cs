using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado inmutable de terminar una sesión: a quién hay que avisar de su salida de la sala y de su presencia,
    /// siempre fuera del candado.
    /// </summary>
    public sealed class SessionEnd
    {
        /// <summary>Crea el resultado de terminar una sesión.</summary>
        /// <param name="userId">Id de la cuenta del jugador cuya sesión terminó.</param>
        /// <param name="nickname">Nickname del jugador.</param>
        /// <param name="roomDeparture">Cambio en la sala del jugador; NotInRoom si no estaba en una.</param>
        /// <param name="presenceRecipientCallbacks">
        /// Callbacks de los amigos en línea a quienes avisar que el jugador dejó de estarlo; no debe modificarse.
        /// </param>
        public SessionEnd(
            int userId,
            string nickname,
            LobbyMembershipChange roomDeparture,
            IReadOnlyList<IFriendsManagerCallback> presenceRecipientCallbacks)
        {
            UserId = userId;
            Nickname = nickname;
            RoomDeparture = roomDeparture;
            PresenceRecipientCallbacks = presenceRecipientCallbacks;
        }

        /// <summary>Obtiene el id de la cuenta del jugador cuya sesión terminó.</summary>
        public int UserId { get; }

        /// <summary>Obtiene el nickname del jugador.</summary>
        public string Nickname { get; }

        /// <summary>Obtiene el cambio en la sala del jugador; nunca es null.</summary>
        public LobbyMembershipChange RoomDeparture { get; }

        /// <summary>
        /// Obtiene los callbacks de los amigos en línea a quienes avisar que el jugador dejó de estarlo; vacía si no
        /// estaba en línea. Nunca es null.
        /// </summary>
        public IReadOnlyList<IFriendsManagerCallback> PresenceRecipientCallbacks { get; }
    }
}