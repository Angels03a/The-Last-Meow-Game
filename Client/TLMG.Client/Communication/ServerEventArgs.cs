using System;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Event data from a server callback. The DTO contains no translated text.
    /// </summary>
    /// <typeparam name="TPayload">Type of DTO received from the server.</typeparam>
    public sealed class ServerEventArgs<TPayload> : EventArgs
    {
        /// <summary>Creates the event data for a server notification.</summary>
        /// <param name="payload">DTO received from the server.</param>
        public ServerEventArgs(TPayload payload)
        {
            Payload = payload;
        }

        /// <summary>DTO received from the server. Result DTOs include IsSuccess and ErrorKey.</summary>
        public TPayload Payload { get; }
    }
}