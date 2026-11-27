using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;
using TLMG.Domain.Authorization;

namespace TLMG.Services.Lobby
{
    public partial class LobbyService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(LobbyService));

        private static readonly Dictionary<LobbyChangeStatus, string> _errorKeysByStatus =
            new Dictionary<LobbyChangeStatus, string>
            {
                { LobbyChangeStatus.InvalidSession, ErrorKeys.Common.InvalidSession },
                { LobbyChangeStatus.AlreadyInRoom, ErrorKeys.Lobby.AlreadyInRoom },
                { LobbyChangeStatus.NotAuthorized, ErrorKeys.Lobby.CannotJoinRoom },
                { LobbyChangeStatus.RoomFull, ErrorKeys.Common.RoomFull },
                { LobbyChangeStatus.NotInRoom, ErrorKeys.Lobby.NotInRoom },
                { LobbyChangeStatus.MatchAlreadyStarted, ErrorKeys.Lobby.MatchAlreadyStarted },
                { LobbyChangeStatus.MatchStartPersistenceFailed, ErrorKeys.Common.Unexpected },
                { LobbyChangeStatus.DataFailure, ErrorKeys.Common.Unexpected }
            };

        private readonly ISessionRegistry _sessionRegistry;
        private readonly ISessionAuthorizer _sessionAuthorizer;
        private readonly IMatchStartCoordinator _matchStartCoordinator;

        public LobbyService(ISessionRegistry sessionRegistry, ISessionAuthorizer sessionAuthorizer,
            IMatchStartCoordinator matchStartCoordinator)
        {
            _sessionRegistry = sessionRegistry;
            _sessionAuthorizer = sessionAuthorizer;
            _matchStartCoordinator = matchStartCoordinator;
        }

        public ResultWithNotification<OperationLobbyRoomResult> CreateRoom(
            SessionChannelCaller caller,
            ILobbyManagerCallback lobbyCallback)
        {
            OperationLobbyRoomResult operationCreateRoomResult = new OperationLobbyRoomResult();
            SessionAuthorization authorization = _sessionAuthorizer.Authorize(
                caller.SessionToken,
                SystemPermission.CreateRoom);

            if (authorization == null)
            {
                operationCreateRoomResult.ErrorKey = ErrorKeys.Common.Unexpected;
                _log.Warn("La autorización para crear una sala no devolvió un resultado.");
            }
            else if (authorization.Status != AuthorizationStatus.Granted)
            {
                operationCreateRoomResult.ErrorKey = authorization.ErrorKey;
                _log.WarnFormat(
                    "Se rechazó crear una sala del jugador {0}: {1}.",
                    authorization.UserId,
                    authorization.Status);
            }
            else
            {
                ApplyRoomCreation(caller, lobbyCallback, operationCreateRoomResult);
            }

            ResultWithNotification<OperationLobbyRoomResult> createRoomResult =
                new ResultWithNotification<OperationLobbyRoomResult>(operationCreateRoomResult);

            if (operationCreateRoomResult.IsSuccess)
            {
                _matchStartCoordinator.ReconcileCancelledStarts();
            }

            return createRoomResult;
        }

        private void ApplyRoomCreation(
            SessionChannelCaller caller,
            ILobbyManagerCallback lobbyCallback,
            OperationLobbyRoomResult operationCreateRoomResult)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.CreateRoom(caller, lobbyCallback);

            if (lobbyMembershipChange == null
                || (lobbyMembershipChange.Status == LobbyChangeStatus.Applied
                    && lobbyMembershipChange.Room == null))
            {
                operationCreateRoomResult.ErrorKey = ErrorKeys.Common.Unexpected;
                _log.Error("No se obtuvo una sala válida al crear la sala.");
            }
            else if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                operationCreateRoomResult.IsSuccess = true;
                operationCreateRoomResult.ErrorKey = string.Empty;
                operationCreateRoomResult.Room = LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);
                _log.InfoFormat(
                    "El jugador {0} creó la sala {1}.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.RoomId);
            }
            else
            {
                operationCreateRoomResult.ErrorKey = ResolveErrorKey(lobbyMembershipChange.Status);
                LogRejectedOperation("crear una sala", lobbyMembershipChange, lobbyMembershipChange.RoomId);
            }
        }

        public ResultWithNotification<OperationLobbyRoomResult> EnterLobby(
            SessionChannelCaller caller,
            int roomId,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.EnterRoom(caller, roomId, lobbyCallback);
            OperationLobbyRoomResult operationEnterLobbyResult = new OperationLobbyRoomResult();
            IReadOnlyList<Action> lobbyNotifications = Array.Empty<Action>();

            if (lobbyMembershipChange == null
                || (lobbyMembershipChange.Status == LobbyChangeStatus.Applied
                    && lobbyMembershipChange.Room == null))
            {
                operationEnterLobbyResult.ErrorKey = ErrorKeys.Common.Unexpected;
                _log.Error("No se obtuvo una sala válida al ingresar a la sala.");
            }
            else if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                lobbyNotifications = SessionNotificationHelper.CreateRoomNotifications(lobbyMembershipChange);
                operationEnterLobbyResult.IsSuccess = true;
                operationEnterLobbyResult.ErrorKey = string.Empty;
                operationEnterLobbyResult.Room = LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);
                _log.InfoFormat("El jugador {0} entró a la sala {1}.", lobbyMembershipChange.UserId, roomId);
            }
            else
            {
                operationEnterLobbyResult.ErrorKey = ResolveErrorKey(lobbyMembershipChange.Status);
                LogRejectedOperation("entrar a una sala", lobbyMembershipChange, roomId);
            }

            ResultWithNotification<OperationLobbyRoomResult> enterLobbyResult =
                new ResultWithNotification<OperationLobbyRoomResult>(
                    operationEnterLobbyResult,
                    lobbyNotifications);

            if (operationEnterLobbyResult.IsSuccess && !operationEnterLobbyResult.Room.MatchId.HasValue)
            {
                _matchStartCoordinator.ReconcileCancelledStarts();
            }

            return enterLobbyResult;
        }

        public ResultWithNotification<OperationResult> LeaveLobby(string sessionToken)
        {
            IReadOnlyList<MatchViewDelivery> matchViewDeliveries;
            LobbyMembershipChange lobbyMembershipChange =
                _sessionRegistry.LeaveRoom(sessionToken, out matchViewDeliveries);
            OperationResult operationLeaveLobbyResult = new OperationResult();
            List<Action> lobbyChangeNotifications = new List<Action>();

            if (lobbyMembershipChange == null || matchViewDeliveries == null)
            {
                operationLeaveLobbyResult.ErrorKey = ErrorKeys.Common.Unexpected;
                _log.Error("No se obtuvo un cambio de sala válido al salir de la sala.");
            }
            else if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                lobbyChangeNotifications.AddRange(SessionNotificationHelper.CreateRoomNotifications(lobbyMembershipChange));
                lobbyChangeNotifications.AddRange(SessionNotificationHelper.CreateMatchViewNotifications(matchViewDeliveries));
                operationLeaveLobbyResult.IsSuccess = true;
                operationLeaveLobbyResult.ErrorKey = string.Empty;
                _log.InfoFormat(
                    "El jugador {0} salió de la sala {1}.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.RoomId);
                LogMatchExit(lobbyMembershipChange);
            }
            else if (lobbyMembershipChange.Status == LobbyChangeStatus.NotInRoom)
            {
                operationLeaveLobbyResult.IsSuccess = true;
                operationLeaveLobbyResult.ErrorKey = string.Empty;
            }
            else
            {
                operationLeaveLobbyResult.ErrorKey = ResolveErrorKey(lobbyMembershipChange.Status);
                LogRejectedOperation("salir de una sala", lobbyMembershipChange, lobbyMembershipChange.RoomId);
            }

            ResultWithNotification<OperationResult> leaveLobbyResult =
                new ResultWithNotification<OperationResult>(
                    operationLeaveLobbyResult,
                    lobbyChangeNotifications);

            if (operationLeaveLobbyResult.IsSuccess && lobbyMembershipChange.Status == LobbyChangeStatus.Applied
                && !lobbyMembershipChange.LeftAuthorizedMatch)
            {
                _matchStartCoordinator.ReconcileCancelledStarts();
            }

            return leaveLobbyResult;
        }

        private static void LogMatchExit(LobbyMembershipChange lobbyMembershipChange)
        {
            if (lobbyMembershipChange.LeftAuthorizedMatch)
            {
                _log.InfoFormat(
                    "El jugador {0} salió voluntariamente de la partida de la sala {1}.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.RoomId);
            }
        }

        public IReadOnlyList<Action> LeaveLobbyByChannel(string channelId)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.LeaveRoomByChannel(channelId);
            IReadOnlyList<Action> lobbyChangeNotifications = Array.Empty<Action>();

            if (lobbyMembershipChange == null)
            {
                _log.Error("No se obtuvo un cambio de sala al finalizar el canal de lobby.");
            }
            else if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                lobbyChangeNotifications = SessionNotificationHelper.CreateRoomNotifications(lobbyMembershipChange);
                _log.InfoFormat(
                    "El jugador {0} salió de la sala {1} porque su canal de lobby terminó.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.RoomId);
                SessionNotificationHelper.LogMatchLeftUntouched(lobbyMembershipChange);
                _matchStartCoordinator.ReconcileCancelledStarts();
            }

            return lobbyChangeNotifications;
        }

        private static string ResolveErrorKey(LobbyChangeStatus lobbyChangeStatus)
        {
            string errorKey;

            if (!_errorKeysByStatus.TryGetValue(lobbyChangeStatus, out errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
        }

        private static LobbyMembershipChange CreateInvalidSessionChange()
        {
            LobbyMembershipChange lobbyMembershipChange =
                LobbyMembershipChange.CreateRejected(LobbyChangeStatus.InvalidSession, LobbyMembershipChange.NoUserId);

            return lobbyMembershipChange;
        }

        private static void ApplyOutcome(OperationResult operationResult, LobbyMembershipChange lobbyMembershipChange)
        {
            operationResult.IsSuccess = lobbyMembershipChange.Status == LobbyChangeStatus.Applied;
            operationResult.ErrorKey = string.Empty;

            if (!operationResult.IsSuccess)
            {
                operationResult.ErrorKey = ResolveErrorKey(lobbyMembershipChange.Status);
            }
        }

        private static LobbyRoom ConvertRoomOrNull(LobbyMembershipChange lobbyMembershipChange)
        {
            LobbyRoom lobbyRoom = null;

            if (lobbyMembershipChange.Room != null)
            {
                lobbyRoom = LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);
            }

            return lobbyRoom;
        }

        private static void LogRejectedOperation(
            string operationDescription,
            LobbyMembershipChange lobbyMembershipChange,
            int requestedRoomId)
        {
            _log.WarnFormat(
                "Se rechazó {0} del jugador {1} para la sala {2}: {3}.",
                operationDescription,
                lobbyMembershipChange.UserId,
                requestedRoomId,
                lobbyMembershipChange.Status);
        }
    }
}