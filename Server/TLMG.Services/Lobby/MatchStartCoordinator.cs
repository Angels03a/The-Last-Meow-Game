using System;
using System.Collections.Generic;

using log4net;

using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    public sealed class MatchStartCoordinator : IMatchStartCoordinator
    {
        private const int EmptyCount = 0;
        private static readonly ILog _log = LogManager.GetLogger(typeof(MatchStartCoordinator));
        private readonly IMatchStartRegistry _matchStartRegistry;
        private readonly IGameHistoryRepository _gameHistoryRepository;
        private readonly object _matchStartLock = new object();
        private bool _isRecoveryComplete;

        public MatchStartCoordinator(
            IMatchStartRegistry matchStartRegistry,
            IGameHistoryRepository gameHistoryRepository)
        {
            _matchStartRegistry = matchStartRegistry;
            _gameHistoryRepository = gameHistoryRepository;
        }

        public bool RecoverInterruptedMatches()
        {
            bool wasRecoverySuccessful;

            lock (_matchStartLock)
            {
                wasRecoverySuccessful = RecoverInterruptedMatchesInLock();
            }

            return wasRecoverySuccessful;
        }

        public LobbyMembershipChange RequestMatchStart(
            LobbyCaller caller, int roomId, long countdownId)
        {
            LobbyMembershipChange lobbyMembershipChange;

            lock (_matchStartLock)
            {
                MatchStartPreparation matchStartPreparation =
                    _matchStartRegistry.PrepareMatchStart(caller, roomId, countdownId);
                lobbyMembershipChange = matchStartPreparation.LobbyMembershipChange;

                if (matchStartPreparation.GameRegistration != null)
                {
                    GameRegistrationResult registrationResult = new GameRegistrationResult
                    {
                        GameRegistrationStatus = GameRegistrationStatus.DataFailure
                    };

                    if (RecoverInterruptedMatchesInLock())
                    {
                        ReconcileCancelledStartsInLock();
                        registrationResult = _gameHistoryRepository.RegisterStartedMatch(matchStartPreparation.GameRegistration);
                    }

                    lobbyMembershipChange = _matchStartRegistry.CompleteMatchStart(caller, matchStartPreparation, registrationResult);

                    if (lobbyMembershipChange.RecipientCallbacks.Count == EmptyCount)
                    {
                        ReconcileCancelledStartsInLock();
                    }
                }
                else if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
                {
                    ReconcileCancelledStartsInLock();
                }
            }

            return lobbyMembershipChange;
        }

        /// <summary>Reconciles cancelled reservations without racing their database registrations.</summary>
        public void ReconcileCancelledStarts()
        {
            if (_matchStartRegistry.FindCancelledMatchIds().Count != EmptyCount)
            {
                lock (_matchStartLock)
                {
                    ReconcileCancelledStartsInLock();
                }
            }
        }

        private bool RecoverInterruptedMatchesInLock()
        {
            if (!_isRecoveryComplete)
            {
                _isRecoveryComplete = _gameHistoryRepository.RecoverInterruptedMatches();
            }

            return _isRecoveryComplete;
        }

        private void ReconcileCancelledStartsInLock()
        {
            IReadOnlyList<Guid> cancelledMatchIds =
                _matchStartRegistry.FindCancelledMatchIds();

            foreach (Guid matchId in cancelledMatchIds)
            {
                if (_gameHistoryRepository.AbandonUnpublishedMatch(matchId))
                {
                    _matchStartRegistry.ConfirmCancelledMatch(matchId);
                }
                else
                {
                    _log.WarnFormat("Could not reconcile cancelled match {0}.", matchId);
                    break;
                }
            }
        }
    }
}
