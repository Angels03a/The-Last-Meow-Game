using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    public partial class LobbyService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(LobbyService));

        private static readonly Dictionary<LobbyChangeStatus, string> _errorKeysByStatus =
            new Dictionary<LobbyChangeStatus, string>
            {
                { LobbyChangeStatus.InvalidSession, ErrorKeys.Common.InvalidSession },
                { LobbyChangeStatus.AlreadyInRoom, ErrorKeys.Lobby.NotInRoom },
                { LobbyChangeStatus.NotAuthorized, ErrorKeys.Lobby.NotInRoom },
                { LobbyChangeStatus.RoomFull, ErrorKeys.Common.RoomFull },
                { LobbyChangeStatus.NotInRoom, ErrorKeys.Lobby.NotInRoom },
                { LobbyChangeStatus.MatchAlreadyStarted, ErrorKeys.Lobby.MatchAlreadyStarted }
            };

        private ISessionRegistry _sessionRegistry;

        public LobbyService(ISessionRegistry sessionRegistry)
        {
            _sessionRegistry = sessionRegistry;
        }

        public ResultWithNotification<OperationLobbyRoomResult> CreateRoom(
            SessionChannelCaller caller,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.CreateRoom(caller, lobbyCallback);
            OperationLobbyRoomResult operationCreateRoomResult = new OperationLobbyRoomResult();

            if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                operationCreateRoomResult.IsSuccess = true;
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

            ResultWithNotification<OperationLobbyRoomResult> createRoomResult =
                new ResultWithNotification<OperationLobbyRoomResult>(operationCreateRoomResult);

            return createRoomResult;
        }

        public ResultWithNotification<OperationLobbyRoomResult> EnterLobby(
            SessionChannelCaller caller,
            int roomId,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.EnterRoom(caller, roomId, lobbyCallback);
            OperationLobbyRoomResult operationEnterLobbyResult = new OperationLobbyRoomResult();
            IReadOnlyList<Action> lobbyNotifications =
                SessionNotificationHelper.CreateRoomNotifications(lobbyMembershipChange);

            if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                operationEnterLobbyResult.IsSuccess = true;
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

            return enterLobbyResult;
        }

        public ResultWithNotification<OperationResult> LeaveLobby(string sessionToken)
        {
            IReadOnlyList<MatchViewDelivery> matchViewDeliveries;
            LobbyMembershipChange lobbyMembershipChange =
                _sessionRegistry.LeaveRoom(sessionToken, out matchViewDeliveries);
            OperationResult operationLeaveLobbyResult = new OperationResult();
            List<Action> lobbyChangeNotifications = new List<Action>(
                SessionNotificationHelper.CreateRoomNotifications(lobbyMembershipChange));
            lobbyChangeNotifications.AddRange(
                SessionNotificationHelper.CreateMatchViewNotifications(matchViewDeliveries));

            if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                operationLeaveLobbyResult.IsSuccess = true;
                _log.InfoFormat(
                    "El jugador {0} salió de la sala {1}.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.RoomId);
                LogMatchExit(lobbyMembershipChange);
            }
            else if (lobbyMembershipChange.Status == LobbyChangeStatus.NotInRoom)
            {
                operationLeaveLobbyResult.IsSuccess = true;
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
            IReadOnlyList<Action> lobbyChangeNotifications =
                SessionNotificationHelper.CreateRoomNotifications(lobbyMembershipChange);

            if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                _log.InfoFormat(
                    "El jugador {0} salió de la sala {1} porque su canal de lobby terminó.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.RoomId);
                SessionNotificationHelper.LogMatchLeftUntouched(lobbyMembershipChange);
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
            operationResult.ErrorKey = null;

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