namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Result of choosing a punishment card or reaching the deadline. Services maps it to an ErrorKey.
    /// </summary>
    public enum PunishmentStatus
    {
        /// <summary>The punished player lost a card. Phase indicates what happens next.</summary>
        CardRemoved,

        /// <summary>Punishment paused, its clock stopped and the remaining time was saved.</summary>
        Suspended,

        /// <summary>Punishment resumed with its remaining time.</summary>
        Resumed,

        /// <summary>The player requesting the choice is outside the match.</summary>
        PlayerNotInMatch,

        /// <summary>There is no pending punishment. It was resolved or there was no failed challenge.</summary>
        NoPunishmentPending,

        /// <summary>Only the designated player can choose the lost card.</summary>
        NotTheChooser,

        /// <summary>The index does not match a card in the punished player's hand.</summary>
        CardIndexNotValid,

        /// <summary>The choice deadline expired. The system picks a random card.</summary>
        DeadlineExpired,

        /// <summary>The deadline is still open. The system cannot choose yet.</summary>
        DeadlineNotReached,

        /// <summary>Suspended punishment cannot accept a choice, expire or pause again.</summary>
        PunishmentSuspended,

        /// <summary>Punishment is active, so there is nothing to resume.</summary>
        PunishmentNotSuspended,

        /// <summary>Only the punished player can pause or resume their punishment.</summary>
        NotThePunished
    }
}
