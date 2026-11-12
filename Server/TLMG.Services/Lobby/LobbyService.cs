using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    /// <summary>
    /// Lógica de las salas: valida, delega la regla atómica en el registro de sesiones y arma las respuestas y los
    /// avisos; no conoce WCF y nunca notifica, eso lo hace quien la llama fuera de cualquier candado.
    /// </summary>
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

        /// <summary>Crea el servicio de salas con el registro compartido de sesiones.</summary>
        /// <param name="sessionRegistry">Registro de sesiones y salas.</param>
        public LobbyService(ISessionRegistry sessionRegistry)
        {
            _sessionRegistry = sessionRegistry;
        }

        /// <summary>Crea una sala nueva para el jugador de la sesión y lo deja dentro.</summary>
        /// <param name="caller">Token de sesión y canal de lobby desde el que llama el jugador.</param>
        /// <param name="lobbyCallback">Callback del canal de lobby del jugador.</param>
        /// <returns>El resultado con la sala o la clave de error; no hay avisos para otros jugadores.</returns>
        public LobbyOperationResultWithNotification<OperationCreateRoomResult> CreateRoom(
            LobbyCaller caller,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.CreateRoom(caller, lobbyCallback);
            OperationCreateRoomResult operationCreateRoomResult = new OperationCreateRoomResult();

            if (lobbyMembershipChange.Status == LobbyChangeStatus.Applied)
            {
                operationCreateRoomResult.IsSuccess = true;
                operationCreateRoomResult.LobbyRoom = LobbyRoomHelper.ConvertToLobbyRoom(lobbyMembershipChange.Room);
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

            LobbyOperationResultWithNotification<OperationCreateRoomResult> createRoomResult =
                new LobbyOperationResultWithNotification<OperationCreateRoomResult>(operationCreateRoomResult);

            return createRoomResult;
        }

        /// <summary>Mete al jugador de la sesión en una sala si tiene autorización de entrada y hay cupo.</summary>
        /// <param name="caller">Token de sesión y canal de lobby desde el que llama el jugador.</param>
        /// <param name="roomId">Identificador de la sala a la que quiere entrar.</param>
        /// <param name="lobbyCallback">Callback del canal de lobby del jugador.</param>
        /// <returns>El resultado con la sala o la clave de error, y los avisos para los demás de la sala.</returns>
        public LobbyOperationResultWithNotification<OperationEnterLobbyResult> EnterLobby(
            LobbyCaller caller,
            int roomId,
            ILobbyManagerCallback lobbyCallback)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.EnterRoom(caller, roomId, lobbyCallback);
            OperationEnterLobbyResult operationEnterLobbyResult = new OperationEnterLobbyResult();
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

            LobbyOperationResultWithNotification<OperationEnterLobbyResult> enterLobbyResult =
                new LobbyOperationResultWithNotification<OperationEnterLobbyResult>(
                    operationEnterLobbyResult,
                    lobbyNotifications);

            return enterLobbyResult;
        }

        /// <summary>Saca al jugador de la sesión de su sala; salir sin estar en una sala también es éxito.</summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <returns>El resultado y los avisos para los jugadores que se quedan en la sala.</returns>
        public LobbyOperationResultWithNotification<OperationLeaveLobbyResult> LeaveLobby(string sessionToken)
        {
            LobbyMembershipChange lobbyMembershipChange = _sessionRegistry.LeaveRoom(sessionToken);
            OperationLeaveLobbyResult operationLeaveLobbyResult = new OperationLeaveLobbyResult();
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

            LobbyOperationResultWithNotification<OperationLeaveLobbyResult> leaveLobbyResult =
                new LobbyOperationResultWithNotification<OperationLeaveLobbyResult>(
                    operationLeaveLobbyResult,
                    lobbyChangeNotifications);

            return leaveLobbyResult;
        }

        /// <summary>Saca de su sala al jugador cuyo canal de lobby se cerró o falló.</summary>
        /// <param name="channelId">Identificador del canal de lobby que terminó.</param>
        /// <returns>Los avisos para quienes se quedan en la sala; vacía si el canal no estaba en una sala.</returns>
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