using System;
using System.Collections.Generic;
using System.Linq;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Engine;

namespace TLMG.Services.Sessions
{
    internal enum FugitiveKittenOutcome { InProgress, Caught, Escaped }

    internal sealed class FugitiveKittenSnapshot
    {
        internal DateTime StartedAtUtc { get; set; }
        internal DateTime ExpiresAtUtc { get; set; }
        internal DateTime? PausedAtUtc { get; set; }
        internal FugitiveKittenRoute Route { get; set; }
        internal string Color { get; set; }
        internal FugitiveKittenOutcome Outcome { get; set; }
        internal int? WinnerUserId { get; set; }
        internal int Version { get; set; }
    }

    public partial class SessionRegistry
    {
        private sealed class FugitiveKittenState
        {
            internal FugitiveKittenState(DateTime startedAtUtc)
            {
                StartedAtUtc = startedAtUtc;
                PausedAtUtc = startedAtUtc;
                ExpiresAtUtc = startedAtUtc.AddMilliseconds(FugitiveKittenRoute.WindowMilliseconds);
                Route = new FugitiveKittenRoute();
                Color = "Orange";
            }

            internal DateTime StartedAtUtc { get; private set; }
            internal DateTime ExpiresAtUtc { get; private set; }
            internal DateTime? PausedAtUtc { get; private set; }

            internal bool SetPaused(bool paused, DateTime nowUtc)
            {
                if (IsResolved || paused == PausedAtUtc.HasValue)
                {
                    return false;
                }

                if (paused)
                {
                    PausedAtUtc = nowUtc;
                }
                else
                {
                    TimeSpan pausedDuration = nowUtc - PausedAtUtc.Value;
                    StartedAtUtc += pausedDuration;
                    ExpiresAtUtc += pausedDuration;
                    PausedAtUtc = null;
                }
                return true;
            }
            internal FugitiveKittenRoute Route { get; }
            internal string Color { get; }
            internal FugitiveKittenOutcome Outcome { get; private set; }
            internal int? WinnerUserId { get; private set; }
            internal bool IsResolved => Outcome != FugitiveKittenOutcome.InProgress;

            internal bool Expire(DateTime nowUtc)
            {
                bool expired = !IsResolved && !PausedAtUtc.HasValue && nowUtc >= ExpiresAtUtc;
                if (expired)
                {
                    Outcome = FugitiveKittenOutcome.Escaped;
                }
                return expired;
            }

            internal void MarkCaught(int userId)
            {
                WinnerUserId = userId;
                Outcome = FugitiveKittenOutcome.Caught;
            }
        }

        /// <summary>Processes an authorized catch attempt using only the server clock.</summary>
        public MatchDelivery TryCatchFugitiveKitten(SessionChannelCaller caller)
        {
            return RunFugitiveKittenOperation(caller, CatchKittenInLock);
        }

        /// <summary>Checks the deadline without resolving a finished match a second time.</summary>
        public MatchDelivery CheckFugitiveKittenDeadline(SessionChannelCaller caller)
        {
            return RunFugitiveKittenOperation(caller, CheckKittenDeadlineInLock);
        }

        private MatchDelivery RunFugitiveKittenOperation(SessionChannelCaller caller,
            Func<MatchRecord, int, DateTime, MatchDelivery> operation)
        {
            MatchDelivery delivery;
            lock (_sessionsLock)
            {
                string errorKey = ValidateMatchActionInLock(caller, out PlayerSession session, out MatchRecord match);
                if (errorKey == null)
                {
                    delivery = operation(match, session.UserId, _readUtcNow());
                }
                else
                {
                    delivery = MatchDelivery.CreateRejected(errorKey);
                }
            }

            return delivery;
        }

        private MatchDelivery CatchKittenInLock(MatchRecord match, int userId, DateTime nowUtc)
        {
            bool changed = ExpireMatchDeadlinesInLock(match, nowUtc);
            FugitiveKittenState kitten = match.Kitten;
            bool available = kitten != null && !kitten.IsResolved && !kitten.PausedAtUtc.HasValue;
            bool rejected = false;
            if (available && match.MatchState.Phase != RoundPhase.MatchFinished
                && kitten.Route.CanCatchAt(nowUtc - kitten.StartedAtUtc))
            {
                rejected = match.MatchState.GrantExtraLife(userId) != ExtraLifeStatus.Granted;
                if (!rejected)
                {
                    kitten.MarkCaught(userId);
                    changed = true;
                }
            }

            IReadOnlyList<MatchViewDelivery> views = PublishKittenChangesInLock(match, changed);
            MatchDelivery delivery;
            if (rejected)
            {
                delivery = MatchDelivery.CreateRejected(ErrorKeys.Common.Unexpected, views);
            }
            else
            {
                delivery = MatchDelivery.CreateAccepted(views);
            }

            return delivery;
        }

