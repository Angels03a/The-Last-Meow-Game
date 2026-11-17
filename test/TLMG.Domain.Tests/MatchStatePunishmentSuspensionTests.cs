using System;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Pruebas de la suspensión y la reanudación del castigo pendiente (Reglas 4.0.6, H-06 y H-14): solo el castigado
    /// suspende y reanuda, la suspensión no consume tiempo, la reanudación no reinicia el plazo y un castigo vencido
    /// no se recupera.
    /// </summary>
    public class MatchStatePunishmentSuspensionTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int PlayerAId = 1;
        private const int PlayerBId = 2;
        private const int PlayerCId = 3;
        private const int UnknownPlayerId = 99;
        private const int OneCardBet = 1;
        private const int TwoCardBet = 2;
        private const int PunishmentChoiceSeconds = 10;
        private const int DisconnectAfterSeconds = 3;
        private const int RemainingSecondsAtDisconnect = 7;
        private const int ReturnAfterSeconds = 12;
        private const int ResumedDeadlineAfterSeconds = 19;
        private const int SecondDisconnectAfterSeconds = 14;
        private const int SecondReturnAfterSeconds = 30;
        private const int SecondResumedDeadlineAfterSeconds = 35;
        private const long OneTick = 1;
        private const long NoTicks = 0;
        private const long OneSecondInTicks = TimeSpan.TicksPerSecond;
        private const int FirstCardIndex = 0;
        private const int FourLives = 4;
        private const int ThreeLives = 3;
        private const int SafeCardsToLoseBeforeLastLife = 3;
        private const CardKind SafeCard = CardKind.Safe;
        private const CardKind FatalCatCard = CardKind.FatalCat;

        private static readonly DateTime _nowUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime _deadlineUtc = _nowUtc.AddSeconds(PunishmentChoiceSeconds);
        private static readonly DateTime _disconnectUtc = _nowUtc.AddSeconds(DisconnectAfterSeconds);
        private static readonly DateTime _returnUtc = _nowUtc.AddSeconds(ReturnAfterSeconds);
        private static readonly DateTime _resumedDeadlineUtc = _nowUtc.AddSeconds(ResumedDeadlineAfterSeconds);
        private static readonly TimeSpan _remainingAtDisconnect = TimeSpan.FromSeconds(RemainingSecondsAtDisconnect);

        /// <summary>Verifica SuspendPunishment_ByPunishedOnForeignCat_StoresRemainingTimeWithoutDeadline.</summary>
        [Fact]
        public void SuspendPunishment_ByPunishedOnForeignCat_StoresRemainingTimeWithoutDeadline()
        {
            // Arrange: B elige a ciegas una carta de A; A, el castigado, se desconecta.
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Assert
            Assert.Equal(PunishmentStatus.Suspended, status);
            Assert.Null(match.PendingPunishment.DeadlineUtc);
            Assert.Equal(_remainingAtDisconnect, match.PendingPunishment.RemainingTime);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
        }

        /// <summary>Verifica SuspendPunishment_ByChooserOfForeignCat_ReturnsNotThePunishedAndKeepsDeadline.</summary>
        [Fact]
        public void SuspendPunishment_ByChooserOfForeignCat_ReturnsNotThePunishedAndKeepsDeadline()
        {
            // Arrange: B, quien elige, se desconecta; sus 10 segundos siguen corriendo.
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.SuspendPunishment(PlayerBId, _disconnectUtc);

            // Assert
            Assert.Equal(PunishmentStatus.NotThePunished, status);
            Assert.Equal(_deadlineUtc, match.PendingPunishment.DeadlineUtc);
            Assert.Null(match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica ExpirePunishment_AfterChooserDisconnects_RemovesRandomCardAtOriginalDeadline.</summary>
        [Fact]
        public void ExpirePunishment_AfterChooserDisconnects_RemovesRandomCardAtOriginalDeadline()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerBId, _disconnectUtc);

            // Act
            PunishmentStatus status = match.ExpirePunishment(_deadlineUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica SuspendPunishment_ByPlayerOutsidePunishment_ReturnsNotThePunished.</summary>
        [Fact]
        public void SuspendPunishment_ByPlayerOutsidePunishment_ReturnsNotThePunished()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.SuspendPunishment(PlayerCId, _disconnectUtc);

            // Assert
            Assert.Equal(PunishmentStatus.NotThePunished, status);
            Assert.Equal(_deadlineUtc, match.PendingPunishment.DeadlineUtc);
        }

        /// <summary>Verifica ChoosePunishmentCard_WhileSuspended_ReturnsPunishmentSuspendedAndKeepsHand.</summary>
        [Fact]
        public void ChoosePunishmentCard_WhileSuspended_ReturnsPunishmentSuspendedAndKeepsHand()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _disconnectUtc);

            // Assert
            Assert.Equal(PunishmentStatus.PunishmentSuspended, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Null(match.PendingPunishment.RemovedCard);
            Assert.Equal(_remainingAtDisconnect, match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica ExpirePunishment_WhileSuspendedPastOriginalDeadline_ReturnsPunishmentSuspended.</summary>
        [Fact]
        public void ExpirePunishment_WhileSuspendedPastOriginalDeadline_ReturnsPunishmentSuspended()
        {
            // Arrange: la suspensión no consume tiempo; el plazo original ya pasó y aun así no vence.
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Act
            PunishmentStatus status = match.ExpirePunishment(_returnUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.PunishmentSuspended, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Equal(_remainingAtDisconnect, match.PendingPunishment.RemainingTime);
            Assert.Null(match.PendingPunishment.DeadlineUtc);
        }

        /// <summary>Verifica SuspendPunishment_OnOwnCat_SuspendsBecausePunishedConditionPrevails.</summary>
        [Fact]
        public void SuspendPunishment_OnOwnCat_SuspendsBecausePunishedConditionPrevails()
        {
            // Arrange: con Gato propio A elige y es castigado a la vez (H-14).
            MatchState match = CreateOwnPunishment();

            // Act
            PunishmentStatus status = match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Assert
            Assert.Equal(PunishmentStatus.Suspended, status);
            Assert.Equal(_remainingAtDisconnect, match.PendingPunishment.RemainingTime);
            AssertExactlyOneTimeValue(match.PendingPunishment);
        }

        /// <summary>Verifica ResumePunishment_OnOwnCat_ContinuesWithRemainingTimeInsteadOfFullPeriod.</summary>
        [Fact]
        public void ResumePunishment_OnOwnCat_ContinuesWithRemainingTimeInsteadOfFullPeriod()
        {
            // Arrange
            MatchState match = CreateOwnPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Act
            PunishmentStatus status = match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.Resumed, status);
            Assert.Equal(_resumedDeadlineUtc, match.PendingPunishment.DeadlineUtc);
            Assert.NotEqual(_returnUtc.AddSeconds(PunishmentChoiceSeconds), match.PendingPunishment.DeadlineUtc);
            Assert.Null(match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica ChoosePunishmentCard_OnOwnCatAfterResume_RemovesTheCardSeenAndChosen.</summary>
        [Fact]
        public void ChoosePunishmentCard_OnOwnCatAfterResume_RemovesTheCardSeenAndChosen()
        {
            // Arrange: A regresa y elige viendo sus cartas una segura.
            MatchState match = CreateOwnPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);
            match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);
            int safeCardIndex = IndexOfCard(match, PlayerAId, SafeCard);

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerAId, safeCardIndex, _returnUtc);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
            Assert.Equal(SafeCard, match.LastPunishment.RemovedCard);
            Assert.False(match.LastPunishment.IsBlindChoice);
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica SuspendPunishment_OneTickBeforeDeadline_KeepsOneTickRemaining.</summary>
        [Fact]
        public void SuspendPunishment_OneTickBeforeDeadline_KeepsOneTickRemaining()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.SuspendPunishment(PlayerAId, _deadlineUtc.AddTicks(-OneTick));

            // Assert
            Assert.Equal(PunishmentStatus.Suspended, status);
            Assert.Equal(TimeSpan.FromTicks(OneTick), match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica SuspendPunishment_AtOrAfterDeadline_ReturnsDeadlineExpiredAndKeepsDeadline.</summary>
        /// <param name="ticksAfterDeadline">Ticks transcurridos desde la hora límite.</param>
        [Theory]
        [InlineData(NoTicks)]
        [InlineData(OneTick)]
        [InlineData(OneSecondInTicks)]
        public void SuspendPunishment_AtOrAfterDeadline_ReturnsDeadlineExpiredAndKeepsDeadline(long ticksAfterDeadline)
        {
            // Arrange: una desconexión tardía no recupera un castigo vencido.
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.SuspendPunishment(PlayerAId, _deadlineUtc.AddTicks(ticksAfterDeadline));

            // Assert
            Assert.Equal(PunishmentStatus.DeadlineExpired, status);
            Assert.Equal(_deadlineUtc, match.PendingPunishment.DeadlineUtc);
            Assert.Null(match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica ExpirePunishment_AfterLateSuspensionRejected_RemovesRandomCard.</summary>
        [Fact]
        public void ExpirePunishment_AfterLateSuspensionRejected_RemovesRandomCard()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _deadlineUtc);

            // Act
            PunishmentStatus status = match.ExpirePunishment(_deadlineUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
            Assert.Null(match.PendingPunishment);
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica SuspendPunishment_Twice_ReturnsPunishmentSuspendedAndKeepsRemainingTime.</summary>
        [Fact]
        public void SuspendPunishment_Twice_ReturnsPunishmentSuspendedAndKeepsRemainingTime()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Act
            PunishmentStatus status = match.SuspendPunishment(PlayerAId, _returnUtc);

            // Assert
            Assert.Equal(PunishmentStatus.PunishmentSuspended, status);
            Assert.Equal(_remainingAtDisconnect, match.PendingPunishment.RemainingTime);
            Assert.Null(match.PendingPunishment.DeadlineUtc);
        }

        /// <summary>Verifica ResumePunishment_WhileActive_ReturnsPunishmentNotSuspendedAndKeepsDeadline.</summary>
        [Fact]
        public void ResumePunishment_WhileActive_ReturnsPunishmentNotSuspendedAndKeepsDeadline()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.ResumePunishment(PlayerAId, _disconnectUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.PunishmentNotSuspended, status);
            Assert.Equal(_deadlineUtc, match.PendingPunishment.DeadlineUtc);
            Assert.Null(match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica ResumePunishment_Twice_ReturnsPunishmentNotSuspendedAndKeepsDeadline.</summary>
        [Fact]
        public void ResumePunishment_Twice_ReturnsPunishmentNotSuspendedAndKeepsDeadline()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);
            match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);

            // Act
            PunishmentStatus status = match.ResumePunishment(PlayerAId, _resumedDeadlineUtc,
                cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.PunishmentNotSuspended, status);
            Assert.Equal(_resumedDeadlineUtc, match.PendingPunishment.DeadlineUtc);
            Assert.Null(match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica ResumePunishment_ByOtherPlayer_ReturnsNotThePunishedAndStaysSuspended.</summary>
        /// <param name="playerId">Jugador de la partida que no es el castigado.</param>
        [Theory]
        [InlineData(PlayerBId)]
        [InlineData(PlayerCId)]
        public void ResumePunishment_ByOtherPlayer_ReturnsNotThePunishedAndStaysSuspended(int playerId)
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Act
            PunishmentStatus status = match.ResumePunishment(playerId, _returnUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.NotThePunished, status);
            Assert.Equal(_remainingAtDisconnect, match.PendingPunishment.RemainingTime);
            Assert.Null(match.PendingPunishment.DeadlineUtc);
        }

        /// <summary>Verifica SuspendPunishment_WithUnknownPlayer_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void SuspendPunishment_WithUnknownPlayer_ReturnsPlayerNotInMatch()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();

            // Act
            PunishmentStatus status = match.SuspendPunishment(UnknownPlayerId, _disconnectUtc);

            // Assert
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, status);
            Assert.Equal(_deadlineUtc, match.PendingPunishment.DeadlineUtc);
        }

        /// <summary>Verifica ResumePunishment_WithUnknownPlayer_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void ResumePunishment_WithUnknownPlayer_ReturnsPlayerNotInMatch()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Act
            PunishmentStatus status = match.ResumePunishment(UnknownPlayerId, _returnUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.PlayerNotInMatch, status);
            Assert.Equal(_remainingAtDisconnect, match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica SuspendPunishment_WithoutPunishmentPending_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void SuspendPunishment_WithoutPunishmentPending_ReturnsNoPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            PunishmentStatus status = match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
        }

        /// <summary>Verifica ResumePunishment_WithoutPunishmentPending_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void ResumePunishment_WithoutPunishmentPending_ReturnsNoPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);

            // Act
            PunishmentStatus status = match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Null(match.PendingPunishment);
        }

        /// <summary>Verifica SuspendPunishment_AfterPunishmentResolved_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void SuspendPunishment_AfterPunishmentResolved_ReturnsNoPunishmentPending()
        {
            // Arrange: B ya eligió; el castigo terminó antes de la desconexión de A.
            MatchState match = CreateBlindPunishment();
            match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _nowUtc);

            // Act
            PunishmentStatus status = match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Null(match.PendingPunishment);
        }

        /// <summary>Verifica SuspendPunishment_AfterMatchFinished_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void SuspendPunishment_AfterMatchFinished_ReturnsNoPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatchFinishedByLastSurvivor();

            // Act
            PunishmentStatus status = match.SuspendPunishment(PlayerCId, _disconnectUtc);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
        }

        /// <summary>Verifica ResumePunishment_AfterMatchFinished_ReturnsNoPunishmentPending.</summary>
        [Fact]
        public void ResumePunishment_AfterMatchFinished_ReturnsNoPunishmentPending()
        {
            // Arrange
            MatchState match = CreateMatchFinishedByLastSurvivor();

            // Act
            PunishmentStatus status = match.ResumePunishment(PlayerCId, _returnUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.NoPunishmentPending, status);
            Assert.Equal(RoundPhase.MatchFinished, match.Phase);
        }

        /// <summary>Verifica ResumePunishment_AfterSuspension_SetsDeadlineFromRemainingTime.</summary>
        [Fact]
        public void ResumePunishment_AfterSuspension_SetsDeadlineFromRemainingTime()
        {
            // Arrange: A se desconecta con 7 s restantes y regresa después del plazo original.
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);

            // Act
            PunishmentStatus status = match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.Resumed, status);
            Assert.Equal(_resumedDeadlineUtc, match.PendingPunishment.DeadlineUtc);
            Assert.Null(match.PendingPunishment.RemainingTime);
            Assert.Equal(RoundPhase.PunishmentPending, match.Phase);
        }

        /// <summary>Verifica ChoosePunishmentCard_AfterResume_RemovesChosenCard.</summary>
        [Fact]
        public void ChoosePunishmentCard_AfterResume_RemovesChosenCard()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);
            match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _returnUtc);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
            Assert.Null(match.PendingPunishment);
        }

        /// <summary>Verifica ExpirePunishment_AfterResumeBeforeNewDeadline_ReturnsDeadlineNotReached.</summary>
        [Fact]
        public void ExpirePunishment_AfterResumeBeforeNewDeadline_ReturnsDeadlineNotReached()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);
            match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);

            // Act
            PunishmentStatus status = match.ExpirePunishment(_resumedDeadlineUtc.AddTicks(-OneTick),
                cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.DeadlineNotReached, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica ExpirePunishment_AtNewDeadlineAfterResume_RemovesRandomCard.</summary>
        [Fact]
        public void ExpirePunishment_AtNewDeadlineAfterResume_RemovesRandomCard()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);
            match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);

            // Act
            PunishmentStatus status = match.ExpirePunishment(_resumedDeadlineUtc, cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.CardRemoved, status);
            Assert.Equal(ThreeLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica ChoosePunishmentCard_AtNewDeadlineAfterResume_ReturnsDeadlineExpired.</summary>
        [Fact]
        public void ChoosePunishmentCard_AtNewDeadlineAfterResume_ReturnsDeadlineExpired()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);
            match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);

            // Act
            PunishmentStatus status = match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _resumedDeadlineUtc);

            // Assert
            Assert.Equal(PunishmentStatus.DeadlineExpired, status);
            Assert.Equal(FourLives, GetPlayer(match, PlayerAId).Lives);
        }

        /// <summary>Verifica ResumePunishment_AfterSecondSuspension_ConsumesOnlyActiveTime.</summary>
        [Fact]
        public void ResumePunishment_AfterSecondSuspension_ConsumesOnlyActiveTime()
        {
            // Arrange: 3 s activos, pausa, 2 s activos, pausa; quedan 5 s al regresar por segunda vez.
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);
            match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);
            match.SuspendPunishment(PlayerAId, _nowUtc.AddSeconds(SecondDisconnectAfterSeconds));

            // Act
            PunishmentStatus status = match.ResumePunishment(PlayerAId, _nowUtc.AddSeconds(SecondReturnAfterSeconds),
                cardCount => FirstCardIndex);

            // Assert
            Assert.Equal(PunishmentStatus.Resumed, status);
            Assert.Equal(_nowUtc.AddSeconds(SecondResumedDeadlineAfterSeconds), match.PendingPunishment.DeadlineUtc);
            Assert.Null(match.PendingPunishment.RemainingTime);
        }

        /// <summary>Verifica SuspendPunishment_WhileSuspended_PreservesCardsLivesChallengesPhaseTurnAndBet.</summary>
        [Fact]
        public void SuspendPunishment_WhileSuspended_PreservesCardsLivesChallengesPhaseTurnAndBet()
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            string matchBeforeSuspension = DescribeMatch(match);

            // Act
            match.SuspendPunishment(PlayerAId, _disconnectUtc);
            match.ChoosePunishmentCard(PlayerBId, FirstCardIndex, _returnUtc);
            match.ExpirePunishment(_returnUtc, cardCount => FirstCardIndex);
            match.PlaceCard(match.PlayerInTurn.PlayerId, SafeCard);

            // Assert
            Assert.Equal(matchBeforeSuspension, DescribeMatch(match));
        }

        /// <summary>Verifica SuspendAndResume_InEveryStep_KeepsExactlyOneTimeValue.</summary>
        [Fact]
        public void SuspendAndResume_InEveryStep_KeepsExactlyOneTimeValue()
        {
            // Arrange
            MatchState match = CreateOwnPunishment();
            PendingPunishment punishment = match.PendingPunishment;
            AssertExactlyOneTimeValue(punishment);

            // Act
            match.SuspendPunishment(PlayerAId, _disconnectUtc);
            AssertExactlyOneTimeValue(punishment);
            match.SuspendPunishment(PlayerAId, _returnUtc);
            AssertExactlyOneTimeValue(punishment);
            match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);
            AssertExactlyOneTimeValue(punishment);
            match.ResumePunishment(PlayerAId, _returnUtc, cardCount => FirstCardIndex);

            // Assert
            AssertExactlyOneTimeValue(punishment);
        }

        /// <summary>Verifica SuspendPunishment_WithTimeThatIsNotUtc_ThrowsArgumentException.</summary>
        /// <param name="timeKind">Tipo de hora distinto de UTC.</param>
        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void SuspendPunishment_WithTimeThatIsNotUtc_ThrowsArgumentException(DateTimeKind timeKind)
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            DateTime notUtcTime = DateTime.SpecifyKind(_disconnectUtc, timeKind);

            // Act
            Action suspendPunishment = () => match.SuspendPunishment(PlayerAId, notUtcTime);

            // Assert
            Assert.Throws<ArgumentException>(suspendPunishment);
            Assert.Equal(_deadlineUtc, match.PendingPunishment.DeadlineUtc);
        }

        /// <summary>Verifica ResumePunishment_WithTimeThatIsNotUtc_ThrowsArgumentException.</summary>
        /// <param name="timeKind">Tipo de hora distinto de UTC.</param>
        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void ResumePunishment_WithTimeThatIsNotUtc_ThrowsArgumentException(DateTimeKind timeKind)
        {
            // Arrange
            MatchState match = CreateBlindPunishment();
            match.SuspendPunishment(PlayerAId, _disconnectUtc);
            DateTime notUtcTime = DateTime.SpecifyKind(_returnUtc, timeKind);

            // Act
            Action resumePunishment = () => match.ResumePunishment(PlayerAId, notUtcTime, cardCount => FirstCardIndex);

            // Assert
            Assert.Throws<ArgumentException>(resumePunishment);
            Assert.Equal(_remainingAtDisconnect, match.PendingPunishment.RemainingTime);
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

        private static int IndexOfCard(MatchState match, int playerId, CardKind card)
        {
            return GetPlayer(match, playerId).HandCards.ToList().IndexOf(card);
        }

        private static void PlaceInTurn(MatchState match, params CardKind[] cards)
        {
            foreach (CardKind card in cards)
            {
                match.PlaceCard(match.PlayerInTurn.PlayerId, card);
            }
        }

        /// <summary>Quien tiene el turno abre la apuesta y todos los demás se retiran: el que abre apuesta.</summary>
        private static void OpenAndConcede(MatchState match, int betValue)
        {
            match.OpenBet(match.PlayerInTurn.PlayerId, betValue);

            while (match.Phase == RoundPhase.Betting)
            {
                match.WithdrawFromBet(match.PlayerInTurn.PlayerId);
            }
        }

        /// <summary>4 cartas, A[S], B[G], C[S]; A apuesta 2 y voltea su S y el Gato de B: B elige a ciegas.</summary>
        private static MatchState CreateBlindPunishment()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, SafeCard, FatalCatCard, SafeCard);
            OpenAndConcede(match, TwoCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
            match.RevealCard(PlayerAId, PlayerBId, _nowUtc);

            return match;
        }

        /// <summary>4 cartas, A[G], B[S], C[S]; A apuesta 1 y voltea su Gato: A elige viendo sus cartas.</summary>
        private static MatchState CreateOwnPunishment()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            PlaceInTurn(match, FatalCatCard, SafeCard, SafeCard);
            OpenAndConcede(match, OneCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            return match;
        }

        /// <summary>
        /// Orden A, B, C: B se elimina sobre su propio Gato, C gana un reto y A pierde su última carta: gana C.
        /// </summary>
        private static MatchState CreateMatchFinishedByLastSurvivor()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            EliminateSecondPlayer(match);
            PlaceInTurn(match, SafeCard, SafeCard);
            match.OpenBet(PlayerAId, OneCardBet);
            match.RaiseBet(PlayerCId, TwoCardBet);
            match.WithdrawFromBet(PlayerAId);
            match.RevealCard(PlayerCId, PlayerCId, _nowUtc);
            match.RevealCard(PlayerCId, PlayerAId, _nowUtc);
            LoseSafeCardsOnOwnCat(match, SafeCardsToLoseBeforeLastLife);
            PlaceInTurn(match, FatalCatCard, SafeCard);
            OpenAndConcede(match, OneCardBet);
            match.RevealCard(PlayerAId, PlayerAId, _nowUtc);

            return match;
        }

        /// <summary>Hasta que B queda eliminado: en cada ronda B apuesta 1 sobre su Gato y pierde una carta.</summary>
        private static void EliminateSecondPlayer(MatchState match)
        {
            while (!GetPlayer(match, PlayerBId).IsEliminated)
            {
                PlaceInTurn(match, SafeCard, FatalCatCard, SafeCard, SafeCard);
                OpenAndConcede(match, OneCardBet);
                match.RevealCard(PlayerBId, PlayerBId, _nowUtc);
                ChooseSafeCardIfPending(match, PlayerBId);
            }
        }

        /// <summary>
        /// Con A primero del orden, A coloca su Gato, los demás una segura, A apuesta 1, revela su Gato y elige perder
        /// una carta segura; se repite las veces indicadas.
        /// </summary>
        private static void LoseSafeCardsOnOwnCat(MatchState match, int roundCount)
        {
            for (int roundIndex = 0; roundIndex < roundCount; roundIndex++)
            {
                PlaceInTurn(match, FatalCatCard);
                PlaceSafeCardsUntilTurnOf(match, PlayerAId);
                OpenAndConcede(match, OneCardBet);
                match.RevealCard(PlayerAId, PlayerAId, _nowUtc);
                match.ChoosePunishmentCard(PlayerAId, IndexOfCard(match, PlayerAId, SafeCard), _nowUtc);
            }
        }

        private static void PlaceSafeCardsUntilTurnOf(MatchState match, int playerId)
        {
            while (match.PlayerInTurn.PlayerId != playerId)
            {
                match.PlaceCard(match.PlayerInTurn.PlayerId, SafeCard);
            }
        }

        private static void ChooseSafeCardIfPending(MatchState match, int playerId)
        {
            if (match.Phase == RoundPhase.PunishmentPending)
            {
                match.ChoosePunishmentCard(playerId, IndexOfCard(match, playerId, SafeCard), _nowUtc);
            }
        }

        /// <summary>Resume en texto cartas, vidas y retos de cada jugador, la fase, el turno y la apuesta.</summary>
        private static string DescribeMatch(MatchState match)
        {
            string players = string.Join(";", match.Players.Select(player =>
                $"{player.PlayerId}:{string.Join(",", player.HandCards)}|{string.Join(",", player.PileCards)}"
                + $"|{player.Lives}|{player.SuccessfulChallengeCount}"));

            string round = $"{match.Phase}#{match.PlayerInTurn.PlayerId}#{match.CurrentBet}"
                + $"#{match.BetLeader?.PlayerId}";

            return $"{players}#{round}";
        }

        /// <summary>Comprueba que exactamente uno de DeadlineUtc y RemainingTime tenga valor.</summary>
        private static void AssertExactlyOneTimeValue(PendingPunishment punishment)
        {
            Assert.NotEqual(punishment.DeadlineUtc.HasValue, punishment.RemainingTime.HasValue);
        }
    }
}
