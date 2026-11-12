using System;
using System.Collections.Generic;

using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Lo que el registro reserva al aceptar un envío: la hora y la copia de destinatarios.</summary>
    public sealed class ChatReservation
    {
        /// <summary>Crea la reserva.</summary>
        /// <param name="sentAtUtc">Hora UTC reservada al aceptar.</param>
        /// <param name="recipients">Copia de solo lectura de callbacks; no se modifica después.</param>
        public ChatReservation(DateTime sentAtUtc, IReadOnlyList<IChatManagerCallback> recipients)
        {
            SentAtUtc = sentAtUtc;
            Recipients = recipients;
        }

        /// <summary>Hora UTC reservada al aceptar.</summary>
        public DateTime SentAtUtc { get; }

        /// <summary>Copia de solo lectura de callbacks; no se modifica después.</summary>
        public IReadOnlyList<IChatManagerCallback> Recipients { get; }
    }
}