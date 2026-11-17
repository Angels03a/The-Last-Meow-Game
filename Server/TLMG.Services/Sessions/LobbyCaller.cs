namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Identidad y canal de quien llama a una operación de lobby, de chat o de partida; se crea una vez por llamada.
    /// </summary>
    public sealed class LobbyCaller
    {
        /// <summary>Crea la copia de los datos recibidos.</summary>
        /// <param name="sessionToken">Token de sesión recibido del cliente; puede ser null o vacío.</param>
        /// <param name="channelId">Identificador del canal desde el que llega la petición.</param>
        public LobbyCaller(string sessionToken, string channelId)
        {
            SessionToken = sessionToken;
            ChannelId = channelId;
        }

        /// <summary>Token de sesión recibido del cliente.</summary>
        public string SessionToken { get; }

        /// <summary>Identificador del canal desde el que llega la petición.</summary>
        public string ChannelId { get; }

        /// <summary>Indica si el token y el canal vienen con valor.</summary>
        public bool HasSessionAndChannel => !string.IsNullOrEmpty(SessionToken) && !string.IsNullOrEmpty(ChannelId);
    }
}