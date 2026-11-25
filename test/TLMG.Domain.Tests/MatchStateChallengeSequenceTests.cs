using System;
using System.Linq;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests the last successful challenge sequence. Each success takes the next number. Failure leaves it unchanged.
    /// An earlier success at the same count breaks a tie (Rule 5.0.4).
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

        /// <summary>Checks MatchState_WhenCreated_HasNoSuccessSequenceForAnyPlayer.</summary>
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

        /// <summary>Checks RevealCard_WhenChallengeSucceeds_AssignsNextSuccessSequence.</summary>
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

        /// <summary>Checks RevealCard_WhenSamePlayerSucceedsAgain_KeepsOnlyLastSequence.</summary>
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

        /// <summary>Checks RevealCard_WhenChallengeFails_KeepsBettorSequence.</summary>
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

        /// <summary>Checks RevealCard_WhenChallengeFails_DoesNotConsumeSequence.</summary>
        [Fact]
        public void RevealCard_WhenChallengeFails_DoesNotConsumeSequence()
        {
            // Arrange: A failed challenge between two successes uses no sequence number.
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

        /// <summary>The current player opens and all others fold, leaving the opener as the final bidder.</summary>
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
        /// Others place safe cards until the player's turn. That player places the given card, waits another cycle and
        /// bids 1 on their own pile. A safe card wins and a Fatal Cat fails.
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

        /// <summary>
        /// Fails on the player's own Fatal Cat and loses a safe card, or the first card if no safe card remains.
        /// </summary>
        private static void LoseCardOnOwnCat(MatchState match, int playerId)
        {
            BetOneOnOwnTopCard(match, playerId, FatalCatCard);
            int safeCardIndex = GetPlayer(match, playerId).HandCards.ToList().IndexOf(SafeCard);
            match.ChoosePunishmentCard(playerId, Math.Max(safeCardIndex, FirstCardIndex), _nowUtc);
        }
    }
}
