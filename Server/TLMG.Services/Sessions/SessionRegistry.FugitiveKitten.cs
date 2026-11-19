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
                ExpiresAtUtc = startedAtUtc.AddMilliseconds(FugitiveKittenRoute.WindowMilliseconds);
                Route = new FugitiveKittenRoute();
                Color = "Orange";
            }

            internal DateTime StartedAtUtc { get; }
            internal DateTime ExpiresAtUtc { get; }
            internal FugitiveKittenRoute Route { get; }
            internal string Color { get; }
            internal FugitiveKittenOutcome Outcome { get; private set; }
            internal int? WinnerUserId { get; private set; }
            internal bool IsResolved => Outcome != FugitiveKittenOutcome.InProgress;

            internal bool Expire(DateTime nowUtc)
            {
                bool expired = !IsResolved && nowUtc >= ExpiresAtUtc;
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

        /// <summary>Procesa un intento autorizado usando exclusivamente el reloj del servidor.</summary>
        public MatchDelivery TryCatchFugitiveKitten(LobbyCaller caller)
        {
            return RunFugitiveKittenOperation(caller, true);
        }

        /// <summary>Comprueba el vencimiento sin volver a resolver una partida terminada.</summary>
        public MatchDelivery CheckFugitiveKittenDeadline(LobbyCaller caller)
        {
            return RunFugitiveKittenOperation(caller, false);
        }

        private MatchDelivery RunFugitiveKittenOperation(LobbyCaller caller, bool tryCatch)
        {
            MatchDelivery delivery;
            lock (_sessionsLock)
            {
                PlayerSession session;
                MatchRecord match;
                string errorKey = ValidateMatchActionInLock(caller, out session, out match);
                if (errorKey != null)
                {
                    delivery = MatchDelivery.CreateRejected(errorKey);
                }
                else
                {
                    DateTime nowUtc = _readUtcNow();
                    bool changed = ExpireMatchDeadlinesInLock(match, nowUtc);
                    FugitiveKittenState kitten = match.Kitten;
                    if (tryCatch && kitten != null && !kitten.IsResolved
                        && kitten.Route.CanCatchAt(nowUtc - kitten.StartedAtUtc))
                    {
                        ExtraLifeStatus status = match.MatchState.GrantExtraLife(session.UserId);
                        if (status == ExtraLifeStatus.Granted)
                        {
                            kitten.MarkCaught(session.UserId);
                            changed = true;
                        }
                        else
                        {
                            errorKey = ErrorKeys.Common.Unexpected;
                        }
                    }
                    IReadOnlyList<MatchViewDelivery> views = _noMatchViewDeliveries;
                    if (changed)
                    {
                        match.Version++;
                        views = CreateViewDeliveriesInLock(match);
                    }
                    else if (!tryCatch && kitten != null && kitten.IsResolved)
                    {
                        MatchChannel channel = _matchChannelsByUserId[session.UserId];
                        MatchViewDelivery ownView = CreateViewDeliveryInLock(match, session.UserId, channel.Callback);
                        views = new List<MatchViewDelivery> { ownView }.AsReadOnly();
                    }
                    delivery = errorKey == null
                        ? MatchDelivery.CreateAccepted(views)
                        : MatchDelivery.CreateRejected(errorKey, views);
                }
            }
            return delivery;
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
                    ExpiresAtUtc = kitten.ExpiresAtUtc,
                    Color = kitten.Color,
                    Result = (FugitiveKittenResult)kitten.Outcome,
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

        /// <summary>Copia interna para pruebas y futura proyección del contrato.</summary>
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
