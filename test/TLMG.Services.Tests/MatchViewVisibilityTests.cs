using System.Linq;
using System.IO;
using System.Runtime.Serialization;

using Xunit;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Engine;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Pruebas de la vista privada de cada destinatario (Regla No. 2): lo público lo ven todos, lo oculto solo su
    /// dueño, y las banderas de acciones salen del estado del motor.
    /// </summary>
    public class MatchViewVisibilityTests
    {
        private const int First = MatchStateScenarios.FirstPlayerId;
        private const int Second = MatchStateScenarios.SecondPlayerId;
        private const int Third = MatchStateScenarios.ThirdPlayerId;
        private const int NoUserId = 0;
        private const int PromisedTwoCards = 2;
        private const int ChallengesToWin = 2;
        private const int CardsInHand = 4;
        private const int OneCard = 1;
        private const int ThreeCardsOnTable = 3;
        private const int UnknownUserId = 99;

        /// <summary>Verifica CreateMatchView_AtMatchStart_ShowsEmptyTableAndFirstPlayerTurn.</summary>
        [Fact]
        public void CreateMatchView_AtMatchStart_ShowsEmptyTableAndFirstPlayerTurn()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.CreateStarted();

            // Act
            MatchView view = ViewOf(matchState, First);

            // Assert
            Assert.Equal(NoUserId, view.BetLeaderUserId);
            Assert.Equal(NoUserId, view.WinnerUserId);
            Assert.Equal(ChallengesToWin, view.ChallengesToWin);
            Assert.Equal(0, view.CurrentBet);
            Assert.Equal(0, view.CardsOnTable);
            Assert.Null(view.Challenge);
            Assert.Null(view.Punishment);
            Assert.Equal(First, view.PlayerInTurnUserId);
            Assert.All(view.Players, player => Assert.Empty(player.RevealedPileCards));
        }

        /// <summary>Verifica CreateMatchView_AtMatchStart_OnlyTheFirstPlayerCanPlaceACard.</summary>
        [Fact]
        public void CreateMatchView_AtMatchStart_OnlyTheFirstPlayerCanPlaceACard()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.CreateStarted();

            // Act
            MatchView firstView = ViewOf(matchState, First);
            MatchView secondView = ViewOf(matchState, Second);

            // Assert
            Assert.True(firstView.CanPlaceCard);
            Assert.False(firstView.CanPlaceBet);
            Assert.False(secondView.CanPlaceCard);
        }

        /// <summary>Verifica CreateMatchView_AfterEachPlayerPlacedACard_AllowsOpeningUpToTableCards.</summary>
        [Fact]
        public void CreateMatchView_AfterEachPlayerPlacedACard_AllowsOpeningUpToTableCards()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.PlaceOneCardEach(CardKind.Safe);

            // Act
            MatchView view = ViewOf(matchState, First);

            // Assert
            Assert.True(view.CanPlaceBet);
            Assert.Equal(1, view.MinimumBetValue);
            Assert.Equal(ThreeCardsOnTable, view.MaximumBetValue);
            Assert.Equal(ThreeCardsOnTable, view.CardsOnTable);
        }

        /// <summary>Verifica CreateMatchView_WithEmptyHand_AllowsOnlyABetOfOne.</summary>
        [Fact]
        public void CreateMatchView_WithEmptyHand_AllowsOnlyABetOfOne()
        {
            // Arrange: el primer jugador coloca sus 4 cartas; los demás, una vuelta cada vez.
            MatchState matchState = MatchStateScenarios.CreateStarted();
            PlaceFullLaps(matchState);

            // Act
            MatchView view = ViewOf(matchState, First);

            // Assert
            Assert.False(view.CanPlaceCard);
            Assert.True(view.CanPlaceBet);
            Assert.Equal(1, view.MinimumBetValue);
            Assert.Equal(1, view.MaximumBetValue);
        }

        /// <summary>Verifica CreateMatchView_WhileBetting_ShowsBetToEveryoneAndRaiseRangeToTheOneInTurn.</summary>
        [Fact]
        public void CreateMatchView_WhileBetting_ShowsBetToEveryoneAndRaiseRangeToTheOneInTurn()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.PlaceOneCardEach(CardKind.Safe);
            matchState.OpenBet(First, 1);

            // Act
            MatchView inTurnView = ViewOf(matchState, Second);
            MatchView leaderView = ViewOf(matchState, First);

            // Assert
            Assert.Equal(MatchRoundPhase.Betting, inTurnView.Phase);
            Assert.Equal(1, inTurnView.CurrentBet);
            Assert.Equal(First, inTurnView.BetLeaderUserId);
            Assert.True(inTurnView.CanWithdrawFromBet);
            Assert.True(inTurnView.CanPlaceBet);
            Assert.Equal(PromisedTwoCards, inTurnView.MinimumBetValue);
            Assert.False(leaderView.CanWithdrawFromBet);
            Assert.False(leaderView.CanPlaceBet);
        }

        /// <summary>Verifica CreateMatchView_WhenSomeoneWithdraws_MarksOnlyThatPlayerAsWithdrawn.</summary>
        [Fact]
        public void CreateMatchView_WhenSomeoneWithdraws_MarksOnlyThatPlayerAsWithdrawn()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.PlaceOneCardEach(CardKind.Safe);
            matchState.OpenBet(First, 1);
            matchState.WithdrawFromBet(Second);

            // Act
            MatchView view = ViewOf(matchState, Third);

            // Assert
            Assert.Equal(new[] { Second }, view.Players.Where(player => player.HasWithdrawnFromBet)
                .Select(player => player.UserId).ToArray());
        }

        /// <summary>Verifica CreateMatchView_WithChallengeReady_OnlyTheBettorCanReveal.</summary>
        [Fact]
        public void CreateMatchView_WithChallengeReady_OnlyTheBettorCanReveal()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.ReachChallengeReady(CardKind.Safe);

            // Act
            MatchView bettorView = ViewOf(matchState, First);
            MatchView otherView = ViewOf(matchState, Second);

            // Assert
            Assert.Equal(MatchRoundPhase.ChallengeReady, bettorView.Phase);
            Assert.True(bettorView.CanRevealCard);
            Assert.False(otherView.CanRevealCard);
            Assert.Equal(First, otherView.PlayerInTurnUserId);
            Assert.False(bettorView.CanPlaceBet);
            Assert.False(bettorView.CanPlaceCard);
        }

        /// <summary>Verifica CreateMatchView_AfterAReveal_ShowsRevealedCardsToEveryoneButNotHiddenOnes.</summary>
        [Fact]
        public void CreateMatchView_AfterAReveal_ShowsRevealedCardsToEveryoneButNotHiddenOnes()
        {
            // Arrange: el primer jugador voltea su carta de arriba (segura); el Gato del segundo sigue oculto.
            MatchState matchState = MatchStateScenarios.ReachChallengeReady(CardKind.FatalCat);
            matchState.RevealCard(First, First, MatchStateScenarios.StartUtc);

            // Act
            MatchView otherView = ViewOf(matchState, Third);

            // Assert
            MatchPlayerView firstPlayer = otherView.Players.Single(player => player.UserId == First);
            MatchPlayerView secondPlayer = otherView.Players.Single(player => player.UserId == Second);
            Assert.Equal(new[] { MatchCardKind.Safe }, firstPlayer.RevealedPileCards);
            Assert.Empty(secondPlayer.RevealedPileCards);
            Assert.Equal(OneCard, secondPlayer.PileCardCount);
            Assert.Equal(new[] { MatchCardKind.Safe }, otherView.OwnPileCards);
        }

        /// <summary>Verifica CreateMatchView_WithBlindPunishment_ExposesNoCardKindsToTheChooserNorOthers.</summary>
        [Fact]
        public void CreateMatchView_WithBlindPunishment_ExposesNoCardKindsToTheChooserNorOthers()
        {
            // Arrange: el primero falla con el Gato del segundo; elige el segundo a ciegas entre 4 cartas.
            MatchState matchState = MatchStateScenarios.ReachBlindPunishment();

            // Act
            MatchView chooserView = ViewOf(matchState, Second);
            MatchView otherView = ViewOf(matchState, Third);

            // Assert
            Assert.Equal(MatchRoundPhase.PunishmentPending, chooserView.Phase);
            Assert.Equal(Second, chooserView.Punishment.ChooserUserId);
            Assert.Equal(First, chooserView.Punishment.PunishedUserId);
            Assert.True(chooserView.Punishment.IsBlindChoice);
            Assert.Equal(CardsInHand, chooserView.Punishment.CardSlotCount);
            Assert.True(chooserView.CanChoosePunishmentCard);
            Assert.False(otherView.CanChoosePunishmentCard);
            Assert.Equal(Second, otherView.PlayerInTurnUserId);
            Assert.Equal(CardsInHand, chooserView.OwnHandCards.Count);
            Assert.DoesNotContain(
                typeof(MatchPunishmentView).GetProperties(),
                property => property.PropertyType.IsGenericType || property.PropertyType == typeof(MatchCardKind));
        }

        /// <summary>Verifica CreateMatchView_WithBlindPunishment_PunishedPlayerSeesOnlyHisOwnHand.</summary>
        [Fact]
        public void CreateMatchView_WithBlindPunishment_PunishedPlayerSeesOnlyHisOwnHand()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.ReachBlindPunishment();

            // Act
            MatchView punishedView = ViewOf(matchState, First);
            MatchView chooserView = ViewOf(matchState, Second);

            // Assert: sus 4 cartas (volvieron a la mano); el que elige a ciegas no recibe ninguna de ellas.
            Assert.Equal(CardsInHand, punishedView.OwnHandCards.Count);
            Assert.False(punishedView.CanChoosePunishmentCard);
            Assert.Empty(chooserView.OwnPileCards);
            Assert.Equal(
                CardsInHand,
                chooserView.Players.Single(player => player.UserId == First).HandCardCount);
        }

        /// <summary>Verifica CreateMatchView_WithOwnPunishment_TheBettorChoosesSeeingHisHand.</summary>
        [Fact]
        public void CreateMatchView_WithOwnPunishment_TheBettorChoosesSeeingHisHand()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.ReachOwnPunishment();

            // Act
            MatchView bettorView = ViewOf(matchState, First);

            // Assert
            Assert.False(bettorView.Punishment.IsBlindChoice);
            Assert.Equal(First, bettorView.Punishment.ChooserUserId);
            Assert.True(bettorView.CanChoosePunishmentCard);
            Assert.Contains(MatchCardKind.FatalCat, bettorView.OwnHandCards);
        }

        /// <summary>Verifica CreateMatchView_AfterTwoSuccessfulChallenges_ShowsWinnerAndNoActions.</summary>
        [Fact]
        public void CreateMatchView_AfterTwoSuccessfulChallenges_ShowsWinnerAndNoActions()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.ReachMatchFinished();

            // Act
            MatchView winnerView = ViewOf(matchState, First);

            // Assert
            Assert.Equal(MatchRoundPhase.MatchFinished, winnerView.Phase);
            Assert.Equal(First, winnerView.WinnerUserId);
            Assert.Equal(NoUserId, winnerView.PlayerInTurnUserId);
            Assert.False(winnerView.CanPlaceCard);
            Assert.False(winnerView.CanPlaceBet);
            Assert.False(winnerView.CanRevealCard);
            Assert.Equal(
                ChallengesToWin,
                winnerView.Players.Single(player => player.UserId == First).SuccessfulChallengeCount);
        }

        /// <summary>Verifica CreateMatchView_ForAPlayerWhoLeft_ShowsHimAsLeftWithNoActions.</summary>
        [Fact]
        public void CreateMatchView_ForAPlayerWhoLeft_ShowsHimAsLeftWithNoActions()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.CreateStarted();
            matchState.RemovePlayer(
                Second,
                PlayerExit.Immediate(MatchStateScenarios.StartUtc),
                count => 0);

            // Act
            MatchView leaverView = ViewOf(matchState, Second);
            MatchView otherView = ViewOf(matchState, First);

            // Assert
            Assert.True(otherView.Players.Single(player => player.UserId == Second).HasLeftMatch);
            Assert.False(otherView.Players.Single(player => player.UserId == First).HasLeftMatch);
            Assert.False(leaverView.CanPlaceCard);
            Assert.False(leaverView.CanPlaceBet);
        }

        /// <summary>Verifica CreateMatchView_ForUnknownViewer_HasNoHandNorActions.</summary>
        [Fact]
        public void CreateMatchView_ForUnknownViewer_HasNoHandNorActions()
        {
            // Arrange
            MatchState matchState = MatchStateScenarios.PlaceOneCardEach(CardKind.Safe);

            // Act
            MatchView view = ViewOf(matchState, UnknownUserId);

            // Assert
            Assert.Empty(view.OwnHandCards);
            Assert.Empty(view.OwnPileCards);
            Assert.False(view.CanPlaceCard);
            Assert.False(view.CanPlaceBet);
            Assert.False(view.CanWithdrawFromBet);
        }

        /// <summary>Verifica CreateMatchView_WithChallengeRecord_ProjectsItWithoutExtraInformation.</summary>
        [Fact]
        public void CreateMatchView_WithChallengeRecord_ProjectsItWithoutExtraInformation()
        {
            // Arrange
            MatchChallengeRecord record = MatchChallengeRecord.Begin(First, PromisedTwoCards);
            record.RecordReveal(First, MatchCardKind.Safe);
            record.RecordReveal(Second, MatchCardKind.FatalCat);
            record.MarkFailed(Second, First, MatchPunishmentResolution.Pending);
            MatchState matchState = MatchStateScenarios.CreateStarted();

            // Act
            MatchChallengeView challenge = MatchViewHelper
                .CreateMatchView(MatchStateScenarios.CreateContext(matchState, record), Third).Challenge;

            // Assert
            Assert.Equal(First, challenge.BettorUserId);
            Assert.Equal(PromisedTwoCards, challenge.PromisedCount);
            Assert.Equal(MatchChallengeOutcome.Failed, challenge.Outcome);
            Assert.Equal(
                new[] { First, Second },
                challenge.RevealedCards.Select(card => card.PileOwnerUserId).ToArray());
            Assert.Equal(MatchCardKind.FatalCat, challenge.RevealedCards.Last().Card);
            Assert.Equal(Second, challenge.PunishmentChooserUserId);
            Assert.Equal(First, challenge.PunishedUserId);
            Assert.Equal(MatchPunishmentResolution.Pending, challenge.PunishmentResolution);
        }

        /// <summary>La tabla se proyecta desde el motor solo con identidades verificadas.</summary>
        [Fact]
        public void CreateMatchView_WithRegisteredParticipants_ProjectsFinalStandings()
        {
            MatchState match = MatchStateScenarios.ReachMatchFinished();
            MatchViewContext context = MatchStateScenarios.CreateContext(match, null, true);
            MatchView view = MatchViewHelper.CreateMatchView(context, First);
            FinalStandings expected = FinalStandings.CalculateFromMatch(match, new int[0]);

            Assert.Equal(expected.Standings.Count, view.FinalStandings.Count);
            Assert.Equal(First, view.WinnerUserId);
            for (int index = 0; index < expected.Standings.Count; index++)
            {
                Assert.Equal(expected.Standings[index].PlayerId, view.FinalStandings[index].UserId);
                Assert.Equal(expected.Standings[index].Position, view.FinalStandings[index].Position);
                Assert.Equal(expected.Standings[index].Points, view.FinalStandings[index].Points);
                Assert.False(string.IsNullOrWhiteSpace(view.FinalStandings[index].Nickname));
            }
        }

        /// <summary>Una identidad no verificada no se trata como cuenta registrada.</summary>
        [Fact]
        public void CreateMatchView_WithUnknownIdentity_DoesNotPublishStandings()
        {
            MatchState match = MatchStateScenarios.ReachMatchFinished();
            Assert.Null(ViewOf(match, First).FinalStandings);
        }

        /// <summary>Al quedar una persona activa, el motor asigna cero a quienes salieron.</summary>
        [Fact]
        public void CreateMatchView_LastActivePlayer_ProjectsWinnerAndDepartures()
        {
            MatchState match = MatchStateScenarios.CreateStarted();
            match.RemovePlayer(Second, PlayerExit.Immediate(MatchStateScenarios.StartUtc), count => 0);
            match.RemovePlayer(Third, PlayerExit.Immediate(MatchStateScenarios.StartUtc.AddSeconds(1)), count => 0);
            MatchViewContext context = MatchStateScenarios.CreateContext(match, null, true);
            MatchView view = MatchViewHelper.CreateMatchView(context, First);

            Assert.Equal(First, view.WinnerUserId);
            Assert.Equal(First, view.FinalStandings[0].UserId);
            Assert.All(view.FinalStandings.Where(row => row.UserId != First), row => Assert.Equal(0, row.Points));
        }

        /// <summary>El contrato conserva la diferencia entre tabla ausente y tabla vacía.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MatchViewSerialization_PreservesUnavailableAndEmptyStandings(bool available)
        {
            MatchView source = new MatchView
            {
                FinalStandings = available ? new System.Collections.Generic.List<MatchStandingView>() : null
            };
            DataContractSerializer serializer = new DataContractSerializer(typeof(MatchView));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, source);
                stream.Position = 0;
                MatchView result = (MatchView)serializer.ReadObject(stream);
                if (available)
                {
                    Assert.Empty(result.FinalStandings);
                }
                else
                {
                    Assert.Null(result.FinalStandings);
                }
            }
        }

        /// <summary>El resultado serializado solo contiene los datos públicos de las filas.</summary>
        [Fact]
        public void MatchStandingSerialization_ContainsOnlyPublicFields()
        {
            MatchStandingView standing = new MatchStandingView { UserId = First, Nickname = "Player", Position = 1, Points = 3 };
            DataContractSerializer serializer = new DataContractSerializer(typeof(MatchStandingView));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, standing);
                string xml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                Assert.Contains("Nickname", xml);
                Assert.Contains("Points", xml);
                Assert.DoesNotContain("Token", xml);
                Assert.DoesNotContain("Hand", xml);
                stream.Position = 0;
                MatchStandingView result = (MatchStandingView)serializer.ReadObject(stream);
                Assert.Equal(standing.UserId, result.UserId);
                Assert.Equal(standing.Position, result.Position);
                Assert.Equal(standing.Points, result.Points);
                Assert.Equal(standing.Nickname, result.Nickname);
            }
        }

        private static MatchView ViewOf(MatchState matchState, int viewerUserId)
        {
            MatchViewContext context = MatchStateScenarios.CreateContext(matchState, null);

            return MatchViewHelper.CreateMatchView(context, viewerUserId);
        }

        private static void PlaceFullLaps(MatchState matchState)
        {
            for (int lapNumber = 0; lapNumber < CardsInHand; lapNumber++)
            {
                CardKind card = lapNumber == CardsInHand - 1 ? CardKind.FatalCat : CardKind.Safe;
                matchState.PlaceCard(First, card);
                matchState.PlaceCard(Second, card);
                matchState.PlaceCard(Third, card);
            }
        }
    }
}
