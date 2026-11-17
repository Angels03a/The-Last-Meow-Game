using System;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Suscripción inmutable de un jugador a una partida; solo se guarda en el registro.</summary>
    internal sealed class MatchChannel
    {
        /// <summary>Crea la copia de los datos recibidos.</summary>
        /// <param name="sessionToken">Token de la sesión que suscribió el canal.</param>
        /// <param name="channelId">Identificador del canal de partida.</param>
        /// <param name="matchId">Partida a la que se suscribió.</param>
        /// <param name="callback">Callback de entrega.</param>
        internal MatchChannel(string sessionToken, string channelId, Guid matchId, IMatchManagerCallback callback)
        {
            SessionToken = sessionToken;
            ChannelId = channelId;
            MatchId = matchId;
            Callback = callback;
        }

        /// <summary>Obtiene el token de la sesión que suscribió el canal.</summary>
        internal string SessionToken { get; }

        /// <summary>Obtiene el identificador del canal de partida.</summary>
        internal string ChannelId { get; }

        /// <summary>Obtiene la partida a la que se suscribió.</summary>
        internal Guid MatchId { get; }

        /// <summary>Obtiene el callback de entrega.</summary>
        internal IMatchManagerCallback Callback { get; }
    }
}
