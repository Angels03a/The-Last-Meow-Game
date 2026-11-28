using System.Collections.Generic;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Leaderboard;

using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Tests of LeaderboardService. The service only asks the repository for the top players and translates them to
    /// the contract type, so the order (points, and who reached them first, see Dominio_del_juego_v3 §5.3) belongs to
    /// the repository. The tests give the service players that are deliberately not sorted, to prove that it keeps
    /// the order it receives and does not apply a ranking rule of its own.
    /// </summary>
    public class LeaderboardServiceTests
    {
        private const int RequestedPlayerLimit = 100;

        private readonly PlayerRepositoryStub _players = new PlayerRepositoryStub();

        [Fact]
        public void GetLeaderboard_WithSeveralPlayers_TranslatesEveryPropertyOfEachPlayer()
        {
            // Arrange
            LeaderboardService service = CreateService();
            _players.Leaderboard = CreateSuccessfulLookup(
                CreateRankedPlayer("Whiskers", "AVATAR_LION", 120, 8, 20),
                CreateRankedPlayer("Mittens", null, 0, 0, 0),
                CreateRankedPlayer("Felix", string.Empty, 45, 3, 11));

            // Act
            OperationGetLeaderboardResult result = service.GetLeaderboard(string.Empty);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.ErrorKey);
            Assert.Equal(3, result.Players.Count);
            AssertPlayer(result.Players[0], "Whiskers", "AVATAR_LION", 120, 8, 20);
            AssertPlayer(result.Players[1], "Mittens", null, 0, 0, 0);
            AssertPlayer(result.Players[2], "Felix", string.Empty, 45, 3, 11);
        }

        [Fact]
        public void GetLeaderboard_WithPlayersNotSortedByPoints_KeepsTheOrderOfTheRepository()
        {
            // Arrange
            LeaderboardService service = CreateService();
            _players.Leaderboard = CreateSuccessfulLookup(
                CreateRankedPlayer("Low", "AVATAR_CAT", 10, 1, 2),
                CreateRankedPlayer("High", "AVATAR_CAT", 90, 5, 6),
                CreateRankedPlayer("Middle", "AVATAR_CAT", 40, 2, 4));

            // Act
            OperationGetLeaderboardResult result = service.GetLeaderboard(string.Empty);

            // Assert
            Assert.Equal(new[] { "Low", "High", "Middle" }, NicknamesOf(result.Players));
        }

        [Fact]
        public void GetLeaderboard_WithPlayersTiedOnPoints_KeepsTheOrderOfTheRepository()
        {
            // Arrange
            LeaderboardService service = CreateService();
            _players.Leaderboard = CreateSuccessfulLookup(
                CreateRankedPlayer("Zed", "AVATAR_CAT", 40, 2, 4),
                CreateRankedPlayer("Amy", "AVATAR_CAT", 40, 2, 4));

            // Act
            OperationGetLeaderboardResult result = service.GetLeaderboard(string.Empty);

            // Assert
            Assert.Equal(new[] { "Zed", "Amy" }, NicknamesOf(result.Players));
        }

        [Fact]
        public void GetLeaderboard_WithNoPlayers_ReturnsSuccessWithAnEmptyList()
        {
            // Arrange
            LeaderboardService service = CreateService();
            _players.Leaderboard = CreateSuccessfulLookup();

            // Act
            OperationGetLeaderboardResult result = service.GetLeaderboard(string.Empty);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.ErrorKey);
            Assert.NotNull(result.Players);
            Assert.Empty(result.Players);
        }

        [Fact]
        public void GetLeaderboard_WhenTheQueryFails_ReturnsTheConnectionErrorWithoutPlayers()
        {
            // Arrange
            LeaderboardService service = CreateService();
            _players.Leaderboard = new LeaderboardLookup
            {
                WasLeaderboardLookupSuccessful = false,
                Players = new List<RankedPlayer> { CreateRankedPlayer("Whiskers", "AVATAR_LION", 120, 8, 20) }
            };

            // Act
            OperationGetLeaderboardResult result = service.GetLeaderboard(string.Empty);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Leaderboard.ConnectionError, result.ErrorKey);
            Assert.Null(result.Players);
        }

        [Fact]
        public void GetLeaderboard_WhenLookupIsNull_ReturnsFailure()
        {
            // Arrange
            LeaderboardService service = CreateService();
            _players.Leaderboard = null;

            // Act
            OperationGetLeaderboardResult result = service.GetLeaderboard(string.Empty);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Leaderboard.ConnectionError, result.ErrorKey);
            Assert.Null(result.Players);
        }

        [Fact]
        public void GetLeaderboard_WhenPlayersAreNull_ReturnsFailure()
        {
            // Arrange
            LeaderboardService service = CreateService();
            _players.Leaderboard = new LeaderboardLookup { WasLeaderboardLookupSuccessful = true, Players = null };

            // Act
            OperationGetLeaderboardResult result = service.GetLeaderboard(string.Empty);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Leaderboard.ConnectionError, result.ErrorKey);
            Assert.Null(result.Players);
        }

        [Fact]
        public void GetLeaderboard_WhenCalled_AsksTheRepositoryOnceForAtMostOneHundredPlayers()
        {
            // Arrange
            LeaderboardService service = CreateService();
            _players.Leaderboard = CreateSuccessfulLookup();

            // Act
            service.GetLeaderboard(string.Empty);

            // Assert
            Assert.Equal(new[] { RequestedPlayerLimit }, _players.RequestedLeaderboardLimits);
        }

        [Fact]
        public void GetLeaderboard_WithSeveralPlayers_DoesNotChangeTheDataReceivedFromTheRepository()
        {
            // Arrange
            LeaderboardService service = CreateService();
            List<RankedPlayer> receivedPlayers = new List<RankedPlayer>
            {
                CreateRankedPlayer("Whiskers", "AVATAR_LION", 120, 8, 20),
                CreateRankedPlayer("Mittens", null, 0, 0, 0)
            };
            _players.Leaderboard = new LeaderboardLookup
            {
                WasLeaderboardLookupSuccessful = true,
                Players = receivedPlayers
            };

            // Act
            service.GetLeaderboard(string.Empty);

            // Assert
            Assert.Equal(2, receivedPlayers.Count);
            AssertRankedPlayer(receivedPlayers[0], "Whiskers", "AVATAR_LION", 120, 8, 20);
            AssertRankedPlayer(receivedPlayers[1], "Mittens", null, 0, 0, 0);
        }

        private LeaderboardService CreateService()
        {
            return new LeaderboardService(_players, new SessionRegistry());
        }

        private static LeaderboardLookup CreateSuccessfulLookup(params RankedPlayer[] rankedPlayers)
        {
            return new LeaderboardLookup
            {
                WasLeaderboardLookupSuccessful = true,
                Players = new List<RankedPlayer>(rankedPlayers)
            };
        }

        private static RankedPlayer CreateRankedPlayer(
            string nickname,
            string profileImage,
            int totalPoints,
            int totalWins,
            int totalGamesPlayed)
        {
            return new RankedPlayer
            {
                Nickname = nickname,
                ProfileImage = profileImage,
                TotalPoints = totalPoints,
                TotalWins = totalWins,
                TotalGamesPlayed = totalGamesPlayed
            };
        }

        private static List<string> NicknamesOf(List<LeaderboardPlayer> players)
        {
            List<string> nicknames = new List<string>();

            foreach (LeaderboardPlayer player in players)
            {
                nicknames.Add(player.Nickname);
            }

            return nicknames;
        }

        private static void AssertPlayer(
            LeaderboardPlayer player,
            string nickname,
            string profileImage,
            int totalPoints,
            int totalWins,
            int totalGamesPlayed)
        {
            Assert.Equal(nickname, player.Nickname);
            Assert.Equal(profileImage, player.ProfileImage);
            Assert.Equal(totalPoints, player.TotalPoints);
            Assert.Equal(totalWins, player.TotalWins);
            Assert.Equal(totalGamesPlayed, player.TotalGamesPlayed);
        }

        private static void AssertRankedPlayer(
            RankedPlayer player,
            string nickname,
            string profileImage,
            int totalPoints,
            int totalWins,
            int totalGamesPlayed)
        {
            Assert.Equal(nickname, player.Nickname);
            Assert.Equal(profileImage, player.ProfileImage);
            Assert.Equal(totalPoints, player.TotalPoints);
            Assert.Equal(totalWins, player.TotalWins);
            Assert.Equal(totalGamesPlayed, player.TotalGamesPlayed);
        }
    }
}
