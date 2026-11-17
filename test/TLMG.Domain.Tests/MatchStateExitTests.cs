using System;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Pruebas de la salida de jugadores en MatchState (Reglas 4.0.6, 2.0.2 y No. 9b): reinicio de la ronda en curso,
    /// contador de rondas, victoria del último en juego, rechazos sin cambio de estado y acciones de quien salió. Los
    /// números de ronda de los escenarios son narrativa: se afirma "ronda anterior + 1".
    /// </summary>
    public class MatchStateExitTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int EightCardsPerPlayer = 8;
        private const int PlayerAId = 1;
        private const int PlayerBId = 2;
        private const int PlayerCId = 3;
        private const int PlayerDId = 4;
        private const int UnknownPlayerId = 99;
        private const int OneCardBet = 1;
        private const int TwoCardBet = 2;
        private const int ThreeCardBet = 3;
        private const int FirstCardIndex = 0;
        private const int FirstRoundNumber = 1;
        private const int OneRound = 1;
        private const int FourLives = 4;
        private const int EightLives = 8;
        private const int NoChallenges = 0;
        private const int NoBet = 0;
        private const int NoCardsOnTable = 0;
        private const CardKind SafeCard = CardKind.Safe;
        private const CardKind FatalCatCard = CardKind.FatalCat;

        private static readonly DateTime _startUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Func<int, int> _pickFirstCard = cardCount => FirstCardIndex;

        /// <summary>Verifica RemovePlayer_ScenarioE61BetLeaderLeavesBeforeRevealing_RestartsRoundOpenedByA.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE61BetLeaderLeavesBeforeRevealing_RestartsRoundOpenedByA()
        {
            // Arrange: E6-1; A abre 2, B sube a 3, C, D y A se retiran: B queda con el reto listo.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            PlaceAllInTurnOrder(match);
            match.OpenBet(PlayerAId, TwoCardBet);
            match.RaiseBet(PlayerBId, ThreeCardBet);
            match.WithdrawFromBet(PlayerCId);
            match.WithdrawFromBet(PlayerDId);
            match.WithdrawFromBet(PlayerAId);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerBId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            AssertRestartedRound(match, PlayerAId);
            AssertFullHandsWithoutChallenges(match, PlayerAId, PlayerCId, PlayerDId);
        }

        /// <summary>Verifica RemovePlayer_ScenarioE61VariantDuringPlacement_RestartsRoundOpenedByA.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE61VariantDuringPlacement_RestartsRoundOpenedByA()
        {
            // Arrange: la variante de E6-1: B se va durante la colocación, sin apuesta abierta.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.PlaceCard(PlayerAId, SafeCard);
            match.PlaceCard(PlayerBId, SafeCard);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerBId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            AssertRestartedRound(match, PlayerAId);
            AssertFullHandsWithoutChallenges(match, PlayerAId, PlayerCId, PlayerDId);
        }

        /// <summary>Verifica RemovePlayer_ScenarioE63FirstPlayerLeavesInTurn_RestartsRoundOpenedByB.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE63FirstPlayerLeavesInTurn_RestartsRoundOpenedByB()
        {
            // Arrange: E6-3; A, B, C y D colocan una segura y A sale en su turno.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            PlaceAllInTurnOrder(match);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerAId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            AssertRestartedRound(match, PlayerBId);
            AssertFullHandsWithoutChallenges(match, PlayerBId, PlayerCId, PlayerDId);
            Assert.All(new[] { PlayerBId, PlayerCId, PlayerDId },
                playerId => Assert.Equal(FourLives, GetPlayer(match, playerId).HandCards.Count));
        }

        /// <summary>Verifica PlaceCard_AfterFirstPlayerLeft_SkipsHimInTurnOrder.</summary>
        [Fact]
        public void PlaceCard_AfterFirstPlayerLeft_SkipsHimInTurnOrder()
        {
            // Arrange: E6-3 continúa; el orden queda B, C, D.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            PlaceAllInTurnOrder(match);
            LeaveNow(match, PlayerAId);
            match.PlaceCard(PlayerBId, SafeCard);
            match.PlaceCard(PlayerCId, SafeCard);

            // Act
            match.PlaceCard(PlayerDId, SafeCard);

            // Assert
            Assert.Equal(PlayerBId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica WithdrawFromBet_AfterFirstPlayerLeft_SkipsHimAmongBettors.</summary>
        [Fact]
        public void WithdrawFromBet_AfterFirstPlayerLeft_SkipsHimAmongBettors()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerAId);
            PlaceAllInTurnOrder(match);
            match.OpenBet(PlayerBId, OneCardBet);
            match.WithdrawFromBet(PlayerCId);

            // Act
            BetActionStatus status = match.WithdrawFromBet(PlayerDId);

            // Assert
            Assert.Equal(BetActionStatus.Accepted, status);
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
            Assert.Equal(PlayerBId, match.BetLeader.PlayerId);
        }

        /// <summary>Verifica RemovePlayer_DuringHalfRevealedChallenge_DiscardsRevealedCardsAndRestarts.</summary>
        [Fact]
        public void RemovePlayer_DuringHalfRevealedChallenge_DiscardsRevealedCardsAndRestarts()
        {
            // Arrange: A apuesta 2 y voltea su carta; falta una cuando C sale.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            PlaceAllInTurnOrder(match);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _startUtc);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerCId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            Assert.Null(match.Resolution);
            Assert.All(match.Players, player => Assert.Equal(NoCardsOnTable, player.RevealedCardCount));
            AssertRestartedRound(match, PlayerAId);
        }

        /// <summary>Verifica RemovePlayer_AtRoundStartWithEmptyTable_RestartsAndCountsOneRound.</summary>
        [Fact]
        public void RemovePlayer_AtRoundStartWithEmptyTable_RestartsAndCountsOneRound()
        {
            // Arrange: H-17; la ronda está activa aunque nadie haya colocado.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerDId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.Equal(FirstRoundNumber + OneRound, match.RoundNumber);
            AssertRestartedRound(match, PlayerAId);
        }

        /// <summary>Verifica RemovePlayer_ScenarioE64SecondLeaveLeavesOnePlayer_FinishesWithoutNewRound.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE64SecondLeaveLeavesOnePlayer_FinishesWithoutNewRound()
        {
            // Arrange: E6-4; B sale y la ronda se reinicia; durante esa ronda A coloca y C sale.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LeaveNow(match, PlayerBId);
            match.PlaceCard(PlayerAId, SafeCard);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerCId);

            // Assert
            Assert.Equal(PlayerExitStatus.MatchFinished, status);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
            Assert.Equal(PlayerAId, match.Winner.PlayerId);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
        }

        /// <summary>Verifica RemovePlayer_LastLeaveWithOpenBet_FinishesWithCleanRoundState.</summary>
        [Fact]
        public void RemovePlayer_LastLeaveWithOpenBet_FinishesWithCleanRoundState()
        {
            // Arrange: F-01; C sale, A y B colocan, A abre una apuesta y B sale con la apuesta activa.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LeaveNow(match, PlayerCId);
            PlaceAllInTurnOrder(match);
            match.OpenBet(PlayerAId, TwoCardBet);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerBId);

            // Assert
            Assert.Equal(PlayerExitStatus.MatchFinished, status);
            Assert.Equal(PlayerAId, match.Winner.PlayerId);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
            Assert.Equal(NoCardsOnTable, match.CardsOnTable);
            Assert.All(match.Players, player => Assert.Equal(FourLives, player.HandCards.Count));
            Assert.Equal(NoBet, match.CurrentBet);
            Assert.Null(match.BetLeader);
            Assert.All(match.Players, player => Assert.False(player.HasWithdrawnFromBet));
            Assert.Null(match.Resolution);
        }

        /// <summary>Verifica Actions_AfterMatchFinishedByLeave_AreRejectedWithExistingStatuses.</summary>
        [Fact]
        public void Actions_AfterMatchFinishedByLeave_AreRejectedWithExistingStatuses()
        {
            // Arrange: la partida terminó por salidas; PlayerInTurn ya no tiene significado.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LeaveNow(match, PlayerCId);
            Assert.Equal(PlayerExitStatus.MatchFinished, LeaveNow(match, PlayerBId));
            string matchBeforeActions = DescribeMatch(match);

            // Act
            CardPlacementStatus placementStatus = match.PlaceCard(PlayerAId, SafeCard);
            BetActionStatus openStatus = match.OpenBet(PlayerAId, OneCardBet);
            BetActionStatus withdrawStatus = match.WithdrawFromBet(PlayerAId);
            ChallengeRevealStatus revealStatus = match.RevealCard(PlayerAId, PlayerAId, _startUtc);
            PunishmentStatus punishmentStatus = match.ChoosePunishmentCard(PlayerAId, FirstCardIndex, _startUtc);

            // Assert
            Assert.Equal(CardPlacementStatus.MatchFinished, placementStatus);
            Assert.Equal(BetActionStatus.MatchFinished, openStatus);
            Assert.Equal(BetActionStatus.MatchFinished, withdrawStatus);
            Assert.Equal(ChallengeRevealStatus.NoChallengeInProgress, revealStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, punishmentStatus);
            Assert.Equal(matchBeforeActions, DescribeMatch(match));
        }

        /// <summary>Verifica RemovePlayer_WhenExtraLifeWasConsumedInRound_KeepsItConsumed.</summary>
        [Fact]
        public void RemovePlayer_WhenExtraLifeWasConsumedInRound_KeepsItConsumed()
        {
            // Arrange: H-15; A con vida extra voltea su propio Gato, la vida extra lo absorbe y luego B sale.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.GrantExtraLife(PlayerAId);
            match.PlaceCard(PlayerAId, FatalCatCard);
            PlaceSafeCardsUntilTurnOf(match, PlayerAId);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _startUtc);

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerBId);

            // Assert
            Assert.Equal(PlayerExitStatus.RoundRestarted, status);
            Assert.False(GetPlayer(match, PlayerAId).HasExtraLife);
            Assert.Equal(EightLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>
        /// Verifica RemovePlayer_ScenarioE55cEliminatedPlayerLeaves_ReturnsEliminatedPlayerLeftWithoutChange.
        /// </summary>
        [Fact]
        public void RemovePlayer_ScenarioE55cEliminatedPlayerLeaves_ReturnsEliminatedPlayerLeftWithoutChange()
        {
            // Arrange: E5-5c; C quedó eliminado y en una ronda posterior, con cartas en la mesa, pulsa Salir.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            EliminateOnOwnCat(match, PlayerCId);
            match.PlaceCard(PlayerAId, SafeCard);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            PlayerExitStatus firstStatus = LeaveNow(match, PlayerCId);
            PlayerExitStatus secondStatus = LeaveNow(match, PlayerCId);

            // Assert
            Assert.Equal(PlayerExitStatus.EliminatedPlayerLeft, firstStatus);
            Assert.Equal(PlayerExitStatus.EliminatedPlayerLeft, secondStatus);
            Assert.False(GetPlayer(match, PlayerCId).HasLeftMatch);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Verifica RemovePlayer_WhenPlayerAlreadyLeft_ReturnsPlayerAlreadyLeftWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_WhenPlayerAlreadyLeft_ReturnsPlayerAlreadyLeftWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerBId);
            match.PlaceCard(PlayerAId, SafeCard);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerBId);

            // Assert
            Assert.Equal(PlayerExitStatus.PlayerAlreadyLeft, status);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Verifica RemovePlayer_WithUnknownPlayer_ReturnsPlayerNotInMatchWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_WithUnknownPlayer_ReturnsPlayerNotInMatchWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.PlaceCard(PlayerAId, SafeCard);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            PlayerExitStatus status = LeaveNow(match, UnknownPlayerId);

            // Assert
            Assert.Equal(PlayerExitStatus.PlayerNotInMatch, status);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Verifica RemovePlayer_AfterMatchFinished_ReturnsMatchAlreadyFinishedWithoutChange.</summary>
        /// <param name="playerId">Jugador que intenta salir: uno en juego, el que ya salió o uno ajeno.</param>
        [Theory]
        [InlineData(PlayerAId)]
        [InlineData(PlayerCId)]
        [InlineData(UnknownPlayerId)]
        public void RemovePlayer_AfterMatchFinished_ReturnsMatchAlreadyFinishedWithoutChange(int playerId)
        {
            // Arrange: B y C salieron y A ganó.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LeaveNow(match, PlayerBId);
            LeaveNow(match, PlayerCId);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            PlayerExitStatus status = LeaveNow(match, playerId);

            // Assert
            Assert.Equal(PlayerExitStatus.MatchAlreadyFinished, status);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Verifica RemovePlayer_WithPendingPunishment_ReturnsNotYetSupportedWithoutChange.</summary>
        /// <param name="playerId">Jugador que intenta salir: el castigado, quien elige o un tercero.</param>
        [Theory]
        [InlineData(PlayerAId)]
        [InlineData(PlayerBId)]
        [InlineData(PlayerCId)]
        public void RemovePlayer_WithPendingPunishment_ReturnsNotYetSupportedWithoutChange(int playerId)
        {
            // Arrange: restricción transitoria hasta E6b; A falla con el Gato de B y B elige a ciegas.
            MatchState match = CreateMatchWithBlindPunishment();
            string matchBeforeExit = DescribeMatch(match);

            // Act
            PlayerExitStatus status = LeaveNow(match, playerId);

            // Assert
            Assert.Equal(PlayerExitStatus.ExitDuringPunishmentNotYetSupported, status);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Verifica RemovePlayer_WithSuspendedPunishment_ReturnsNotYetSupportedWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_WithSuspendedPunishment_ReturnsNotYetSupportedWithoutChange()
        {
            // Arrange: restricción transitoria hasta E6b; el castigo de A está suspendido.
            MatchState match = CreateMatchWithBlindPunishment();
            match.SuspendPunishment(PlayerAId, _startUtc);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            PlayerExitStatus status = LeaveNow(match, PlayerAId);

            // Assert
            Assert.Equal(PlayerExitStatus.ExitDuringPunishmentNotYetSupported, status);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
            Assert.NotNull(match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica RemovePlayer_WithNullExit_ThrowsArgumentNullExceptionWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_WithNullExit_ThrowsArgumentNullExceptionWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            Action removePlayer = () => match.RemovePlayer(PlayerBId, null, _pickFirstCard);

            // Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(removePlayer);
            Assert.Equal("exit", exception.ParamName);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Verifica RemovePlayer_WithNullRandomSelector_ThrowsArgumentNullExceptionWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_WithNullRandomSelector_ThrowsArgumentNullExceptionWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            Action removePlayer = () => match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_startUtc), null);

            // Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(removePlayer);
            Assert.Equal("chooseRandomCardIndex", exception.ParamName);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Verifica ActionsOfPlayerWhoLeft_InPlacementAndBetting_ReturnPlayerNotInMatch.</summary>
        [Fact]
        public void ActionsOfPlayerWhoLeft_InPlacementAndBetting_ReturnPlayerNotInMatch()
        {
            // Arrange: B salió; la ronda reiniciada ya tiene una apuesta abierta por A.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerBId);
            CardPlacementStatus placementStatus = match.PlaceCard(PlayerBId, SafeCard);
            PlaceAllInTurnOrder(match);
            BetActionStatus openStatus = match.OpenBet(PlayerBId, OneCardBet);
            match.OpenBet(PlayerAId, OneCardBet);
            string matchBeforeActions = DescribeMatch(match);

            // Act
            BetActionStatus raiseStatus = match.RaiseBet(PlayerBId, TwoCardBet);
            BetActionStatus withdrawStatus = match.WithdrawFromBet(PlayerBId);

            // Assert
            Assert.Equal(CardPlacementStatus.PlayerNotInMatch, placementStatus);
            Assert.Equal(BetActionStatus.PlayerNotInMatch, openStatus);
            Assert.Equal(BetActionStatus.PlayerNotInMatch, raiseStatus);
            Assert.Equal(BetActionStatus.PlayerNotInMatch, withdrawStatus);
            Assert.Equal(matchBeforeActions, DescribeMatch(match));
        }

        /// <summary>Verifica RevealCard_ByOrOnPlayerWhoLeft_ReturnsNotInMatchStatuses.</summary>
        [Fact]
        public void RevealCard_ByOrOnPlayerWhoLeft_ReturnsNotInMatchStatuses()
        {
            // Arrange: D salió; A apuesta 2 con el reto listo.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerDId);
            PlaceAllInTurnOrder(match);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _startUtc);
            string matchBeforeReveals = DescribeMatch(match);

            // Act
            ChallengeRevealStatus byLeaverStatus = match.RevealCard(PlayerDId, PlayerBId, _startUtc);
            ChallengeRevealStatus onLeaverStatus = match.RevealCard(PlayerAId, PlayerDId, _startUtc);

            // Assert
            Assert.Equal(ChallengeRevealStatus.PlayerNotInMatch, byLeaverStatus);
            Assert.Equal(ChallengeRevealStatus.PileOwnerNotInMatch, onLeaverStatus);
            Assert.Equal(matchBeforeReveals, DescribeMatch(match));
        }

        /// <summary>Verifica PunishmentActions_ByPlayerWhoLeft_ReturnPlayerNotInMatch.</summary>
        [Fact]
        public void PunishmentActions_ByPlayerWhoLeft_ReturnPlayerNotInMatch()
        {
            // Arrange: D salió antes; luego A falla con el Gato de B.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerDId);
            FailOnCatOfB(match);
            string matchBeforeActions = DescribeMatch(match);

            // Act
            PunishmentStatus chooseStatus = match.ChoosePunishmentCard(PlayerDId, FirstCardIndex, _startUtc);
            PunishmentStatus suspendStatus = match.SuspendPunishment(PlayerDId, _startUtc);
            PunishmentStatus resumeStatus = match.ResumePunishment(PlayerDId, _startUtc);

            // Assert
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, chooseStatus);
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, suspendStatus);
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, resumeStatus);
            Assert.Equal(matchBeforeActions, DescribeMatch(match));
        }

        /// <summary>Verifica GrantExtraLife_ToPlayerWhoLeftBeforeAnyCard_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void GrantExtraLife_ToPlayerWhoLeftBeforeAnyCard_ReturnsPlayerNotInMatch()
        {
            // Arrange: modalidad de 8; B sale antes de que alguien coloque.
            MatchState match = CreateMatch(EightCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerBId);

            // Act
            ExtraLifeStatus status = match.GrantExtraLife(PlayerBId);

            // Assert
            Assert.Equal(ExtraLifeStatus.PlayerNotInMatch, status);
            Assert.False(GetPlayer(match, PlayerBId).HasExtraLife);
        }

        /// <summary>Verifica RoundNumber_WhenMatchStarts_IsOne.</summary>
        [Fact]
        public void RoundNumber_WhenMatchStarts_IsOne()
        {
            // Arrange
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer);

            // Act
            MatchState match = new MatchState(modeRules, new[] { PlayerAId, PlayerBId, PlayerCId });

            // Assert
            Assert.Equal(FirstRoundNumber, match.RoundNumber);
        }

        /// <summary>Verifica RevealCard_WhenChallengeSucceeds_CountsOneRound.</summary>
        [Fact]
        public void RevealCard_WhenChallengeSucceeds_CountsOneRound()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            WinChallenge(match, PlayerAId);

            // Assert
            Assert.Equal(FirstRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Verifica ChoosePunishmentCard_AfterFailedChallenge_CountsOneRoundOnly.</summary>
        [Fact]
        public void ChoosePunishmentCard_AfterFailedChallenge_CountsOneRoundOnly()
        {
            // Arrange: el reto fallido deja el castigo pendiente sin contar todavía la ronda.
            MatchState match = CreateMatchWithBlindPunishment();
            int roundNumberWithPunishment = match.RoundNumber;

            // Act
            match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _startUtc);

            // Assert
            Assert.Equal(FirstRoundNumber, roundNumberWithPunishment);
            Assert.Equal(FirstRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Verifica RemovePlayer_FirstPlayerLeavesTwice_CountsOneRoundOnly.</summary>
        [Fact]
        public void RemovePlayer_FirstPlayerLeavesTwice_CountsOneRoundOnly()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            LeaveNow(match, PlayerAId);

            // Act
            LeaveNow(match, PlayerAId);

            // Assert
            Assert.Equal(FirstRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Cada jugador en juego, empezando por quien tiene el turno, coloca una segura.</summary>
        private static void PlaceAllInTurnOrder(MatchState match)
        {
            int firstPlayerId = match.PlayerInTurn.PlayerId;
            match.PlaceCard(firstPlayerId, SafeCard);
            PlaceSafeCardsUntilTurnOf(match, firstPlayerId);
        }

        /// <summary>A coloca una segura, B su Gato y los demás una segura; A apuesta 2 y falla con el de B.</summary>
        private static void FailOnCatOfB(MatchState match)
        {
            match.PlaceCard(PlayerAId, SafeCard);
            match.PlaceCard(PlayerBId, FatalCatCard);
            PlaceSafeCardsUntilTurnOf(match, PlayerAId);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _startUtc);
            match.RevealCard(PlayerAId, PlayerBId, _startUtc);
        }

        private static MatchState CreateMatchWithBlindPunishment()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            FailOnCatOfB(match);

            return match;
        }

        /// <summary>Comprueba la ronda reiniciada: colocando, sin apuesta, mesa vacía y el turno indicado.</summary>
        private static void AssertRestartedRound(MatchState match, int openingPlayerId)
        {
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
            Assert.Equal(NoBet, match.CurrentBet);
            Assert.Null(match.BetLeader);
            Assert.Equal(NoCardsOnTable, match.CardsOnTable);
            Assert.Equal(openingPlayerId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Comprueba que los jugadores indicados tienen sus 4 vidas y ningún reto.</summary>
        private static void AssertFullHandsWithoutChallenges(MatchState match, params int[] playerIds)
        {
            Assert.All(playerIds, playerId => Assert.Equal(FourLives, GetPlayer(match, playerId).Lives));
            Assert.All(playerIds,
                playerId => Assert.Equal(NoChallenges, GetPlayer(match, playerId).SuccessfulChallengeCount));
        }

        /// <summary>Resume en texto el estado observable de la partida para comparar antes y después.</summary>
        private static string DescribeMatch(MatchState match)
        {
            string players = string.Join(";", match.Players.Select(player =>
                $"{player.PlayerId}:{string.Join(",", player.HandCards)}|{string.Join(",", player.PileCards)}"
                + $"|{player.RevealedCardCount}|{player.SuccessfulChallengeCount}|{player.HasLeftMatch}"));
            string round = $"{match.RoundNumber}#{match.Phase}#{match.PlayerInTurn.PlayerId}#{match.CurrentBet}"
                + $"#{match.BetLeader?.PlayerId}#{match.Winner?.PlayerId}#{match.PendingPunishment?.DeadlineUtc}";

            return $"{players}#{round}";
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
