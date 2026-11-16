using System;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Pruebas de la secuencia del último reto exitoso: cada éxito de la partida toma la siguiente y un reto fallido
    /// no la cambia. Sirve para desempatar a quien alcanzó antes su número de retos (Reglas 5.0.4).
    /// </summary>
    public class MatchStateChallengeSequenceTests
    {
        private const int FourCardsPerPlayer = 4;
        private const int SixCardsPerPlayer = 6;
        private const int PlayerAId = 1;
        private const int PlayerBId = 2;
        private const int PlayerCId = 3;
        private const int OneCardBet = 1;
        private const int FirstCardIndex = 0;
        private const int NoSequence = 0;
        private const int FirstSequence = 1;
        private const int SecondSequence = 2;
        private const int ThirdSequence = 3;
        private const int OneChallenge = 1;
        private const CardKind SafeCard = CardKind.Safe;
        private const CardKind FatalCatCard = CardKind.FatalCat;

        private static readonly DateTime _nowUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>Verifica MatchState_WhenCreated_HasNoSuccessSequenceForAnyPlayer.</summary>
        [Fact]
        public void MatchState_WhenCreated_HasNoSuccessSequenceForAnyPlayer()
        {
            // Arrange
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(FourCardsPerPlayer);

            // Act
            MatchState match = new MatchState(modeRules, new[] { PlayerAId, PlayerBId, PlayerCId });

            // Assert
            Assert.All(match.Players, player => Assert.Equal(NoSequence, player.LastSuccessfulChallengeSequence));
        }

        /// <summary>Verifica RevealCard_WhenChallengeSucceeds_AssignsNextSuccessSequence.</summary>
        [Fact]
        public void RevealCard_WhenChallengeSucceeds_AssignsNextSuccessSequence()
        {
            // Arrange
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            WinChallenge(match, PlayerCId);

            // Act
            WinChallenge(match, PlayerBId);

            // Assert
            Assert.Equal(FirstSequence, GetPlayer(match, PlayerCId).LastSuccessfulChallengeSequence);
            Assert.Equal(SecondSequence, GetPlayer(match, PlayerBId).LastSuccessfulChallengeSequence);
            Assert.Equal(NoSequence, GetPlayer(match, PlayerAId).LastSuccessfulChallengeSequence);
        }

        /// <summary>Verifica RevealCard_WhenSamePlayerSucceedsAgain_KeepsOnlyLastSequence.</summary>
        [Fact]
        public void RevealCard_WhenSamePlayerSucceedsAgain_KeepsOnlyLastSequence()
        {
            // Arrange
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            WinChallenge(match, PlayerCId);
            WinChallenge(match, PlayerBId);

            // Act
            WinChallenge(match, PlayerCId);

            // Assert
            Assert.Equal(ThirdSequence, GetPlayer(match, PlayerCId).LastSuccessfulChallengeSequence);
            Assert.Equal(SecondSequence, GetPlayer(match, PlayerBId).LastSuccessfulChallengeSequence);
        }

        /// <summary>Verifica RevealCard_WhenChallengeFails_KeepsBettorSequence.</summary>
        [Fact]
        public void RevealCard_WhenChallengeFails_KeepsBettorSequence()
        {
            // Arrange
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            WinChallenge(match, PlayerCId);

            // Act
            LoseCardOnOwnCat(match, PlayerCId);

            // Assert
            Assert.Equal(FirstSequence, GetPlayer(match, PlayerCId).LastSuccessfulChallengeSequence);
            Assert.Equal(OneChallenge, GetPlayer(match, PlayerCId).SuccessfulChallengeCount);
        }

        /// <summary>Verifica RevealCard_WhenChallengeFails_DoesNotConsumeSequence.</summary>
        [Fact]
        public void RevealCard_WhenChallengeFails_DoesNotConsumeSequence()
        {
            // Arrange: un reto fallido entre dos éxitos no gasta secuencia.
            MatchState match = CreateMatch(SixCardsPerPlayer, PlayerAId, PlayerBId, PlayerCId);
            WinChallenge(match, PlayerCId);
            LoseCardOnOwnCat(match, PlayerAId);

            // Act
            WinChallenge(match, PlayerBId);

            // Assert
            Assert.Equal(SecondSequence, GetPlayer(match, PlayerBId).LastSuccessfulChallengeSequence);
            Assert.Equal(NoSequence, GetPlayer(match, PlayerAId).LastSuccessfulChallengeSequence);
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

            while (match.Phase == RoundPhase.Betting)
            {
                match.WithdrawFromBet(match.PlayerInTurn.PlayerId);
            }
        }

        private static void PlaceSafeCardsUntilTurnOf(MatchState match, int playerId)
        {
            while (match.PlayerInTurn.PlayerId != playerId)
            {
                match.PlaceCard(match.PlayerInTurn.PlayerId, SafeCard);
            }
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
    }
}
