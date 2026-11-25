namespace TLMG.Domain.Engine
{
    /// <summary>Result of a player leaving the match.</summary>
    public enum PlayerExitStatus
    {
        /// <summary>The player left and the current round restarted. RoundNumber increased once.</summary>
        RoundRestarted,

        /// <summary>The player left and the sole remaining player won. No new round starts.</summary>
        MatchFinished,

        /// <summary>
        /// An already eliminated player keeps their rank and points. Nothing changes (H-12 and H-16).
        /// </summary>
        EliminatedPlayerLeft,

        /// <summary>The match has ended. The exit changes nothing.</summary>
        MatchAlreadyFinished,

        /// <summary>The player does not belong to the match.</summary>
        PlayerNotInMatch,

        /// <summary>The player already left the match.</summary>
        PlayerAlreadyLeft,

        /// <summary>
        /// The punished player left during pending punishment. Cancel it without taking a life and start the next round
        /// (Rule 2.0.3.1 and H-07).
        /// </summary>
        PunishmentCancelled,

        /// <summary>
        /// The chooser left during active punishment. The system picked a random card and completed the punishment
        /// (Rule 2.0.3.1).
        /// </summary>
        PunishmentResolvedAtRandom,

        /// <summary>
        /// Records a third player's exit or the chooser's exit during suspension. Punishment continues in the same
        /// round.
        /// </summary>
        ExitRecorded
    }
}
