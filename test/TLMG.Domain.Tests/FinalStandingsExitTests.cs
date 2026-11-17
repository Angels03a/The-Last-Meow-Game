using System;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Pruebas de la tabla final con salidas (Reglas 5.0.1, 5.0.2 y 5.0.4; Dominio §4.3; H-11 y H-12): quienes
    /// salieron van al final por salida efectiva, inicio y orden de turnos, cobran 0 y no cuentan para la base.
    /// </summary>
    public class FinalStandingsExitTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int PlayerAId = 1;
        private const int PlayerBId = 2;
        private const int PlayerCId = 3;
        private const int PlayerDId = 4;
        private const int PlayerEId = 5;
        private const int OneCardBet = 1;
        private const int TwoCardBet = 2;
        private const int ThreeCardBet = 3;
        private const int FirstCardIndex = 0;
        private const int FirstPosition = 1;
        private const int NoChallenges = 0;
        private const int OneChallenge = 1;
        private const int GraceSeconds = 15;
        private const int FirstDisconnectionSeconds = 60;
        private const int SecondDisconnectionSeconds = 120;
        private const int LastExitSeconds = 300;
        private const CardKind SafeCard = CardKind.Safe;
        private const CardKind FatalCatCard = CardKind.FatalCat;

        private static readonly DateTime _startUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime _firstDisconnectionUtc = _startUtc.AddSeconds(FirstDisconnectionSeconds);
        private static readonly DateTime _firstGraceEndUtc = _firstDisconnectionUtc.AddSeconds(GraceSeconds);
        private static readonly DateTime _secondDisconnectionUtc = _startUtc.AddSeconds(SecondDisconnectionSeconds);
        private static readonly int[] _noGuests = new int[0];
        private static readonly Func<int, int> _pickFirstCard = cardCount => FirstCardIndex;

        /// <summary>Verifica CalculateFromMatch_ScenarioE61AfterBetLeaderLeft_PutsHimLastWithZero.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE61AfterBetLeaderLeft_PutsHimLastWithZero()
        {
            // Arrange: E6-1; B sale con el reto listo y después A gana la partida con 2 retos.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.PlaceCard(PlayerAId, SafeCard);
            PlaceSafeCardsUntilTurnOf(match, PlayerAId);
            match.OpenBet(PlayerAId, TwoCardBet);
            match.RaiseBet(PlayerBId, ThreeCardBet);
            match.WithdrawFromBet(PlayerCId);
            match.WithdrawFromBet(PlayerDId);
            match.WithdrawFromBet(PlayerAId);
            LeaveNow(match, PlayerBId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerCId, PlayerDId, PlayerBId }, new[] { 3, 1, 0, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_ScenarioE63AfterFirstPlayerLeft_PutsHimFourthWithZero.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE63AfterFirstPlayerLeft_PutsHimFourthWithZero()
        {
            // Arrange: E6-3; todos colocan, A sale en su turno y después B gana la partida con 2 retos.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.PlaceCard(PlayerAId, SafeCard);
            PlaceSafeCardsUntilTurnOf(match, PlayerAId);
            LeaveNow(match, PlayerAId);
            WinChallenges(match, PlayerBId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerBId, PlayerCId, PlayerDId, PlayerAId }, new[] { 3, 1, 0, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_ScenarioE64AfterTwoLeaves_Pays4And0And0.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE64AfterTwoLeaves_Pays4And0And0()
        {
            // Arrange: E6-4; modalidad de 6; B sale y luego C sale: A gana con base mínima 2 y bono +2.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_startUtc), _pickFirstCard);
            match.PlaceCard(PlayerAId, SafeCard);
            match.RemovePlayer(PlayerCId, PlayerExit.Immediate(_firstDisconnectionUtc), _pickFirstCard);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerCId, PlayerBId }, new[] { 4, 0, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_ScenarioE55WithTwoExits_RanksLaterExitHigher.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE55WithTwoExits_RanksLaterExitHigher()
        {
            // Arrange: E5-5; D sale voluntariamente y después E es expulsado al vencer su gracia.
            PlayerExit exitOfD = PlayerExit.Immediate(_startUtc);
            PlayerExit exitOfE = new PlayerExit(_firstDisconnectionUtc, _firstGraceEndUtc);

            // Act
            FinalStandings finalStandings = FinishAfterExitsOfDAndE(exitOfD, exitOfE);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerEId, PlayerDId },
                new[] { 3, 1, 0, 0, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_ScenarioE55bSimultaneousDisconnections_RanksByTurnOrder.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE55bSimultaneousDisconnections_RanksByTurnOrder()
        {
            // Arrange: E5-5b; D y E pierden la conexión a la vez y ambos vencen a los 15 s.
            PlayerExit exitOfD = new PlayerExit(_firstDisconnectionUtc, _firstGraceEndUtc);
            PlayerExit exitOfE = new PlayerExit(_firstDisconnectionUtc, _firstGraceEndUtc);

            // Act
            FinalStandings finalStandings = FinishAfterExitsOfDAndE(exitOfD, exitOfE);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId },
                new[] { 3, 1, 0, 0, 0 });
        }

        /// <summary>
        /// Verifica CalculateFromMatch_ScenarioE55bDisconnectionsAtDifferentTimes_RanksLaterExitHigher.
        /// </summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE55bDisconnectionsAtDifferentTimes_RanksLaterExitHigher()
        {
            // Arrange: E5-5b con fechas distintas; la desconexión de E empieza y vence después que la de D.
            PlayerExit exitOfD = new PlayerExit(_firstDisconnectionUtc, _firstGraceEndUtc);
            PlayerExit exitOfE = new PlayerExit(_secondDisconnectionUtc,
                _secondDisconnectionUtc.AddSeconds(GraceSeconds));

            // Act
            FinalStandings finalStandings = FinishAfterExitsOfDAndE(exitOfD, exitOfE);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerEId, PlayerDId },
                new[] { 3, 1, 0, 0, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_ScenarioE55bVoluntaryExitAtGraceEnd_RanksAboveDisconnection.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE55bVoluntaryExitAtGraceEnd_RanksAboveDisconnection()
        {
            // Arrange: E5-5b; la gracia de D vence en el mismo instante en que E sale voluntariamente.
            PlayerExit exitOfD = new PlayerExit(_firstDisconnectionUtc, _firstGraceEndUtc);
            PlayerExit exitOfE = PlayerExit.Immediate(_firstGraceEndUtc);

            // Act
            FinalStandings finalStandings = FinishAfterExitsOfDAndE(exitOfD, exitOfE);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerEId, PlayerDId },
                new[] { 3, 1, 0, 0, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_ScenarioE55cEliminatedPlayerLeft_KeepsPositionAndPoints.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE55cEliminatedPlayerLeft_KeepsPositionAndPoints()
        {
            // Arrange: E5-5c en modalidad de 6; C queda eliminado, sale después y A gana: C cuenta para la base.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            EliminateOnOwnCat(match, PlayerCId);
            LeaveNow(match, PlayerCId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerDId, PlayerCId }, new[] { 5, 3, 2, 1 });
        }

        /// <summary>Verifica CalculateFromMatch_ScenarioE56WithOneCountedPlayer_Pays5WithMinimumBase.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE56WithOneCountedPlayer_Pays5WithMinimumBase()
        {
            // Arrange: E5-6; modalidad de 8; B sale, C es expulsado tras su gracia y D sale: A gana con 0 retos.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_startUtc), _pickFirstCard);
            match.RemovePlayer(PlayerCId, new PlayerExit(_firstDisconnectionUtc, _firstGraceEndUtc), _pickFirstCard);
            match.RemovePlayer(PlayerDId, PlayerExit.Immediate(_startUtc.AddSeconds(LastExitSeconds)), _pickFirstCard);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            Assert.Equal(PlayerAId, match.Winner.PlayerId);
            Assert.Equal(NoChallenges, match.Winner.SuccessfulChallengeCount);
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerDId, PlayerCId, PlayerBId }, new[] { 5, 0, 0, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_DomainScenario8WithOneLeaver_Pays5And3And2And1And0.</summary>
        [Fact]
        public void CalculateFromMatch_DomainScenario8WithOneLeaver_Pays5And3And2And1And0()
        {
            // Arrange: Dominio escenario 8; 5 jugadores en modalidad de 6; P5 abandona a mitad de partida y gana P1.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId);
            WinChallenge(match, PlayerAId);
            LeaveNow(match, PlayerEId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, _noGuests);

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId },
                new[] { 5, 3, 2, 1, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_GuestWhoLeft_IsNotCountedForBase.</summary>
        [Fact]
        public void CalculateFromMatch_GuestWhoLeft_IsNotCountedForBase()
        {
            // Arrange: B es invitado y sale; quedan 4 contados (base 3).
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId);
            LeaveNow(match, PlayerBId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, new[] { PlayerBId });

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerCId, PlayerDId, PlayerEId, PlayerBId },
                new[] { 4, 2, 1, 0, 0 });
        }

        /// <summary>Verifica CalculateFromMatch_GuestWhoStays_IsCountedForBase.</summary>
        [Fact]
        public void CalculateFromMatch_GuestWhoStays_IsCountedForBase()
        {
            // Arrange: C es invitado y se queda, E sale; quedan 4 contados con C (base 3, no 2).
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId);
            LeaveNow(match, PlayerEId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, new[] { PlayerCId });

            // Assert
            AssertStandings(finalStandings, new[] { PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId },
                new[] { 4, 2, 0, 0, 0 });
        }

        /// <summary>
        /// Cinco jugadores en modalidad de 4: salen D y después E, B gana 1 reto y A gana con 2; C queda con 0.
        /// </summary>
        private static FinalStandings FinishAfterExitsOfDAndE(PlayerExit exitOfD, PlayerExit exitOfE)
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId, PlayerEId);
            match.RemovePlayer(PlayerDId, exitOfD, _pickFirstCard);
            match.RemovePlayer(PlayerEId, exitOfE, _pickFirstCard);
            WinChallenge(match, PlayerBId);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);
            Assert.Equal(OneChallenge, GetPlayer(match, PlayerBId).SuccessfulChallengeCount);

            return FinalStandings.CalculateFromMatch(match, _noGuests);
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
            match.OpenBet(match.PlayerInTurn.PlayerId, betValue);
            int maximumWithdrawals = match.Players.Count;

            for (int withdrawal = 0; withdrawal < maximumWithdrawals && match.Phase == RoundPhase.Betting;
                withdrawal++)
            {
                int blockingPlayerId = match.PlayerInTurn.PlayerId;
                BetActionStatus status = match.WithdrawFromBet(blockingPlayerId);
                Assert.True(status == BetActionStatus.Accepted,
                    $"El jugador {blockingPlayerId} no pudo retirarse en la ronda {match.RoundNumber}: {status}.");
            }

            Assert.True(match.Phase != RoundPhase.Betting,
                $"La apuesta no se cerró en la ronda {match.RoundNumber}; turno de {match.PlayerInTurn.PlayerId}.");
        }

        /// <summary>
        /// Los jugadores en turno colocan una segura hasta el turno del jugador indicado. Una vuelta completa basta; si
        /// una colocación se rechaza o se agota la vuelta, falla con el jugador y la ronda que bloquearon el avance.
        /// </summary>
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
            match.RevealCard(playerId, playerId, _startUtc);
        }

        private static void WinChallenge(MatchState match, int playerId)
        {
            BetOneOnOwnTopCard(match, playerId, SafeCard);
        }

        private static void WinChallenges(MatchState match, int playerId, int challengeCount)
        {
            for (int attempt = 0; attempt < challengeCount
                && GetPlayer(match, playerId).SuccessfulChallengeCount < challengeCount; attempt++)
            {
                WinChallenge(match, playerId);
            }

            Assert.True(GetPlayer(match, playerId).SuccessfulChallengeCount >= challengeCount,
                $"El jugador {playerId} no llegó a {challengeCount} retos en la ronda {match.RoundNumber}.");
        }

        /// <summary>Falla sobre su propio Gato y pierde una segura; si ya no tiene, la primera carta.</summary>
        private static void LoseCardOnOwnCat(MatchState match, int playerId)
        {
            BetOneOnOwnTopCard(match, playerId, FatalCatCard);
            int safeCardIndex = GetPlayer(match, playerId).HandCards.ToList().IndexOf(SafeCard);
            match.ChoosePunishmentCard(playerId, Math.Max(safeCardIndex, FirstCardIndex), _startUtc);
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

        private static PlayerExitStatus LeaveNow(MatchState match, int playerId)
        {
            return match.RemovePlayer(playerId, PlayerExit.Immediate(_startUtc), _pickFirstCard);
        }
    }
}
