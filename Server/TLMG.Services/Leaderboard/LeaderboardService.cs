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

        public LeaderboardService(
            IPlayerRepository playerRepository,
            ISessionRegistry sessionRegistry)
        {
            _playerRepository = playerRepository;
            _sessionRegistry = sessionRegistry;
        }

        /// <summary>
        /// Retrieves the global leaderboard and personal standing when available.
        /// </summary>
        public OperationGetLeaderboardResult GetLeaderboard(string sessionToken)
        {
            OperationGetLeaderboardResult leaderboardResponse =
                new OperationGetLeaderboardResult
                {
                    Players = new List<LeaderboardPlayer>()
                };

            string errorKey = string.Empty;
            int? registeredUserId = null;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                PlayerSession playerSession;

                if (!_sessionRegistry.TryFindSessionByToken(
                    sessionToken,
                    out playerSession)
                    || playerSession == null)
                {
                    errorKey = ErrorKeys.Common.InvalidSession;
                }
                else if (!GuestIdentity.IsGuestUserId(playerSession.UserId))
                {
                    registeredUserId = playerSession.UserId;
                }
            }

            if (errorKey == string.Empty)
            {
                LeaderboardLookup leaderboardLookup =
                    _playerRepository.FindTopRankedPlayers(
                        MaximumLeaderboardPlayerCount,
                        registeredUserId);

                if (leaderboardLookup != null
                    && leaderboardLookup.WasLeaderboardLookupSuccessful
                    && leaderboardLookup.Players != null)
                {
                    leaderboardResponse.Players =
                        ConvertToLeaderboardPlayers(leaderboardLookup.Players);

                    if (registeredUserId.HasValue)
                    {
                        leaderboardResponse.PersonalRank =
                            leaderboardLookup.PersonalRank;

                        leaderboardResponse.PersonalPoints =
                            leaderboardLookup.PersonalPoints;
                    }

                    leaderboardResponse.IsSuccess = true;
                }
                else
                {
                    errorKey = ErrorKeys.Leaderboard.ConnectionError;
                }
            }

            leaderboardResponse.ErrorKey = errorKey;

            return leaderboardResponse;
        }

        private static List<LeaderboardPlayer> ConvertToLeaderboardPlayers(
            List<RankedPlayer> rankedPlayers)
        {
            List<LeaderboardPlayer> leaderboardPlayers =
                new List<LeaderboardPlayer>();

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
