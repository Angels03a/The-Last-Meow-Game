using System;
using System.Collections.Generic;
using System.Linq;

namespace TLMG.Domain.Engine
{
    public sealed partial class MatchState
    {
        private const int LeavingPlayerCount = 1;

        /// <summary>
        /// Saca de la partida a un jugador por abandono, desconexión no recuperada o votación (Reglas 4.0.6 y
        /// 2.0.3.1). El jugador queda en Players con su salida registrada y no vuelve a participar. Si queda un solo
        /// jugador en juego, gana: se limpian el castigo pendiente y la apuesta y no empieza otra ronda. Sin castigo
        /// pendiente, la ronda en curso se reinicia: las cartas colocadas vuelven a las manos, se descartan la
        /// apuesta y lo revelado y empieza una ronda nueva. Con castigo pendiente la ronda no se reinicia: si sale el
        /// castigado, el castigo se cancela y empieza la siguiente ronda; si sale quien elige con el castigo activo,
        /// el sistema elige al azar y el castigo se completa; con el castigo suspendido, o si sale un tercero, solo
        /// se registra la salida. Un eliminado que sale no cambia nada. Si rechaza o el azar devuelve un índice
        /// inválido, nada cambia.
        /// </summary>
        /// <param name="playerId">Jugador que sale.</param>
        /// <param name="exit">Inicio y hora efectiva de la salida, en UTC.</param>
        /// <param name="chooseRandomCardIndex">
        /// Azar uniforme sobre las cartas del castigado: recibe n y da [0, n). Se usa solo si sale quien elige con el
        /// castigo activo.
        /// </param>
        /// <returns>El efecto de la salida; MatchFinished si la partida terminó; o el motivo del rechazo.</returns>
        /// <exception cref="ArgumentNullException">exit o chooseRandomCardIndex es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">El azar devolvió un índice fuera de [0, n).</exception>
        public PlayerExitStatus RemovePlayer(int playerId, PlayerExit exit, Func<int, int> chooseRandomCardIndex)
        {
            ValidateExitArguments(exit, chooseRandomCardIndex);
            PlayerExitStatus exitStatus = ValidateExit(playerId);

            if (exitStatus == PlayerExitStatus.RoundRestarted)
            {
                MatchPlayer leavingPlayer = FindParticipant(playerId);
                int? randomCardIndex = null;

                if (!LeavesSinglePlayerInPlay() && IsActiveChooserOnly(leavingPlayer))
                {
                    randomCardIndex = PickRandomCardIndex(chooseRandomCardIndex);
                }

                leavingPlayer.Exit = exit;
                exitStatus = ApplyExit(leavingPlayer, randomCardIndex);
            }

            return exitStatus;
        }

        /// <summary>Aplica las bajas de un mismo instante antes de evaluar al último jugador activo.</summary>
        public PlayerExitStatus RemoveSimultaneousPlayers(IReadOnlyDictionary<int, PlayerExit> exits,
            Func<int, int> chooseRandomCardIndex)
        {
            if (exits == null)
            {
                throw new ArgumentNullException(nameof(exits));
            }

            if (chooseRandomCardIndex == null)
            {
                throw new ArgumentNullException(nameof(chooseRandomCardIndex));
            }

            if (exits.Count < 2 || exits.Values.Any(exit => exit == null)
                || exits.Values.Select(exit => exit.EffectiveAtUtc).Distinct().Count() != 1)
            {
                throw new ArgumentException("Las bajas deben tener la misma hora efectiva.", nameof(exits));
            }

            foreach (int playerId in exits.Keys)
            {
                PlayerExitStatus status = ValidateExit(playerId);
                if (status != PlayerExitStatus.RoundRestarted)
                {
                    return status;
                }
            }

            MatchPlayer[] leavingPlayers = _players.Where(player => exits.ContainsKey(player.PlayerId)).ToArray();
            int remaining = _players.Count(player => player.IsInPlay) - leavingPlayers.Length;
            MatchPlayer affectedPlayer = leavingPlayers[0];
            int? randomCardIndex = null;

            if (remaining > LastSurvivorCount && PendingPunishment != null)
            {
                affectedPlayer = leavingPlayers.FirstOrDefault(player =>
                    player.PlayerId == PendingPunishment.PunishedPlayerId)
                    ?? leavingPlayers.FirstOrDefault(IsActiveChooserOnly) ?? affectedPlayer;
                if (IsActiveChooserOnly(affectedPlayer))
                {
                    randomCardIndex = PickRandomCardIndex(chooseRandomCardIndex);
                }
            }

            foreach (MatchPlayer player in leavingPlayers)
            {
                player.Exit = exits[player.PlayerId];
            }

            if (remaining == 0)
            {
                PendingPunishment = null;
                DiscardRoundInProgress();
                Phase = RoundPhase.MatchFinished;
                return PlayerExitStatus.MatchFinished;
            }

            return ApplyExit(affectedPlayer, randomCardIndex);
        }

