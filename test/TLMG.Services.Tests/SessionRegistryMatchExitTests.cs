using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Services.Lobby;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Pruebas de C1 (abandono voluntario inmediato): solo la salida voluntaria saca al jugador del motor; las demás
    /// causas se conservan y dejan la partida intacta hasta la ventana de reconexión (C2).
    /// </summary>
    public class SessionRegistryMatchExitTests
    {
        private const int MinimumPlayerCount = 3;
        private const int SecondRound = 2;
        private const int AfterOnePlacementAndExitVersion = 3;
        private const int CardsPerPlayer = MatchPlayScenario.CardsPerPlayerCount;

        /// <summary>Verifica LeaveRoom_VoluntaryDuringMatch_RemovesPlayerFromEngineAndRestartsRound.</summary>
        [Fact]
        public void LeaveRoom_VoluntaryDuringMatch_RemovesPlayerFromEngineAndRestartsRound()
        {
            // Arrange: el primero coloca una carta y el segundo sale por su voluntad.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int creatorUserId = scenario.CreatorUserId;
            int leavingUserId = scenario.UserIds[1];
            scenario.Place(creatorUserId, MatchCardKind.Safe);

            // Act
            IReadOnlyList<MatchViewDelivery> deliveries = LeaveVoluntarily(scenario, leavingUserId);

            // Assert
            MatchView creatorView = deliveries.Single(item => ReferenceEquals(
                item.Callback, scenario.CallbackOf(creatorUserId))).MatchView;
            Assert.Equal(SecondRound, creatorView.RoundNumber);
            Assert.Equal(AfterOnePlacementAndExitVersion, creatorView.Version);
            Assert.Equal(creatorUserId, creatorView.PlayerInTurnUserId);
            Assert.True(creatorView.Players.Single(player => player.UserId == leavingUserId).HasLeftMatch);
            MatchPlayerView creatorPlayer = creatorView.Players.Single(player => player.UserId == creatorUserId);
            Assert.Equal(0, creatorPlayer.PileCardCount);
            Assert.Equal(CardsPerPlayer, creatorPlayer.HandCardCount);
        }

        /// <summary>Verifica LeaveRoom_VoluntaryDuringMatch_NotifiesOnlyTheOtherSubscribedPlayers.</summary>
        [Fact]
        public void LeaveRoom_VoluntaryDuringMatch_NotifiesOnlyTheOtherSubscribedPlayers()
        {
            // Arrange: se suscriben el que sale y solo uno de los otros dos.
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            int leavingUserId = scenario.UserIds[1];
            int subscribedUserId = scenario.UserIds[2];
            scenario.Subscribe(leavingUserId);
            scenario.Subscribe(subscribedUserId);

            // Act
            IReadOnlyList<MatchViewDelivery> deliveries = LeaveVoluntarily(scenario, leavingUserId);

            // Assert
            MatchViewDelivery delivery = Assert.Single(deliveries);
            Assert.Same(scenario.CallbackOf(subscribedUserId), delivery.Callback);
        }

        /// <summary>Verifica LeaveRoom_WhenTurnPlayerLeaves_GivesTurnToFirstActivePlayer.</summary>
        [Fact]
        public void LeaveRoom_WhenTurnPlayerLeaves_GivesTurnToFirstActivePlayer()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();

            // Act
            IReadOnlyList<MatchViewDelivery> deliveries = LeaveVoluntarily(scenario, scenario.CreatorUserId);

            // Assert
            Assert.All(deliveries, item => Assert.Equal(scenario.UserIds[1], item.MatchView.PlayerInTurnUserId));
        }

        /// <summary>Verifica LeaveRoom_WhenOnlyOneActivePlayerRemains_HeWinsAtOnce.</summary>
        [Fact]
        public void LeaveRoom_WhenOnlyOneActivePlayerRemains_HeWinsAtOnce()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int survivorUserId = scenario.UserIds[2];
            LeaveVoluntarily(scenario, scenario.CreatorUserId);

            // Act
            IReadOnlyList<MatchViewDelivery> deliveries = LeaveVoluntarily(scenario, scenario.UserIds[1]);

            // Assert
            MatchView survivorView = Assert.Single(deliveries).MatchView;
            Assert.Equal(MatchRoundPhase.MatchFinished, survivorView.Phase);
            Assert.Equal(survivorUserId, survivorView.WinnerUserId);
            Assert.Equal(survivorUserId, survivorView.FinalStandings[0].UserId);
            Assert.All(survivorView.FinalStandings.Where(row => row.UserId != survivorUserId),
                row => Assert.Equal(0, row.Points));
            Assert.False(survivorView.CanPlaceCard);
        }

        /// <summary>Verifica LeaveRoom_VoluntaryDuringMatch_ReportsCauseAndAuthorizedMatch.</summary>
        [Fact]
        public void LeaveRoom_VoluntaryDuringMatch_ReportsCauseAndAuthorizedMatch()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            LobbyMembershipChange change = scenario.Registry.LeaveRoom(
                scenario.TokenOf(scenario.UserIds[1]), out deliveries);

            // Assert
            Assert.Equal(RoomExitCause.Voluntary, change.ExitCause);
            Assert.True(change.LeftAuthorizedMatch);
        }

        /// <summary>Verifica LeaveRoom_BeforeTheMatch_DoesNotTouchAnyMatch.</summary>
        [Fact]
        public void LeaveRoom_BeforeTheMatch_DoesNotTouchAnyMatch()
        {
            // Arrange: una sala en el lobby, sin partida autorizada.
            LobbyScenario scenario = new LobbyScenario();
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            LobbyMembershipChange change = scenario.Registry.LeaveRoom(scenario.CreatorToken, out deliveries);

            // Assert
            Assert.False(change.LeftAuthorizedMatch);
            Assert.Empty(deliveries);
            Assert.Equal(RoomExitCause.Voluntary, change.ExitCause);
        }

        /// <summary>Verifica LeaveRoom_TwiceInARow_SecondIsRejectedWithoutDeliveries.</summary>
        [Fact]
        public void LeaveRoom_TwiceInARow_SecondIsRejectedWithoutDeliveries()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int leavingUserId = scenario.UserIds[1];
            LeaveVoluntarily(scenario, leavingUserId);
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            LobbyMembershipChange secondChange = scenario.Registry.LeaveRoom(
                scenario.TokenOf(leavingUserId), out deliveries);

            // Assert
            Assert.Equal(LobbyChangeStatus.NotInRoom, secondChange.Status);
            Assert.Empty(deliveries);
        }

        /// <summary>Verifica LeaveRoom_WithInvalidToken_IsRejectedWithoutDeliveries.</summary>
        [Fact]
        public void LeaveRoom_WithInvalidToken_IsRejectedWithoutDeliveries()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            IReadOnlyList<MatchViewDelivery> deliveries;

            // Act
            LobbyMembershipChange change = scenario.Registry.LeaveRoom("unknown-token", out deliveries);

            // Assert
            Assert.Equal(LobbyChangeStatus.InvalidSession, change.Status);
            Assert.Empty(deliveries);
        }

        /// <summary>Verifica LeaveRoom_ByInvoluntaryCause_KeepsCauseAndLeavesEngineUntouched.</summary>
        /// <param name="exitCause">Causa involuntaria de la salida.</param>
        [Theory]
        [InlineData(RoomExitCause.LobbyChannelClosed)]
        [InlineData(RoomExitCause.SessionEnded)]
        [InlineData(RoomExitCause.SessionReplaced)]
        public void LeaveRoom_ByInvoluntaryCause_KeepsCauseAndLeavesEngineUntouched(RoomExitCause exitCause)
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            int creatorUserId = scenario.CreatorUserId;
            int leavingUserId = scenario.UserIds[1];
            MatchView before = scenario.ReadViewAs(creatorUserId);

            // Act
            LobbyMembershipChange change = LeaveByCause(scenario, leavingUserId, exitCause);

            // Assert
            MatchView after = scenario.ReadViewAs(creatorUserId);
            Assert.Equal(exitCause, change.ExitCause);
            Assert.True(change.LeftAuthorizedMatch);
            Assert.Equal(before.Version, after.Version);
            Assert.Equal(before.RoundNumber, after.RoundNumber);
            Assert.False(after.Players.Single(player => player.UserId == leavingUserId).HasLeftMatch);
        }

        /// <summary>Verifica LeaveLobby_WhileInMatch_DeliversLobbyAndMatchNoticesSeparately.</summary>
        [Fact]
        public void LeaveLobby_WhileInMatch_DeliversLobbyAndMatchNoticesSeparately()
        {
            // Arrange
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            LobbyService lobbyService = new LobbyService(scenario.Registry);
            int leavingUserId = scenario.UserIds[1];
            int remainingUserId = scenario.CreatorUserId;
            int lobbyCallsBefore = scenario.LobbyCallbackOf(remainingUserId).CallCount;
            int matchViewsBefore = scenario.CallbackOf(remainingUserId).Views.Count;

            // Act
            ResultWithNotification<OperationResult> result = lobbyService.LeaveLobby(scenario.TokenOf(leavingUserId));
            Array.ForEach(result.Notifications.ToArray(), notification => notification());

            // Assert
            Assert.True(result.OperationResult.IsSuccess);
            Assert.Equal(lobbyCallsBefore + 1, scenario.LobbyCallbackOf(remainingUserId).CallCount);
            Assert.Equal(matchViewsBefore + 1, scenario.CallbackOf(remainingUserId).Views.Count);
            Assert.Equal(SecondRound, scenario.CallbackOf(remainingUserId).LastView.RoundNumber);
        }

        /// <summary>Salir tras la victoria no cambia el ganador ni los puntos de la tabla final.</summary>
        [Fact]
        public void LeaveRoom_AfterMatchFinished_PreservesFinalStandings()
        {
            MatchPlayScenario scenario = new MatchPlayScenario(MinimumPlayerCount);
            scenario.SubscribeAll();
            scenario.WinRoundWithABetOfOne();
            scenario.WinRoundWithABetOfOne();
            MatchView before = scenario.ReadViewAs(scenario.UserIds[1]);

            IReadOnlyList<MatchViewDelivery> deliveries;
            LobbyMembershipChange departure = scenario.Registry.LeaveRoom(
                scenario.TokenOf(scenario.UserIds[0]), out deliveries);
            MatchView after = scenario.ReadViewAs(scenario.UserIds[1]);

            Assert.Equal(LobbyChangeStatus.Applied, departure.Status);
            Assert.Empty(deliveries);
            Assert.Equal(before.WinnerUserId, after.WinnerUserId);
            Assert.Equal(before.Version, after.Version);
            Assert.Equal(before.FinalStandings.Select(row => (row.UserId, row.Position, row.Points)),
                after.FinalStandings.Select(row => (row.UserId, row.Position, row.Points)));
        }

        private static IReadOnlyList<MatchViewDelivery> LeaveVoluntarily(MatchPlayScenario scenario, int userId)
        {
            IReadOnlyList<MatchViewDelivery> deliveries;
            scenario.Registry.LeaveRoom(scenario.TokenOf(userId), out deliveries);

            return deliveries;
        }

        private static LobbyMembershipChange LeaveByCause(MatchPlayScenario scenario, int userId, RoomExitCause cause)
        {
            LobbyMembershipChange change;
            SessionEnd sessionEnd;

            if (cause == RoomExitCause.LobbyChannelClosed)
            {
                change = scenario.Registry.LeaveRoomByChannel(MatchPlayScenario.LobbyChannelPrefix + userId);
            }
            else if (cause == RoomExitCause.SessionEnded)
            {
                scenario.Registry.EndSession(scenario.TokenOf(userId), out sessionEnd);
                change = sessionEnd.RoomDeparture;
            }
            else
            {
                scenario.Registry.StartRegisteredSession(userId, MatchPlayScenario.NicknamePrefix + userId, out sessionEnd);
                change = sessionEnd.RoomDeparture;
            }

            return change;
        }
    }
}
