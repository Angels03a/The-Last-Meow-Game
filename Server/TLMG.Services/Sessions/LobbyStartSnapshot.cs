using System;
using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    /// <summary>Copia inmutable de preparación, cuenta e inicio; no contiene callbacks ni tokens.</summary>
    public sealed class LobbyStartSnapshot
    {
        /// <summary>Crea la copia; la lista recibida debe ser una copia de solo lectura.</summary>
        /// <param name="readyUserIds">Jugadores Listo en el instante de la copia.</param>
        /// <param name="version">Versión creciente del estado; permite descartar avisos atrasados de esta sala.</param>
        /// <param name="countdownId">
        /// Última generación creada; solo está activa si CountdownEndsAtUtc tiene valor.
        /// </param>
        /// <param name="countdownEndsAtUtc">Hora límite UTC del servidor; null cuando no hay cuenta activa.</param>
        /// <param name="serverTimeUtc">Hora UTC de la copia; el cliente no decide el vencimiento de la cuenta.</param>
        /// <param name="matchId">Identificador estable de la partida autorizada; null antes del inicio.</param>
        /// <param name="matchStartedAtUtc">Hora UTC de autorización; null antes del inicio.</param>
        /// <param name="cardsPerPlayerCount">
        /// Cantidad de cartas por jugador de la partida autorizada o por iniciar.
        /// </param>
        public LobbyStartSnapshot(
            IReadOnlyList<int> readyUserIds,
            long version,
            long countdownId,
            DateTime? countdownEndsAtUtc,
            DateTime serverTimeUtc,
            Guid? matchId,
            DateTime? matchStartedAtUtc,
            int cardsPerPlayerCount)
        {
            ReadyUserIds = readyUserIds;
            Version = version;
            CountdownId = countdownId;
            CountdownEndsAtUtc = countdownEndsAtUtc;
            ServerTimeUtc = serverTimeUtc;
            MatchId = matchId;
            MatchStartedAtUtc = matchStartedAtUtc;
            CardsPerPlayerCount = cardsPerPlayerCount;
        }

        /// <summary>Jugadores Listo en el instante de la copia.</summary>
        public IReadOnlyList<int> ReadyUserIds { get; }

        /// <summary>Versión creciente del estado; permite descartar avisos atrasados de esta sala.</summary>
        public long Version { get; }

        /// <summary>Última generación creada; solo está activa si CountdownEndsAtUtc tiene valor.</summary>
        public long CountdownId { get; }

        /// <summary>Hora límite UTC del servidor; null cuando no hay cuenta activa.</summary>
        public DateTime? CountdownEndsAtUtc { get; }

        /// <summary>Hora UTC de la copia; el cliente no decide el vencimiento de la cuenta.</summary>
        public DateTime ServerTimeUtc { get; }

        /// <summary>Identificador estable de la partida autorizada; null antes del inicio.</summary>
        public Guid? MatchId { get; }

        /// <summary>Hora UTC de autorización; null antes del inicio.</summary>
        public DateTime? MatchStartedAtUtc { get; }

        /// <summary>Cantidad de cartas por jugador de la partida autorizada o por iniciar.</summary>
        public int CardsPerPlayerCount { get; }
    }
}
