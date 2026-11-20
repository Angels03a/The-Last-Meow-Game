namespace TLMG.Domain.Engine
{
    /// <summary>Extra life result. Services maps it to an ErrorKey.</summary>
    public enum ExtraLifeStatus
    {
        /// <summary>The player received the extra life.</summary>
        Granted,

        /// <summary>The player is outside this match.</summary>
        PlayerNotInMatch,

        /// <summary>Only eight-card mode allows an extra life.</summary>
        ModeDoesNotAllowExtraLife,

        /// <summary>The match has already awarded its one extra life to a player.</summary>
        ExtraLifeAlreadyGranted,

        /// <summary>
        /// A card has already been placed. The extra life must be awarded before the first placement.
        /// </summary>
        CardAlreadyPlaced
    }
}
