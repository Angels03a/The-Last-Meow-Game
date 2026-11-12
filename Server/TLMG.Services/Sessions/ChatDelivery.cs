using System;
using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Copia efímera de un envío aceptado; no se almacena en el registro.</summary>
    public sealed class ChatDelivery
    {
        /// <summary>Crea la copia de los datos recibidos.</summary>
        /// <param name="submission">Petición aceptada, con sala, correlación y texto validado.</param>
        /// <param name="sender">Identidad del emisor copiada de la sesión.</param>
        /// <param name="reservation">Hora y destinatarios reservados al aceptar.</param>
        public ChatDelivery(ChatSubmission submission, PlayerSession sender, ChatReservation reservation)
        {
            RoomId = submission.RoomId;
            ClientMessageId = submission.ClientMessageId;
            Sender = sender;
            Text = submission.Text;
            SentAtUtc = reservation.SentAtUtc;
            Recipients = reservation.Recipients;
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