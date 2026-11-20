using System;
using System.Collections.Generic;

using TLMG.Domain.Engine;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Immutable snapshot of an authorized match taken under _sessionsLock, without MatchState references.
    /// </summary>
    internal sealed class MatchSnapshot
    {
        /// <summary>Creates the snapshot.</summary>
        /// <param name="matchId">Match ID.</param>
        /// <param name="roomId">Room that authorized the match.</param>
        /// <param name="participantUserIds">Participants in entry order, already fixed.</param>
        /// <param name="cardsPerPlayerCount">Cards per player in the engine's mode.</param>
        /// <param name="roundNumber">Current engine round.</param>
        /// <param name="phase">Current engine phase.</param>
        /// <param name="playerInTurnUserId">Current player.</param>
        internal MatchSnapshot(
            Guid matchId,
            int roomId,
            IReadOnlyList<int> participantUserIds,
            int cardsPerPlayerCount,
            int roundNumber,
            RoundPhase phase,
            int playerInTurnUserId)
        {
            MatchId = matchId;
            RoomId = roomId;
            ParticipantUserIds = participantUserIds;
            CardsPerPlayerCount = cardsPerPlayerCount;
            RoundNumber = roundNumber;
            Phase = phase;
            PlayerInTurnUserId = playerInTurnUserId;
        }

        /// <summary>Match ID.</summary>
        internal Guid MatchId { get; }

        /// <summary>Room that authorized the match.</summary>
        internal int RoomId { get; }

        /// <summary>Participants in room entry order.</summary>
        internal IReadOnlyList<int> ParticipantUserIds { get; }

        /// <summary>Cards per player for the mode used to create the engine.</summary>
        internal int CardsPerPlayerCount { get; }

        /// <summary>Current round.</summary>
        internal int RoundNumber { get; }

        /// <summary>Current phase.</summary>
        internal RoundPhase Phase { get; }

        /// <summary>Current player.</summary>
        internal int PlayerInTurnUserId { get; }
    }
}
