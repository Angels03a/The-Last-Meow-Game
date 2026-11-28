using System;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests exits during punishment (Rules 2.0.3.1 and 4.0.6, H-06, H-07, H-08 and H-14). The punished player's exit
    /// cancels it. An active chooser's exit triggers a random choice. Other exits are recorded without changing
    /// punishment. Resuming without the chooser resolves it in one step.
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

        /// <summary>Checks RemovePlayer_ScenarioE41ChooserLeavesActivePunishment_ResolvesAtRandomOnce.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE41ChooserLeavesActivePunishment_ResolvesAtRandomOnce()
        {
            // Arrange: In E4-1, A fails on B's Fatal Cat. B leaves three seconds into the choice.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc),
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

        /// <summary>Checks CalculateFromMatch_ScenarioE41AfterChooserLeft_PutsHimLastWithZero.</summary>
        [Fact]
        public void CalculateFromMatch_ScenarioE41AfterChooserLeft_PutsHimLastWithZero()
        {
            // Arrange: E4-1 continues. A wins with two challenges and B ranks last.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc), _pickFirstCard);
            WinChallenges(match, PlayerAId, match.ModeRules.SuccessfulChallengesToWin);

            // Act
            FinalStandings finalStandings = FinalStandings.CalculateFromMatch(match, new int[0]);

            // Assert
            Assert.Equal(new[] { PlayerAId, PlayerCId, PlayerBId },
                finalStandings.Standings.Select(standing => standing.PlayerId));
            Assert.Equal(new[] { 3, 1, 0 }, finalStandings.Standings.Select(standing => standing.Points));
        }

        /// <summary>Checks RemovePlayer_ScenarioE41bChooserExpelledAfterExpiration_RestartsStartedRound.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE41bChooserExpelledAfterExpiration_RestartsStartedRound()
        {
            // Arrange: In E4-1b, B disconnects at 3 seconds. The choice expires at 10 seconds and B is removed at 18
            // seconds.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            int previousRoundNumber = match.RoundNumber;
            PunishmentStatus suspendStatus = match.SuspendPunishment(PlayerBId, _chooserLeavesUtc);
            PunishmentStatus expireStatus = match.ExpirePunishment(_deadlineUtc, _pickFirstCard);
            PlayerExit exitOfB = PlayerExit.CreateDisconnection(
                _chooserLeavesUtc, _chooserLeavesUtc.AddSeconds(GraceSeconds));

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
        /// Checks ExpirePunishment_AfterChooserLeftFirst_ReturnsNoPunishmentPendingWithoutSecondResolution.
        /// </summary>
        [Fact]
        public void ExpirePunishment_AfterChooserLeftFirst_ReturnsNoPunishmentPendingWithoutSecondResolution()
        {
            // Arrange: Alternate E4-1b order. B's removal arrives before the deadline check.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            int previousRoundNumber = match.RoundNumber;
            match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc), _pickFirstCard);
            string matchAfterExit = DescribeMatch(match);

            // Act
            PunishmentStatus status = match.ExpirePunishment(_deadlineUtc, _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Equal(matchAfterExit, DescribeMatch(match));
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Checks RemovePlayer_ScenarioE62PunishedLeavesActivePunishment_CancelsWithoutLosingLife.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE62PunishedLeavesActivePunishment_CancelsWithoutLosingLife()
        {
            // Arrange: In E6-2 case 2, A leaves while B is choosing.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerAId, PlayerExit.CreateVoluntary(_chooserLeavesUtc),
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

        /// <summary>Checks RemovePlayer_ScenarioE62PunishedExpelledWhileSuspended_CancelsWithoutLosingLife.</summary>
        [Fact]
        public void RemovePlayer_ScenarioE62PunishedExpelledWhileSuspended_CancelsWithoutLosingLife()
        {
            // Arrange: In E6-2 point 5, A disconnects, punishment pauses and A does not return within 15 seconds.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            int previousRoundNumber = match.RoundNumber;
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);
            PlayerExit exitOfA = PlayerExit.CreateDisconnection(
                _chooserLeavesUtc, _chooserLeavesUtc.AddSeconds(GraceSeconds));

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerAId, exitOfA, _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PunishmentCancelled, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
            Assert.Equal(PlayerBId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Checks RemovePlayer_ChooserLeavesSuspendedPunishment_RecordsExitOnly.</summary>
        [Fact]
        public void RemovePlayer_ChooserLeavesSuspendedPunishment_RecordsExitOnly()
        {
            // Arrange: A's punishment is suspended when B, the chooser, leaves.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            int previousRoundNumber = match.RoundNumber;
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.ExitRecorded, status);
            Assert.True(GetPlayer(match, PlayerBId).HasLeftMatch);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(_remainingAtSuspension, match.PendingPunishment.RemainingTime);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
        }

        /// <summary>Checks ResumePunishment_AfterChooserLeftWhileSuspended_ResolvesAtRandomInSameOperation.</summary>
        [Fact]
        public void ResumePunishment_AfterChooserLeftWhileSuspended_ResolvesAtRandomInSameOperation()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            int previousRoundNumber = match.RoundNumber;
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);
            match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc), _pickFirstCard);

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
        /// Checks RemovePlayer_PunishedLeavesAfterChooserLeftWhileSuspended_CancelsWithoutLosingLife.
        /// </summary>
        [Fact]
        public void RemovePlayer_PunishedLeavesAfterChooserLeftWhileSuspended_CancelsWithoutLosingLife()
        {
            // Arrange: A disconnects and punishment pauses. B leaves and A misses the 15-second grace period.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            int previousRoundNumber = match.RoundNumber;
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);
            match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc), _pickFirstCard);
            PlayerExit exitOfA = PlayerExit.CreateDisconnection(
                _chooserLeavesUtc, _chooserLeavesUtc.AddSeconds(GraceSeconds));

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerAId, exitOfA, _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PunishmentCancelled, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Checks ResumePunishment_WithChooserPresent_KeepsExactRemainingTimeWithoutRandom.</summary>
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

        /// <summary>Checks ResumePunishment_WithChooserAbsentAndInvalidRandomIndex_ThrowsWithoutChange.</summary>
        [Fact]
        public void ResumePunishment_WithChooserAbsentAndInvalidRandomIndex_ThrowsWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);
            match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc), _pickFirstCard);
            string matchBeforeResume = DescribeMatch(match);

            // Act
            Action resumePunishment = () => match.ResumePunishment(PlayerAId, _returnUtc, _pickInvalidCard);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(resumePunishment);
            Assert.Equal(matchBeforeResume, DescribeMatch(match));
            Assert.Equal(_remainingAtSuspension, match.PendingPunishment.RemainingTime);
        }

        /// <summary>Checks RemovePlayer_ChooserLeavesWithInvalidRandomIndex_ThrowsWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_ChooserLeavesWithInvalidRandomIndex_ThrowsWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            Action removePlayer = () =>
                match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc), _pickInvalidCard);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(removePlayer);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Checks ResumePunishment_WithNullRandom_ThrowsArgumentNullExceptionWithoutChange.</summary>
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
        /// Checks RemovePlayer_WithNullRandomDuringPunishment_ThrowsArgumentNullExceptionWithoutChange.
        /// </summary>
        [Fact]
        public void RemovePlayer_WithNullRandomDuringPunishment_ThrowsArgumentNullExceptionWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            string matchBeforeExit = DescribeMatch(match);

            // Act
            Action removePlayer = () => match.RemovePlayer(
                PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc), null);

            // Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(removePlayer);
            Assert.Equal("chooseRandomCardIndex", exception.ParamName);
            Assert.Equal(matchBeforeExit, DescribeMatch(match));
        }

        /// <summary>Checks SuspendAndResumePunishment_OnOwnCat_ResumesWithRemainingTimeWithoutRandom.</summary>
        [Fact]
        public void SuspendAndResumePunishment_OnOwnCat_ResumesWithRemainingTimeWithoutRandom()
        {
            // Arrange: In H-14, A is both the chooser and the punished player.
            MatchState match = CreateMatchWithOwnPunishment();
            PunishmentStatus suspendStatus = match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);

            // Act
            PunishmentStatus resumeStatus = match.ResumePunishment(PlayerAId, _returnUtc, _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PunishmentStatus.Suspended, suspendStatus);
            Assert.Equal(PunishmentStatus.Resumed, resumeStatus);
            Assert.Equal(_returnUtc + _remainingAtSuspension, match.PendingPunishment.DeadlineUtc);
        }

        /// <summary>Checks RemovePlayer_OnOwnCat_CancelsBecausePunishedConditionPrevails.</summary>
        [Fact]
        public void RemovePlayer_OnOwnCat_CancelsBecausePunishedConditionPrevails()
        {
            // Arrange: In H-14, A leaves while both choosing and receiving punishment.
            MatchState match = CreateMatchWithOwnPunishment();
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerAId, PlayerExit.CreateVoluntary(_chooserLeavesUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PunishmentCancelled, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(previousRoundNumber + OneRound, match.RoundNumber);
        }

        /// <summary>Checks RemovePlayer_ThirdPlayerLeavesActivePunishment_RecordsExitOnly.</summary>
        [Fact]
        public void RemovePlayer_ThirdPlayerLeavesActivePunishment_RecordsExitOnly()
        {
            // Arrange: In H-07, C has no role in A's punishment.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            int previousRoundNumber = match.RoundNumber;
            DateTime? deadlineUtc = match.PendingPunishment.DeadlineUtc;

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerCId, PlayerExit.CreateVoluntary(_chooserLeavesUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.ExitRecorded, status);
            Assert.Equal(deadlineUtc, match.PendingPunishment.DeadlineUtc);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
            Assert.Equal(PunishmentStatus.CardRemoved,
                match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _chooserLeavesUtc));
        }

        /// <summary>Checks RemovePlayer_ThirdPlayerLeavesSuspendedPunishment_RecordsExitOnly.</summary>
        [Fact]
        public void RemovePlayer_ThirdPlayerLeavesSuspendedPunishment_RecordsExitOnly()
        {
            // Arrange: H-07 with punishment suspended.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            int previousRoundNumber = match.RoundNumber;
            match.SuspendPunishment(PlayerAId, _chooserLeavesUtc);

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerCId, PlayerExit.CreateVoluntary(_chooserLeavesUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.ExitRecorded, status);
            Assert.Equal(_remainingAtSuspension, match.PendingPunishment.RemainingTime);
            Assert.Equal(previousRoundNumber, match.RoundNumber);
        }

        /// <summary>Checks RemovePlayer_LeaveDuringPunishmentLeavesOnePlayer_FinishesWithCleanState.</summary>
        /// <param name="leavingPlayerId">Player leaving, either the punished player (A) or the chooser (B).</param>
        /// <param name="winnerId">Sole remaining active player.</param>
        [Theory]
        [InlineData(PlayerAId, PlayerBId)]
        [InlineData(PlayerBId, PlayerAId)]
        public void RemovePlayer_LeaveDuringPunishmentLeavesOnePlayer_FinishesWithCleanState(int leavingPlayerId,
            int winnerId)
        {
            // Arrange: C already left. A fails on B's Fatal Cat.
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            LeaveNow(match, PlayerCId);
            FailOnCatOfB(match);
            int previousRoundNumber = match.RoundNumber;

            // Act
            PlayerExitStatus status = match.RemovePlayer(leavingPlayerId, PlayerExit.CreateVoluntary(_chooserLeavesUtc),
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

        /// <summary>Checks RemovePlayer_SameChooserLeavesTwice_ReturnsPlayerAlreadyLeftWithoutChange.</summary>
        [Fact]
        public void RemovePlayer_SameChooserLeavesTwice_ReturnsPlayerAlreadyLeftWithoutChange()
        {
            // Arrange
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc), _pickFirstCard);
            string matchAfterExit = DescribeMatch(match);

            // Act
            PlayerExitStatus status = match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_returnUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PlayerAlreadyLeft, status);
            Assert.Equal(matchAfterExit, DescribeMatch(match));
        }

        /// <summary>Checks PunishmentActions_AfterCancellation_ReturnExistingStatusesWithoutChange.</summary>
        [Fact]
        public void PunishmentActions_AfterCancellation_ReturnExistingStatusesWithoutChange()
        {
            // Arrange: A left and their punishment was canceled.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            match.RemovePlayer(PlayerAId, PlayerExit.CreateVoluntary(_chooserLeavesUtc), _failIfRandomIsUsed);
            string matchAfterExit = DescribeMatch(match);

            // Act
            PunishmentStatus resumeByPunishedStatus =
                match.ResumePunishment(PlayerAId, _returnUtc, _failIfRandomIsUsed);
            PunishmentStatus resumeStatus = match.ResumePunishment(PlayerCId, _returnUtc, _failIfRandomIsUsed);
            PunishmentStatus chooseStatus = match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _returnUtc);
            PunishmentStatus expireStatus = match.ExpirePunishment(_returnUtc, _failIfRandomIsUsed);
            PlayerExitStatus repeatedExitStatus = match.RemovePlayer(PlayerAId, PlayerExit.CreateVoluntary(_returnUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PlayerExitStatus.PlayerAlreadyLeft, repeatedExitStatus);
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, resumeByPunishedStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, resumeStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, chooseStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, expireStatus);
            Assert.Equal(matchAfterExit, DescribeMatch(match));
        }

        /// <summary>Checks PunishmentActions_AfterRandomResolution_ReturnExistingStatusesWithoutChange.</summary>
        [Fact]
        public void PunishmentActions_AfterRandomResolution_ReturnExistingStatusesWithoutChange()
        {
            // Arrange: B left and A's punishment was resolved at random.
            MatchState match = CreateMatchWithBlindPunishment(PlayerAId, PlayerBId, PlayerCId);
            match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_chooserLeavesUtc), _pickFirstCard);
            string matchAfterExit = DescribeMatch(match);

            // Act
            PunishmentStatus resumeStatus = match.ResumePunishment(PlayerAId, _returnUtc, _failIfRandomIsUsed);
            PunishmentStatus chooseStatus = match.ChoosePunishmentCard(PlayerAId, FirstCardIndex, _returnUtc);
            PunishmentStatus expireStatus = match.ExpirePunishment(_returnUtc, _failIfRandomIsUsed);
            PlayerExitStatus repeatedExitStatus = match.RemovePlayer(PlayerBId, PlayerExit.CreateVoluntary(_returnUtc),
                _failIfRandomIsUsed);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, expireStatus);
            Assert.Equal(PlayerExitStatus.PlayerAlreadyLeft, repeatedExitStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, resumeStatus);
            Assert.Equal(PunishmentStatus.NoPunishmentPending, chooseStatus);
            Assert.Equal(matchAfterExit, DescribeMatch(match));
        }

        /// <summary>
        /// A places a safe card, B their Fatal Cat and the others safe cards. A bids 2 and fails on B's cat.
        /// </summary>
        private static void FailOnCatOfB(MatchState match)
        {
            match.PlaceCard(PlayerAId, SafeCard);
            match.PlaceCard(PlayerBId, FatalCatCard);
            PlaceSafeCardsUntilTurnOf(match, PlayerAId);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _startUtc);
            match.RevealCard(PlayerAId, PlayerBId, _startUtc);
        }

        /// <summary>Four-card mode in the given order, with A and B first. B chooses blindly from A's cards.</summary>
        private static MatchState CreateMatchWithBlindPunishment(params int[] playerIdsInOrder)
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, playerIdsInOrder);
            FailOnCatOfB(match);

            return match;
        }

        /// <summary>
        /// Four-card mode in order A, B, C, D. A bids 1 on their own Fatal Cat and sees their cards when choosing.
        /// </summary>
        private static MatchState CreateMatchWithOwnPunishment()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId, PlayerDId);
            BetOneOnOwnTopCard(match, PlayerAId, FatalCatCard);

            return match;
        }

        /// <summary>Summarizes visible match state as text for a before-and-after comparison.</summary>
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

        /// <summary>The current player opens and all others fold, leaving the opener as the final bidder.</summary>
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
        /// Players place a safe card in turn until the target player's turn. One full cycle is enough. A rejected
        /// placement or exhausted cycle reports the player and round that blocked progress.
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
        /// Others place safe cards until the player's turn. That player places the given card, waits another cycle and
        /// bids 1 on their own pile. A safe card wins and a Fatal Cat fails.
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
        /// Round 1 starts at _startUtc. Each new round starts one minute later so that later rounds never use an
        /// earlier time.
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
            return match.RemovePlayer(playerId, PlayerExit.CreateVoluntary(_startUtc), _pickFirstCard);
        }
    }
}
