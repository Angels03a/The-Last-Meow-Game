using System;
using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private const int MinimumPlayersToStartCount = 3;
        // TODO: Llevar la modalidad colaborativa al servidor; mientras tanto se usan cuatro cartas. (BL-105).
        private const int DefaultCardsPerPlayerCount = 4;
        private const int LobbyStartCountdownSeconds = 3;

        private readonly Dictionary<int, LobbyStartState> _startsByRoomId =
            new Dictionary<int, LobbyStartState>();

        /// <inheritdoc />
        public LobbyMembershipChange ChangePlayerReadiness(
            string sessionToken,
            string channelId,
            int roomId,
            bool isReady)
        {
            LobbyMembershipChange lobbyMembershipChange;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                LobbyChangeStatus lobbyChangeStatus = ValidateLobbyCallerInLock(
                    sessionToken, channelId, roomId, out playerSession);

                if (lobbyChangeStatus != LobbyChangeStatus.Applied)
                {
                    lobbyMembershipChange = LobbyMembershipChange.CreateRejected(
                        lobbyChangeStatus, LobbyMembershipChange.NoUserId);
                }
                else
                {
                    lobbyMembershipChange = ChangePlayerReadinessInLock(playerSession.UserId, roomId, isReady);
                }
            }

            return lobbyMembershipChange;
        }

        /// <inheritdoc />
        public LobbyMembershipChange RequestMatchStart(
            string sessionToken,
            string channelId,
            int roomId,
            long countdownId)
        {
            LobbyMembershipChange lobbyMembershipChange;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                LobbyChangeStatus lobbyChangeStatus = ValidateLobbyCallerInLock(
                    sessionToken, channelId, roomId, out playerSession);

                if (lobbyChangeStatus != LobbyChangeStatus.Applied)
                {
                    lobbyMembershipChange = LobbyMembershipChange.CreateRejected(
                        lobbyChangeStatus, LobbyMembershipChange.NoUserId);
                }
                else
                {
                    lobbyMembershipChange = RequestMatchStartInLock(playerSession.UserId, roomId, countdownId);
                }
            }

            return lobbyMembershipChange;
        }

        /// <summary>Valida identidad y canal antes de consultar la sala; requiere _sessionsLock.</summary>
        /// <param name="sessionToken">Token recibido del cliente.</param>
        /// <param name="channelId">Canal desde el que llega la petición.</param>
        /// <param name="roomId">Sala indicada por el cliente.</param>
        /// <param name="playerSession">Sesión encontrada, o null si el token no existe.</param>
        /// <returns>
        /// Applied si coinciden identidad, canal y sala; de lo contrario, el motivo sin estado de sala.
        /// </returns>
        private LobbyChangeStatus ValidateLobbyCallerInLock(
            string sessionToken,
            string channelId,
            int roomId,
            out PlayerSession playerSession)
        {
            LobbyChangeStatus lobbyChangeStatus = LobbyChangeStatus.InvalidSession;
            playerSession = null;
            int channelUserId;
            int currentRoomId;

            if (!string.IsNullOrEmpty(sessionToken) && !string.IsNullOrEmpty(channelId)
                && _sessionsByToken.TryGetValue(sessionToken, out playerSession))
            {
                bool hasChannel = _userIdsByChannelId.TryGetValue(channelId, out channelUserId);
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

        /// <summary>Autoriza una sola partida al vencer la generación vigente; requiere _sessionsLock.</summary>
        /// <param name="userId">Jugador ya validado.</param>
        /// <param name="roomId">Sala actual del jugador.</param>
        /// <param name="countdownId">Generación observada por el cliente.</param>
        /// <returns>Estado actual; incluye destinatarios únicamente cuando se autoriza la partida.</returns>
        private LobbyMembershipChange RequestMatchStartInLock(int userId, int roomId, long countdownId)
        {
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            DateTime nowUtc = DateTime.UtcNow;
            LobbyMembershipChange lobbyMembershipChange;

            if (!lobbyStartState.MatchId.HasValue && lobbyStartState.CountdownEndsAtUtc.HasValue
                && lobbyStartState.CountdownId == countdownId && nowUtc >= lobbyStartState.CountdownEndsAtUtc.Value
                && AreLobbyPlayersReadyInLock(roomId))
            {
                lobbyStartState.MatchId = Guid.NewGuid();
                lobbyStartState.MatchStartedAtUtc = nowUtc;
                lobbyStartState.CountdownEndsAtUtc = null;
                lobbyStartState.Version++;
                lobbyMembershipChange = CopyLobbyChangeInLock(userId, roomId);
            }
            else
            {
                lobbyMembershipChange = LobbyMembershipChange.CreateApplied(
                    userId, roomId, CreateRoomSnapshot(roomId, _membersByRoomId[roomId]));
            }

            return lobbyMembershipChange;
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
                    lobbyStartState.CountdownEndsAtUtc = DateTime.UtcNow.AddSeconds(LobbyStartCountdownSeconds);
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
            bool areReady = lobbyMembers.Count >= MinimumPlayersToStartCount
                && lobbyMembers.Count <= MaximumPlayersPerRoom
                && lobbyMembers.TrueForAll(member => lobbyStartState.ReadyUserIds.Contains(member.UserId));

            return areReady;
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
            LobbyStartState lobbyStartState = _startsByRoomId[roomId];
            LobbyStartSnapshot lobbyStartSnapshot = new LobbyStartSnapshot(
                new List<int>(lobbyStartState.ReadyUserIds).AsReadOnly(),
                lobbyStartState.Version,
                lobbyStartState.CountdownId,
                lobbyStartState.CountdownEndsAtUtc,
                DateTime.UtcNow,
                lobbyStartState.MatchId,
                lobbyStartState.MatchStartedAtUtc,
                DefaultCardsPerPlayerCount);

            return lobbyStartSnapshot;
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
        }
    }
}
