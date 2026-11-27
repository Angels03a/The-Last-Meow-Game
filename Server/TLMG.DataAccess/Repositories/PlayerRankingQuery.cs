using System;
using System.Linq;

using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Constants;

namespace TLMG.DataAccess.Repositories
{
    internal static class PlayerRankingQuery
    {
        internal static IQueryable<Player> FindActivePlayers(TLMG_DBEntities context)
        {
            IQueryable<Player> activePlayers =
                from player in context.Player.AsNoTracking()
                join user in context.User.AsNoTracking() on player.id_user equals user.id_user
                where user.state == ValidationRules.ActiveUserState
                select player;

            return activePlayers;
        }

        internal static int FindRank(IQueryable<Player> activePlayers, Player currentPlayer)
        {
            const int FirstRank = 1;
            int currentPoints = currentPlayer.total_points;
            DateTime pointsReachedAtUtc = currentPlayer.points_reached_at_utc;
            int currentPlayerId = currentPlayer.id_player;

            int higherRankedPlayerCount = (
                from player in activePlayers
                where player.total_points > currentPoints
                    || (player.total_points == currentPoints
                        && (player.points_reached_at_utc < pointsReachedAtUtc
                            || (player.points_reached_at_utc == pointsReachedAtUtc
                                && player.id_player < currentPlayerId)))
                select player.id_player).Count();

            int rank = higherRankedPlayerCount + FirstRank;

            return rank;
        }
    }
}