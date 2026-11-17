using System;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Castigo que deja un reto fallido: quién elige la carta que se pierde, a quién se le quita y hasta cuándo puede
    /// elegir. Mientras está pendiente, el motor rechaza toda acción de juego. El índice de la carta se refiere a la
    /// mano real del castigado; el orden aleatorio que ve quien elige a ciegas lo arma Services al proyectar.
    /// Su plazo está activo (DeadlineUtc con valor y RemainingTime nulo) o suspendido (DeadlineUtc nulo y
    /// RemainingTime con valor); nunca ambos ni ninguno. Solo esta clase cambia de un estado a otro.
    /// </summary>
    public sealed class PendingPunishment
    {
        private const int FirstCardIndex = 0;

        private readonly MatchPlayer _punishedPlayer;

        /// <summary>Crea el castigo de un reto fallido.</summary>
        /// <param name="chooserPlayerId">Jugador que elige la carta: el dueño de la pila del Gato.</param>
        /// <param name="punishedPlayer">Jugador al que se le quita la carta: el apostador.</param>
        /// <param name="isBlindChoice">true si quien elige no ve las cartas del castigado.</param>
        /// <param name="deadlineUtc">Hora UTC en la que vence el plazo para elegir.</param>
        internal PendingPunishment(
            int chooserPlayerId,
            MatchPlayer punishedPlayer,
            bool isBlindChoice,
            DateTime deadlineUtc)
        {
            ChooserPlayerId = chooserPlayerId;
            _punishedPlayer = punishedPlayer;
            IsBlindChoice = isBlindChoice;
            DeadlineUtc = deadlineUtc;
        }

        /// <summary>Obtiene el jugador que elige la carta que se pierde.</summary>
        public int ChooserPlayerId { get; }

        /// <summary>Obtiene el jugador castigado, que es el apostador del reto fallido.</summary>
        public int PunishedPlayerId => _punishedPlayer.PlayerId;

        /// <summary>Obtiene si quien elige lo hace a ciegas; false si el Gato era del propio apostador.</summary>
        public bool IsBlindChoice { get; }

        /// <summary>
        /// Obtiene la hora UTC límite para elegir mientras el plazo está activo; con nowUtc igual o posterior, el plazo
        /// venció. Es null mientras el castigo está suspendido.
        /// </summary>
        public DateTime? DeadlineUtc { get; private set; }

        /// <summary>
        /// Obtiene el tiempo que le quedaba al plazo cuando se suspendió, o null mientras el plazo está activo.
        /// </summary>
        public TimeSpan? RemainingTime { get; private set; }

        /// <summary>Obtiene la carta que perdió el castigado, o null si sigue pendiente; solo él debe verla.</summary>
        public CardKind? RemovedCard { get; private set; }

        /// <summary>Obtiene al jugador castigado.</summary>
        internal MatchPlayer PunishedPlayer => _punishedPlayer;

        /// <summary>Obtiene cuántas cartas tiene el castigado para elegir: todas sus vidas, ya en su mano.</summary>
        internal int PunishedCardCount => _punishedPlayer.HandCards.Count;

        /// <summary>Obtiene si el castigo está suspendido; se deduce de RemainingTime, sin otra marca.</summary>
        internal bool IsSuspended => RemainingTime.HasValue;

        /// <summary>
        /// Indica si el plazo ya venció: vence exactamente cuando nowUtc alcanza DeadlineUtc. Suspendido, el plazo no
        /// corre y nunca vence.
        /// </summary>
        /// <param name="nowUtc">Hora UTC del servidor.</param>
        /// <returns>true si el plazo está activo y nowUtc es igual o posterior a DeadlineUtc.</returns>
        internal bool HasExpired(DateTime nowUtc)
        {
            return DeadlineUtc.HasValue && nowUtc >= DeadlineUtc.Value;
        }

        /// <summary>Indica si el índice corresponde a una carta del castigado.</summary>
        /// <param name="cardIndex">Índice en la mano del castigado.</param>
        /// <returns>true si está en [0, PunishedCardCount).</returns>
        internal bool IsValidCardIndex(int cardIndex)
        {
            return cardIndex >= FirstCardIndex && cardIndex < PunishedCardCount;
        }

        /// <summary>Valida la elección: suspensión, quién elige, plazo e índice. No cambia nada.</summary>
        /// <param name="playerId">Jugador que pide elegir.</param>
        /// <param name="cardIndex">Índice elegido en la mano del castigado; llega del cliente, no es confiable.</param>
        /// <param name="nowUtc">Hora UTC del servidor.</param>
        /// <returns>CardRemoved si la elección es válida; en otro caso, el motivo del rechazo.</returns>
        internal PunishmentStatus ValidateChoice(int playerId, int cardIndex, DateTime nowUtc)
        {
            PunishmentStatus punishmentStatus = PunishmentStatus.CardRemoved;

            if (IsSuspended)
            {
                punishmentStatus = PunishmentStatus.PunishmentSuspended;
            }
            else if (playerId != ChooserPlayerId)
            {
                punishmentStatus = PunishmentStatus.NotTheChooser;
            }
            else if (HasExpired(nowUtc))
            {
                punishmentStatus = PunishmentStatus.DeadlineExpired;
            }
            else if (!IsValidCardIndex(cardIndex))
            {
                punishmentStatus = PunishmentStatus.CardIndexNotValid;
            }

            return punishmentStatus;
        }

        /// <summary>Valida que el sistema pueda elegir al azar: castigo no suspendido y plazo vencido.</summary>
        /// <param name="nowUtc">Hora UTC del servidor.</param>
        /// <returns>CardRemoved si ya puede resolverse; en otro caso, el motivo del rechazo.</returns>
        internal PunishmentStatus ValidateExpiration(DateTime nowUtc)
        {
            PunishmentStatus punishmentStatus = PunishmentStatus.CardRemoved;

            if (IsSuspended)
            {
                punishmentStatus = PunishmentStatus.PunishmentSuspended;
            }
            else if (!HasExpired(nowUtc))
            {
                punishmentStatus = PunishmentStatus.DeadlineNotReached;
            }

            return punishmentStatus;
        }

        /// <summary>
        /// Suspende el castigo porque el castigado se desconectó: guarda el tiempo restante y deja de tener hora
        /// límite. Solo el castigado suspende; un castigo ya suspendido o vencido no se suspende. Si rechaza, nada
        /// cambia.
        /// </summary>
        /// <param name="playerId">Jugador que se desconectó.</param>
        /// <param name="nowUtc">Hora UTC del servidor.</param>
        /// <returns>Suspended si se suspendió; en otro caso, el motivo del rechazo.</returns>
        internal PunishmentStatus Suspend(int playerId, DateTime nowUtc)
        {
            PunishmentStatus punishmentStatus = PunishmentStatus.Suspended;

            if (playerId != PunishedPlayerId)
            {
                punishmentStatus = PunishmentStatus.NotThePunished;
            }
            else if (IsSuspended)
            {
                punishmentStatus = PunishmentStatus.PunishmentSuspended;
            }
            else if (HasExpired(nowUtc))
            {
                punishmentStatus = PunishmentStatus.DeadlineExpired;
            }
            else
            {
                RemainingTime = DeadlineUtc.Value - nowUtc;
                DeadlineUtc = null;
            }

            return punishmentStatus;
        }

        /// <summary>
        /// Reanuda el castigo porque el castigado regresó: la nueva hora límite es nowUtc más el tiempo restante; nunca
        /// se reinicia el plazo completo. Solo el castigado reanuda y solo un castigo suspendido. Si rechaza, nada
        /// cambia.
        /// </summary>
        /// <param name="playerId">Jugador que regresó.</param>
        /// <param name="nowUtc">Hora UTC del servidor.</param>
        /// <returns>Resumed si se reanudó; en otro caso, el motivo del rechazo.</returns>
        internal PunishmentStatus Resume(int playerId, DateTime nowUtc)
        {
            PunishmentStatus punishmentStatus = ValidateResume(playerId);

            if (punishmentStatus == PunishmentStatus.Resumed)
            {
                DeadlineUtc = nowUtc + RemainingTime.Value;
                RemainingTime = null;
            }

            return punishmentStatus;
        }

        /// <summary>Valida que pueda reanudar: es el castigado y el castigo está suspendido. No cambia nada.</summary>
        /// <param name="playerId">Jugador que regresó.</param>
        /// <returns>Resumed si puede reanudar; en otro caso, el motivo del rechazo.</returns>
        internal PunishmentStatus ValidateResume(int playerId)
        {
            PunishmentStatus punishmentStatus = PunishmentStatus.Resumed;

            if (playerId != PunishedPlayerId)
            {
                punishmentStatus = PunishmentStatus.NotThePunished;
            }
            else if (!IsSuspended)
            {
                punishmentStatus = PunishmentStatus.PunishmentNotSuspended;
            }

            return punishmentStatus;
        }

        /// <summary>Quita para siempre la carta elegida de la mano del castigado y la recuerda.</summary>
        /// <param name="cardIndex">Índice válido en la mano del castigado.</param>
        internal void RemoveCard(int cardIndex)
        {
            RemovedCard = _punishedPlayer.RemoveHandCardAt(cardIndex);
        }
    }
}
