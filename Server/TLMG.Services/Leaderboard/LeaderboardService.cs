using System.Collections.Generic;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Leaderboard
{
    public class LeaderboardService
    {
        private const int MaximumLeaderboardPlayerCount = 100;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISessionRegistry _sessionRegistry;

        public LeaderboardService(IPlayerRepository playerRepository, ISessionRegistry sessionRegistry)
        {
            _playerRepository = playerRepository;
            _sessionRegistry = sessionRegistry;
        }

        public OperationGetLeaderboardResult GetLeaderboard(string sessionToken)
        {
            OperationGetLeaderboardResult leaderboardResponse = new OperationGetLeaderboardResult
            {
                Players = new List<LeaderboardPlayer>()
            };
            int? registeredUserId = null;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                PlayerSession playerSession;

                if (!_sessionRegistry.TryFindSessionByToken(sessionToken, out playerSession))
                {
                    leaderboardResponse.ErrorKey = ErrorKeys.Common.InvalidSession;
                }
                else if (!GuestIdentity.IsGuestUserId(playerSession.UserId))
                {
                    registeredUserId = playerSession.UserId;
                }
            }

            if (leaderboardResponse.ErrorKey == null)
            {
                LeaderboardLookup leaderboardLookup = _playerRepository.FindTopRankedPlayers(
                    MaximumLeaderboardPlayerCount, registeredUserId);

                if (leaderboardLookup.WasLeaderboardLookupSuccessful)
                {
                    leaderboardResponse.Players = ConvertToLeaderboardPlayers(leaderboardLookup.Players);
                    leaderboardResponse.PersonalRank = leaderboardLookup.PersonalRank;
                    leaderboardResponse.PersonalPoints = leaderboardLookup.PersonalPoints;
                    leaderboardResponse.IsSuccess = true;
                }
                else
                {
                    leaderboardResponse.ErrorKey = ErrorKeys.Leaderboard.ConnectionError;
                }
            }

            return leaderboardResponse;
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
