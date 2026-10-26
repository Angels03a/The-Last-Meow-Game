using System.Collections.Generic;

using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;

namespace TLMG.Services.Leaderboard
{
    /// <summary>
    /// Lógica de la tabla de posiciones; no conoce WCF.
    /// </summary>
    public class LeaderboardService
    {
        private const int MaximumLeaderboardPlayerCount = 100;
        private const string DatabaseErrorKey = "Leaderboard_ConnectionErrorMessage";

        private readonly IPlayerRepository _playerRepository;

        /// <summary>Crea el servicio de la tabla de posiciones con su repositorio.</summary>
        /// <param name="playerRepository">Repositorio de cuentas.</param>
        public LeaderboardService(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        /// <summary>Obtiene los mejores jugadores activos, ordenados por puntos.</summary>
        /// <returns>Resultado con los jugadores o, si falla, la clave de error.</returns>
        public OperationGetLeaderboardResult GetLeaderboard()
        {
            OperationGetLeaderboardResult operationGetLeaderboardResult = new OperationGetLeaderboardResult();
            LeaderboardLookup leaderboardLookup = _playerRepository.FindTopRankedPlayers(MaximumLeaderboardPlayerCount);

            if (leaderboardLookup.WasSuccessful)
            {
                operationGetLeaderboardResult.IsSuccess = true;
                operationGetLeaderboardResult.Players = ConvertToLeaderboardPlayers(leaderboardLookup.Players);
            }
            else
            {
                operationGetLeaderboardResult.ErrorKey = DatabaseErrorKey;
            }

            return operationGetLeaderboardResult;
        }

        private static List<LeaderboardPlayer> ConvertToLeaderboardPlayers(List<RankedPlayer> rankedPlayers)
        {
            List<LeaderboardPlayer> leaderboardPlayers = new List<LeaderboardPlayer>();

            foreach (RankedPlayer rankedPlayer in rankedPlayers)
            {
                leaderboardPlayers.Add(new LeaderboardPlayer
                {
                    Nickname = rankedPlayer.Nickname,
                    ProfileImage = rankedPlayer.ProfileImage,
                    TotalPoints = rankedPlayer.TotalPoints,
                    TotalWins = rankedPlayer.TotalWins,
                    TotalGamesPlayed = rankedPlayer.TotalGamesPlayed
                });
            }

            return leaderboardPlayers;
        }
    }
}