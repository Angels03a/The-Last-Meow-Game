using System;
using System.Collections.Generic;

using TLMG.Domain.Engine;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Builds known states with a real four-card MatchState and players 1, 2 and 3 in turn order. Tests views without
    /// the registry. The starting hand has the Fatal Cat first.
    /// </summary>
    internal static class MatchStateScenarios
    {
        internal const int FirstPlayerId = 1;
        internal const int SecondPlayerId = 2;
        internal const int ThirdPlayerId = 3;
        internal const int CardsPerPlayerCount = 4;

        private const int OneCardBet = 1;
        private const int TwoCardBet = 2;
        private const int RoundsToWin = 2;
        private const string NicknamePrefix = "Jugador";

        internal static readonly DateTime StartUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        internal static MatchState CreateStarted()
        {
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(CardsPerPlayerCount);

            return new MatchState(modeRules, new List<int> { FirstPlayerId, SecondPlayerId, ThirdPlayerId });
        }

        internal static MatchViewContext CreateContext(MatchState matchState, MatchChallengeRecord lastChallenge,
            IReadOnlyList<int> guestUserIds = null)
        {
            Dictionary<int, string> nicknames = new Dictionary<int, string>
            {
                { FirstPlayerId, NicknamePrefix + FirstPlayerId },
                { SecondPlayerId, NicknamePrefix + SecondPlayerId },
                { ThirdPlayerId, NicknamePrefix + ThirdPlayerId }
            };

            return new MatchViewContext(Guid.NewGuid(), 1, matchState, nicknames, lastChallenge, guestUserIds);
        }

        /// <summary>Each player places a card in turn. The test chooses the second player's card.</summary>
        internal static MatchState PlaceOneCardEach(CardKind secondPlayerCard)
        {
            MatchState matchState = CreateStarted();
            matchState.PlaceCard(FirstPlayerId, CardKind.Safe);
            matchState.PlaceCard(SecondPlayerId, secondPlayerCard);
            matchState.PlaceCard(ThirdPlayerId, CardKind.Safe);

            return matchState;
        }

        /// <summary>The first player opens at 2 and both others fold, leaving the challenge ready.</summary>
        internal static MatchState ReachChallengeReady(CardKind secondPlayerCard)
        {
            MatchState matchState = PlaceOneCardEach(secondPlayerCard);
            matchState.OpenBet(FirstPlayerId, TwoCardBet);
            matchState.WithdrawFromBet(SecondPlayerId);
            matchState.WithdrawFromBet(ThirdPlayerId);

            return matchState;
        }

        /// <summary>
        /// The first player fails on the second player's Fatal Cat. The second player chooses blindly.
        /// </summary>
        internal static MatchState ReachBlindPunishment()
        {
            MatchState matchState = ReachChallengeReady(CardKind.FatalCat);
            matchState.RevealCard(FirstPlayerId, FirstPlayerId, StartUtc);
            matchState.RevealCard(FirstPlayerId, SecondPlayerId, StartUtc);

            return matchState;
        }

        /// <summary>The first player fails on their own Fatal Cat and sees their cards when choosing.</summary>
        internal static MatchState ReachOwnPunishment()
        {
            MatchState matchState = CreateStarted();
            matchState.PlaceCard(FirstPlayerId, CardKind.FatalCat);
            matchState.PlaceCard(SecondPlayerId, CardKind.Safe);
            matchState.PlaceCard(ThirdPlayerId, CardKind.Safe);
            matchState.OpenBet(FirstPlayerId, OneCardBet);
            matchState.WithdrawFromBet(SecondPlayerId);
            matchState.WithdrawFromBet(ThirdPlayerId);
            matchState.RevealCard(FirstPlayerId, FirstPlayerId, StartUtc);

            return matchState;
        }

        /// <summary>The first player wins two one-card challenges and wins the match.</summary>
        internal static MatchState ReachMatchFinished()
        {
            MatchState matchState = CreateStarted();

            for (int roundNumber = 0; roundNumber < RoundsToWin; roundNumber++)
            {
                matchState.PlaceCard(FirstPlayerId, CardKind.Safe);
                matchState.PlaceCard(SecondPlayerId, CardKind.Safe);
                matchState.PlaceCard(ThirdPlayerId, CardKind.Safe);
                matchState.OpenBet(FirstPlayerId, OneCardBet);
                matchState.WithdrawFromBet(SecondPlayerId);
                matchState.WithdrawFromBet(ThirdPlayerId);
                matchState.RevealCard(FirstPlayerId, FirstPlayerId, StartUtc);
            }

            return matchState;
        }
    }
}