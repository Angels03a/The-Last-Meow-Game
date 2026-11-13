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

        public LobbyOperationResultWithNotification<OperationLobbyRoomResult> CreateRoom(
            LobbyCaller caller,
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

            LobbyOperationResultWithNotification<OperationLobbyRoomResult> createRoomResult =
                new LobbyOperationResultWithNotification<OperationLobbyRoomResult>(operationCreateRoomResult);

            return createRoomResult;
        }

        public LobbyOperationResultWithNotification<OperationLobbyRoomResult> EnterLobby(
            LobbyCaller caller,
            int roomId,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.EnterRoom(caller, roomId, lobbyCallback);
            OperationLobbyRoomResult operationEnterLobbyResult = new OperationLobbyRoomResult();
            IReadOnlyList<LobbyChangeNotification> lobbyNotifications = LobbyChangeNotification.None;

            if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                operationEnterLobbyResult.IsSuccess = true;
                operationEnterLobbyResult.Room = LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);
                lobbyNotifications = CreateNotifications(lobbyMembershipChange);
                _log.InfoFormat("El jugador {0} entró a la sala {1}.", lobbyMembershipChange.UserId, roomId);
            }
            else
            {
                operationEnterLobbyResult.ErrorKey = ResolveErrorKey(lobbyMembershipChange.Status);
                LogRejectedOperation("entrar a una sala", lobbyMembershipChange, roomId);
            }

            LobbyOperationResultWithNotification<OperationLobbyRoomResult> enterLobbyResult =
                new LobbyOperationResultWithNotification<OperationLobbyRoomResult>(
                    operationEnterLobbyResult,
                    lobbyNotifications);

            return enterLobbyResult;
        }

        public LobbyOperationResultWithNotification<OperationResult> LeaveLobby(string sessionToken)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.LeaveRoom(sessionToken);
            OperationResult operationLeaveLobbyResult = new OperationResult();
            IReadOnlyList<LobbyChangeNotification> lobbyChangeNotifications = LobbyChangeNotification.None;

            if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                operationLeaveLobbyResult.IsSuccess = true;
                lobbyChangeNotifications = CreateNotifications(lobbyMembershipChange);
                _log.InfoFormat(
                    "El jugador {0} salió de la sala {1}.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.RoomId);
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

            LobbyOperationResultWithNotification<OperationResult> leaveLobbyResult =
                new LobbyOperationResultWithNotification<OperationResult>(
                    operationLeaveLobbyResult,
                    lobbyChangeNotifications);

            return leaveLobbyResult;
        }

        public IReadOnlyList<LobbyChangeNotification> LeaveLobbyByChannel(string channelId)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.LeaveRoomByChannel(channelId);
            IReadOnlyList<LobbyChangeNotification> lobbyChangeNotifications = LobbyChangeNotification.None;

            if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                lobbyChangeNotifications = CreateNotifications(lobbyMembershipChange);
                _log.InfoFormat(
                    "El jugador {0} salió de la sala {1} porque su canal de lobby terminó.",
                    lobbyMembershipChange.UserId,
                    lobbyMembershipChange.RoomId);
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

        private static IReadOnlyList<LobbyChangeNotification> CreateNotifications(
            LobbyMembershipChange lobbyMembershipChange)
        {
            List<LobbyChangeNotification> lobbyChangeNotifications = new List<LobbyChangeNotification>();

            if (lobbyMembershipChange.Room != null)
            {
                LobbyRoom lobbyRoom = LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);

                foreach (ILobbyManagerCallback recipientCallback in lobbyMembershipChange.RecipientCallbacks)
                {
                    lobbyChangeNotifications.Add(new LobbyChangeNotification(recipientCallback, lobbyRoom));
                }
            }

            IReadOnlyList<LobbyChangeNotification> readOnlyLobbyChangeNotifications =
                lobbyChangeNotifications.AsReadOnly();

            return readOnlyLobbyChangeNotifications;
        }
    }
}