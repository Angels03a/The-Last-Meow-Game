using System.Collections.Generic;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Leaderboard
{
    public class LeaderboardService
    {
        private const int MaximumLeaderboardPlayerCount = 100;

        private IPlayerRepository _playerRepository;

        public LeaderboardService(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        public OperationGetLeaderboardResult GetLeaderboard()
        {
            OperationGetLeaderboardResult operationGetLeaderboardResult = 
                new OperationGetLeaderboardResult();
            LeaderboardLookup leaderboardLookup = 
                _playerRepository.FindTopRankedPlayers(MaximumLeaderboardPlayerCount);

            if (leaderboardLookup.WasLeaderboardLookupSuccessful)
            {
                operationGetLeaderboardResult.IsSuccess = true;
                operationGetLeaderboardResult.Players = ConvertToLeaderboardPlayers(leaderboardLookup.Players);
            }
            else
            {
                operationGetLeaderboardResult.ErrorKey = ErrorKeys.Leaderboard.ConnectionError;
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