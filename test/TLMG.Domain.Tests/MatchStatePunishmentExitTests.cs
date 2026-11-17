using System;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Pruebas de la salida de jugadores durante un castigo pendiente (Reglas 2.0.3.1 y 4.0.6; H-06, H-07, H-08 y
    /// H-14): cancelación si sale el castigado, azar inmediato si sale quien elige con el castigo activo, registro
    /// sin cambios en los demás casos y reanudación atómica con quien elige ausente.
    /// </summary>
    public class MatchStatePunishmentExitTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int PlayerAId = 1;
        private const int PlayerBId = 2;
        private const int PlayerCId = 3;
        private const int PlayerDId = 4;
        private const int OneCardBet = 1;
        private const int TwoCardBet = 2;
        private const int FirstCardIndex = 0;
        private const int FirstRoundNumber = 1;
        private const int OneRound = 1;
        private const int TwoRounds = 2;
        private const int ThreeLives = 3;
        private const int FourLives = 4;
        private const int NoBet = 0;
        private const int ChooserLeavesAfterSeconds = 3;
        private const int PunishmentChoiceSeconds = 10;
        private const int GraceSeconds = 15;
        private const int ReturnAfterSeconds = 12;
        private const CardKind SafeCard = CardKind.Safe;
        private const CardKind FatalCatCard = CardKind.FatalCat;

        private static readonly DateTime _startUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime _chooserLeavesUtc = _startUtc.AddSeconds(ChooserLeavesAfterSeconds);
        private static readonly DateTime _deadlineUtc = _startUtc.AddSeconds(PunishmentChoiceSeconds);
        private static readonly DateTime _returnUtc = _startUtc.AddSeconds(ReturnAfterSeconds);
        private static readonly TimeSpan _remainingAtSuspension =
            TimeSpan.FromSeconds(PunishmentChoiceSeconds - ChooserLeavesAfterSeconds);
        private static readonly Func<int, int> _pickFirstCard = cardCount => FirstCardIndex;
        private static readonly Func<int, int> _pickInvalidCard = cardCount => cardCount;
        private static readonly Func<int, int> _failIfRandomIsUsed =
            cardCount => throw new InvalidOperationException("No se debía usar el azar.");

        /// <summary>Verifica RemovePlayer_ScenarioE41ChooserLeavesActivePunishment_ResolvesAtRandomOnce.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE41ChooserLeavesActivePunishment_ResolvesAtRandomOnce()
        {
            // Arrange: E4-1; A falla con el Gato de B y, a los 3 s de la elección, B pulsa Salir.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc),
                _pickFirstCard);

            // Assert
            Assert.Equal(PlayerExitStatus.PunishmentResolvedAtRandom, status);
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(FourLives, GetPlayer(match, PlayerCId).Lives);
            Assert.True(GetPlayer(match, PlayerBId).HasLeftMatch);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            Assert.Equal(PlayerAId, match.PlayerInTurn.PlayerId);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
        }

        /// <summary>Verifica CalculateFromMatch_ScenarioE41AfterChooserLeft_PutsHimLastWithZero.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE41AfterChooserLeft_PutsHimLastWithZero()
        {
            // Arrange: E4-1 continúa; A gana la partida con 2 retos y B queda último.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc), _pickFirstCard);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, new int[0]);

            // Assert
            Assert.Equal(new[] { PlayerAId, PlayerCId, PlayerBId },
                finalStandings.Standings.Select(standing => standing.PlayerId));
            Assert.Equal(new[] { 3, 1, 0 }, finalStandings.Standings.Select(standing => standing.Points));
        }

        /// <summary>Verifica RemovePlayer_ScenarioE41bChooserExpelledAfterExpiration_RestartsStartedRound.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE41bChooserExpelledAfterExpiration_RestartsStartedRound()
        {
            // Arrange: E4-1b; B se desconecta a los 3 s, su plazo sigue, vence a los 10 s y lo expulsan a los 18 s.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            int previousRoundNumber = match.RoundNumber;
            PunishmentStatus suspendStatus = match.SuspendPunishment(PlayerBId, _chooserLeavesUtc);
            PunishmentStatus expireStatus = match.ExpirePunishment(_deadlineUtc, _pickFirstCard);
            PlayerExit exitOfB = new PlayerExit(_chooserLeavesUtc, _chooserLeavesUtc.AddSeconds(GraceSeconds));

            // Act
            PlayerExitStatus exitStatus = match.RemovePlayer(PlayerBId, exitOfB, _pickFirstCard);

            // Assert
            Assert.Equal(PunishmentStatus.NotThePunished, suspendStatus);
            Assert.Equal(PunishmentStatus.CardRemoved, expireStatus);
            Assert.Equal(PlayerExitStatus.RoundRestarted, exitStatus);
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(FourLives, GetPlayer(match, PlayerCId).Lives);
            Assert.Equal(previousRoundNumber + TwoRounds, match.RoundNumber);
        }

        /// <summary>
        /// Verifica ExpirePunishment_AfterChooserLeftFirst_ReturnsNoPunishmentPendingWithoutSecondResolution.
        /// </summary>
        [Fact]
        public void ExpirePunishment_AfterChooserLeftFirst_ReturnsNoPunishmentPendingWithoutSecondResolution()
        {
            // Arrange: orden alterno de E4-1b; la expulsión de B llega antes que el vencimiento.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            int previousRoundNumber = match.RoundNumber;
            match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc), _pickFirstCard);
            string matchAfterExit = DescribeMatch(match);

            // Act
            PunishmentStatus status = match.ExpirePunishment(_deadlineUtc, _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Equal(matchAfterExit, DescribeMatch(match));
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Verifica RemovePlayer_ScenarioE62PunishedLeavesActivePunishment_CancelsWithoutLosingLife.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE62PunishedLeavesActivePunishment_CancelsWithoutLosingLife()
        {
            // Arrange: E6-2 caso 2; A pulsa Salir mientras B elige.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerAId, PlayerExit.Immediate(_chooserLeavesUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PunishmentCancelled, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(FourLives, GetPlayer(match, PlayerBId).Lives);
            Assert.Equal(FourLives, GetPlayer(match, PlayerCId).Lives);
            Assert.Null(match.PendingPunishment);
            Assert.Null(match.LastPunishment);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            Assert.Equal(PlayerBId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica RemovePlayer_ScenarioE62PunishedExpelledWhileSuspended_CancelsWithoutLosingLife.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE62PunishedExpelledWhileSuspended_CancelsWithoutLosingLife()
        {
            // Arrange: E6-2 punto 5; A se desconecta, el castigo se suspende y A no regresa en 15 s.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            int previousRoundNumber = match.RoundNumber;
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);
            PlayerExit exitOfA = new PlayerExit(_chooserLeavesUtc, _chooserLeavesUtc.AddSeconds(GraceSeconds));

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerAId, exitOfA, _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PunishmentCancelled, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            Assert.Equal(PlayerBId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica RemovePlayer_ChooserLeavesSuspendedPunishment_RecordsExitOnly.</summary>
        [Fact]
        public void RemovePlayer_ChooserLeavesSuspendedPunishment_RecordsExitOnly()
        {
            // Arrange: el castigo de A está suspendido cuando B, quien elige, sale.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            int previousRoundNumber = match.RoundNumber;
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.ExitRecorded, status);
            Assert.True(GetPlayer(match, PlayerBId).HasLeftMatch);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(_remainingAtSuspension, match.PendingPunishment.RemainingTime);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
        }

        /// <summary>Verifica ResumePunishment_AfterChooserLeftWhileSuspended_ResolvesAtRandomInSameOperation.</summary>
        [Fact]
        public void ResumePunishment_AfterChooserLeftWhileSuspended_ResolvesAtRandomInSameOperation()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            int previousRoundNumber = match.RoundNumber;
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);
            match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc), _pickFirstCard);

            // Act
            PunishmentStatus status = match.ResumePunishment(PlayerAId, _returnUtc, _pickFirstCard);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
        }

        /// <summary>
        /// Verifica RemovePlayer_PunishedLeavesAfterChooserLeftWhileSuspended_CancelsWithoutLosingLife.
        /// </summary>
        [Fact]
        public void RemovePlayer_PunishedLeavesAfterChooserLeftWhileSuspended_CancelsWithoutLosingLife()
        {
            // Arrange: A se desconecta y su castigo se suspende, B sale y A no regresa en los 15 s de gracia.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            int previousRoundNumber = match.RoundNumber;
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);
            match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc), _pickFirstCard);
            PlayerExit exitOfA = new PlayerExit(_chooserLeavesUtc, _chooserLeavesUtc.AddSeconds(GraceSeconds));

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerAId, exitOfA, _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PunishmentCancelled, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Verifica ResumePunishment_WithChooserPresent_KeepsExactRemainingTimeWithoutRandom.</summary>
        [Fact]
        public void ResumePunishment_WithChooserPresent_KeepsExactRemainingTimeWithoutRandom()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);

            // Act
            PunishmentStatus status = match.ResumePunishment(PlayerAId, _returnUtc, _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PunishmentStatus.Resumed, status);
            Assert.Equal(_returnUtc + _remainingAtSuspension, match.PendingPunishment.DeadlineUtc);
            Assert.Null(match.PendingPunishment.RemainingTime);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica ResumePunishment_WithChooserAbsentAndInvalidRandomIndex_ThrowsWithoutChange.</summary>
        [Fact]
        public void ResumePunishment_WithChooserAbsentAndInvalidRandomIndex_ThrowsWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);
            match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc), _pickFirstCard);
            string matchBeforeResume = DescribeMatch(match);

            // Act
            Action resumePunishment = () => match.ResumePunishment(PlayerAId, _returnUtc, _pickInvalidCard);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(resumePunishment);
            Assert.Equal(matchBeforeResume, DescribeMatch(match));
            Assert.Equal(_remainingAtSuspension, match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica RemovePlayer_ChooserLeavesWithInvalidRandomIndex_ThrowsWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_ChooserLeavesWithInvalidRandomIndex_ThrowsWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            Action removePlayer = () =>
                match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc), _pickInvalidCard);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(removePlayer);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Verifica ResumePunishment_WithNullRandom_ThrowsArgumentNullExceptionWithoutChange.</summary>
        [Fact]
        public void ResumePunishment_WithNullRandom_ThrowsArgumentNullExceptionWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);
            string matchBeforeResume = DescribeMatch(match);

            // Act
            Action resumePunishment = () => match.ResumePunishment(PlayerAId, _returnUtc, null);

            // Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(resumePunishment);
            Assert.Equal("chooseRandomCardIndex", exception.ParamName);
            Assert.Equal(matchBeforeResume, DescribeMatch(match));
        }

        /// <summary>
        /// Verifica RemovePlayer_WithNullRandomDuringPunishment_ThrowsArgumentNullExceptionWithoutChange.
        /// </summary>
        [Fact]
        public void RemovePlayer_WithNullRandomDuringPunishment_ThrowsArgumentNullExceptionWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            Action removePlayer = () => match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc), null);

            // Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(removePlayer);
            Assert.Equal("chooseRandomCardIndex", exception.ParamName);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Verifica SuspendAndResumePunishment_OnOwnCat_ResumesWithRemainingTimeWithoutRandom.</summary>
        [Fact]
        public void SuspendAndResumePunishment_OnOwnCat_ResumesWithRemainingTimeWithoutRandom()
        {
            // Arrange: H-14; A elige y es castigado a la vez.
            MatchState match = CreateMatchWithOwnPunishment();
            PunishmentStatus suspendStatus = match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);

            // Act
            PunishmentStatus resumeStatus = match.ResumePunishment(PlayerAId, _returnUtc, _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PunishmentStatus.Suspended, suspendStatus);
            Assert.Equal(PunishmentStatus.Resumed, resumeStatus);
            Assert.Equal(_returnUtc + _remainingAtSuspension, match.PendingPunishment.DeadlineUtc);
        }

        /// <summary>Verifica RemovePlayer_OnOwnCat_CancelsBecausePunishedConditionPrevails.</summary>
        [Fact]
        public void RemovePlayer_OnOwnCat_CancelsBecausePunishedConditionPrevails()
        {
            // Arrange: H-14; A, castigado y quien elige, sale.
            MatchState match = CreateMatchWithOwnPunishment();
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerAId, PlayerExit.Immediate(_chooserLeavesUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PunishmentCancelled, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Verifica RemovePlayer_ThirdPlayerLeavesActivePunishment_RecordsExitOnly.</summary>
        [Fact]
        public void RemovePlayer_ThirdPlayerLeavesActivePunishment_RecordsExitOnly()
        {
            // Arrange: H-07; C no participa en el castigo de A.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            int previousRoundNumber = match.RoundNumber;
            DateTime? deadlineUtc = match.PendingPunishment.DeadlineUtc;

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerCId, PlayerExit.Immediate(_chooserLeavesUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.ExitRecorded, status);
            Assert.Equal(deadlineUtc, match.PendingPunishment.DeadlineUtc);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
            Assert.Equal(PunishmentStatus.CardRemoved,
                match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _chooserLeavesUtc));
        }

        /// <summary>Verifica RemovePlayer_ThirdPlayerLeavesSuspendedPunishment_RecordsExitOnly.</summary>
        [Fact]
        public void RemovePlayer_ThirdPlayerLeavesSuspendedPunishment_RecordsExitOnly()
        {
            // Arrange: H-07 con el castigo suspendido.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            int previousRoundNumber = match.RoundNumber;
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerCId, PlayerExit.Immediate(_chooserLeavesUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.ExitRecorded, status);
            Assert.Equal(_remainingAtSuspension, match.PendingPunishment.RemainingTime);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
        }

        /// <summary>Verifica RemovePlayer_LeaveDuringPunishmentLeavesOnePlayer_FinishesWithCleanState.</summary>
        /// <param name="leavingPlayerId">Quien sale: el castigado (A) o quien elige (B).</param>
        /// <param name="winnerId">Quien queda como único jugador en juego.</param>
        [Theory]
        [InlineData(PlayerAId, PlayerBId)]
        [InlineData(PlayerBId, PlayerAId)]
        public void RemovePlayer_LeaveDuringPunishmentLeavesOnePlayer_FinishesWithCleanState(int leavingPlayerId,
            int winnerId)
        {
            // Arrange: C ya salió; A falla con el Gato de B.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LeaveNow(match, PlayerCId);
            FailOnCatOfB(match);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = match.RemovePlayer(leavingPlayerId, PlayerExit.Immediate(_chooserLeavesUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.MatchFinished, status);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
            Assert.Equal(winnerId, match.Winner.PlayerId);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(NoBet, match.CurrentBet);
            Assert.Null(match.BetLeader);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
        }

        /// <summary>Verifica RemovePlayer_SameChooserLeavesTwice_ReturnsPlayerAlreadyLeftWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_SameChooserLeavesTwice_ReturnsPlayerAlreadyLeftWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc), _pickFirstCard);
            string matchAfterExit = DescribeMatch(match);

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_returnUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PlayerAlreadyLeft, status);
            Assert.Equal(matchAfterExit, DescribeMatch(match));
        }

        /// <summary>Verifica PunishmentActions_AfterCancellation_ReturnExistingStatusesWithoutChange.</summary>
        [Fact]
        public void PunishmentActions_AfterCancellation_ReturnExistingStatusesWithoutChange()
        {
            // Arrange: A salió y su castigo se canceló.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            match.RemovePlayer(PlayerAId, PlayerExit.Immediate(_chooserLeavesUtc), _failIfRandomIsUsed);
            string matchAfterExit = DescribeMatch(match);

            // Act
            PunishmentStatus resumeByPunishedStatus =
                match.ResumePunishment(PlayerAId, _returnUtc, _failIfRandomIsUsed);
            PunishmentStatus resumeStatus = match.ResumePunishment(PlayerCId, _returnUtc, _failIfRandomIsUsed);
            PunishmentStatus chooseStatus = match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _returnUtc);
            PunishmentStatus expireStatus = match.ExpirePunishment(_returnUtc, _failIfRandomIsUsed);
            PlayerExitStatus repeatedExitStatus = match.RemovePlayer(PlayerAId, PlayerExit.Immediate(_returnUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PlayerAlreadyLeft, repeatedExitStatus);
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, resumeByPunishedStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, resumeStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, chooseStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, expireStatus);
            Assert.Equal(matchAfterExit, DescribeMatch(match));
        }

        /// <summary>Verifica PunishmentActions_AfterRandomResolution_ReturnExistingStatusesWithoutChange.</summary>
        [Fact]
        public void PunishmentActions_AfterRandomResolution_ReturnExistingStatusesWithoutChange()
        {
            // Arrange: B salió y el castigo de A se resolvió al azar.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_chooserLeavesUtc), _pickFirstCard);
            string matchAfterExit = DescribeMatch(match);

            // Act
            PunishmentStatus resumeStatus = match.ResumePunishment(PlayerAId, _returnUtc, _failIfRandomIsUsed);
            PunishmentStatus chooseStatus = match.ChoosePunishmentCard(PlayerAId, FirstCardIndex, _returnUtc);
            PunishmentStatus expireStatus = match.ExpirePunishment(_returnUtc, _failIfRandomIsUsed);
            PlayerExitStatus repeatedExitStatus = match.RemovePlayer(PlayerBId, PlayerExit.Immediate(_returnUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, expireStatus);
            Assert.Equal(PlayerExitStatus.PlayerAlreadyLeft, repeatedExitStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, resumeStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, chooseStatus);
            Assert.Equal(matchAfterExit, DescribeMatch(match));
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

        /// <summary>Modalidad de 4 con el orden indicado (A y B primero); B elige a ciegas entre las de A.</summary>
        private static MatchState CreateMatchWithBlindPunishment(params int[] playerIdsInOrder)
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, playerIdsInOrder);
            FailOnCatOfB(match);

            return match;
        }

        /// <summary>Modalidad de 4, orden A, B, C, D; A apuesta 1 sobre su Gato y elige viendo sus cartas.</summary>
        private static MatchState CreateMatchWithOwnPunishment()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            BetOneOnOwnTopCard(match, PlayerAId, FatalCatCard);

            return match;
        }

        /// <summary>Resume en texto el estado observable de la partida para comparar antes y después.</summary>
        private static string DescribeMatch(MatchState match)
        {
            string players = string.Join(";", match.Players.Select(player =>
                $"{player.PlayerId}:{string.Join(",", player.HandCards)}|{string.Join(",", player.PileCards)}"
                + $"|{player.SuccessfulChallengeCount}|{player.HasLeftMatch}"));
            string round = $"{match.RoundNumber}#{match.Phase}#{match.CurrentBet}#{match.Winner?.PlayerId}"
                + $"#{match.PendingPunishment?.DeadlineUtc}#{match.PendingPunishment?.RemainingTime}";
            string details = $"{match.PlayerInTurn.PlayerId}#{match.BetLeader?.PlayerId}"
                + $"#{match.LastPunishment?.PunishedPlayerId}#{match.LastPunishment?.RemovedCard}"
                + $"#{match.Resolution?.CountedCardCount}#{match.Resolution?.LastRevealedPlayerId}";

            return $"{players}#{round}#{details}";
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
            match.RevealCard(playerId, playerId, GetRoundTime(match));
        }

        /// <summary>
        /// Hora de las acciones de la ronda en curso: la ronda 1 empieza en _startUtc y cada ronda siguiente un minuto
        /// después, así una ronda posterior nunca usa una hora anterior a lo ya ocurrido.
        /// </summary>
        private static DateTime GetRoundTime(MatchState match)
        {
            return _startUtc.AddMinutes(match.RoundNumber - FirstRoundNumber);
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

        private static PlayerExitStatus LeaveNow(MatchState match, int playerId)
        {
            return match.RemovePlayer(playerId, PlayerExit.Immediate(_startUtc), _pickFirstCard);
        }
    }
}
