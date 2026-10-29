using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Jugador dentro de una sala junto con el canal de lobby por el que se le avisa; es inmutable.
    /// </summary>
    internal sealed class LobbyMember
    {
        /// <summary>Crea un miembro de sala.</summary>
        /// <param name="userId">Identificador de la cuenta del jugador.</param>
        /// <param name="channelId">Identificador del canal de lobby del jugador.</param>
        /// <param name="callback">Callback del canal de lobby del jugador.</param>
        public LobbyMember(int userId, string channelId, ILobbyManagerCallback callback)
        {
            UserId = userId;
            ChannelId = channelId;
            Callback = callback;
        }

        /// <summary>Obtiene el identificador de la cuenta del jugador.</summary>
        public int UserId { get; }

        /// <summary>Obtiene el identificador del canal de lobby del jugador.</summary>
        public string ChannelId { get; }

        /// <summary>Obtiene el callback del canal de lobby del jugador.</summary>
        public ILobbyManagerCallback Callback { get; }
    }
}