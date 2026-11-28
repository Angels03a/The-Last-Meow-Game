using System;
using System.Collections.Generic;
using TLMG.Domain.Repositories;
using TLMG.Services.Lobby;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Keeps match registrations and their completion local to one scenario.</summary>
    internal sealed class MatchHistoryStub : IGameHistoryRepository
    {
        private readonly Dictionary<Guid, int> _gameIds = new Dictionary<Guid, int>();
        private readonly HashSet<Guid> _finished = new HashSet<Guid>();
        private int _lastGameId;

        internal static MatchStartCoordinator CreateCoordinator(SessionRegistry registry)
        {
            MatchHistoryStub history = new MatchHistoryStub();
            registry.ConfigureMatchHistory(history);
            return new MatchStartCoordinator(registry, history);
        }

        public GameRegistrationResult RegisterStartedMatch(GameRegistration registration)
        {
            GameRegistrationResult result = new GameRegistrationResult
            {
                GameRegistrationStatus = GameRegistrationStatus.InvalidRegistration
            };
            if (registration != null && registration.MatchId != Guid.Empty)
            {
                if (_gameIds.TryGetValue(registration.MatchId, out int gameId))
                {
                    result.GameRegistrationStatus = GameRegistrationStatus.AlreadyRegistered;
                    result.GameId = gameId;
                }
                else
                {
                    _lastGameId++;
                    _gameIds.Add(registration.MatchId, _lastGameId);
                    result.GameRegistrationStatus = GameRegistrationStatus.Created;
                    result.GameId = _lastGameId;
                }
            }

            return result;
        }

        public GameFinalizationStatus FinalizeMatch(GameFinalization gameFinalization)
        {
            GameFinalizationStatus status = GameFinalizationStatus.InvalidFinalization;
            if (gameFinalization != null && _gameIds.ContainsKey(gameFinalization.MatchId))
            {
                status = GameFinalizationStatus.AlreadySaved;
                if (_finished.Add(gameFinalization.MatchId))
                {
                    status = GameFinalizationStatus.Saved;
                }
            }

            return status;
        }

        public bool AbandonUnpublishedMatch(Guid matchId)
        {
            return !_finished.Contains(matchId) && _gameIds.Remove(matchId);
        }

        public bool RecoverInterruptedMatches()
        {
            // Each scenario starts with a fresh history.
            return _gameIds.Count == 0;
        }
    }
}
