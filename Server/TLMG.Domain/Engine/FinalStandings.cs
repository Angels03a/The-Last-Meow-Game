using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Final standings for one finished match, with a unique rank and points for each player (Rules 5.0.1 to 5.0.4).
    /// Reads MatchState through its public API without changing it. Cumulative scores are handled separately.
    /// </summary>
    public sealed class FinalStandings
    {
        private const int FirstPosition = 1;
        private const int BasePointsOffset = 1;
        private const int MinimumPoints = 0;
        private const int GuestPoints = 0;

        private FinalStandings(FinalStandingsStatus status, IReadOnlyList<PlayerStanding> standings)
        {
            Status = status;
            Standings = standings;
        }

        /// <summary>Whether the standings were calculated or why they were rejected.</summary>
        public FinalStandingsStatus Status { get; }

        /// <summary>Rows from first to last place, empty on rejection.</summary>
        public IReadOnlyList<PlayerStanding> Standings { get; }

        /// <summary>
        /// Calculates final standings for a finished match. Everyone who stayed counts toward the base, including
        /// guests. Players who left rank last with 0 points. Guests keep their place and earn 0. An unfinished match is
        /// rejected without changes. Duplicate guest IDs count only once.
        /// </summary>
        /// <param name="match">Match to rank.</param>
        /// <param name="guestPlayerIds">
        /// Guest players identified by Services using trusted server data. The client cannot supply this list.
        /// </param>
        /// <returns>Calculated standings, or a rejection with empty standings.</returns>
        /// <exception cref="ArgumentNullException">match or guestPlayerIds is null.</exception>
        /// <exception cref="ArgumentException">A listed guest is outside the match.</exception>
        public static FinalStandings CalculateFromMatch(MatchState match, IEnumerable<int> guestPlayerIds)
        {
            HashSet<int> guestIds = ValidateGuests(match, guestPlayerIds);
            FinalStandings finalStandings;

            if (match.Phase != RoundPhase.MatchFinished)
            {
                finalStandings = new FinalStandings(FinalStandingsStatus.MatchNotFinished, new List<PlayerStanding>());
            }
            else
            {
                finalStandings = new FinalStandings(FinalStandingsStatus.Calculated, CreateStandings(match, guestIds));
            }

            return finalStandings;
        }

        /// <summary>
        /// Calculates points by rank (Rule 5.0.1). The base is max(2, counted players − 1). Rank k earns base − (k − 1)
        /// plus the mode bonus for winners at rank 1 or for non-winners elsewhere, with a minimum of 0. Players who
        /// left or were removed do not count. CalculateFromMatch assigns guests 0 after counting and ranking them.
        /// </summary>
        /// <param name="position">Final rank among counted players, starting at 1.</param>
        /// <param name="countedPlayerCount">Number of players counted toward the base, from 1 to 6.</param>
        /// <param name="modeRules">Mode rules that set the bonuses.</param>
        /// <returns>Points earned for this rank.</returns>
        /// <exception cref="ArgumentNullException">modeRules is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Rank or counted player total is outside the allowed range.
        /// </exception>
        public static int CalculatePoints(int position, int countedPlayerCount, MatchModeRules modeRules)
        {
            return CalculatePoints(position, countedPlayerCount, modeRules, position == FirstPosition);
        }

        private static int CalculatePoints(int position, int countedPlayerCount, MatchModeRules modeRules,
            bool isWinner)
        {
            ValidatePointsArguments(position, countedPlayerCount, modeRules);
            int basePoints = Math.Max(ValidationRules.MinimumBasePoints, countedPlayerCount - BasePointsOffset);
            int bonusPoints = modeRules.NonWinnerBonusPoints;

            if (isWinner)
            {
                bonusPoints = modeRules.WinnerBonusPoints;
            }

            int points = Math.Max(MinimumPoints, basePoints - (position - FirstPosition) + bonusPoints);

            return points;
        }

        /// <summary>
        /// Ranks the winner first (Rule 5.0.4). Active players follow by most challenges, most lives, then earliest
        /// last success. Eliminated players follow in reverse elimination order. Players who left rank last, by latest
        /// effective exit and then latest exit start (H-11). A stable sort preserves turn order for remaining ties.
        /// </summary>
        private static List<MatchPlayer> OrderPlayersByPosition(MatchState match)
        {
            List<MatchPlayer> playersByPosition = new List<MatchPlayer>();
            if (match.Winner != null)
            {
                playersByPosition.Add(match.Winner);
            }

            playersByPosition.AddRange(match.Players
                .Where(player => player.IsInPlay && !ReferenceEquals(player, match.Winner))
                .OrderByDescending(player => player.SuccessfulChallengeCount)
                .ThenByDescending(player => player.Lives)
                .ThenBy(player => player.LastSuccessfulChallengeSequence));
            playersByPosition.AddRange(match.EliminatedPlayers.Reverse());
            playersByPosition.AddRange(match.Players
                .Where(player => player.HasLeftMatch)
                .OrderByDescending(player => player.Exit.EffectiveAtUtc)
                .ThenByDescending(player => player.Exit.StartedAtUtc));

            return playersByPosition;
        }

        /// <summary>
        /// Builds one row per player in rank order. Players who stayed count toward the base. Guests keep their rank
        /// with 0 points. Players who left receive 0 without calling CalculatePoints (Rule 5.0.2).
        /// </summary>
        private static IReadOnlyList<PlayerStanding> CreateStandings(MatchState match, HashSet<int> guestIds)
        {
            List<MatchPlayer> playersByPosition = OrderPlayersByPosition(match);
            List<PlayerStanding> standings = new List<PlayerStanding>();
            int countedPlayerCount = match.Players.Count(player => !player.HasLeftMatch);

            foreach (MatchPlayer player in playersByPosition)
            {
                int position = standings.Count + FirstPosition;
                int points = GuestPoints;

                if (!player.HasLeftMatch && !guestIds.Contains(player.PlayerId))
                {
                    points = CalculatePoints(position, countedPlayerCount, match.ModeRules,
                        ReferenceEquals(player, match.Winner));
                }

                standings.Add(new PlayerStanding(player.PlayerId, position, points));
            }

            return standings.AsReadOnly();
        }

        /// <summary>Checks the arguments and returns unique guests.</summary>
        private static HashSet<int> ValidateGuests(MatchState match, IEnumerable<int> guestPlayerIds)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (guestPlayerIds == null)
            {
                throw new ArgumentNullException(nameof(guestPlayerIds));
            }

            HashSet<int> guestIds = new HashSet<int>(guestPlayerIds);

            if (!guestIds.IsSubsetOf(match.Players.Select(player => player.PlayerId)))
            {
                throw new ArgumentException("Un invitado no es jugador de la partida.", nameof(guestPlayerIds));
            }

            return guestIds;
        }

        private static void ValidatePointsArguments(int position, int countedPlayerCount, MatchModeRules modeRules)
        {
            if (modeRules == null)
            {
                throw new ArgumentNullException(nameof(modeRules));
            }

            bool hasValidCount = countedPlayerCount >= FirstPosition
                && countedPlayerCount <= ValidationRules.MaximumPlayersPerMatchCount;

            if (!hasValidCount)
            {
                throw new ArgumentOutOfRangeException(nameof(countedPlayerCount));
            }

            if (position < FirstPosition || position > countedPlayerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }
        }
    }
}
