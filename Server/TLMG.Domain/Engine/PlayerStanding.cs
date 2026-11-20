namespace TLMG.Domain.Engine
{
    /// <summary>Final match row with player, unique rank and points earned.</summary>
    public sealed class PlayerStanding
    {
        /// <summary>Creates a player's row.</summary>
        /// <param name="playerId">Player ID within the match.</param>
        /// <param name="position">Final rank starting at 1.</param>
        /// <param name="points">Points earned in the match, always zero or more.</param>
        internal PlayerStanding(int playerId, int position, int points)
        {
            PlayerId = playerId;
            Position = position;
            Points = points;
        }

        /// <summary>Player ID within the match.</summary>
        public int PlayerId { get; }

        /// <summary>Final player rank. The winner ranks first.</summary>
        public int Position { get; }

        /// <summary>Points earned by the player. Guests always receive 0.</summary>
        public int Points { get; }
    }
}
