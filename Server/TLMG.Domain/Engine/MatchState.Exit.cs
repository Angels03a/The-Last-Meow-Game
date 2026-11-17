using System;
using System.Linq;

namespace TLMG.Domain.Engine
{
    public sealed partial class MatchState
    {
        /// <summary>
        /// Saca de la partida a un jugador por abandono, desconexión no recuperada o votación (Reglas 4.0.6). El
        /// jugador queda en Players con su salida registrada y no vuelve a participar. La ronda en curso se reinicia:
        /// las cartas colocadas vuelven a las manos, se descartan la apuesta y lo revelado, la mesa queda vacía y
        /// empieza una ronda nueva con el primer jugador en juego. Si queda un solo jugador en juego, gana y no empieza
        /// otra ronda. Un eliminado que sale no cambia nada. Con un castigo pendiente se rechaza por ahora. Si
        /// rechaza, nada cambia.
        /// </summary>
        /// <param name="playerId">Jugador que sale.</param>
        /// <param name="exit">Inicio y hora efectiva de la salida, en UTC.</param>
        /// <param name="chooseRandomCardIndex">
        /// Azar para la carta del castigo; se valida desde ahora, pero solo lo usará la salida durante el castigo
        /// (E6b).
        /// </param>
        /// <returns>RoundRestarted, MatchFinished o EliminatedPlayerLeft; si no, el motivo del rechazo.</returns>
        /// <exception cref="ArgumentNullException">exit o chooseRandomCardIndex es null.</exception>
        public PlayerExitStatus RemovePlayer(int playerId, PlayerExit exit, Func<int, int> chooseRandomCardIndex)
        {
            ValidateExitArguments(exit, chooseRandomCardIndex);
            PlayerExitStatus exitStatus = ValidateExit(playerId);

            if (exitStatus == PlayerExitStatus.RoundRestarted)
            {
                FindParticipant(playerId).Exit = exit;
                DiscardRoundInProgress();

                if (HasSinglePlayerInPlay())
                {
                    FinishWithLastPlayerInPlay();
                    exitStatus = PlayerExitStatus.MatchFinished;
                }
                else
                {
                    StartNextRound();
                }
            }

            return exitStatus;
        }

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
            else if (Phase == RoundPhase.PunishmentPending)
            {
                exitStatus = PlayerExitStatus.ExitDuringPunishmentNotYetSupported;
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