        /// <summary>Indica que todos los activos restantes abandonaron en el mismo instante.</summary>
        public bool EndedByCollectiveAbandonment => Phase == RoundPhase.MatchFinished && Winner == null;


        /// <summary>Aplica la salida ya registrada: victoria del último, reinicio o efecto en el castigo.</summary>
        private PlayerExitStatus ApplyExit(MatchPlayer leavingPlayer, int? randomCardIndex)
        {
            PlayerExitStatus exitStatus = PlayerExitStatus.RoundRestarted;

            if (HasSinglePlayerInPlay())
            {
                PendingPunishment = null;
                DiscardRoundInProgress();
                FinishWithLastPlayerInPlay();
                exitStatus = PlayerExitStatus.MatchFinished;
            }
            else if (Phase == RoundPhase.PunishmentPending)
            {
                exitStatus = ApplyExitDuringPunishment(leavingPlayer, randomCardIndex);
            }
            else
            {
                DiscardRoundInProgress();
                StartNextRound();
            }

            return exitStatus;
        }

        /// <summary>
        /// Efecto de la salida en el castigo pendiente: cancelarlo si salió el castigado, completarlo con la carta al
        /// azar ya validada si salió quien elige con el castigo activo, o dejarlo igual.
        /// </summary>
        private PlayerExitStatus ApplyExitDuringPunishment(MatchPlayer leavingPlayer, int? randomCardIndex)
        {
            PlayerExitStatus exitStatus = PlayerExitStatus.ExitRecorded;

            if (leavingPlayer.PlayerId == PendingPunishment.PunishedPlayerId)
            {
                PendingPunishment = null;
                StartNextRound();
                exitStatus = PlayerExitStatus.PunishmentCancelled;
            }
            else if (randomCardIndex.HasValue)
            {
                ApplyPunishment(randomCardIndex.Value);
                exitStatus = PlayerExitStatus.PunishmentResolvedAtRandom;
            }

            if (Phase == RoundPhase.MatchFinished)
            {
                exitStatus = PlayerExitStatus.MatchFinished;
            }

            return exitStatus;
        }

        /// <summary>Indica si sale quien elige, no es el castigado y el castigo está activo (no suspendido).</summary>
        private bool IsActiveChooserOnly(MatchPlayer leavingPlayer)
        {
            bool isChooserOnly = Phase == RoundPhase.PunishmentPending
                && leavingPlayer.PlayerId == PendingPunishment.ChooserPlayerId
                && leavingPlayer.PlayerId != PendingPunishment.PunishedPlayerId;

            return isChooserOnly && !PendingPunishment.IsSuspended;
        }

        /// <summary>Indica si, al salir un jugador en juego, quedaría un solo jugador en juego.</summary>
        private bool LeavesSinglePlayerInPlay() =>
            _players.Count(player => player.IsInPlay) - LeavingPlayerCount == LastSurvivorCount;

        private static void ValidateExitArguments(PlayerExit exit, Func<int, int> chooseRandomCardIndex)
        {
            if (exit == null)
            {
                throw new ArgumentNullException(nameof(exit));
            }

            if (chooseRandomCardIndex == null)
            {
                throw new ArgumentNullException(nameof(chooseRandomCardIndex));
            }
        }

        /// <summary>Valida la salida sin cambiar nada; RoundRestarted indica que puede aplicarse.</summary>
        private PlayerExitStatus ValidateExit(int playerId)
        {
            PlayerExitStatus exitStatus = PlayerExitStatus.RoundRestarted;
            MatchPlayer participant = FindParticipant(playerId);

            if (Phase == RoundPhase.MatchFinished)
            {
                exitStatus = PlayerExitStatus.MatchAlreadyFinished;
            }
            else if (participant == null)
            {
                exitStatus = PlayerExitStatus.PlayerNotInMatch;
            }
            else if (participant.HasLeftMatch)
            {
                exitStatus = PlayerExitStatus.PlayerAlreadyLeft;
            }
            else if (participant.IsEliminated)
            {
                exitStatus = PlayerExitStatus.EliminatedPlayerLeft;
            }

            return exitStatus;
        }

        /// <summary>
        /// Descarta la ronda en curso: todas las cartas colocadas vuelven a las manos, sin cartas volteadas, y se
        /// descartan la resolución a medias y la apuesta. El turno lo da StartNextRound si la partida sigue.
        /// </summary>
        private void DiscardRoundInProgress()
        {
            foreach (MatchPlayer player in _players)
            {
                player.ReturnPileToHand();
            }

            Resolution = null;
            ResetBetting();
        }

        /// <summary>Busca a un participante por su identificador, aunque ya haya salido.</summary>
        private MatchPlayer FindParticipant(int playerId) =>
            _players.FirstOrDefault(player => player.PlayerId == playerId);
    }
}
