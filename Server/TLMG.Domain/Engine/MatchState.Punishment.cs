using System;
using System.Collections.Generic;
using System.Linq;

namespace TLMG.Domain.Engine
{
    public sealed partial class MatchState
    {
        private const int LastSurvivorCount = 1;

        /// <summary>Obtiene el último castigo aplicado, con la carta que se perdió, o null si no hubo.</summary>
        public PendingPunishment LastPunishment { get; private set; }

        /// <summary>Obtiene a los jugadores eliminados, en el orden en que quedaron sin vidas.</summary>
        public IReadOnlyList<MatchPlayer> EliminatedPlayers { get; }

        /// <summary>
        /// Elige, una sola vez y antes de que venza el plazo, la carta que pierde el castigado. Solo puede elegir quien
        /// indica PendingPunishment. Después se aplica la eliminación si quedó sin vidas, se comprueba el fin de la
        /// partida y, si no termina, empieza una ronda con el primer jugador activo. Si rechaza, nada cambia.
        /// </summary>
        /// <param name="playerId">Jugador que elige.</param>
        /// <param name="cardIndex">Índice en la mano real del castigado; llega del cliente y no es confiable.</param>
        /// <param name="nowUtc">Hora UTC del servidor.</param>
        /// <returns>CardRemoved si se eliminó la carta; en otro caso, el motivo del rechazo.</returns>
        /// <exception cref="ArgumentException">nowUtc no es una hora UTC.</exception>
        public PunishmentStatus ChoosePunishmentCard(int playerId, int cardIndex, DateTime nowUtc)
        {
            ValidateUtc(nowUtc);
            PunishmentStatus punishmentStatus;

            if (FindPlayer(playerId) == null)
            {
                punishmentStatus = PunishmentStatus.PlayerNotInMatch;
            }
            else if (Phase != RoundPhase.PunishmentPending)
            {
                punishmentStatus = PunishmentStatus.NoPunishmentPending;
            }
            else
            {
                punishmentStatus = PendingPunishment.ValidateChoice(playerId, cardIndex, nowUtc);
            }

            if (punishmentStatus == PunishmentStatus.CardRemoved)
            {
                ApplyPunishment(cardIndex);
            }

            return punishmentStatus;
        }

        /// <summary>
        /// Resuelve el castigo cuando venció el plazo (nowUtc igual o posterior a DeadlineUtc): el sistema elige al
        /// azar, con la misma probabilidad para cada carta del castigado, incluido el Gato. Antes del plazo se
        /// rechaza. Si rechaza, nada cambia.
        /// </summary>
        /// <param name="nowUtc">Hora UTC del servidor.</param>
        /// <param name="pickRandomIndex">
        /// Azar que provee Services: recibe la cantidad n de cartas y devuelve un índice uniforme en [0, n).
        /// Nunca debe venir del cliente.
        /// </param>
        /// <returns>CardRemoved si se eliminó la carta; en otro caso, el motivo del rechazo.</returns>
        /// <exception cref="ArgumentException">nowUtc no es una hora UTC.</exception>
        /// <exception cref="ArgumentNullException">pickRandomIndex es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">El azar devolvió un índice fuera de [0, n).</exception>
        public PunishmentStatus ExpirePunishment(DateTime nowUtc, Func<int, int> pickRandomIndex)
        {
            ValidateUtc(nowUtc);

            if (pickRandomIndex == null)
            {
                throw new ArgumentNullException(nameof(pickRandomIndex));
            }

            PunishmentStatus punishmentStatus = PunishmentStatus.CardRemoved;

            if (Phase != RoundPhase.PunishmentPending)
            {
                punishmentStatus = PunishmentStatus.NoPunishmentPending;
            }
            else if (!PendingPunishment.HasExpired(nowUtc))
            {
                punishmentStatus = PunishmentStatus.DeadlineNotReached;
            }
            else
            {
                ResolvePunishmentAtRandom(pickRandomIndex);
            }

            return punishmentStatus;
        }

        /// <summary>
        /// Elige al azar la carta del castigado y aplica el castigo. Lo usa el vencimiento del plazo y lo reutilizará
        /// la expulsión de quien elige (E6). Valida el índice antes de cambiar cualquier estado.
        /// </summary>
        private void ResolvePunishmentAtRandom(Func<int, int> pickRandomIndex)
        {
            int cardIndex = pickRandomIndex(PendingPunishment.PunishedCardCount);

            if (!PendingPunishment.IsValidCardIndex(cardIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(pickRandomIndex), "El azar devolvió un índice inválido.");
            }

            ApplyPunishment(cardIndex);
        }

        /// <summary>
        /// Quita la carta, registra la eliminación si el castigado quedó sin vidas y comprueba el fin: con un solo
        /// jugador activo gana él; si no, empieza la siguiente ronda con el primer jugador activo del orden.
        /// </summary>
        private void ApplyPunishment(int cardIndex)
        {
            PendingPunishment punishment = PendingPunishment;
            punishment.RemoveCard(cardIndex);
            LastPunishment = punishment;
            PendingPunishment = null;

            if (punishment.PunishedPlayer.IsEliminated)
            {
                _eliminatedPlayers.Add(punishment.PunishedPlayer);
            }

            if (_players.Count(player => !player.IsEliminated) == LastSurvivorCount)
            {
                Winner = _players.First(player => !player.IsEliminated);
                Phase = RoundPhase.MatchFinished;
            }
            else
            {
                StartNextRound();
            }
        }
    }
}
