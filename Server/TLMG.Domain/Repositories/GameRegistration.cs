using System;
using System.Collections.Generic;

namespace TLMG.Domain.Repositories
{
    public sealed class GameRegistration
    {
        public Guid MatchId { get; set; }
        public DateTime LobbyOpenedAtUtc { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public int CardsPerPlayerCount { get; set; }
        public IReadOnlyList<int> RegisteredUserIds { get; set; }
    }

    public enum GameRegistrationStatus
    {
        Created,
        AlreadyRegistered,
        InvalidRegistration,
        RegisteredPlayerNotFound,
        DataFailure,
        RegistrationConflict
    }

    public sealed class GameRegistrationResult
    {
        public GameRegistrationStatus GameRegistrationStatus { get; set; }
        public int GameId { get; set; }
    }
}
