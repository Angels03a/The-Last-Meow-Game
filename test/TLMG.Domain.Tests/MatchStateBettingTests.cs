using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests opening, raising and folding in MatchState. Uses the placement and bidding parts of Domain v3 scenarios 1
    /// (round 1), 2 and 3 (§4.4).
    /// </summary>
    public class MatchStateBettingTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int EightCardsPerPlayer = 8;
        private const int FirstPlayerId = 1;
        private const int SecondPlayerId = 2;
        private const int ThirdPlayerId = 3;
        private const int FourthPlayerId = 4;
        private const int UnknownPlayerId = 99;
        private const int ThreePlayers = 3;
        private const int FourPlayers = 4;
        private const int PlacementsBeforeFatalCat = 4;
        private const int PlacementsForThreeCardsOnTable = 3;
        private const int PlacementsForEightCardsOnTable = 8;
        private const int TwoPlacementsBeforeThirdPlayer = 2;
        private const int ThreePlacementsUntilThirdPlayerAgain = 3;
        private const int TwoCardBet = 2;
        private const int PlacementsForTwelveCardsOnTable = 12;

        /// <summary>Checks OpenBet_Scenario1Round1_ThirdPlayerOpensTwoWithFiveCardsOnTable.</summary>
        [Fact]
        public void OpenBet_Scenario1Round1_ThirdPlayerOpensTwoWithFiveCardsOnTable()
        {
            // Arrange: Order C, B, A (IDs 1, 2, 3). C, B, A and C place safe cards, then B places the Fatal Cat.
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsBeforeFatalCat);
            match.PlaceCard(SecondPlayerId, CardKind.FatalCat);

            // Act
            BetActionStatus status = match.OpenBet(ThirdPlayerId, 2);

            // Assert
            Assert.Equal(BetActionStatus.Accepted, status);
            Assert.Equal(5, match.CardsOnTable);
            Assert.Equal(RoundPhase.Betting, match.Phase);
            Assert.Equal(2, match.CurrentBet);
            Assert.Equal(ThirdPlayerId, match.BetLeader.PlayerId);
        }

        /// <summary>Checks WithdrawFromBet_Scenario1Round1_LeavesChallengeReadyForThirdPlayer.</summary>
        [Fact]
        public void WithdrawFromBet_Scenario1Round1_LeavesChallengeReadyForThirdPlayer()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsBeforeFatalCat);
            match.PlaceCard(SecondPlayerId, CardKind.FatalCat);
            match.OpenBet(ThirdPlayerId, 2);

            // Act
            match.WithdrawFromBet(FirstPlayerId);
            match.WithdrawFromBet(SecondPlayerId);

            // Assert
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
            Assert.Equal(ThirdPlayerId, match.BetLeader.PlayerId);
            Assert.Equal(2, match.CurrentBet);
            Assert.Equal(ThirdPlayerId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Checks OpenBet_Scenario2WithThreeCardsOnTable_LeavesChallengeReadyForThree.</summary>
        [Fact]
        public void OpenBet_Scenario2WithThreeCardsOnTable_LeavesChallengeReadyForThree()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);

            // Act
            BetActionStatus status = match.OpenBet(FirstPlayerId, 3);
            match.WithdrawFromBet(SecondPlayerId);
            match.WithdrawFromBet(ThirdPlayerId);

            // Assert
            Assert.Equal(BetActionStatus.Accepted, status);
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
            Assert.Equal(FirstPlayerId, match.BetLeader.PlayerId);
            Assert.Equal(3, match.CurrentBet);
        }

        /// <summary>Checks OpenBet_Scenario2WithValueAboveTable_ReturnsBetValueNotValid.</summary>
        [Fact]
        public void OpenBet_Scenario2WithValueAboveTable_ReturnsBetValueNotValid()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);

            // Act
            BetActionStatus status = match.OpenBet(FirstPlayerId, 4);

            // Assert
            Assert.Equal(BetActionStatus.BetValueNotValid, status);
        }

        /// <summary>Checks RaiseBet_Scenario3WithRaiseAndWithdrawals_LeavesChallengeReadyForSecondPlayer.</summary>
        [Fact]
        public void RaiseBet_Scenario3WithRaiseAndWithdrawals_LeavesChallengeReadyForSecondPlayer()
        {
            // Arrange: Four players, eight cards on the table and A opens at 2.
            MatchState match = CreateMatch(FourCardsPerPlayer, FourPlayers);
            PlaceSafeCards(match, PlacementsForEightCardsOnTable);
            match.OpenBet(FirstPlayerId, 2);

            // Act
            BetActionStatus raiseStatus = match.RaiseBet(SecondPlayerId, 3);
            match.WithdrawFromBet(ThirdPlayerId);
            match.WithdrawFromBet(FourthPlayerId);
            BetActionStatus withdrawStatus = match.WithdrawFromBet(FirstPlayerId);

            // Assert
            Assert.Equal(BetActionStatus.Accepted, raiseStatus);
            Assert.Equal(BetActionStatus.Accepted, withdrawStatus);
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
            Assert.Equal(SecondPlayerId, match.BetLeader.PlayerId);
            Assert.Equal(3, match.CurrentBet);
        }

        /// <summary>Checks RaiseBet_Scenario3WithValueAboveCap_ReturnsBetValueNotValid.</summary>
        [Fact]
        public void RaiseBet_Scenario3WithValueAboveCap_ReturnsBetValueNotValid()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, FourPlayers);
            PlaceSafeCards(match, PlacementsForEightCardsOnTable);
            match.OpenBet(FirstPlayerId, 2);

            // Act
            BetActionStatus status = match.RaiseBet(SecondPlayerId, 5);

            // Assert
            Assert.Equal(BetActionStatus.BetValueNotValid, status);
        }

        /// <summary>Checks RaiseBet_Scenario3WithValueEqualToCap_ReturnsAccepted.</summary>
        [Fact]
        public void RaiseBet_Scenario3WithValueEqualToCap_ReturnsAccepted()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, FourPlayers);
            PlaceSafeCards(match, PlacementsForEightCardsOnTable);
            match.OpenBet(FirstPlayerId, 2);

            // Act
            BetActionStatus status = match.RaiseBet(SecondPlayerId, 4);

            // Assert
            Assert.Equal(BetActionStatus.Accepted, status);
        }

        /// <summary>Checks OpenBet_WithEightCardMode_LimitsValueToCardsOnTable.</summary>
        [Fact]
        public void OpenBet_WithEightCardMode_LimitsValueToCardsOnTable()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);

            // Act
            BetActionStatus aboveTableStatus = match.OpenBet(FirstPlayerId, 4);
            BetActionStatus tableStatus = match.OpenBet(FirstPlayerId, 3);

            // Assert
            Assert.Equal(BetActionStatus.BetValueNotValid, aboveTableStatus);
            Assert.Equal(BetActionStatus.Accepted, tableStatus);
        }

        /// <summary>Checks OpenBet_WithValueAboveModeCap_ReturnsBetValueNotValid.</summary>
        [Fact]
        public void OpenBet_WithValueAboveModeCap_ReturnsBetValueNotValid()
        {
            // Arrange: Twelve cards are on the table, but four-card mode caps the bid at 4.
            MatchState match = CreateMatch(FourCardsPerPlayer, FourPlayers);
            PlaceSafeCards(match, PlacementsForTwelveCardsOnTable);

            // Act
            BetActionStatus aboveCapStatus = match.OpenBet(FirstPlayerId, 5);
            BetActionStatus capStatus = match.OpenBet(FirstPlayerId, 4);

            // Assert
            Assert.Equal(BetActionStatus.BetValueNotValid, aboveCapStatus);
            Assert.Equal(BetActionStatus.Accepted, capStatus);
        }

        /// <summary>Checks OpenBet_WithZeroOrNegativeValue_ReturnsBetValueNotValid.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void OpenBet_WithZeroOrNegativeValue_ReturnsBetValueNotValid(int betValue)
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);

            // Act
            BetActionStatus status = match.OpenBet(FirstPlayerId, betValue);

            // Assert
            Assert.Equal(BetActionStatus.BetValueNotValid, status);
        }

        /// <summary>Checks OpenBet_WithEmptyTable_ReturnsTableEmpty.</summary>
        [Fact]
        public void OpenBet_WithEmptyTable_ReturnsTableEmpty()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            BetActionStatus status = match.OpenBet(FirstPlayerId, 1);

            // Assert
            Assert.Equal(BetActionStatus.TableEmpty, status);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
        }

        /// <summary>Checks OpenBet_WithEmptyHandAndValueOne_ReturnsAccepted.</summary>
        [Fact]
        public void OpenBet_WithEmptyHandAndValueOne_ReturnsAccepted()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            EmptyEveryHand(match);

            // Act
            BetActionStatus status = match.OpenBet(FirstPlayerId, 1);

            // Assert
            Assert.Equal(BetActionStatus.Accepted, status);
            Assert.Equal(1, match.CurrentBet);
        }

        /// <summary>Checks OpenBet_WithEmptyHandAndValueAboveOne_ReturnsEmptyHandRequiresBetOfOne.</summary>
        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        public void OpenBet_WithEmptyHandAndValueAboveOne_ReturnsEmptyHandRequiresBetOfOne(int betValue)
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            EmptyEveryHand(match);

            // Act
            BetActionStatus status = match.OpenBet(FirstPlayerId, betValue);

            // Assert
            Assert.Equal(BetActionStatus.EmptyHandRequiresBetOfOne, status);
        }

        /// <summary>Checks RaiseBet_ByEmptyHandPlayer_ReturnsAccepted.</summary>
        [Fact]
        public void RaiseBet_ByEmptyHandPlayer_ReturnsAccepted()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            EmptyEveryHand(match);
            match.OpenBet(FirstPlayerId, 1);

            // Act
            BetActionStatus status = match.RaiseBet(SecondPlayerId, 3);

            // Assert
            Assert.Equal(BetActionStatus.Accepted, status);
            Assert.Equal(3, match.CurrentBet);
        }

        /// <summary>Checks WithdrawFromBet_ByEmptyHandPlayer_ReturnsAccepted.</summary>
        [Fact]
        public void WithdrawFromBet_ByEmptyHandPlayer_ReturnsAccepted()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            EmptyEveryHand(match);
            match.OpenBet(FirstPlayerId, 1);

            // Act
            BetActionStatus status = match.WithdrawFromBet(SecondPlayerId);

            // Assert
            Assert.Equal(BetActionStatus.Accepted, status);
            Assert.True(match.Players[1].HasWithdrawnFromBet);
        }

        /// <summary>Checks OpenBet_OutOfTurn_ReturnsNotPlayersTurn.</summary>
        [Fact]
        public void OpenBet_OutOfTurn_ReturnsNotPlayersTurn()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);

            // Act
            BetActionStatus status = match.OpenBet(SecondPlayerId, 1);

            // Assert
            Assert.Equal(BetActionStatus.NotPlayersTurn, status);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
        }

        /// <summary>Checks OpenBet_ByUnknownPlayer_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void OpenBet_ByUnknownPlayer_ReturnsPlayerNotInMatch()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);

            // Act
            BetActionStatus status = match.OpenBet(UnknownPlayerId, 1);

            // Assert
            Assert.Equal(BetActionStatus.PlayerNotInMatch, status);
        }

        /// <summary>Checks OpenBet_WithBetInProgress_ReturnsBetInProgress.</summary>
        [Fact]
        public void OpenBet_WithBetInProgress_ReturnsBetInProgress()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);
            match.OpenBet(FirstPlayerId, 1);

            // Act
            BetActionStatus status = match.OpenBet(SecondPlayerId, 2);

            // Assert
            Assert.Equal(BetActionStatus.BetInProgress, status);
        }

        /// <summary>Checks OpenBet_WithChallengeReady_ReturnsBetInProgress.</summary>
        [Fact]
        public void OpenBet_WithChallengeReady_ReturnsBetInProgress()
        {
            // Arrange
            MatchState match = CreateReadyChallenge();

            // Act
            BetActionStatus status = match.OpenBet(FirstPlayerId, 2);

            // Assert
            Assert.Equal(BetActionStatus.BetInProgress, status);
        }

        /// <summary>Checks RaiseBet_WithEqualValue_ReturnsBetValueNotValid.</summary>
        [Fact]
        public void RaiseBet_WithEqualValue_ReturnsBetValueNotValid()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsBeforeFatalCat);
            match.OpenBet(SecondPlayerId, 2);

            // Act
            BetActionStatus status = match.RaiseBet(ThirdPlayerId, 2);

            // Assert
            Assert.Equal(BetActionStatus.BetValueNotValid, status);
        }

        /// <summary>Checks RaiseBet_WithLowerValue_KeepsBetAndTurn.</summary>
        [Fact]
        public void RaiseBet_WithLowerValue_KeepsBetAndTurn()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsBeforeFatalCat);
            match.OpenBet(SecondPlayerId, 3);

            // Act
            BetActionStatus status = match.RaiseBet(ThirdPlayerId, 2);

            // Assert
            Assert.Equal(BetActionStatus.BetValueNotValid, status);
            Assert.Equal(3, match.CurrentBet);
            Assert.Equal(ThirdPlayerId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Checks RaiseBet_WithoutBet_ReturnsNoBetInProgress.</summary>
        [Fact]
        public void RaiseBet_WithoutBet_ReturnsNoBetInProgress()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);

            // Act
            BetActionStatus status = match.RaiseBet(FirstPlayerId, 2);

            // Assert
            Assert.Equal(BetActionStatus.NoBetInProgress, status);
        }

        /// <summary>Checks RaiseBet_WithChallengeReady_ReturnsNoBetInProgress.</summary>
        [Fact]
        public void RaiseBet_WithChallengeReady_ReturnsNoBetInProgress()
        {
            // Arrange
            MatchState match = CreateReadyChallenge();

            // Act
            BetActionStatus status = match.RaiseBet(FirstPlayerId, 3);

            // Assert
            Assert.Equal(BetActionStatus.NoBetInProgress, status);
        }

        /// <summary>Checks RaiseBet_OutOfTurn_ReturnsNotPlayersTurn.</summary>
        [Fact]
        public void RaiseBet_OutOfTurn_ReturnsNotPlayersTurn()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);
            match.OpenBet(FirstPlayerId, 1);

            // Act
            BetActionStatus status = match.RaiseBet(ThirdPlayerId, 2);

            // Assert
            Assert.Equal(BetActionStatus.NotPlayersTurn, status);
        }

        /// <summary>Checks RaiseBet_ByWithdrawnPlayer_ReturnsNotPlayersTurn.</summary>
        [Fact]
        public void RaiseBet_ByWithdrawnPlayer_ReturnsNotPlayersTurn()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, FourPlayers);
            PlaceSafeCards(match, PlacementsForEightCardsOnTable);
            match.OpenBet(FirstPlayerId, 1);
            match.WithdrawFromBet(SecondPlayerId);

            // Act
            BetActionStatus status = match.RaiseBet(SecondPlayerId, 2);

            // Assert
            Assert.Equal(BetActionStatus.NotPlayersTurn, status);
        }

        /// <summary>Checks WithdrawFromBet_WithBetInProgress_SkipsWithdrawnPlayerInNextTurns.</summary>
        [Fact]
        public void WithdrawFromBet_WithBetInProgress_SkipsWithdrawnPlayerInNextTurns()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, FourPlayers);
            PlaceSafeCards(match, PlacementsForEightCardsOnTable);
            match.OpenBet(FirstPlayerId, 1);
            match.WithdrawFromBet(SecondPlayerId);
            match.RaiseBet(ThirdPlayerId, 2);
            match.RaiseBet(FourthPlayerId, 3);

            // Act
            match.RaiseBet(FirstPlayerId, 4);

            // Assert
            Assert.Equal(ThirdPlayerId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Checks WithdrawFromBet_WithoutBet_ReturnsNoBetInProgress.</summary>
        [Fact]
        public void WithdrawFromBet_WithoutBet_ReturnsNoBetInProgress()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            BetActionStatus status = match.WithdrawFromBet(FirstPlayerId);

            // Assert
            Assert.Equal(BetActionStatus.NoBetInProgress, status);
            Assert.False(match.Players[0].HasWithdrawnFromBet);
        }

        /// <summary>Checks WithdrawFromBet_OutOfTurn_ReturnsNotPlayersTurn.</summary>
        [Fact]
        public void WithdrawFromBet_OutOfTurn_ReturnsNotPlayersTurn()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);
            match.OpenBet(FirstPlayerId, 1);

            // Act
            BetActionStatus status = match.WithdrawFromBet(ThirdPlayerId);

            // Assert
            Assert.Equal(BetActionStatus.NotPlayersTurn, status);
        }

        /// <summary>Checks PlaceCard_WithBetInProgress_ReturnsBetInProgress.</summary>
        [Fact]
        public void PlaceCard_WithBetInProgress_ReturnsBetInProgress()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);
            match.OpenBet(FirstPlayerId, 1);

            // Act
            CardPlacementStatus status = match.PlaceCard(SecondPlayerId, CardKind.Safe);

            // Assert
            Assert.Equal(CardPlacementStatus.BetInProgress, status);
            Assert.Equal(PlacementsForThreeCardsOnTable, match.CardsOnTable);
        }

        /// <summary>Checks PlaceCard_WithChallengeReady_ReturnsBetInProgress.</summary>
        [Fact]
        public void PlaceCard_WithChallengeReady_ReturnsBetInProgress()
        {
            // Arrange
            MatchState match = CreateReadyChallenge();

            // Act
            CardPlacementStatus status = match.PlaceCard(FirstPlayerId, CardKind.Safe);

            // Assert
            Assert.Equal(CardPlacementStatus.BetInProgress, status);
        }

        /// <summary>Checks RaiseBet_WithSuccessiveRaises_NeverGivesTurnToBetLeaderWhileOthersRemain.</summary>
        [Fact]
        public void RaiseBet_WithSuccessiveRaises_NeverGivesTurnToBetLeaderWhileOthersRemain()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            EmptyEveryHand(match);

            // Act
            match.OpenBet(FirstPlayerId, 1);
            AssertPlayerInTurnIsNotBetLeader(match);
            match.RaiseBet(SecondPlayerId, 2);
            AssertPlayerInTurnIsNotBetLeader(match);
            match.RaiseBet(ThirdPlayerId, 3);
            AssertPlayerInTurnIsNotBetLeader(match);
            match.RaiseBet(FirstPlayerId, 4);
            AssertPlayerInTurnIsNotBetLeader(match);
            match.WithdrawFromBet(SecondPlayerId);
            AssertPlayerInTurnIsNotBetLeader(match);
            match.WithdrawFromBet(ThirdPlayerId);

            // Assert
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
            Assert.Equal(FirstPlayerId, match.BetLeader.PlayerId);
            Assert.Equal(4, match.CurrentBet);
        }

        /// <summary>Checks WithdrawFromBet_WhenOnlyOneBettorRemains_PutsTurnOnTheBettor.</summary>
        [Fact]
        public void WithdrawFromBet_WhenOnlyOneBettorRemains_PutsTurnOnTheBettor()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, FourPlayers);
            PlaceSafeCards(match, PlacementsForEightCardsOnTable);
            match.OpenBet(FirstPlayerId, 2);
            match.WithdrawFromBet(SecondPlayerId);
            match.WithdrawFromBet(ThirdPlayerId);

            // Act
            match.WithdrawFromBet(FourthPlayerId);

            // Assert
            Assert.Equal(RoundPhase.ChallengeReady, match.Phase);
            Assert.Equal(FirstPlayerId, match.PlayerInTurn.PlayerId);
            Assert.Equal(2, match.CurrentBet);
        }

        /// <summary>Checks OpenBet_WithoutOwnCardPlacedInRound_ReturnsNoCardPlacedInRound.</summary>
        [Fact]
        public void OpenBet_WithoutOwnCardPlacedInRound_ReturnsNoCardPlacedInRound()
        {
            // Arrange: A and B placed a card. It is C's turn and C has placed none.
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, TwoPlacementsBeforeThirdPlayer);

            // Act
            BetActionStatus status = match.OpenBet(ThirdPlayerId, TwoCardBet);

            // Assert
            Assert.Equal(BetActionStatus.NoCardPlacedInRound, status);
            Assert.Equal(RoundPhase.PlacingCards, match.Phase);
            Assert.Equal(ThirdPlayerId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Checks OpenBet_AfterPlacingOwnCard_ReturnsAccepted.</summary>
        [Fact]
        public void OpenBet_AfterPlacingOwnCard_ReturnsAccepted()
        {
            // Arrange: C tries to open before placing a card, then places one and gets another turn.
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, TwoPlacementsBeforeThirdPlayer);
            match.OpenBet(ThirdPlayerId, TwoCardBet);
            PlaceSafeCards(match, ThreePlacementsUntilThirdPlayerAgain);

            // Act
            BetActionStatus status = match.OpenBet(ThirdPlayerId, TwoCardBet);

            // Assert
            Assert.Equal(BetActionStatus.Accepted, status);
        }

        private static MatchState CreateMatch(int cardsPerPlayerCount, int playerCount)
        {
            List<int> playerIds = Enumerable.Range(FirstPlayerId, playerCount).ToList();
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            MatchState match = new MatchState(modeRules, playerIds);

            return match;
        }

        private static MatchState CreateReadyChallenge()
        {
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceSafeCards(match, PlacementsForThreeCardsOnTable);
            match.OpenBet(FirstPlayerId, 2);
            match.WithdrawFromBet(SecondPlayerId);
            match.WithdrawFromBet(ThirdPlayerId);

            return match;
        }

        private static void PlaceSafeCards(MatchState match, int placementCount)
        {
            for (int placementIndex = 0; placementIndex < placementCount; placementIndex++)
            {
                match.PlaceCard(match.PlayerInTurn.PlayerId, CardKind.Safe);
            }
        }

        private static void PlaceFatalCats(MatchState match, int placementCount)
        {
            for (int placementIndex = 0; placementIndex < placementCount; placementIndex++)
            {
                match.PlaceCard(match.PlayerInTurn.PlayerId, CardKind.FatalCat);
            }
        }

        private static void EmptyEveryHand(MatchState match)
        {
            int playerCount = match.Players.Count;

            PlaceSafeCards(match, match.ModeRules.SafeCardCount * playerCount);
            PlaceFatalCats(match, match.ModeRules.FatalCatCount * playerCount);
        }

        private static void AssertPlayerInTurnIsNotBetLeader(MatchState match)
        {
            Assert.NotSame(match.BetLeader, match.PlayerInTurn);
        }
    }
}
