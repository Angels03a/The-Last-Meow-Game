using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Suscripción inmutable de una sesión; solo se guarda en el registro.</summary>
    public sealed class ChatChannel
    {
        /// <summary>Crea la copia de los datos recibidos.</summary>
        /// <param name="sessionToken">Token de la sesión que suscribió el canal.</param>
        /// <param name="channelId">Identificador del canal.</param>
        /// <param name="callback">Callback de entrega.</param>
        public ChatChannel(
            string sessionToken,
            string channelId,
            IChatManagerCallback callback)
        {
            SessionToken = sessionToken;
            ChannelId = channelId;
            Callback = callback;
        }

        /// <summary>Token de la sesión que suscribió el canal.</summary>
        public string SessionToken { get; }

        /// <summary>Identificador del canal.</summary>
        public string ChannelId { get; }

        /// <summary>Callback de entrega.</summary>
        public IChatManagerCallback Callback { get; }
    }
}
