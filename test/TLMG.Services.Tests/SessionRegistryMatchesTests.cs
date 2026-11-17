using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Pruebas de la partida en memoria que SessionRegistry crea al autorizar el inicio: un solo MatchState por
    /// partida, participantes congelados en orden de ingreso y vida independiente de la sala.
    /// </summary>
    public class SessionRegistryMatchesTests
    {
        private const int CreatorUserId = 1;
        private const int FirstGuestUserId = 2;
        private const int MinimumPlayerCount = 3;
        private const int MaximumPlayerCount = 6;
        private const int TwoPlayers = 2;
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int CountdownSeconds = 3;
        private const int OneSecond = 1;
        private const int FirstRoundNumber = 1;
        private const int NonParticipantUserId = 99;
        private const string LobbyChannelPrefix = "lobby";

        private static readonly DateTime _startUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>Verifica RequestMatchStart_WithEveryPlayerReady_CreatesMatchWithAllParticipants.</summary>
        /// <param name="playerCount">Jugadores en la sala.</param>
        [Theory]
        [InlineData(MinimumPlayerCount)]
        [InlineData(MaximumPlayerCount)]
        public void RequestMatchStart_WithEveryPlayerReady_CreatesMatchWithAllParticipants(int playerCount)
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(playerCount, FourCardsPerPlayer);

            // Act
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Assert
            MatchSnapshot match = start.FindMatch(change, CreatorUserId);
            Assert.NotNull(match);
            Assert.Equal(playerCount, match.ParticipantUserIds.Count);
            Assert.Equal(FirstRoundNumber, match.RoundNumber);
            Assert.Equal(CreatorUserId, match.PlayerInTurnUserId);
        }

        /// <summary>Verifica RequestMatchStart_WithRoomMode_CreatesEngineWithSameCardsPerPlayer.</summary>
        /// <param name="cardsPerPlayerCount">Modalidad de la sala.</param>
        [Theory]
        [InlineData(FourCardsPerPlayer)]
        [InlineData(SixCardsPerPlayer)]
        [InlineData(EightCardsPerPlayer)]
        public void RequestMatchStart_WithRoomMode_CreatesEngineWithSameCardsPerPlayer(int cardsPerPlayerCount)
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, cardsPerPlayerCount);

            // Act
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Assert
            Assert.Equal(cardsPerPlayerCount, start.FindMatch(change, CreatorUserId).CardsPerPlayerCount);
        }

        /// <summary>Verifica RequestMatchStart_WithSeveralPlayers_KeepsJoinOrder.</summary>
        [Fact]
        public void RequestMatchStart_WithSeveralPlayers_KeepsJoinOrder()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MaximumPlayerCount, FourCardsPerPlayer);

            // Act
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Assert
            Assert.Equal(start.UserIdsInJoinOrder, start.FindMatch(change, CreatorUserId).ParticipantUserIds);
        }

        /// <summary>Verifica RequestMatchStart_WhenAuthorized_LinksMatchIdAndRoomId.</summary>
        [Fact]
        public void RequestMatchStart_WhenAuthorized_LinksMatchIdAndRoomId()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);

            // Act
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Assert
            MatchSnapshot match = start.FindMatch(change, CreatorUserId);
            Assert.Equal(change.Room.Start.MatchId, match.MatchId);
            Assert.Equal(start.Scenario.RoomId, match.RoomId);
            Assert.Equal(start.Scenario.RoomId, change.RoomId);
        }

        /// <summary>Verifica RequestMatchStart_SequentialReentryOfSameGeneration_KeepsMatchIdAndMatch.</summary>
        [Fact]
        public void RequestMatchStart_SequentialReentryOfSameGeneration_KeepsMatchIdAndMatch()
        {
            // Arrange: reentrada secuencial; otro jugador pide el inicio de la misma generación después.
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange firstChange = start.RequestStartAfterCountdown();

            // Act
            LobbyMembershipChange secondChange = start.RequestStartAs(FirstGuestUserId);

            // Assert
            Assert.Equal(firstChange.Room.Start.MatchId, secondChange.Room.Start.MatchId);
            Assert.Empty(secondChange.RecipientCallbacks);
            Assert.Equal(firstChange.Room.Start.Version, secondChange.Room.Start.Version);
            Assert.Equal(start.UserIdsInJoinOrder, start.FindMatch(secondChange, CreatorUserId).ParticipantUserIds);
        }

        /// <summary>Verifica RequestMatchStart_BeforeCountdownEnds_CreatesNoMatch.</summary>
        [Fact]
        public void RequestMatchStart_BeforeCountdownEnds_CreatesNoMatch()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            start.Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds - OneSecond));

            // Act
            LobbyMembershipChange change = start.RequestStartAs(CreatorUserId);

            // Assert
            Assert.False(change.Room.Start.MatchId.HasValue);
            Assert.Empty(change.RecipientCallbacks);
        }

        /// <summary>Verifica RequestMatchStart_WhenOnePlayerIsNotReady_CreatesNoMatch.</summary>
        [Fact]
        public void RequestMatchStart_WhenOnePlayerIsNotReady_CreatesNoMatch()
        {
            // Arrange: el último jugador vuelve a No Listo; no queda cuenta vigente.
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            start.ChangeReadiness(start.UserIdsInJoinOrder.Last(), false);
            start.Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));

            // Act
            LobbyMembershipChange change = start.RequestStartAs(CreatorUserId);

            // Assert
            Assert.False(change.Room.Start.MatchId.HasValue);
            Assert.Empty(change.RecipientCallbacks);
        }

        /// <summary>Verifica RequestMatchStart_WithFewerThanThreePlayers_CreatesNoMatch.</summary>
        [Fact]
        public void RequestMatchStart_WithFewerThanThreePlayers_CreatesNoMatch()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(TwoPlayers, FourCardsPerPlayer);
            start.Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));

            // Act
            LobbyMembershipChange change = start.RequestStartAs(CreatorUserId);

            // Assert
            Assert.False(change.Room.Start.MatchId.HasValue);
            Assert.False(change.Room.Start.CountdownEndsAtUtc.HasValue);
        }

        /// <summary>Verifica LeaveRoom_AfterMatchStarted_KeepsMatchParticipants.</summary>
        [Fact]
        public void LeaveRoom_AfterMatchStarted_KeepsMatchParticipants()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Act
            start.Scenario.Registry.LeaveRoom(start.TokenOf(FirstGuestUserId));

            // Assert
            Assert.Equal(start.UserIdsInJoinOrder, start.FindMatch(change, CreatorUserId).ParticipantUserIds);
        }

        /// <summary>Verifica LeaveRoom_WhenRoomBecomesEmpty_ReleasesMatch.</summary>
        [Fact]
        public void LeaveRoom_WhenRoomBecomesEmpty_ReleasesMatch()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Act
            LobbyMembershipChange lastDeparture = start.LeaveAll();

            // Assert
            Assert.Null(lastDeparture.Room);
            Assert.Null(start.FindMatch(change, CreatorUserId));
        }

        /// <summary>Verifica EndSession_OfLastRoomMember_ReleasesMatch.</summary>
        [Fact]
        public void EndSession_OfLastRoomMember_ReleasesMatch()
        {
            // Arrange: todos salen menos el creador, que después termina su sesión.
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange change = start.RequestStartAfterCountdown();
            start.LeaveAllExceptCreator();

            // Act
            bool wasEnded = start.Scenario.Registry.EndSession(start.TokenOf(CreatorUserId), out SessionEnd sessionEnd);

            // Assert
            Assert.True(wasEnded);
            Assert.Null(sessionEnd.RoomDeparture.Room);
            Assert.Null(start.FindMatch(change, CreatorUserId));
        }

        /// <summary>Verifica FindMatchOfParticipant_WithUserOutsideMatch_ReturnsNull.</summary>
        [Fact]
        public void FindMatchOfParticipant_WithUserOutsideMatch_ReturnsNull()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            LobbyMembershipChange change = start.RequestStartAfterCountdown();

            // Act
            MatchSnapshot match = start.FindMatch(change, NonParticipantUserId);

            // Assert
            Assert.Null(match);
        }

        /// <summary>Verifica FindMatchOfParticipant_WithUnknownMatchId_ReturnsNull.</summary>
        [Fact]
        public void FindMatchOfParticipant_WithUnknownMatchId_ReturnsNull()
        {
            // Arrange
            MatchStartScenario start = new MatchStartScenario(MinimumPlayerCount, FourCardsPerPlayer);
            start.RequestStartAfterCountdown();

            // Act
            MatchSnapshot match = start.Scenario.Registry.FindMatchOfParticipant(Guid.NewGuid(), CreatorUserId);

            // Assert
            Assert.Null(match);
        }

        /// <summary>Reloj UTC que la prueba avanza a mano.</summary>
        private sealed class TestClock
        {
            internal DateTime NowUtc { get; private set; } = _startUtc;

            internal void Advance(TimeSpan elapsed)
            {
                NowUtc = NowUtc.Add(elapsed);
            }

            internal DateTime ReadUtcNow()
            {
                return NowUtc;
            }
        }

        /// <summary>
        /// Sala con reloj controlado: el creador y los demás jugadores entran en orden de id, se fija la modalidad y
        /// todos quedan Listo, con la cuenta regresiva en marcha.
        /// </summary>
        private sealed class MatchStartScenario
        {
            private readonly Dictionary<int, string> _tokensByUserId = new Dictionary<int, string>();

            internal MatchStartScenario(int playerCount, int cardsPerPlayerCount)
            {
                Clock = new TestClock();
                Scenario = new LobbyScenario(new SessionRegistry(Clock.ReadUtcNow));
                _tokensByUserId[CreatorUserId] = Scenario.CreatorToken;
                UserIdsInJoinOrder = Enumerable.Range(CreatorUserId, playerCount).ToList().AsReadOnly();

                foreach (int userId in UserIdsInJoinOrder.Skip(1))
                {
                    _tokensByUserId[userId] = LobbyReadinessHelper.AddPlayer(
                        Scenario, userId, ChannelOf(userId), null);
                }

                Scenario.Registry.ChangeCardsPerPlayer(CallerOf(CreatorUserId), Scenario.RoomId, cardsPerPlayerCount);

                foreach (int userId in UserIdsInJoinOrder)
                {
                    ChangeReadiness(userId, true);
                }
            }

            internal TestClock Clock { get; }

            internal LobbyScenario Scenario { get; }

            internal IReadOnlyList<int> UserIdsInJoinOrder { get; }

            internal string TokenOf(int userId)
            {
                return _tokensByUserId[userId];
            }

            internal LobbyMembershipChange ChangeReadiness(int userId, bool isReady)
            {
                return Scenario.Registry.ChangePlayerReadiness(CallerOf(userId), Scenario.RoomId, isReady);
            }

            /// <summary>Deja vencer la cuenta vigente y pide el inicio como el creador.</summary>
            internal LobbyMembershipChange RequestStartAfterCountdown()
            {
                Clock.Advance(TimeSpan.FromSeconds(CountdownSeconds));

                return RequestStartAs(CreatorUserId);
            }

            internal LobbyMembershipChange RequestStartAs(int userId)
            {
                long countdownId = CurrentCountdownId();

                return Scenario.Registry.RequestMatchStart(CallerOf(userId), Scenario.RoomId, countdownId);
            }

            internal MatchSnapshot FindMatch(LobbyMembershipChange change, int userId)
            {
                return Scenario.Registry.FindMatchOfParticipant(change.Room.Start.MatchId.Value, userId);
            }

            /// <summary>Salen todos menos el creador, en orden de ingreso.</summary>
            internal void LeaveAllExceptCreator()
            {
                foreach (int userId in UserIdsInJoinOrder.Skip(1))
                {
                    Scenario.Registry.LeaveRoom(TokenOf(userId));
                }
            }

            /// <summary>Todos salen de la sala en orden; devuelve la última salida, la que la deja vacía.</summary>
            internal LobbyMembershipChange LeaveAll()
            {
                LobbyMembershipChange lastDeparture = null;

                foreach (int userId in UserIdsInJoinOrder)
                {
                    lastDeparture = Scenario.Registry.LeaveRoom(TokenOf(userId));
                }

                return lastDeparture;
            }

            private long CurrentCountdownId()
            {
                LobbyMembershipChange current = ChangeReadiness(CreatorUserId, true);

                return current.Room.Start.CountdownId;
            }

            private LobbyCaller CallerOf(int userId)
            {
                string channelId = userId == CreatorUserId ? LobbyReadinessHelper.CreatorChannelId : ChannelOf(userId);

                return new LobbyCaller(TokenOf(userId), channelId);
            }

            private static string ChannelOf(int userId)
            {
                return LobbyChannelPrefix + userId;
            }
        }
    }
}
