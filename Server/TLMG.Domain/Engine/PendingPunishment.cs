using System;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Tracks who chooses the lost card, who loses it and the deadline after a failed challenge. Pending punishment
    /// blocks other game actions. The index refers to the actual hand. Services shuffles the slots shown for a blind
    /// choice. An active punishment has only DeadlineUtc set. A suspended one has only RemainingTime set. This class
    /// alone switches between the two states.
    /// </summary>
    public sealed class PendingPunishment
    {
        private const int FirstCardIndex = 0;

        private readonly MatchPlayer _punishedPlayer;

        /// <summary>Creates the punishment for a failed challenge.</summary>
        /// <param name="chooserPlayerId">Player choosing the card, who owns the Fatal Cat's pile.</param>
        /// <param name="punishedPlayer">Player losing a card, who made the bid.</param>
        /// <param name="isBlindChoice">True if the chooser cannot see the punished player's cards.</param>
        /// <param name="deadlineUtc">UTC deadline for choosing.</param>
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

        /// <summary>Player who chooses the lost card.</summary>
        public int ChooserPlayerId { get; }

        /// <summary>Punished player, who made the failed bid.</summary>
        public int PunishedPlayerId => _punishedPlayer.PlayerId;

        /// <summary>Whether the choice is blind. False when the bidder revealed their own Fatal Cat.</summary>
        public bool IsBlindChoice { get; }

        /// <summary>
        /// UTC deadline while active. The choice expires when nowUtc reaches or passes it. Null during suspension.
        /// </summary>
        public DateTime? DeadlineUtc { get; private set; }

        /// <summary>
        /// Time remaining when punishment paused, or null while active.
        /// </summary>
        public TimeSpan? RemainingTime { get; private set; }

        /// <summary>Card lost by the punished player, or null while pending. Only that player may see it.</summary>
        public CardKind? RemovedCard { get; private set; }

        /// <summary>Punished player.</summary>
        internal MatchPlayer PunishedPlayer => _punishedPlayer;

        /// <summary>Number of choices, equal to all remaining lives, now back in hand.</summary>
        internal int PunishedCardCount => _punishedPlayer.HandCards.Count;

        /// <summary>Whether punishment is suspended, determined by RemainingTime alone.</summary>
        internal bool IsSuspended => RemainingTime.HasValue;

        /// <summary>
        /// Checks expiry exactly when nowUtc reaches DeadlineUtc. A suspended deadline never runs or expires.
        /// </summary>
        /// <param name="nowUtc">Server time in UTC.</param>
        /// <returns>True if the deadline is active and nowUtc has reached or passed DeadlineUtc.</returns>
        internal bool HasExpired(DateTime nowUtc)
        {
            return DeadlineUtc.HasValue && nowUtc >= DeadlineUtc.Value;
        }

        /// <summary>Checks whether the index points to a card in the punished player's hand.</summary>
        /// <param name="cardIndex">Index in the punished player's hand.</param>
        /// <returns>True if the index is in [0, PunishedCardCount).</returns>
        internal bool IsValidCardIndex(int cardIndex)
        {
            return cardIndex >= FirstCardIndex && cardIndex < PunishedCardCount;
        }

        /// <summary>Checks suspension, chooser, deadline and index without changing state.</summary>
        /// <param name="playerId">Player requesting the choice.</param>
        /// <param name="cardIndex">Client-supplied index in the punished player's hand. It must be checked.</param>
        /// <param name="nowUtc">Server time in UTC.</param>
        /// <returns>CardRemoved for a valid choice, otherwise the rejection reason.</returns>
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

        /// <summary>Checks that punishment is active and the deadline has expired before a random choice.</summary>
        /// <param name="nowUtc">Server time in UTC.</param>
        /// <returns>CardRemoved if punishment can be resolved, otherwise the rejection reason.</returns>
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
        /// Pauses punishment when the punished player disconnects, saves the remaining time and clears the deadline.
        /// Only that player can pause it. Already suspended or expired punishment is rejected without changes.
        /// </summary>
        /// <param name="playerId">Disconnected player.</param>
        /// <param name="nowUtc">Server time in UTC.</param>
        /// <returns>Suspended on success, otherwise the rejection reason.</returns>
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
        /// Resumes suspended punishment only when the punished player returns. The deadline becomes nowUtc plus the
        /// saved time, never the full original interval. Rejection leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Returning player.</param>
        /// <param name="nowUtc">Server time in UTC.</param>
        /// <returns>Resumed on success, otherwise the rejection reason.</returns>
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

        /// <summary>
        /// Checks that the returning player is the punished player and punishment is suspended. Leaves state unchanged.
        /// </summary>
        /// <param name="playerId">Returning player.</param>
        /// <returns>Resumed if punishment can resume, otherwise the rejection reason.</returns>
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

        /// <summary>Permanently removes the chosen card from the punished player's hand and records it.</summary>
        /// <param name="cardIndex">Valid index in the punished player's hand.</param>
        internal void RemoveCard(int cardIndex)
        {
            RemovedCard = _punishedPlayer.RemoveHandCardAt(cardIndex);
        }
    }
}
