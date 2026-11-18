using System;
using System.Collections.Generic;

using TLMG.Domain.Engine;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Lleva un MatchState real a situaciones conocidas de la partida (modalidad de 4 cartas, jugadores 1, 2 y 3 en
    /// ese orden) para probar las vistas sin pasar por el registro. La mano inicial trae el Gato primero.
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

        internal static MatchViewContext CreateContext(MatchState matchState, MatchChallengeRecord lastChallenge)
        {
            Dictionary<int, string> nicknames = new Dictionary<int, string>
            {
                { FirstPlayerId, NicknamePrefix + FirstPlayerId },
                { SecondPlayerId, NicknamePrefix + SecondPlayerId },
                { ThirdPlayerId, NicknamePrefix + ThirdPlayerId }
            };

            return new MatchViewContext(Guid.NewGuid(), 1, matchState, nicknames, lastChallenge);
        }

        /// <summary>Cada jugador coloca una carta en su turno; la del segundo la elige la prueba.</summary>
        internal static MatchState PlaceOneCardEach(CardKind secondPlayerCard)
        {
            MatchState matchState = CreateStarted();
            matchState.PlaceCard(FirstPlayerId, CardKind.Safe);
            matchState.PlaceCard(SecondPlayerId, secondPlayerCard);
            matchState.PlaceCard(ThirdPlayerId, CardKind.Safe);

            return matchState;
        }

        /// <summary>El primero abre una apuesta de 2 y los otros dos se retiran: el reto queda listo.</summary>
        internal static MatchState ReachChallengeReady(CardKind secondPlayerCard)
        {
            MatchState matchState = PlaceOneCardEach(secondPlayerCard);
            matchState.OpenBet(FirstPlayerId, TwoCardBet);
            matchState.WithdrawFromBet(SecondPlayerId);
            matchState.WithdrawFromBet(ThirdPlayerId);

            return matchState;
        }

        /// <summary>El primero falla con el Gato del segundo: castigo a ciegas, elige el segundo.</summary>
        internal static MatchState ReachBlindPunishment()
        {
            MatchState matchState = ReachChallengeReady(CardKind.FatalCat);
            matchState.RevealCard(FirstPlayerId, FirstPlayerId, StartUtc);
            matchState.RevealCard(FirstPlayerId, SecondPlayerId, StartUtc);

            return matchState;
        }

        /// <summary>El primero falla con su propio Gato: castigo propio, elige él viendo sus cartas.</summary>
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

        /// <summary>El primer jugador cumple dos retos de una carta: gana la partida.</summary>
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