        private MatchDelivery CheckKittenDeadlineInLock(MatchRecord match, int userId, DateTime nowUtc)
        {
            bool changed = ExpireMatchDeadlinesInLock(match, nowUtc);
            IReadOnlyList<MatchViewDelivery> views = PublishKittenChangesInLock(match, changed);
            if (!changed && match.Kitten != null && match.Kitten.IsResolved)
            {
                MatchChannel channel = _matchChannelsByUserId[userId];
                MatchViewDelivery ownView = CreateViewDeliveryInLock(match, userId, channel.Callback);
                views = new List<MatchViewDelivery> { ownView }.AsReadOnly();
            }

            return MatchDelivery.CreateAccepted(views);
        }

        private IReadOnlyList<MatchViewDelivery> PublishKittenChangesInLock(MatchRecord match, bool changed)
        {
            IReadOnlyList<MatchViewDelivery> views = _noMatchViewDeliveries;
            if (changed)
            {
                match.Version++;
                views = CreateViewDeliveriesInLock(match);
            }

            return views;
        }

        private static FugitiveKittenResult ResolveKittenResult(FugitiveKittenOutcome outcome)
        {
            FugitiveKittenResult result = FugitiveKittenResult.Escaped;
            if (outcome == FugitiveKittenOutcome.InProgress)
            {
                result = FugitiveKittenResult.InProgress;
            }
            else if (outcome == FugitiveKittenOutcome.Caught)
            {
                result = FugitiveKittenResult.Caught;
            }

            return result;
        }

        private static FugitiveKittenView CreateFugitiveKittenViewInLock(MatchRecord match, DateTime nowUtc)
        {
            FugitiveKittenView view = null;
            FugitiveKittenState kitten = match.Kitten;
            if (kitten != null)
            {
                string nickname = null;
                if (kitten.WinnerUserId.HasValue)
                {
                    match.ParticipantNicknames.TryGetValue(kitten.WinnerUserId.Value, out nickname);
                }
                view = new FugitiveKittenView
                {
                    ServerTimeUtc = nowUtc,
                    StartedAtUtc = kitten.StartedAtUtc,
                    PausedAtUtc = kitten.PausedAtUtc,
                    ExpiresAtUtc = kitten.ExpiresAtUtc,
                    Color = kitten.Color,
                    Result = ResolveKittenResult(kitten.Outcome),
                    WinnerUserId = kitten.WinnerUserId,
                    WinnerNickname = nickname,
                    Segments = kitten.Route.Segments.Select(segment => new KittenSegmentView
                    {
                        Kind = segment.Kind,
                        Origin = segment.Origin,
                        Destination = segment.Destination,
                        StartMilliseconds = segment.StartMilliseconds,
                        EndMilliseconds = segment.EndMilliseconds,
                        IsCatchable = segment.IsCatchable
                    }).ToList()
                };
            }
            return view;
        }

        /// <summary>Internal snapshot for tests and future contract mapping.</summary>
        internal FugitiveKittenSnapshot FindFugitiveKitten(Guid matchId, int userId)
        {
            FugitiveKittenSnapshot snapshot = null;
            lock (_sessionsLock)
            {
                MatchRecord match;
                if (_matchesById.TryGetValue(matchId, out match) && match.ParticipantUserIds.Contains(userId)
                    && match.Kitten != null)
                {
                    FugitiveKittenState kitten = match.Kitten;
                    snapshot = new FugitiveKittenSnapshot
                    {
                        StartedAtUtc = kitten.StartedAtUtc,
                        PausedAtUtc = kitten.PausedAtUtc,
                        ExpiresAtUtc = kitten.ExpiresAtUtc,
                        Route = kitten.Route,
                        Color = kitten.Color,
                        Outcome = kitten.Outcome,
                        WinnerUserId = kitten.WinnerUserId,
                        Version = match.Version
                    };
                }
            }
            return snapshot;
        }
    }
}
