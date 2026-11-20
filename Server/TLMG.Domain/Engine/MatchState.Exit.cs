using System;
using System.Collections.Generic;
using System.Linq;

namespace TLMG.Domain.Engine
{
    public sealed partial class MatchState
    {
        private const int LeavingPlayerCount = 1;

        /// <summary>
        /// Removes a player after a voluntary exit, missed reconnection or vote (Rules 4.0.6 and 2.0.3.1). Players
        /// keeps their exit record and they take no further part. A sole remaining player wins immediately. The
        /// punishment and bid are cleared without a new round. With no pending punishment, cards return to hands, the
        /// bid and reveals are cleared, and a new round starts. During punishment, the punished player's exit cancels
        /// it and starts the next round. If the chooser leaves during active punishment, a random choice completes it.
        /// If the chooser leaves during suspension or a third player leaves, only the exit is recorded. An eliminated
        /// player's exit, rejection or invalid random index leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Player leaving.</param>
        /// <param name="exit">UTC start and effective times of the exit.</param>
        /// <param name="chooseRandomCardIndex">
        /// Uniform random choice from the punished player's cards. Takes n and returns an index in [0, n). Used only if
        /// the chooser leaves during active punishment.
        /// </param>
        /// <returns>Exit effect, MatchFinished if the match ended, or the rejection reason.</returns>
        /// <exception cref="ArgumentNullException">exit or chooseRandomCardIndex is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The random index is outside [0, n).</exception>
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

        /// <summary>Applies all exits at the same instant before checking for a sole remaining player.</summary>
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

        /// <summary>Whether all remaining active players left at the same instant.</summary>
        public bool EndedByCollectiveAbandonment => Phase == RoundPhase.MatchFinished && Winner == null;


        /// <summary>
        /// Applies the recorded exit, resolving a last-player win, round restart or punishment change.
        /// </summary>
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
        /// Cancels punishment if the punished player left. If the chooser left during active punishment, completes it
        /// with the checked random card. Otherwise keeps it pending.
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

        /// <summary>
        /// Whether the chooser is leaving while punishment is active and another player is being punished.
        /// </summary>
        private bool IsActiveChooserOnly(MatchPlayer leavingPlayer)
        {
            bool isChooserOnly = Phase == RoundPhase.PunishmentPending
                && leavingPlayer.PlayerId == PendingPunishment.ChooserPlayerId
                && leavingPlayer.PlayerId != PendingPunishment.PunishedPlayerId;

            return isChooserOnly && !PendingPunishment.IsSuspended;
        }

        /// <summary>Whether one active player would remain after this active player leaves.</summary>
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

        /// <summary>Checks the exit without changing state. RoundRestarted means it may proceed.</summary>
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
        /// Clears the current round. All placed cards return to hands, with no reveals, unfinished challenge or bid
        /// left. StartNextRound assigns the turn if play continues.
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

        /// <summary>Finds a participant by ID, including players who have left.</summary>
        private MatchPlayer FindParticipant(int playerId) =>
            _players.FirstOrDefault(player => player.PlayerId == playerId);
    }
}
