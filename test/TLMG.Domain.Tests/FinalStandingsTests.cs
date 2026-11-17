using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Pruebas de la tabla final (Reglas 5.0.1 a 5.0.4): orden de posiciones sin empates, puntos por posición y
    /// modalidad, invitados y rechazos. Los estados se arman solo con acciones públicas del motor; los escenarios
    /// E5-1 a E5-4 se reproducen exactos y la secuencia de retos hace las veces del número de ronda.
    /// </summary>
    public class FinalStandingsTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int PlayerAId = 1;
        private const int PlayerBId = 2;
        private const int PlayerCId = 3;
        private const int PlayerDId = 4;
        private const int PlayerEId = 5;
        private const int PlayerFId = 6;
        private const int PlayerR1Id = 1;
        private const int GuestPlayerId = 2;
        private const int PlayerR2Id = 3;
        private const int PlayerR3Id = 4;
        private const int UnknownPlayerId = 99;
        private const int OneCardBet = 1;
        private const int FirstCardIndex = 0;
        private const int NoChallenges = 0;
        private const int OneChallenge = 1;
        private const int ThreeChallenges = 3;
        private const int ThreeLives = 3;
        private const int FourLives = 4;
        private const int FiveLives = 5;
        private const int FirstPosition = 1;
        private const int NoPoints = 0;
        private const int ZeroPosition = 0;
        private const int ThreeCountedPlayers = 3;
        private const int FourCountedPlayers = 4;
        private const int NoCountedPlayers = 0;
        private const int SevenCountedPlayers = 7;
        private const CardKind SafeCard = CardKind.Safe;
        private const CardKind FatalCatCard = CardKind.FatalCat;

        private static readonly DateTime _nowUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        private static readonly int[] _noGuests = new int[0];

        /// <summary>Verifica CalculateFromMatch_RulesExampleOneWithThreePlayers_Pays3And1And0.</summary>
        [Fact]
        public void CalculateFromMatch_RulesExampleOneWithThreePlayers_Pays3And1And0()
        {
            // Arrange: Reglas 5.0.3 ejemplo 1 y Dominio escenario 2; B y C con 0 retos e iguales vidas.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            WinChallenge(match, PlayerAId);
            WinChallenge(match, PlayerAId);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            Assert.Equal(FinalStandingsStatus.Calculated, finalStandings.Status);
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId }, new[] { 3, 1, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_RulesExampleTwoWithSixPlayersInEightCardMode_Pays8To2.</summary>
        [Fact]
        public void CalculateFromMatch_RulesExampleTwoWithSixPlayersInEightCardMode_Pays8To2()
        {
            // Arrange: Reglas 5.0.3 ejemplo 2; A gana con los 4 retos de la modalidad de 8.
            MatchState match = CreateSixPlayerMatch();
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId, PlayerFId },
                new[] { 8, 6, 5, 4, 3, 2 });
        }

        /// <summary>
        /// Verifica CalculateFromMatch_ScenarioE51WithSixPlayers_RanksByChallengesLivesSequenceAndTurn.
        /// </summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE51WithSixPlayers_RanksByChallengesLivesSequenceAndTurn()
        {
            // Arrange: E5-1 exacto en la modalidad de 6; B logra su reto antes que D y D antes que C.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId,
                PlayerFId);
            LoseCardsUntilLives(match, PlayerBId, FiveLives);
            WinChallenge(match, PlayerBId);
            LoseCardsUntilLives(match, PlayerDId, ThreeLives);
            WinChallenge(match, PlayerDId);
            LoseCardsUntilLives(match, PlayerCId, FiveLives);
            WinChallenge(match, PlayerCId);
            LoseCardsUntilLives(match, PlayerEId, FourLives);
            LoseCardsUntilLives(match, PlayerFId, FourLives);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);
            int[] survivorIds = { PlayerBId, PlayerCId, PlayerDId, PlayerEId, PlayerFId };

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            Assert.Equal(new[] { 1, 1, 1, 0, 0 },
                survivorIds.Select(playerId => GetPlayer(match, playerId).SuccessfulChallengeCount));
            Assert.Equal(new[] { 5, 5, 3, 4, 4 }, survivorIds.Select(playerId => GetPlayer(match, playerId).Lives));
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId, PlayerFId },
                new[] { 7, 5, 4, 3, 2, 1 });
        }

        /// <summary>Verifica CalculateFromMatch_SurvivorWithMoreChallenges_RanksAboveEarlierTurn.</summary>
        [Fact]
        public void CalculateFromMatch_SurvivorWithMoreChallenges_RanksAboveEarlierTurn()
        {
            // Arrange: más retos gana aunque el otro vaya antes en el orden de turnos; C tiene 1 reto y B ninguno.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            WinChallenge(match, PlayerCId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerCId, PlayerBId }, new[] { 4, 2, 1 });
        }

        /// <summary>Verifica CalculateFromMatch_SurvivorsWithEqualChallenges_RankByMoreLives.</summary>
        [Fact]
        public void CalculateFromMatch_SurvivorsWithEqualChallenges_RankByMoreLives()
        {
            // Arrange: con retos iguales, más vidas gana aunque el otro vaya antes en el turno; B perdió una carta.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LoseCardOnOwnCat(match, PlayerBId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerCId, PlayerBId }, new[] { 4, 2, 1 });
        }

        /// <summary>Verifica CalculateFromMatch_SurvivorsWithEqualChallengesAndLives_RankByEarlierSequence.</summary>
        [Fact]
        public void CalculateFromMatch_SurvivorsWithEqualChallengesAndLives_RankByEarlierSequence()
        {
            // Arrange: la secuencia menor gana aunque el otro vaya antes en el turno; C llega a 1 reto antes que B.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            WinChallenge(match, PlayerCId);
            WinChallenge(match, PlayerBId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            Assert.Equal(GetPlayer(match, PlayerBId).Lives, GetPlayer(match, PlayerCId).Lives);
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerCId, PlayerBId, PlayerDId }, new[] { 5, 3, 2, 1 });
        }

        /// <summary>Verifica CalculateFromMatch_ScenarioE52WithTwoEliminated_RanksLaterEliminationHigher.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE52WithTwoEliminated_RanksLaterEliminationHigher()
        {
            // Arrange: E5-2 exacto en la modalidad de 4; B se elimina primero, C después y A gana con su 2.º reto.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            EliminateOnOwnCat(match, PlayerBId);
            EliminateOnOwnCat(match, PlayerCId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            Assert.Equal(new[] { PlayerBId, PlayerCId }, match.EliminatedPlayers.Select(player => player.PlayerId));
            Assert.Equal(NoChallenges, GetPlayer(match, PlayerDId).SuccessfulChallengeCount);
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerDId, PlayerCId, PlayerBId }, new[] { 4, 2, 1, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_LastSurvivorWinsWithOneChallenge_Pays3And1And0.</summary>
        [Fact]
        public void CalculateFromMatch_LastSurvivorWinsWithOneChallenge_Pays3And1And0()
        {
            // Arrange: victoria por último en pie con 1 reto; B se elimina, C gana 1 reto y A se elimina.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            EliminateOnOwnCat(match, PlayerBId);
            WinChallenge(match, PlayerCId);
            EliminateOnOwnCat(match, PlayerAId);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            Assert.Equal(OneChallenge, match.Winner.SuccessfulChallengeCount);
            AssertStandings(finalStandings, new[] { PlayerCId, PlayerAId, PlayerBId }, new[] { 3, 1, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_ScenarioE54WithSixPlayersInEightCardMode_Pays8To2.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE54WithSixPlayersInEightCardMode_Pays8To2()
        {
            // Arrange: E5-4 exacto; P6 se elimina primero y P5 después; P2 llega a 3 retos, P3 a 1 y P1 gana con 4.
            MatchState match = CreateSixPlayerMatch();
            EliminateOnOwnCat(match, PlayerFId);
            EliminateOnOwnCat(match, PlayerEId);
            WinChallenges(match, PlayerBId, ThreeChallenges);
            WinChallenge(match, PlayerCId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);
            int[] survivorIds = { PlayerBId, PlayerCId, PlayerDId };

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            Assert.Equal(new[] { PlayerFId, PlayerEId }, match.EliminatedPlayers.Select(player => player.PlayerId));
            Assert.Equal(new[] { 3, 1, 0 },
                survivorIds.Select(playerId => GetPlayer(match, playerId).SuccessfulChallengeCount));
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId, PlayerFId },
                new[] { 8, 6, 5, 4, 3, 2 });
        }

        /// <summary>
        /// Verifica CalculateFromMatch_ScenarioE53WithGuestInSecondPlace_CountsGuestForBaseAndPaysZero.
        /// </summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE53WithGuestInSecondPlace_CountsGuestForBaseAndPaysZero()
        {
            // Arrange: E5-3 exacto (H-10) en la modalidad de 6; orden R1, G, R2, R3 y G es invitado.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerR1Id, GuestPlayerId, PlayerR2Id, PlayerR3Id);
            EliminateOnOwnCat(match, PlayerR3Id);
            LoseCardsUntilLives(match, GuestPlayerId, FiveLives);
            WinChallenge(match, GuestPlayerId);
            LoseCardsUntilLives(match, PlayerR2Id, FourLives);
            WinChallenges(match, PlayerR1Id, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, new[] { GuestPlayerId });

            // Assert
            Assert.Equal(OneChallenge, GetPlayer(match, GuestPlayerId).SuccessfulChallengeCount);
            Assert.Equal(FiveLives, GetPlayer(match, GuestPlayerId).Lives);
            Assert.Equal(NoChallenges, GetPlayer(match, PlayerR2Id).SuccessfulChallengeCount);
            Assert.Equal(FourLives, GetPlayer(match, PlayerR2Id).Lives);
            Assert.True(GetPlayer(match, PlayerR3Id).IsEliminated);
            AssertStandings(finalStandings, new[] { PlayerR1Id, GuestPlayerId, PlayerR2Id, PlayerR3Id },
                new[] { 5, 0, 2, 1 });
        }

        /// <summary>Verifica CalculateFromMatch_GuestWins_GetsFirstPositionWithZeroPoints.</summary>
        [Fact]
        public void CalculateFromMatch_GuestWins_GetsFirstPositionWithZeroPoints()
        {
            // Arrange: Dominio escenario 6; A es el invitado G y gana con 2 retos en la modalidad de 6.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, new[] { PlayerAId });

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId }, new[] { 0, 2, 1 });
        }

        /// <summary>Verifica CalculateFromMatch_WithRepeatedGuest_CountsGuestOnce.</summary>
        [Fact]
        public void CalculateFromMatch_WithRepeatedGuest_CountsGuestOnce()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, new[] { PlayerBId, PlayerBId });

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerDId }, new[] { 4, 0, 1, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_WithEmptyGuests_PaysEveryPosition.</summary>
        [Fact]
        public void CalculateFromMatch_WithEmptyGuests_PaysEveryPosition()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, new List<int>());

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerDId }, new[] { 4, 2, 1, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_WhenMatchNotFinished_ReturnsMatchNotFinishedWithoutStandings.</summary>
        [Fact]
        public void CalculateFromMatch_WhenMatchNotFinished_ReturnsMatchNotFinishedWithoutStandings()
        {
            // Arrange: A lleva 1 reto de los 2 que necesita.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            WinChallenge(match, PlayerAId);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            Assert.Equal(FinalStandingsStatus.MatchNotFinished, finalStandings.Status);
            Assert.Empty(finalStandings.Standings);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
            Assert.Null(match.Winner);
            Assert.Equal(OneChallenge, GetPlayer(match, PlayerAId).SuccessfulChallengeCount);
        }

        /// <summary>Verifica CalculateFromMatch_WithNullGuests_ThrowsArgumentNullException.</summary>
        [Fact]
        public void CalculateFromMatch_WithNullGuests_ThrowsArgumentNullException()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            Action calculateStandings = () => FinalStandings.CalculateFromMatch(match, null);

            // Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(calculateStandings);
            Assert.Equal("guestPlayerIds", exception.ParamName);
        }

        /// <summary>Verifica CalculateFromMatch_WithNullMatch_ThrowsArgumentNullException.</summary>
        [Fact]
        public void CalculateFromMatch_WithNullMatch_ThrowsArgumentNullException()
        {
            // Arrange
            MatchState match = null;

            // Act
            Action calculateStandings = () => FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(calculateStandings);
            Assert.Equal("match", exception.ParamName);
        }

        /// <summary>Verifica CalculateFromMatch_WithGuestOutsideMatch_ThrowsArgumentException.</summary>
        [Fact]
        public void CalculateFromMatch_WithGuestOutsideMatch_ThrowsArgumentException()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            Action calculateStandings = () => FinalStandings.CalculateFromMatch(match, new[] { UnknownPlayerId });

            // Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(calculateStandings);
            Assert.Equal("guestPlayerIds", exception.ParamName);
        }

        /// <summary>Verifica CalculatePoints_ForEveryCountedPlayerCountModeAndPosition_ReturnsRulesPoints.</summary>
        /// <param name="countedPlayerCount">Jugadores contados para la base.</param>
        /// <param name="cardsPerPlayerCount">Modalidad: 4, 6 u 8 cartas.</param>
        /// <param name="position">Posición final.</param>
        /// <param name="expectedPoints">Puntos según Reglas 5.0.1.</param>
        [Theory]
        [InlineData(1, 4, 1, 3)]
        [InlineData(1, 6, 1, 4)]
        [InlineData(1, 8, 1, 5)]
        [InlineData(2, 4, 1, 3)]
        [InlineData(2, 4, 2, 1)]
        [InlineData(2, 6, 1, 4)]
        [InlineData(2, 6, 2, 2)]
        [InlineData(2, 8, 1, 5)]
        [InlineData(2, 8, 2, 3)]
        [InlineData(3, 4, 1, 3)]
        [InlineData(3, 4, 2, 1)]
        [InlineData(3, 4, 3, 0)]
        [InlineData(3, 6, 1, 4)]
        [InlineData(3, 6, 2, 2)]
        [InlineData(3, 6, 3, 1)]
        [InlineData(3, 8, 1, 5)]
        [InlineData(3, 8, 2, 3)]
        [InlineData(3, 8, 3, 2)]
        [InlineData(4, 4, 1, 4)]
        [InlineData(4, 4, 2, 2)]
        [InlineData(4, 4, 3, 1)]
        [InlineData(4, 4, 4, 0)]
        [InlineData(4, 6, 1, 5)]
        [InlineData(4, 6, 2, 3)]
        [InlineData(4, 6, 3, 2)]
        [InlineData(4, 6, 4, 1)]
        [InlineData(4, 8, 1, 6)]
        [InlineData(4, 8, 2, 4)]
        [InlineData(4, 8, 3, 3)]
        [InlineData(4, 8, 4, 2)]
        [InlineData(5, 4, 1, 5)]
        [InlineData(5, 4, 2, 3)]
        [InlineData(5, 4, 3, 2)]
        [InlineData(5, 4, 4, 1)]
        [InlineData(5, 4, 5, 0)]
        [InlineData(5, 6, 1, 6)]
        [InlineData(5, 6, 2, 4)]
        [InlineData(5, 6, 3, 3)]
        [InlineData(5, 6, 4, 2)]
        [InlineData(5, 6, 5, 1)]
        [InlineData(5, 8, 1, 7)]
        [InlineData(5, 8, 2, 5)]
        [InlineData(5, 8, 3, 4)]
        [InlineData(5, 8, 4, 3)]
        [InlineData(5, 8, 5, 2)]
        [InlineData(6, 4, 1, 6)]
        [InlineData(6, 4, 2, 4)]
        [InlineData(6, 4, 3, 3)]
        [InlineData(6, 4, 4, 2)]
        [InlineData(6, 4, 5, 1)]
        [InlineData(6, 4, 6, 0)]
        [InlineData(6, 6, 1, 7)]
        [InlineData(6, 6, 2, 5)]
        [InlineData(6, 6, 3, 4)]
        [InlineData(6, 6, 4, 3)]
        [InlineData(6, 6, 5, 2)]
        [InlineData(6, 6, 6, 1)]
        [InlineData(6, 8, 1, 8)]
        [InlineData(6, 8, 2, 6)]
        [InlineData(6, 8, 3, 5)]
        [InlineData(6, 8, 4, 4)]
        [InlineData(6, 8, 5, 3)]
        [InlineData(6, 8, 6, 2)]
        public void CalculatePoints_ForEveryCountedPlayerCountModeAndPosition_ReturnsRulesPoints(
            int countedPlayerCount, int cardsPerPlayerCount, int position, int expectedPoints)
        {
            // Arrange
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);

            // Act
            int points = FinalStandings.CalculatePoints(position, countedPlayerCount, modeRules);

            // Assert
            Assert.Equal(expectedPoints, points);
        }

        /// <summary>Verifica CalculatePoints_ForLastPositionInFourCardMode_ReturnsZeroAndNeverNegative.</summary>
        /// <param name="countedPlayerCount">Jugadores contados; la última posición es igual a ellos.</param>
        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void CalculatePoints_ForLastPositionInFourCardMode_ReturnsZeroAndNeverNegative(int countedPlayerCount)
        {
            // Arrange
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer);

            // Act
            int points = FinalStandings.CalculatePoints(countedPlayerCount, countedPlayerCount, modeRules);

            // Assert
            Assert.Equal(NoPoints, points);
        }

        /// <summary>Verifica CalculatePoints_WithPositionOutOfRange_ThrowsArgumentOutOfRangeException.</summary>
        /// <param name="position">Posición fuera de 1 a los contados.</param>
        [Theory]
        [InlineData(ZeroPosition)]
        [InlineData(FourCountedPlayers)]
        public void CalculatePoints_WithPositionOutOfRange_ThrowsArgumentOutOfRangeException(int position)
        {
            // Arrange
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer);

            // Act
            Action calculatePoints = () => FinalStandings.CalculatePoints(position, ThreeCountedPlayers, modeRules);

            // Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(calculatePoints);
            Assert.Equal("position", exception.ParamName);
        }

        /// <summary>
        /// Verifica CalculatePoints_WithCountedPlayerCountOutOfRange_ThrowsArgumentOutOfRangeException.
        /// </summary>
        /// <param name="countedPlayerCount">Contados fuera de 1 a 6.</param>
        [Theory]
        [InlineData(NoCountedPlayers)]
        [InlineData(SevenCountedPlayers)]
        public void CalculatePoints_WithCountedPlayerCountOutOfRange_ThrowsArgumentOutOfRangeException(
            int countedPlayerCount)
        {
            // Arrange
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer);

            // Act
            Action calculatePoints = () =>
                FinalStandings.CalculatePoints(FirstPosition, countedPlayerCount, modeRules);

            // Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(calculatePoints);
            Assert.Equal("countedPlayerCount", exception.ParamName);
        }

        /// <summary>Verifica CalculatePoints_WithNullModeRules_ThrowsArgumentNullException.</summary>
        [Fact]
        public void CalculatePoints_WithNullModeRules_ThrowsArgumentNullException()
        {
            // Arrange
            MatchModeRules modeRules = null;

            // Act
            Action calculatePoints = () =>
                FinalStandings.CalculatePoints(FirstPosition, ThreeCountedPlayers, modeRules);

            // Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(calculatePoints);
            Assert.Equal("modeRules", exception.ParamName);
        }

        private static MatchState CreateSixPlayerMatch()
        {
            return CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId,
                PlayerFId);
        }

        private static void WinChallenges(MatchState match, int playerId, int challengesToWin)
        {
            for (int attempt = 0; attempt < challengesToWin
                && GetPlayer(match, playerId).SuccessfulChallengeCount < challengesToWin; attempt++)
            {
                WinChallenge(match, playerId);
            }

            Assert.True(GetPlayer(match, playerId).SuccessfulChallengeCount >= challengesToWin,
                $"El jugador {playerId} no llegó a {challengesToWin} retos en la ronda {match.RoundNumber}.");
        }

        /// <summary>Comprueba el orden de jugadores, las posiciones consecutivas desde 1 y los puntos.</summary>
        private static void AssertStandings(FinalStandings finalStandings, int[] expectedPlayerIds,
            int[] expectedPoints)
        {
            Assert.Equal(FinalStandingsStatus.Calculated, finalStandings.Status);
            Assert.Equal(expectedPlayerIds, finalStandings.Standings.Select(standing => standing.PlayerId));
            Assert.Equal(Enumerable.Range(FirstPosition, expectedPlayerIds.Length),
                finalStandings.Standings.Select(standing => standing.Position));
            Assert.Equal(expectedPoints, finalStandings.Standings.Select(standing => standing.Points));
        }

        private static MatchState CreateMatch(int cardsPerPlayerCount, params int[] playerIdsInOrder)
        {
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            MatchState match = new MatchState(modeRules, playerIdsInOrder);

            return match;
        }

        private static MatchPlayer GetPlayer(MatchState match, int playerId)
        {
            return match.Players.First(player => player.PlayerId == playerId);
        }

        /// <summary>Quien tiene el turno abre la apuesta y todos los demás se retiran: el que abre apuesta.</summary>
        private static void OpenAndConcede(MatchState match, int betValue)
        {
            int openingPlayerId = match.PlayerInTurn.PlayerId;
            BetActionStatus openStatus = match.OpenBet(openingPlayerId, betValue);
            Assert.True(openStatus == BetActionStatus.Accepted,
                $"El jugador {openingPlayerId} no pudo abrir la apuesta de {betValue} en la ronda "
                + $"{match.RoundNumber}: {openStatus}.");
            int maximumWithdrawals = match.Players.Count;

            for (int withdrawal = 0; withdrawal < maximumWithdrawals && match.Phase == RoundPhase.Betting;
                withdrawal++)
            {
                int blockingPlayerId = match.PlayerInTurn.PlayerId;
                BetActionStatus status = match.WithdrawFromBet(blockingPlayerId);
                Assert.True(status == BetActionStatus.Accepted,
                    $"El jugador {blockingPlayerId} no pudo retirarse en la ronda {match.RoundNumber}: {status}.");
            }

            Assert.True(match.Phase == RoundPhase.ChallengeReady,
                $"El reto no quedó listo en la ronda {match.RoundNumber}: fase {match.Phase}, turno de "
                + $"{match.PlayerInTurn.PlayerId}.");
        }

        private static void PlaceSafeCardsUntilTurnOf(MatchState match, int playerId)
        {
            int maximumPlacements = match.Players.Count;

            for (int placement = 0; placement < maximumPlacements && match.PlayerInTurn.PlayerId != playerId;
                placement++)
            {
                int blockingPlayerId = match.PlayerInTurn.PlayerId;
                CardPlacementStatus status = match.PlaceCard(blockingPlayerId, SafeCard);
                Assert.True(status == CardPlacementStatus.Placed,
                    $"El jugador {blockingPlayerId} no pudo colocar en la ronda {match.RoundNumber}: {status}.");
            }

            Assert.True(match.PlayerInTurn.PlayerId == playerId,
                $"El turno quedó en {match.PlayerInTurn.PlayerId} en la ronda {match.RoundNumber}, no en {playerId}.");
        }

        /// <summary>
        /// Los demás colocan una segura hasta el turno del jugador, él coloca la carta indicada, los demás vuelven a
        /// colocar hasta su turno y apuesta 1 sobre su propia pila: con segura gana el reto y con Gato lo falla.
        /// </summary>
        private static void BetOneOnOwnTopCard(MatchState match, int playerId, CardKind card)
        {
            PlaceSafeCardsUntilTurnOf(match, playerId);
            match.PlaceCard(playerId, card);
            PlaceSafeCardsUntilTurnOf(match, playerId);
            OpenAndConcede(match, OneCardBet);
            match.RevealCard(playerId, playerId, _nowUtc);
        }

        private static void WinChallenge(MatchState match, int playerId)
        {
            BetOneOnOwnTopCard(match, playerId, SafeCard);
        }

        /// <summary>Falla sobre su propio Gato y pierde una segura; si ya no tiene, la primera carta.</summary>
        private static void LoseCardOnOwnCat(MatchState match, int playerId)
        {
            BetOneOnOwnTopCard(match, playerId, FatalCatCard);
            int safeCardIndex = GetPlayer(match, playerId).HandCards.ToList().IndexOf(SafeCard);
            match.ChoosePunishmentCard(playerId, Math.Max(safeCardIndex, FirstCardIndex), _nowUtc);
        }

        private static void LoseCardsUntilLives(MatchState match, int playerId, int remainingLives)
        {
            int maximumLosses = match.ModeRules.CardsPerPlayerCount;

            for (int loss = 0; loss < maximumLosses && GetPlayer(match, playerId).Lives > remainingLives; loss++)
            {
                LoseCardOnOwnCat(match, playerId);
            }

            Assert.True(GetPlayer(match, playerId).Lives == remainingLives,
                $"El jugador {playerId} no quedó con {remainingLives} vidas en la ronda {match.RoundNumber}.");
        }

        private static void EliminateOnOwnCat(MatchState match, int playerId)
        {
            int maximumLosses = match.ModeRules.CardsPerPlayerCount;

            for (int loss = 0; loss < maximumLosses && !GetPlayer(match, playerId).IsEliminated; loss++)
            {
                LoseCardOnOwnCat(match, playerId);
            }

            Assert.True(GetPlayer(match, playerId).IsEliminated,
                $"El jugador {playerId} no quedó eliminado en la ronda {match.RoundNumber}.");
        }
    }
}
