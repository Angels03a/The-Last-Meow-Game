using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>Pruebas de MatchState: creación de la partida y colocación de una carta.</summary>
    public class MatchStateTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int EightCardsPerPlayer = 8;
        private const int FirstPlayerId = 1;
        private const int SecondPlayerId = 2;
        private const int ThirdPlayerId = 3;
        private const int UnknownPlayerId = 99;
        private const int ThreePlayers = 3;
        private const int SixPlayers = 6;
        private const CardKind UndefinedCardKind = (CardKind)99;
        private const string ModeRulesParameterName = "modeRules";
        private const string PlayersParameterName = "playerIdsInJoinOrder";

        /// <summary>Verifica Constructor_WithSupportedMode_DealsFatalCatsAndSafeCardsToEveryPlayer.</summary>
        [Theory]
        [InlineData(4, 1, 3)]
        [InlineData(6, 1, 5)]
        [InlineData(8, 2, 6)]
        public void Constructor_WithSupportedMode_DealsFatalCatsAndSafeCardsToEveryPlayer(
            int cardsPerPlayerCount,
            int expectedFatalCatCount,
            int expectedSafeCardCount)
        {
            // Act
            MatchState match = CreateMatch(cardsPerPlayerCount, ThreePlayers);

            // Assert
            Assert.All(match.Players, player => Assert.Equal(
                expectedFatalCatCount, player.HandCards.Count(card => card == CardKind.FatalCat)));
            Assert.All(match.Players, player => Assert.Equal(
                expectedSafeCardCount, player.HandCards.Count(card => card == CardKind.Safe)));
        }

        /// <summary>Verifica Constructor_WithSupportedMode_StartsWithLivesEqualToCardsPerPlayer.</summary>
        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        public void Constructor_WithSupportedMode_StartsWithLivesEqualToCardsPerPlayer(int cardsPerPlayerCount)
        {
            // Act
            MatchState match = CreateMatch(cardsPerPlayerCount, ThreePlayers);

            // Assert
            Assert.All(match.Players, player => Assert.Equal(cardsPerPlayerCount, player.Lives));
        }

        /// <summary>Verifica Constructor_WithValidPlayers_StartsWithEmptyPileAndNoChallenges.</summary>
        [Fact]
        public void Constructor_WithValidPlayers_StartsWithEmptyPileAndNoChallenges()
        {
            // Act
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Assert
            Assert.All(match.Players, player => Assert.Empty(player.PileCards));
            Assert.All(match.Players, player => Assert.Equal(0, player.SuccessfulChallengeCount));
        }

        /// <summary>Verifica Constructor_WithValidPlayers_FreezesTurnOrderAsJoinOrder.</summary>
        [Fact]
        public void Constructor_WithValidPlayers_FreezesTurnOrderAsJoinOrder()
        {
            // Arrange
            List<int> joinOrder = new List<int> { ThirdPlayerId, FirstPlayerId, SecondPlayerId };
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer);

            // Act
            MatchState match = new MatchState(modeRules, joinOrder);

            // Assert
            Assert.Equal(joinOrder, match.Players.Select(player => player.PlayerId));
        }

        /// <summary>Verifica Constructor_WithValidPlayers_StartsWithFirstPlayerActive.</summary>
        [Fact]
        public void Constructor_WithValidPlayers_StartsWithFirstPlayerActive()
        {
            // Act
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Assert
            Assert.Equal(FirstPlayerId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica Constructor_WithBoundaryPlayerCount_CreatesMatch.</summary>
        [Theory]
        [InlineData(3)]
        [InlineData(6)]
        public void Constructor_WithBoundaryPlayerCount_CreatesMatch(int playerCount)
        {
            // Act
            MatchState match = CreateMatch(FourCardsPerPlayer, playerCount);

            // Assert
            Assert.Equal(playerCount, match.Players.Count);
        }

        /// <summary>Verifica Constructor_WithTooFewPlayers_ThrowsArgumentOutOfRange.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Constructor_WithTooFewPlayers_ThrowsArgumentOutOfRange(int playerCount)
        {
            // Act
            Exception thrown = Record.Exception(() => CreateMatch(FourCardsPerPlayer, playerCount));

            // Assert
            Assert.IsType<ArgumentOutOfRangeException>(thrown);
        }

        /// <summary>Verifica Constructor_WithTooManyPlayers_ThrowsArgumentOutOfRange.</summary>
        [Theory]
        [InlineData(7)]
        [InlineData(10)]
        public void Constructor_WithTooManyPlayers_ThrowsArgumentOutOfRange(int playerCount)
        {
            // Act
            Exception thrown = Record.Exception(() => CreateMatch(FourCardsPerPlayer, playerCount));

            // Assert
            Assert.IsType<ArgumentOutOfRangeException>(thrown);
        }

        /// <summary>Verifica Constructor_WithDuplicatePlayerIds_ThrowsArgumentException.</summary>
        [Fact]
        public void Constructor_WithDuplicatePlayerIds_ThrowsArgumentException()
        {
            // Arrange
            List<int> playerIds = new List<int> { FirstPlayerId, SecondPlayerId, FirstPlayerId };

            // Act
            Exception thrown = Record.Exception(
                () => new MatchState(MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer), playerIds));

            // Assert
            Assert.IsType<ArgumentException>(thrown);
        }

        /// <summary>Verifica Constructor_WithNullModeRules_ThrowsArgumentNullException.</summary>
        [Fact]
        public void Constructor_WithNullModeRules_ThrowsArgumentNullException()
        {
            // Act
            Exception thrown = Record.Exception(() => new MatchState(null, new List<int> { 1, 2, 3 }));

            // Assert
            Assert.IsType<ArgumentNullException>(thrown);
        }

        /// <summary>Verifica Constructor_WithNullPlayers_ThrowsArgumentNullException.</summary>
        [Fact]
        public void Constructor_WithNullPlayers_ThrowsArgumentNullException()
        {
            // Act
            Exception thrown = Record.Exception(
                () => new MatchState(MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer), null));

            // Assert
            Assert.IsType<ArgumentNullException>(thrown);
        }

        /// <summary>Verifica PlaceCard_WithSafeCardOnTurn_ReturnsPlacedAndMovesCardToPile.</summary>
        [Fact]
        public void PlaceCard_WithSafeCardOnTurn_ReturnsPlacedAndMovesCardToPile()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            CardPlacementStatus status = match.PlaceCard(FirstPlayerId, CardKind.Safe);

            // Assert
            Assert.Equal(CardPlacementStatus.Placed, status);
            Assert.Equal(3, match.Players[0].HandCards.Count);
            Assert.Equal(new[] { CardKind.Safe }, match.Players[0].PileCards);
        }

        /// <summary>Verifica PlaceCard_WithFatalCatOnTurn_LeavesCatOnTopOfPile.</summary>
        [Fact]
        public void PlaceCard_WithFatalCatOnTurn_LeavesCatOnTopOfPile()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            CardPlacementStatus status = match.PlaceCard(FirstPlayerId, CardKind.FatalCat);

            // Assert
            Assert.Equal(CardPlacementStatus.Placed, status);
            Assert.Equal(0, match.Players[0].HandCards.Count(card => card == CardKind.FatalCat));
            Assert.Equal(new[] { CardKind.FatalCat }, match.Players[0].PileCards);
        }

        /// <summary>Verifica PlaceCard_OnTurn_DoesNotChangeLives.</summary>
        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        public void PlaceCard_OnTurn_DoesNotChangeLives(int cardsPerPlayerCount)
        {
            // Arrange
            MatchState match = CreateMatch(cardsPerPlayerCount, ThreePlayers);

            // Act
            match.PlaceCard(FirstPlayerId, CardKind.Safe);

            // Assert
            Assert.Equal(cardsPerPlayerCount, match.Players[0].Lives);
        }

        /// <summary>Verifica PlaceCard_OnTurn_PassesTurnToNextPlayer.</summary>
        [Fact]
        public void PlaceCard_OnTurn_PassesTurnToNextPlayer()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            match.PlaceCard(FirstPlayerId, CardKind.Safe);

            // Assert
            Assert.Equal(SecondPlayerId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica PlaceCard_ByLastPlayerOfOrder_PassesTurnToFirstPlayer.</summary>
        [Fact]
        public void PlaceCard_ByLastPlayerOfOrder_PassesTurnToFirstPlayer()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            PlaceRound(match, CardKind.Safe);

            // Assert
            Assert.Equal(FirstPlayerId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica PlaceCard_WithSixPlayers_PassesTurnInJoinOrder.</summary>
        [Fact]
        public void PlaceCard_WithSixPlayers_PassesTurnInJoinOrder()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, SixPlayers);

            // Act
            PlaceRound(match, CardKind.Safe);

            // Assert
            Assert.All(match.Players, player => Assert.Equal(1, player.PileCards.Count));
            Assert.Equal(FirstPlayerId, match.PlayerInTurn.PlayerId);
        }

        /// <summary>Verifica PlaceCard_WithSecondCard_KeepsLatestCardOnTop.</summary>
        [Fact]
        public void PlaceCard_WithSecondCard_KeepsLatestCardOnTop()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceRound(match, CardKind.Safe);

            // Act
            match.PlaceCard(FirstPlayerId, CardKind.FatalCat);

            // Assert
            Assert.Equal(new[] { CardKind.Safe, CardKind.FatalCat }, match.Players[0].PileCards);
        }

        /// <summary>Verifica PlaceCard_ByPlayerOutOfTurn_ReturnsNotPlayersTurn.</summary>
        [Fact]
        public void PlaceCard_ByPlayerOutOfTurn_ReturnsNotPlayersTurn()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            CardPlacementStatus status = match.PlaceCard(SecondPlayerId, CardKind.Safe);

            // Assert
            Assert.Equal(CardPlacementStatus.NotPlayersTurn, status);
        }

        /// <summary>Verifica PlaceCard_ByPlayerOutOfTurn_KeepsStateUnchanged.</summary>
        [Fact]
        public void PlaceCard_ByPlayerOutOfTurn_KeepsStateUnchanged()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            match.PlaceCard(SecondPlayerId, CardKind.Safe);

            // Assert
            Assert.Equal(FirstPlayerId, match.PlayerInTurn.PlayerId);
            Assert.Equal(FourCardsPerPlayer, match.Players[1].HandCards.Count);
            Assert.Empty(match.Players[1].PileCards);
        }

        /// <summary>Verifica PlaceCard_ByUnknownPlayer_ReturnsPlayerNotInMatch.</summary>
        [Fact]
        public void PlaceCard_ByUnknownPlayer_ReturnsPlayerNotInMatch()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            CardPlacementStatus status = match.PlaceCard(UnknownPlayerId, CardKind.Safe);

            // Assert
            Assert.Equal(CardPlacementStatus.PlayerNotInMatch, status);
        }

        /// <summary>Verifica PlaceCard_WithEmptyHand_ReturnsHandEmpty.</summary>
        [Fact]
        public void PlaceCard_WithEmptyHand_ReturnsHandEmpty()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            EmptyEveryHand(match);

            // Act
            CardPlacementStatus status = match.PlaceCard(FirstPlayerId, CardKind.Safe);

            // Assert
            Assert.Equal(CardPlacementStatus.HandEmpty, status);
        }

        /// <summary>Verifica PlaceCard_WithEmptyHand_KeepsLivesInThePile.</summary>
        [Fact]
        public void PlaceCard_WithEmptyHand_KeepsLivesInThePile()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            EmptyEveryHand(match);

            // Assert
            Assert.Equal(0, match.Players[0].HandCards.Count);
            Assert.Equal(FourCardsPerPlayer, match.Players[0].Lives);
        }

        /// <summary>Verifica PlaceCard_WithFatalCatAlreadyPlaced_ReturnsCardNotInHand.</summary>
        [Fact]
        public void PlaceCard_WithFatalCatAlreadyPlaced_ReturnsCardNotInHand()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);
            PlaceRound(match, CardKind.FatalCat);

            // Act
            CardPlacementStatus status = match.PlaceCard(FirstPlayerId, CardKind.FatalCat);

            // Assert
            Assert.Equal(CardPlacementStatus.CardNotInHand, status);
        }

        /// <summary>Verifica PlaceCard_WithEightCards_PlacesBothFatalCats.</summary>
        [Fact]
        public void PlaceCard_WithEightCards_PlacesBothFatalCats()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer, ThreePlayers);

            // Act
            PlaceRound(match, CardKind.FatalCat);
            PlaceRound(match, CardKind.FatalCat);

            // Assert
            Assert.Equal(new[] { CardKind.FatalCat, CardKind.FatalCat }, match.Players[0].PileCards);
        }

        /// <summary>Verifica PlaceCard_WithEightCardsAndTwoCatsPlaced_ReturnsCardNotInHandForThird.</summary>
        [Fact]
        public void PlaceCard_WithEightCardsAndTwoCatsPlaced_ReturnsCardNotInHandForThird()
        {
            // Arrange
            MatchState match = CreateMatch(EightCardsPerPlayer, ThreePlayers);
            PlaceRound(match, CardKind.FatalCat);
            PlaceRound(match, CardKind.FatalCat);

            // Act
            CardPlacementStatus status = match.PlaceCard(FirstPlayerId, CardKind.FatalCat);

            // Assert
            Assert.Equal(CardPlacementStatus.CardNotInHand, status);
        }

        /// <summary>Verifica PlaceCard_WithUndefinedCardKind_ReturnsCardNotInHand.</summary>
        [Fact]
        public void PlaceCard_WithUndefinedCardKind_ReturnsCardNotInHand()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            CardPlacementStatus status = match.PlaceCard(FirstPlayerId, UndefinedCardKind);

            // Assert
            Assert.Equal(CardPlacementStatus.CardNotInHand, status);
        }

        /// <summary>Verifica PlaceCard_WhenCardIsNotInHand_KeepsTurnWithSamePlayer.</summary>
        [Fact]
        public void PlaceCard_WhenCardIsNotInHand_KeepsTurnWithSamePlayer()
        {
            // Arrange
            MatchState match = CreateMatch(FourCardsPerPlayer, ThreePlayers);

            // Act
            match.PlaceCard(FirstPlayerId, UndefinedCardKind);

            // Assert
            Assert.Equal(FirstPlayerId, match.PlayerInTurn.PlayerId);
            Assert.Equal(FourCardsPerPlayer, match.Players[0].HandCards.Count);
        }

        /// <summary>Verifica Constructor_WithBothArgumentsNull_ReportsModeRulesFirst.</summary>
        [Fact]
        public void Constructor_WithBothArgumentsNull_ReportsModeRulesFirst()
        {
            // Act
            Exception thrown = Record.Exception(() => new MatchState(null, null));

            // Assert
            ArgumentNullException nullException = Assert.IsType<ArgumentNullException>(thrown);
            Assert.Equal(ModeRulesParameterName, nullException.ParamName);
        }

        /// <summary>Verifica Constructor_WithTooFewPlayersThatRepeat_ReportsCountBeforeDuplicates.</summary>
        [Fact]
        public void Constructor_WithTooFewPlayersThatRepeat_ReportsCountBeforeDuplicates()
        {
            // Arrange
            List<int> playerIds = new List<int> { FirstPlayerId, FirstPlayerId };
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer);

            // Act
            Exception thrown = Record.Exception(() => new MatchState(modeRules, playerIds));

            // Assert
            Assert.IsType<ArgumentOutOfRangeException>(thrown);
        }

        /// <summary>Verifica Constructor_WithDuplicatePlayerIds_ReportsPlayersParameter.</summary>
        [Fact]
        public void Constructor_WithDuplicatePlayerIds_ReportsPlayersParameter()
        {
            // Arrange
            List<int> playerIds = new List<int> { FirstPlayerId, SecondPlayerId, FirstPlayerId };
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer);

            // Act
            Exception thrown = Record.Exception(() => new MatchState(modeRules, playerIds));

            // Assert
            ArgumentException duplicateException = Assert.IsType<ArgumentException>(thrown);
            Assert.Equal(PlayersParameterName, duplicateException.ParamName);
        }

        /// <summary>Verifica ModeRules_Always_HasNoPublicSetter.</summary>
        [Fact]
        public void ModeRules_Always_HasNoPublicSetter()
        {
            // Arrange
            PropertyInfo modeRulesProperty = typeof(MatchState).GetProperty(nameof(MatchState.ModeRules));

            // Act
            MethodInfo publicSetter = modeRulesProperty.GetSetMethod(false);

            // Assert
            Assert.Null(publicSetter);
        }

        /// <summary>Verifica PublicMethods_Always_DoNotAcceptModeRules.</summary>
        [Fact]
        public void PublicMethods_Always_DoNotAcceptModeRules()
        {
            // Arrange
            MethodInfo[] publicMethods = typeof(MatchState).GetMethods(BindingFlags.Instance | BindingFlags.Public);

            // Act
            IEnumerable<MethodInfo> methodsThatAcceptModeRules = publicMethods.Where(
                method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(MatchModeRules)));

            // Assert
            Assert.Empty(methodsThatAcceptModeRules);
        }

        private static MatchState CreateMatch(int cardsPerPlayerCount, int playerCount)
        {
            List<int> playerIds = Enumerable.Range(FirstPlayerId, playerCount).ToList();
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            MatchState match = new MatchState(modeRules, playerIds);

            return match;
        }

        private static void PlaceRound(MatchState match, CardKind card)
        {
            foreach (MatchPlayer player in match.Players.ToList())
            {
                match.PlaceCard(player.PlayerId, card);
            }
        }

        private static void EmptyEveryHand(MatchState match)
        {
            int safeCardCount = match.ModeRules.SafeCardCount;
            int fatalCatCount = match.ModeRules.FatalCatCount;

            for (int safeRoundIndex = 0; safeRoundIndex < safeCardCount; safeRoundIndex++)
            {
                PlaceRound(match, CardKind.Safe);
            }

            for (int fatalRoundIndex = 0; fatalRoundIndex < fatalCatCount; fatalRoundIndex++)
            {
                PlaceRound(match, CardKind.FatalCat);
            }
        }
    }
}
