using System;
using System.Collections.Generic;

namespace TLMG.Domain.Engine
{
    public sealed partial class MatchState
    {
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
        /// azar, con la misma probabilidad para cada carta del castigado, incluido el Gato. Antes del plazo o con el
        /// castigo suspendido se rechaza. Si rechaza, nada cambia.
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

            PunishmentStatus punishmentStatus;

            if (Phase != RoundPhase.PunishmentPending)
            {
                punishmentStatus = PunishmentStatus.NoPunishmentPending;
            }
            else
            {
                punishmentStatus = PendingPunishment.ValidateExpiration(nowUtc);
            }

            if (punishmentStatus == PunishmentStatus.CardRemoved)
            {
                ResolvePunishmentAtRandom(pickRandomIndex);
            }

            return punishmentStatus;
        }

        /// <summary>
        /// Suspende el castigo pendiente porque el jugador castigado se desconectó (Reglas 4.0.6): su plazo deja de
        /// correr y se guarda el tiempo restante. Si quien se desconecta es quien elige y no es el castigado, se
        /// rechaza y su plazo sigue corriendo; con Gato propio ambos son el mismo jugador y sí se suspende (H-14). Un
        /// castigo vencido no se suspende. La fase sigue en PunishmentPending. Si rechaza, nada cambia.
        /// </summary>
        /// <param name="playerId">Jugador que se desconectó; lo detecta Services.</param>
        /// <param name="nowUtc">Hora UTC del servidor; el motor nunca lee el reloj.</param>
        /// <returns>Suspended si se suspendió; en otro caso, el motivo del rechazo.</returns>
        /// <exception cref="ArgumentException">nowUtc no es una hora UTC.</exception>
        public PunishmentStatus SuspendPunishment(int playerId, DateTime nowUtc)
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
                punishmentStatus = PendingPunishment.Suspend(playerId, nowUtc);
            }

            return punishmentStatus;
        }

        /// <summary>
        /// Reanuda el castigo suspendido porque el jugador castigado regresó a tiempo, en una sola transición. Si quien
        /// elige sigue en la partida, la nueva hora límite es nowUtc más el tiempo que quedaba, nunca el plazo
        /// completo, y el azar no se usa. Si quien elige ya salió, el plazo no se reanuda: el sistema elige al azar y
        /// el castigo se completa (Reglas 2.0.3.1). La ventana de reconexión la mide Services. Si rechaza o el azar
        /// devuelve un índice inválido, nada cambia.
        /// </summary>
        /// <param name="playerId">Jugador que regresó; debe ser el castigado.</param>
        /// <param name="nowUtc">Hora UTC del servidor; el motor nunca lee el reloj.</param>
        /// <param name="chooseRandomCardIndex">Azar uniforme sobre la mano del castigado: recibe n, da [0, n).</param>
        /// <returns>Resumed si se reanudó, CardRemoved si se resolvió al azar; si no, el motivo del rechazo.</returns>
        /// <exception cref="ArgumentException">nowUtc no es una hora UTC.</exception>
        /// <exception cref="ArgumentNullException">chooseRandomCardIndex es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">El azar devolvió un índice fuera de [0, n).</exception>
        public PunishmentStatus ResumePunishment(int playerId, DateTime nowUtc, Func<int, int> chooseRandomCardIndex)
        {
            ValidateUtc(nowUtc);

            if (chooseRandomCardIndex == null)
            {
                throw new ArgumentNullException(nameof(chooseRandomCardIndex));
            }

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
                punishmentStatus = PendingPunishment.ValidateResume(playerId);
            }

            if (punishmentStatus == PunishmentStatus.Resumed)
            {
                punishmentStatus = ContinueResumedPunishment(playerId, nowUtc, chooseRandomCardIndex);
            }

            return punishmentStatus;
        }

        /// <summary>Reanuda el plazo si quien elige sigue en la partida; si ya salió, resuelve al azar.</summary>
        private PunishmentStatus ContinueResumedPunishment(int playerId, DateTime nowUtc,
            Func<int, int> chooseRandomCardIndex)
        {
            PunishmentStatus punishmentStatus = PunishmentStatus.Resumed;

            if (FindPlayer(PendingPunishment.ChooserPlayerId) == null)
            {
                ResolvePunishmentAtRandom(chooseRandomCardIndex);
                punishmentStatus = PunishmentStatus.CardRemoved;
            }
            else
            {
                PendingPunishment.Resume(playerId, nowUtc);
            }

            return punishmentStatus;
        }

        /// <summary>
        /// Elige al azar la carta del castigado y aplica el castigo. Lo usan el vencimiento del plazo y la reanudación
        /// cuando quien elige ya salió. Valida el índice antes de cambiar cualquier estado.
        /// </summary>
        private void ResolvePunishmentAtRandom(Func<int, int> pickRandomIndex)
        {
            ApplyPunishment(PickRandomCardIndex(pickRandomIndex));
        }

        /// <summary>Pide al azar un índice de la mano del castigado y lo valida; no cambia nada.</summary>
        private int PickRandomCardIndex(Func<int, int> pickRandomIndex)
        {
            int cardIndex = pickRandomIndex(PendingPunishment.PunishedCardCount);

            if (!PendingPunishment.IsValidCardIndex(cardIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(pickRandomIndex), "El azar devolvió un índice inválido.");
            }

            return cardIndex;
        }

        /// <summary>
        /// Quita la carta, registra la eliminación si el castigado quedó sin vidas y comprueba el fin: con un solo
        /// jugador en juego gana él; si no, empieza la siguiente ronda con el primer jugador en juego del orden.
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

            if (HasSinglePlayerInPlay())
            {
                ResetBetting();
                FinishWithLastPlayerInPlay();
            }
            else
            {
                StartNextRound();
            }
        }
    }
}
