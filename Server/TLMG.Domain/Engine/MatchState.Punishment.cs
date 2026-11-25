using System;
using System.Collections.Generic;

namespace TLMG.Domain.Engine
{
    public sealed partial class MatchState
    {
        /// <summary>Last completed punishment and card lost, or null if none.</summary>
        public PendingPunishment LastPunishment { get; private set; }

        /// <summary>Eliminated players in the order they ran out of lives.</summary>
        public IReadOnlyList<MatchPlayer> EliminatedPlayers { get; }

        /// <summary>
        /// Allows the player named by PendingPunishment to choose the lost card once before the deadline. Eliminates
        /// the punished player if out of lives, checks for a winner and otherwise starts a round with the first active
        /// player. Rejection leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Player making the choice.</param>
        /// <param name="cardIndex">
        /// Index in the punished player's actual hand. The client supplies it and it must be checked.
        /// </param>
        /// <param name="nowUtc">Server time in UTC.</param>
        /// <returns>CardRemoved if the card was removed, otherwise the rejection reason.</returns>
        /// <exception cref="ArgumentException">nowUtc is not a UTC time.</exception>
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
        /// Resolves punishment when nowUtc reaches or passes DeadlineUtc. Every card, including a Fatal Cat, has the
        /// same chance of removal. Before the deadline or during suspension, rejects without changing state.
        /// </summary>
        /// <param name="nowUtc">Server time in UTC.</param>
        /// <param name="pickRandomIndex">
        /// Random choice supplied by Services. Takes the card count n and returns a uniform index in [0, n). The client
        /// must never supply it.
        /// </param>
        /// <returns>CardRemoved if the card was removed, otherwise the rejection reason.</returns>
        /// <exception cref="ArgumentException">nowUtc is not a UTC time.</exception>
        /// <exception cref="ArgumentNullException">pickRandomIndex is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The random index is outside [0, n).</exception>
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
        /// Pauses punishment when the punished player disconnects (Rule 4.0.6). Saves the remaining time and stops the
        /// deadline. If only a different chooser disconnects, the deadline keeps running. With the bidder's own Fatal
        /// Cat, both roles belong to the same player and punishment pauses (H-14). Expired punishment cannot pause. The
        /// phase stays PunishmentPending. Rejection leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Player whose disconnection Services detected.</param>
        /// <param name="nowUtc">Server time in UTC. The engine never reads the clock.</param>
        /// <returns>Suspended on success, otherwise the rejection reason.</returns>
        /// <exception cref="ArgumentException">nowUtc is not a UTC time.</exception>
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
        /// Resumes suspended punishment in one step when the punished player returns in time. If the chooser remains,
        /// the deadline becomes nowUtc plus the saved time and no random choice is used. If the chooser left, a random
        /// card completes punishment without restarting the deadline (Rule 2.0.3.1). Services measures the reconnection
        /// window. Rejection or an invalid random index leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Returning player. Must be the punished player.</param>
        /// <param name="nowUtc">Server time in UTC. The engine never reads the clock.</param>
        /// <param name="chooseRandomCardIndex">
        /// Uniform choice from the punished player's hand. Takes n and returns an index in [0, n).
        /// </param>
        /// <returns>
        /// Resumed if the deadline restarted, CardRemoved for a random choice, otherwise the rejection reason.
        /// </returns>
        /// <exception cref="ArgumentException">nowUtc is not a UTC time.</exception>
        /// <exception cref="ArgumentNullException">chooseRandomCardIndex is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The random index is outside [0, n).</exception>
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

        /// <summary>Resumes the deadline if the chooser remains. Otherwise completes punishment at random.</summary>
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
        /// Chooses a random card and applies punishment after expiry or on return when the chooser has left. Checks the
        /// index before changing state.
        /// </summary>
        private void ResolvePunishmentAtRandom(Func<int, int> pickRandomIndex)
        {
            ApplyPunishment(PickRandomCardIndex(pickRandomIndex));
        }

        /// <summary>Requests and checks a random index in the punished player's hand without changing state.</summary>
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
        /// Removes the card and records elimination if the player has no lives. A sole remaining player wins. Otherwise
        /// the next round starts with the first active player in turn order.
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
