using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Domain.Constants;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private const int DefaultCardsPerPlayerCount = ValidationRules.FourCardsPerPlayerCount;
        private const int LobbyStartCountdownSeconds = 3;

        private readonly Dictionary<int, LobbyStartState> _startsByRoomId =
            new Dictionary<int, LobbyStartState>();

        public LobbyMembershipChange ChangePlayerReadiness(LobbyCaller caller, int roomId, bool isReady)
        {
            LobbyMembershipChange lobbyMembershipChange;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                LobbyChangeStatus lobbyChangeStatus = ValidateLobbyCallerInLock(caller, roomId, out playerSession);

                if (lobbyChangeStatus != LobbyChangeStatus.Applied)
                {
                    lobbyMembershipChange = CreateRejectedWithoutUser(lobbyChangeStatus);
                }
                else
                {
                    lobbyMembershipChange = ChangePlayerReadinessInLock(playerSession.UserId, roomId, isReady);
                }
            }

            return lobbyMembershipChange;
        }

        public LobbyMembershipChange ChangeCardsPerPlayer(LobbyCaller caller, int roomId, int cardsPerPlayerCount)
        {
            LobbyMembershipChange lobbyMembershipChange;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                LobbyChangeStatus lobbyChangeStatus = ValidateLobbyCallerInLock(caller, roomId, out playerSession);

                if (lobbyChangeStatus != LobbyChangeStatus.Applied)
                {
                    lobbyMembershipChange = CreateRejectedWithoutUser(lobbyChangeStatus);
                }
                else
                {
                    lobbyMembershipChange =
                        ChangeCardsPerPlayerInLock(playerSession.UserId, roomId, cardsPerPlayerCount);
                }
            }

            return lobbyMembershipChange;
        }

        public LobbyMembershipChange RequestMatchStart(LobbyCaller caller, int roomId, long countdownId)
        {
            LobbyMembershipChange lobbyMembershipChange;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                LobbyChangeStatus lobbyChangeStatus = ValidateLobbyCallerInLock(caller, roomId, out playerSession);

                if (lobbyChangeStatus != LobbyChangeStatus.Applied)
                {
                    lobbyMembershipChange = CreateRejectedWithoutUser(lobbyChangeStatus);
                }
                else
                {
                    lobbyMembershipChange = RequestMatchStartInLock(playerSession.UserId, roomId, countdownId);
                }
            }

            return lobbyMembershipChange;
        }

        /// <summary>Valida identidad y canal antes de consultar la sala; requiere _sessionsLock.</summary>
        /// <param name="caller">Token y canal recibidos del cliente.</param>
        /// <param name="roomId">Sala indicada por el cliente.</param>
        /// <param name="playerSession">Sesión encontrada, o null si el token no existe.</param>
        /// <returns>
        /// Applied si coinciden identidad, canal y sala; de lo contrario, el motivo sin estado de sala.
        /// </returns>
        private LobbyChangeStatus ValidateLobbyCallerInLock(
            LobbyCaller caller,
            int roomId,
            out PlayerSession playerSession)
        {
            LobbyChangeStatus lobbyChangeStatus = LobbyChangeStatus.InvalidSession;
            playerSession = null;
            int channelUserId;
            int currentRoomId;

            if (caller.HasSessionAndChannel && _sessionsByToken.TryGetValue(caller.SessionToken, out playerSession))
            {
                bool hasChannel = _userIdsByChannelId.TryGetValue(caller.ChannelId, out channelUserId);
                bool hasRoom = _roomIdsByUserId.TryGetValue(playerSession.UserId, out currentRoomId);

                if (hasChannel && channelUserId == playerSession.UserId)
                {
                    lobbyChangeStatus = hasRoom && currentRoomId == roomId
                        ? LobbyChangeStatus.Applied
                        : LobbyChangeStatus.NotInRoom;
                }
                else if (!hasChannel && !hasRoom)
                {
                    lobbyChangeStatus = LobbyChangeStatus.NotInRoom;
                }
            }

            return lobbyChangeStatus;
        }

        /// <summary>
        /// Aplica un cambio real de preparación; una repetición solo copia el estado bajo _sessionsLock.
        /// </summary>
        /// <param name="userId">Jugador ya validado.</param>
        /// <param name="roomId">Sala actual del jugador.</param>
        /// <param name="isReady">Estado de preparación deseado.</param>
        /// <returns>Resultado y destinatarios del cambio, o rechazo si la partida ya comenzó.</returns>
        private LobbyMembershipChange ChangePlayerReadinessInLock(int userId, int roomId, bool isReady)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            LobbyMembershipChange lobbyMembershipChange;

            if (lobbyStartState.ReadyUserIds.Contains(userId) == isReady)
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId, roomId, CreateRoomSnapshot(roomId, _membersByRoomId[roomId]));
            }
            else if (lobbyStartState.MatchId.HasValue)
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.MatchAlreadyStarted, userId);
            }
            else
            {
                if (isReady)
                {
                    lobbyStartState.ReadyUserIds.Add(userId);
                }
                else
                {
                    lobbyStartState.ReadyUserIds.Remove(userId);
                }

                RefreshLobbyCountdownInLock(roomId);
                lobbyMembershipChange = CopyLobbyChangeInLock(userId, roomId);
            }

            return lobbyMembershipChange;
        }

        /// <summary>
        /// Autoriza una sola partida al vencer la generación vigente y crea su MatchState; requiere _sessionsLock.
        /// Todo lo que puede lanzar (identificador, partida, estado de inicio nuevo, destinatarios, foto de la sala y
        /// resultado) se arma antes de cambiar nada; al final solo hay dos asignaciones: la partida y luego el estado
        /// de inicio que ya trae su MatchId.
        /// </summary>
        /// <param name="userId">Jugador ya validado.</param>
        /// <param name="roomId">Sala actual del jugador.</param>
        /// <param name="countdownId">Generación observada por el cliente.</param>
        /// <returns>Estado actual; incluye destinatarios únicamente cuando se autoriza la partida.</returns>
        private LobbyMembershipChange RequestMatchStartInLock(int userId, int roomId, long countdownId)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            DateTime nowUtc = _readUtcNow();
            LobbyMembershipChange lobbyMembershipChange;

            if (CanAuthorizeMatchStartInLock(roomId, countdownId, nowUtc))
            {
                List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
                Guid matchId = Guid.NewGuid();
                MatchRecord matchRecord = CreateMatchRecordInLock(matchId, roomId, nowUtc);
                LobbyStartState authorizedStart = CreateAuthorizedStart(lobbyStartState, matchId, nowUtc);
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId,
                    roomId,
                    CreateRoomSnapshot(roomId, lobbyMembers, authorizedStart),
                    CopyCallbacks(lobbyMembers));

                _matchesById[matchId] = matchRecord;
                _startsByRoomId[roomId] = authorizedStart;
            }
            else
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId, roomId, CreateRoomSnapshot(roomId, _membersByRoomId[roomId]));
            }

            return lobbyMembershipChange;
        }

        /// <summary>Comprueba una cuenta vigente y vencida antes de autorizar bajo _sessionsLock.</summary>
        /// <param name="roomId">Sala existente.</param>
        /// <param name="countdownId">Generación solicitada.</param>
        /// <param name="nowUtc">Reloj UTC leído bajo el candado.</param>
        /// <returns>true si la cuenta vigente venció y todos los miembros cumplen la preparación.</returns>
        private bool CanAuthorizeMatchStartInLock(int roomId, long countdownId, DateTime nowUtc)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            bool hasActiveCountdown = !lobbyStartState.MatchId.HasValue
                && lobbyStartState.CountdownEndsAtUtc.HasValue;
            bool canAuthorize = false;

            if (hasActiveCountdown)
            {
                bool hasRequestedGeneration = lobbyStartState.CountdownId == countdownId;
                bool hasCountdownExpired = nowUtc >= lobbyStartState.CountdownEndsAtUtc.Value;
                canAuthorize = hasRequestedGeneration && hasCountdownExpired && AreLobbyPlayersReadyInLock(roomId);
            }

            return canAuthorize;
        }

        /// <summary>Renueva la versión y recalcula la cuenta tras un cambio real; requiere _sessionsLock.</summary>
        /// <param name="roomId">Sala cuya pertenencia o preparación cambió.</param>
        private void RefreshLobbyCountdownInLock(int roomId)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            lobbyStartState.Version++;

            if (!lobbyStartState.MatchId.HasValue)
            {
                lobbyStartState.CountdownEndsAtUtc = null;

                if (AreLobbyPlayersReadyInLock(roomId))
                {
                    lobbyStartState.CountdownId++;
                    lobbyStartState.CountdownEndsAtUtc = _readUtcNow().AddSeconds(LobbyStartCountdownSeconds);
                }
            }
        }

        /// <summary>Comprueba cantidad y preparación de todos los miembros bajo _sessionsLock.</summary>
        /// <param name="roomId">Sala existente que se comprueba.</param>
        /// <returns>true si la sala tiene entre el mínimo y el máximo de jugadores, todos Listo.</returns>
        private bool AreLobbyPlayersReadyInLock(int roomId)
        {
            List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            bool hasRequiredPlayerCount = lobbyMembers.Count >= ValidationRules.MinimumPlayersPerMatchCount
                && lobbyMembers.Count <= ValidationRules.MaximumPlayersPerMatchCount;
            bool areReady = hasRequiredPlayerCount
                && lobbyMembers.TrueForAll(member => lobbyStartState.ReadyUserIds.Contains(member.UserId));

            return areReady;
        }

        /// <summary>
        /// Aplica un cambio real de cartas por jugador y deja a todos en No Listo; un valor repetido solo copia el
        /// estado. Requiere _sessionsLock y no invoca callbacks: solo los copia para avisar después de liberarlo.
        /// </summary>
        /// <param name="userId">Jugador ya validado.</param>
        /// <param name="roomId">Sala actual del jugador.</param>
        /// <param name="cardsPerPlayerCount">Cantidad ya validada de cartas por jugador.</param>
        /// <returns>Resultado y destinatarios del cambio, o rechazo si la partida ya comenzó.</returns>
        private LobbyMembershipChange ChangeCardsPerPlayerInLock(int userId, int roomId, int cardsPerPlayerCount)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
            LobbyMembershipChange lobbyMembershipChange;

            if (lobbyStartState.CardsPerPlayerCount == cardsPerPlayerCount)
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId, roomId, CreateRoomSnapshot(roomId, lobbyMembers));
            }
            else if (lobbyStartState.MatchId.HasValue)
            {
                lobbyMembershipChange =
                    LobbyMembershipChange.CreateRejected(LobbyChangeStatus.MatchAlreadyStarted, userId);
            }
            else
            {
                lobbyStartState.CardsPerPlayerCount = cardsPerPlayerCount;
                lobbyStartState.ReadyUserIds.Clear();
                RefreshLobbyCountdownInLock(roomId);
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId,
                    roomId,
                    CreateRoomSnapshot(roomId, lobbyMembers),
                    CopyCallbacksExceptUser(lobbyMembers, userId));
            }

            return lobbyMembershipChange;
        }

        /// <summary>Copia los callbacks de los demás jugadores de la sala; requiere _sessionsLock.</summary>
        /// <param name="lobbyMembers">Miembros de la sala.</param>
        /// <param name="excludedUserId">Jugador que causó el cambio y que no necesita el aviso.</param>
        /// <returns>Callbacks de todos los miembros salvo el excluido.</returns>
        private static IReadOnlyList<ILobbyManagerCallback> CopyCallbacksExceptUser(
            List<LobbyMember> lobbyMembers,
            int excludedUserId)
        {
            List<ILobbyManagerCallback> callbacks = new List<ILobbyManagerCallback>();

            foreach (LobbyMember lobbyMember in lobbyMembers)
            {
                if (lobbyMember.UserId != excludedUserId && lobbyMember.Callback != null)
                {
                    callbacks.Add(lobbyMember.Callback);
                }
            }

            IReadOnlyList<ILobbyManagerCallback> readOnlyCallbacks = callbacks.AsReadOnly();

            return readOnlyCallbacks;
        }

        /// <summary>Copia estado y callbacks bajo _sessionsLock para notificar después de liberarlo.</summary>
        /// <param name="userId">Jugador que causó el cambio.</param>
        /// <param name="roomId">Sala modificada.</param>
        /// <returns>Estado copiado y destinatarios, incluido el solicitante.</returns>
        private LobbyMembershipChange CopyLobbyChangeInLock(int userId, int roomId)
        {
            List<LobbyMember> lobbyMembers = _membersByRoomId[roomId];
            LobbyMembershipChange lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                userId, roomId, CreateRoomSnapshot(roomId, lobbyMembers), CopyCallbacks(lobbyMembers));

            return lobbyMembershipChange;
        }

        /// <summary>Copia la preparación e inicio sin compartir colecciones mutables; requiere _sessionsLock.</summary>
        /// <param name="roomId">Sala existente que se copia.</param>
        /// <returns>Copia de solo lectura con la hora UTC del servidor.</returns>
        private LobbyStartSnapshot CreateStartSnapshotInLock(int roomId)
        {
            return CreateStartSnapshotInLock(_startsByRoomId[roomId]);
        }

        /// <summary>Copia un estado de inicio, publicado o por publicar; requiere _sessionsLock.</summary>
        private LobbyStartSnapshot CreateStartSnapshotInLock(LobbyStartState lobbyStartState)
        {
            LobbyStartSnapshot lobbyStartSnapshot = new LobbyStartSnapshot(
                new List<int>(lobbyStartState.ReadyUserIds).AsReadOnly(),
                lobbyStartState.Version,
                lobbyStartState.CountdownId,
                lobbyStartState.CountdownEndsAtUtc,
                _readUtcNow(),
                lobbyStartState.MatchId,
                lobbyStartState.MatchStartedAtUtc,
                lobbyStartState.CardsPerPlayerCount);

            return lobbyStartSnapshot;
        }

        /// <summary>
        /// Arma, sin publicarlo, el estado de inicio que reemplazará al actual al autorizar: misma preparación y
        /// modalidad, cuenta cerrada, MatchId y hora de inicio asignados y versión siguiente.
        /// </summary>
        private static LobbyStartState CreateAuthorizedStart(
            LobbyStartState currentStart,
            Guid matchId,
            DateTime nowUtc)
        {
            LobbyStartState authorizedStart = new LobbyStartState
            {
                Version = currentStart.Version + 1,
                CountdownId = currentStart.CountdownId,
                CountdownEndsAtUtc = null,
                MatchId = matchId,
                MatchStartedAtUtc = nowUtc,
                CardsPerPlayerCount = currentStart.CardsPerPlayerCount
            };
            authorizedStart.ReadyUserIds.UnionWith(currentStart.ReadyUserIds);

            return authorizedStart;
        }

        /// <summary>Estado mutable privado; solo se accede bajo _sessionsLock y nunca viaja en los avisos.</summary>
        private sealed class LobbyStartState
        {
            /// <summary>Jugadores cuyo estado actual es Listo.</summary>
            public HashSet<int> ReadyUserIds { get; } = new HashSet<int>();

            /// <summary>Versión creciente de los cambios de sala.</summary>
            public long Version { get; set; }

            /// <summary>Última generación de cuenta creada en esta sala.</summary>
            public long CountdownId { get; set; }

            /// <summary>Plazo UTC de la cuenta activa, o null si no hay cuenta.</summary>
            public DateTime? CountdownEndsAtUtc { get; set; }

            /// <summary>Identificador de la partida autorizada, o null antes de iniciar.</summary>
            public Guid? MatchId { get; set; }

            /// <summary>Hora UTC de autorización de la partida, o null antes de iniciar.</summary>
            public DateTime? MatchStartedAtUtc { get; set; }

            /// <summary>Cartas por jugador de la sala; una sala nueva empieza con el valor por defecto.</summary>
            public int CardsPerPlayerCount { get; set; } = DefaultCardsPerPlayerCount;
        }
    }
}