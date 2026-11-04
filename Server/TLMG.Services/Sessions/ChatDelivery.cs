using System;
using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Copia efímera de un envío aceptado; no se almacena en el registro.</summary>
    public sealed class ChatDelivery
    {
        /// <summary>Crea la copia de los datos recibidos.</summary>
        /// <param name="roomId">Sala de los destinatarios.</param>
        /// <param name="clientMessageId">Correlación proporcionada por el emisor.</param>
        /// <param name="sender">Identidad del emisor copiada de la sesión.</param>
        /// <param name="text">Texto validado.</param>
        /// <param name="sentAtUtc">Hora UTC reservada al aceptar.</param>
        /// <param name="recipients">Copia de solo lectura de callbacks; no se modifica después.</param>
        public ChatDelivery(
            int roomId,
            Guid clientMessageId,
            PlayerSession sender,
            string text,
            DateTime sentAtUtc,
            IReadOnlyList<IChatManagerCallback> recipients)
        {
            RoomId = roomId;
            ClientMessageId = clientMessageId;
            Sender = sender;
            Text = text;
            SentAtUtc = sentAtUtc;
            Recipients = recipients;
        }

        /// <summary>Sala de los destinatarios.</summary>
        public int RoomId { get; }

        /// <summary>Correlación proporcionada por el emisor.</summary>
        public Guid ClientMessageId { get; }

        /// <summary>Identidad del emisor copiada de la sesión.</summary>
        public PlayerSession Sender { get; }

        /// <summary>Texto validado.</summary>
        public string Text { get; }

        /// <summary>Hora UTC reservada al aceptar.</summary>
        public DateTime SentAtUtc { get; }

        /// <summary>Copia de solo lectura de callbacks; no se modifica después.</summary>
        public IReadOnlyList<IChatManagerCallback> Recipients { get; }
    }
}
