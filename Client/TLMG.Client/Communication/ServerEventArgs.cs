using System;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Datos de un evento que el servidor originó: el DTO que llegó por el callback, sin textos traducidos.
    /// </summary>
    /// <typeparam name="TPayload">Tipo del DTO que llegó del servidor.</typeparam>
    public sealed class ServerEventArgs<TPayload> : EventArgs
    {
        /// <summary>Crea los datos de un evento del servidor.</summary>
        /// <param name="payload">DTO que llegó del servidor.</param>
        public ServerEventArgs(TPayload payload)
        {
            Payload = payload;
        }

        /// <summary>Obtiene el DTO que llegó del servidor; si es un resultado, trae IsSuccess y ErrorKey.</summary>
        public TPayload Payload { get; }
    }
}