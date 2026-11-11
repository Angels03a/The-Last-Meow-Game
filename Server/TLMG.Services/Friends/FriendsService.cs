using System;
using System.Collections.Generic;
using log4net;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Friends
{
    /// <summary>
    /// Lógica de la lista de amigos y las invitaciones a sala: consulta la amistad en la base de datos fuera de
    /// cualquier candado, delega en el registro de sesiones la parte atómica y arma las respuestas y los avisos; no
    /// conoce WCF y nunca notifica, eso lo hace quien la llama.
    /// </summary>
    public class FriendsService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FriendsService));

        private static readonly Dictionary<FriendsChangeStatus, string> _errorKeysByStatus =
            new Dictionary<FriendsChangeStatus, string>
            {
                { FriendsChangeStatus.InvalidSession, ErrorKeys.Common.InvalidSession },
                { FriendsChangeStatus.NotInRoom, ErrorKeys.Lobby.NotInRoom },
                { FriendsChangeStatus.RoomFull, ErrorKeys.Common.RoomFull },
                { FriendsChangeStatus.CannotInvite, ErrorKeys.Friends.CannotInvite },
                { FriendsChangeStatus.InvitationNotValid, ErrorKeys.Friends.InvitationNotValid }
            };

        private static readonly IReadOnlyList<Action> _noNotifications = new List<Action>().AsReadOnly();

        private IFriendshipRepository _friendshipRepository;
        private ISessionRegistry _sessionRegistry;

        /// <summary>Crea el servicio de amigos con su repositorio y el registro compartido de sesiones.</summary>
        /// <param name="friendshipRepository">Repositorio de amistades.</param>
        /// <param name="sessionRegistry">Registro de sesiones, salas, canales de amigos e invitaciones.</param>
        public FriendsService(IFriendshipRepository friendshipRepository, ISessionRegistry sessionRegistry)
        {
            _friendshipRepository = friendshipRepository;
            _sessionRegistry = sessionRegistry;
        }

        /// <summary>
        /// Registra el canal de amigos de la sesión, lo que deja al jugador en línea, y devuelve sus amigos aceptados
        /// indicando cuáles están en línea.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <param name="channelId">Identificador del canal de amigos desde el que llama el jugador.</param>
        /// <param name="friendsCallback">Callback del canal de amigos del jugador.</param>
        /// <returns>El resultado con los amigos o la clave de error.</returns>
        public ResultWithNotification<OperationSubscribeToFriendsResult> SubscribeToFriends(string sessionToken,
            string channelId,IFriendsManagerCallback friendsCallback)
        {
            ResultWithNotification<OperationSubscribeToFriendsResult> subscribeToFriendsResult;
            PlayerSession playerSession;

            if (!_sessionRegistry.TryFindSessionByToken(sessionToken, out playerSession))
            {
                LogRejectedOperation("suscribirse a los amigos", ValidationRules.NoUserId, "sesión no válida");
                subscribeToFriendsResult = new ResultWithNotification<OperationSubscribeToFriendsResult>(
                    new OperationSubscribeToFriendsResult { ErrorKey = ErrorKeys.Common.InvalidSession });
            }
            else
            {
                subscribeToFriendsResult = SubscribeFriendsChannel(playerSession, sessionToken, channelId, 
                    friendsCallback);
            }

            return subscribeToFriendsResult;
        }

        /// <summary>Invita a un amigo en línea a la sala del jugador de la sesión.</summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <param name="friendUserId">Id de la cuenta del amigo al que se invita.</param>
        /// <returns>El resultado para quien invita y el aviso de invitación para el invitado.</returns>
        public ResultWithNotification<OperationInviteFriendToRoomResult> InviteFriendToRoom(string sessionToken, 
            int friendUserId)
        {
            OperationInviteFriendToRoomResult operationInviteFriendToRoomResult = new OperationInviteFriendToRoomResult();
            IReadOnlyList<Action> createInvitationNotifications = new List<Action>().AsReadOnly();
            PlayerSession playerSession;

            if (!_sessionRegistry.TryFindSessionByToken(sessionToken, out playerSession))
            {
                operationInviteFriendToRoomResult.ErrorKey = ErrorKeys.Common.InvalidSession;
                LogRejectedOperation("invitar a un amigo", ValidationRules.NoUserId, "sesión no válida");
            }
            else
            {
                string errorKey = FindInvitationErrorKey(playerSession, friendUserId);

                if (errorKey == null)
                {
                    RoomInvitationCreation roomInvitationCreation = 
                        _sessionRegistry.CreateRoomInvitation(sessionToken, friendUserId);
                    operationInviteFriendToRoomResult = 
                        CreateInviteResult(playerSession, friendUserId, roomInvitationCreation);
                    createInvitationNotifications = 
                        CreateInvitationNotifications(roomInvitationCreation);
                }
                else
                {
                    operationInviteFriendToRoomResult.ErrorKey = errorKey;
                }
            }

            ResultWithNotification<OperationInviteFriendToRoomResult> inviteFriendToRoomResult =
                new ResultWithNotification<OperationInviteFriendToRoomResult>(operationInviteFriendToRoomResult, 
                createInvitationNotifications);

            return inviteFriendToRoomResult;
        }

        /// <summary>
        /// Acepta una invitación del jugador de la sesión y lo autoriza a entrar a la sala; no lo mete en ella.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <param name="invitationId">Id de la invitación recibida.</param>
        /// <returns>El resultado con el id de la sala o la clave de error.</returns>
        public ResultWithNotification<OperationAcceptRoomInvitationResult> AcceptRoomInvitation(string sessionToken, 
            int invitationId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance = 
                _sessionRegistry.AcceptRoomInvitation(sessionToken, invitationId);
            OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult 
                = new OperationAcceptRoomInvitationResult();

            if (roomInvitationAcceptance.Status == FriendsChangeStatus.Applied)
            {
                operationAcceptRoomInvitationResult.IsSuccess = true;
                operationAcceptRoomInvitationResult.RoomId = roomInvitationAcceptance.RoomId;
                _log.InfoFormat(
                    "El jugador {0} aceptó una invitación a la sala {1}.",
                    roomInvitationAcceptance.UserId,
                    roomInvitationAcceptance.RoomId);
            }
            else
            {
                operationAcceptRoomInvitationResult.ErrorKey = ResolveErrorKey(roomInvitationAcceptance.Status);
                LogRejectedOperation(
                    "aceptar una invitación",
                    roomInvitationAcceptance.UserId,
                    roomInvitationAcceptance.Status.ToString());
            }

            ResultWithNotification<OperationAcceptRoomInvitationResult> acceptRoomInvitationResult =
                new ResultWithNotification<OperationAcceptRoomInvitationResult>(operationAcceptRoomInvitationResult);

            return acceptRoomInvitationResult;
        }

        /// <summary>
        /// Solicita amistad al jugador con ese nickname: valida el nickname y la sesión de registrado, aplica las
        /// reglas de pareja (solicitud cruzada se acepta sola, rechazada se reactiva) y prepara los avisos que quien
        /// llama envía fuera de cualquier candado. Un error inesperado se registra y responde Common_UnexpectedError.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <param name="friendNickname">Nickname buscado.</param>
        /// <returns>El resultado y los avisos para el receptor o para los dos jugadores.</returns>
        public ResultWithNotification<OperationSendFriendRequestResult> SendFriendRequest(
            string sessionToken,
            string friendNickname)
        {
            OperationSendFriendRequestResult operationSendFriendRequestResult =
                new OperationSendFriendRequestResult();
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;

            try
            {
                string normalizedNickname;
                PlayerSession playerSession;
                string errorKey = FindSendRequestErrorKey(
                    sessionToken, friendNickname, out normalizedNickname, out playerSession);

                if (errorKey == null)
                {
                    friendRequestNotifications = ApplySendFriendRequest(
                        playerSession, normalizedNickname, operationSendFriendRequestResult);
                }
                else
                {
                    operationSendFriendRequestResult.ErrorKey = errorKey;
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _log.Error("Error inesperado al enviar una solicitud de amistad.", exception);
                operationSendFriendRequestResult =
                    new OperationSendFriendRequestResult { ErrorKey = ErrorKeys.Common.Unexpected };
                friendRequestNotifications = _noNotifications;
            }

            ResultWithNotification<OperationSendFriendRequestResult> sendFriendRequestResult =
                new ResultWithNotification<OperationSendFriendRequestResult>(
                    operationSendFriendRequestResult,
                    friendRequestNotifications);

            return sendFriendRequestResult;
        }

        /// <summary>
        /// Acepta una solicitud de amistad: la identidad sale de la sesión, solo el receptor de una solicitud
        /// pendiente puede aceptarla y los dos jugadores en línea reciben OnFriendAdded.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <param name="friendRequestId">Identificador de la solicitud recibida.</param>
        /// <returns>El resultado con el amigo nuevo o la clave de error, y los avisos.</returns>
        public ResultWithNotification<OperationAcceptFriendRequestResult> AcceptFriendRequest(
            string sessionToken,
            int friendRequestId)
        {
            OperationAcceptFriendRequestResult operationAcceptFriendRequestResult =
                new OperationAcceptFriendRequestResult();
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;

            try
            {
                FriendRequestAnswer friendRequestAnswer =
                    AnswerFriendRequest(sessionToken, friendRequestId, FriendshipState.Accepted);
                operationAcceptFriendRequestResult.ErrorKey = friendRequestAnswer.ErrorKey;

                if (friendRequestAnswer.ErrorKey == null)
                {
                    operationAcceptFriendRequestResult.IsSuccess = true;
                    operationAcceptFriendRequestResult.FriendRequestId = friendRequestId;
                    operationAcceptFriendRequestResult.NewFriend = friendRequestAnswer.NewFriend;
                    friendRequestNotifications = friendRequestAnswer.Notifications;
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _log.Error("Error inesperado al aceptar una solicitud de amistad.", exception);
                operationAcceptFriendRequestResult =
                    new OperationAcceptFriendRequestResult { ErrorKey = ErrorKeys.Common.Unexpected };
                friendRequestNotifications = _noNotifications;
            }

            ResultWithNotification<OperationAcceptFriendRequestResult> acceptFriendRequestResult =
                new ResultWithNotification<OperationAcceptFriendRequestResult>(
                    operationAcceptFriendRequestResult,
                    friendRequestNotifications);

            return acceptFriendRequestResult;
        }

        /// <summary>
        /// Rechaza una solicitud de amistad: la identidad sale de la sesión y solo el receptor de una solicitud
        /// pendiente puede rechazarla. Rechazar no se avisa a quien la envió.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <param name="friendRequestId">Identificador de la solicitud recibida.</param>
        /// <returns>El resultado con el id rechazado o la clave de error; sin avisos.</returns>
        public ResultWithNotification<OperationDeclineFriendRequestResult> DeclineFriendRequest(
            string sessionToken,
            int friendRequestId)
        {
            OperationDeclineFriendRequestResult operationDeclineFriendRequestResult =
                new OperationDeclineFriendRequestResult();

            try
            {
                FriendRequestAnswer friendRequestAnswer =
                    AnswerFriendRequest(sessionToken, friendRequestId, FriendshipState.Rejected);
                operationDeclineFriendRequestResult.ErrorKey = friendRequestAnswer.ErrorKey;

                if (friendRequestAnswer.ErrorKey == null)
                {
                    operationDeclineFriendRequestResult.IsSuccess = true;
                    operationDeclineFriendRequestResult.FriendRequestId = friendRequestId;
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _log.Error("Error inesperado al rechazar una solicitud de amistad.", exception);
                operationDeclineFriendRequestResult =
                    new OperationDeclineFriendRequestResult { ErrorKey = ErrorKeys.Common.Unexpected };
            }

            ResultWithNotification<OperationDeclineFriendRequestResult> declineFriendRequestResult =
                new ResultWithNotification<OperationDeclineFriendRequestResult>(operationDeclineFriendRequestResult);

            return declineFriendRequestResult;
        }

        /// <summary>
        /// Termina la sesión del jugador cuyo canal de amigos se cerró o falló: sale de su sala, pierde sus
        /// invitaciones y su callback, y se avisa a su sala y a sus amigos en línea.
        /// </summary>
        /// <param name="channelId">Identificador del canal de amigos que terminó.</param>
        /// <returns>Los avisos para los demás jugadores; vacía si el canal ya no era el de ninguna sesión.</returns>
        public IReadOnlyList<Action> EndSessionByFriendsChannel(string channelId)
        {
            SessionEnd sessionEnd;
            _sessionRegistry.TryEndSessionByFriendsChannel(channelId, out sessionEnd);
            IReadOnlyList<Action> createSessionEndNotifications = 
                SessionNotificationHelper.CreateSessionEndNotifications(sessionEnd);

            return createSessionEndNotifications;
        }

        private ResultWithNotification<OperationSubscribeToFriendsResult> SubscribeFriendsChannel(
            PlayerSession playerSession,
            string sessionToken,
            string channelId,
            IFriendsManagerCallback friendsCallback)
        {
            OperationSubscribeToFriendsResult operationSubscribeToFriendsResult = new OperationSubscribeToFriendsResult();
            IReadOnlyList<Action> subscribeToFriendsNotifications = new List<Action>().AsReadOnly();
            FriendListLookup friendListLookup = _friendshipRepository.FindAcceptedFriends(playerSession.UserId);
            PendingFriendRequestsLookup pendingFriendRequestsLookup =
                _friendshipRepository.FindPendingFriendRequests(playerSession.UserId);

            if (!friendListLookup.WasFriendListLookupSuccessful || !pendingFriendRequestsLookup.WasLookupSuccessful)
            {
                operationSubscribeToFriendsResult.ErrorKey = ErrorKeys.Common.Unexpected;
                LogRejectedOperation("suscribirse a los amigos", playerSession.UserId, "fallo de datos");
            }
            else
            {
                FriendsSubscription friendsSubscription = _sessionRegistry.SubscribeFriendsChannel(
                    sessionToken, channelId, friendsCallback, ExtractUserIds(friendListLookup.Friends));

                if (friendsSubscription.Status == FriendsChangeStatus.Applied)
                {
                    operationSubscribeToFriendsResult.IsSuccess = true;
                    operationSubscribeToFriendsResult.Friends = ConvertToFriends(friendListLookup.Friends, 
                        friendsSubscription.OnlineFriendUserIds);
                    operationSubscribeToFriendsResult.PendingFriendRequests =
                        ConvertToFriendRequests(pendingFriendRequestsLookup.Requests);
                    subscribeToFriendsNotifications = SessionNotificationHelper.CreatePresenceNotifications(
                        friendsSubscription.PresenceRecipientCallbacks, friendsSubscription.UserId,
                        friendsSubscription.Nickname,true);
                    _log.InfoFormat("El jugador {0} se suscribió a sus amigos.", friendsSubscription.UserId);
                }
                else
                {
                    operationSubscribeToFriendsResult.ErrorKey = ResolveErrorKey(friendsSubscription.Status);
                    LogRejectedOperation(
                        "suscribirse a los amigos",
                        playerSession.UserId,
                        friendsSubscription.Status.ToString());
                }
            }

            ResultWithNotification<OperationSubscribeToFriendsResult> subscribeToFriendsResult =
                new ResultWithNotification<OperationSubscribeToFriendsResult>(operationSubscribeToFriendsResult, 
                subscribeToFriendsNotifications);

            return subscribeToFriendsResult;
        }

        private string FindInvitationErrorKey(PlayerSession inviterSession, int friendUserId)
        {
            string errorKey = null;
            FriendshipLookupStatus friendshipLookupStatus = _friendshipRepository.FindFriendshipStatus(
                inviterSession.UserId, friendUserId);

            if (friendshipLookupStatus == FriendshipLookupStatus.DataFailure)
            {
                errorKey = ErrorKeys.Common.Unexpected;
                LogRejectedOperation("invitar a un amigo", inviterSession.UserId, "fallo de datos");
            }
            else if (friendshipLookupStatus == FriendshipLookupStatus.NotAccepted)
            {
                errorKey = ErrorKeys.Friends.CannotInvite;
                LogRejectedOperation("invitar a un amigo", inviterSession.UserId, "no son amigos aceptados");
            }

            return errorKey;
        }

        private OperationInviteFriendToRoomResult CreateInviteResult(PlayerSession inviterSession, int friendUserId,
            RoomInvitationCreation roomInvitationCreation)
        {
            OperationInviteFriendToRoomResult operationInviteFriendToRoomResult = 
                new OperationInviteFriendToRoomResult();

            if (roomInvitationCreation.Status == FriendsChangeStatus.Applied)
            {
                operationInviteFriendToRoomResult.IsSuccess = true;
                _log.InfoFormat(
                    "El jugador {0} invitó al jugador {1} a su sala.",
                    inviterSession.UserId,
                    friendUserId);
            }
            else
            {
                operationInviteFriendToRoomResult.ErrorKey = ResolveErrorKey(roomInvitationCreation.Status);
                LogRejectedOperation("invitar a un amigo", inviterSession.UserId, 
                    roomInvitationCreation.Status.ToString());
            }

            return operationInviteFriendToRoomResult;
        }

        private static IReadOnlyList<Action> CreateInvitationNotifications(RoomInvitationCreation 
            roomInvitationCreation)
        {
            List<Action> roomInvitationNotifications = new List<Action>();

            if (roomInvitationCreation.Status == FriendsChangeStatus.Applied && roomInvitationCreation.IsNewInvitation)
            {
                IFriendsManagerCallback inviteCallback = roomInvitationCreation.InviteeCallback;
                RoomInvitation roomInvitation = new RoomInvitation
                {
                    InvitationId = roomInvitationCreation.InvitationId,
                    InviterNickname = roomInvitationCreation.InviterNickname
                };

                roomInvitationNotifications.Add(() => inviteCallback.OnRoomInvitationReceived(roomInvitation));
            }

            IReadOnlyList<Action> invitationNotifications = roomInvitationNotifications.AsReadOnly();

            return invitationNotifications;
        }

        private string FindSendRequestErrorKey(
            string sessionToken,
            string friendNickname,
            out string normalizedNickname,
            out PlayerSession playerSession)
        {
            playerSession = null;
            string errorKey = FriendNicknameValidator.ValidateNickname(friendNickname, out normalizedNickname);

            if (errorKey != null)
            {
                LogRejectedOperation("solicitar amistad", ValidationRules.NoUserId, "nickname con formato no válido");
            }
            else if (!_sessionRegistry.TryFindSessionByToken(sessionToken, out playerSession))
            {
                errorKey = ErrorKeys.Common.InvalidSession;
                LogRejectedOperation("solicitar amistad", ValidationRules.NoUserId, "sesión no válida");
            }

            return errorKey;
        }

        private IReadOnlyList<Action> ApplySendFriendRequest(
            PlayerSession senderSession,
            string nickname,
            OperationSendFriendRequestResult operationSendFriendRequestResult)
        {
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;
            FriendCandidateLookup friendCandidateLookup = _friendshipRepository.FindFriendCandidateByNickname(nickname);

            if (friendCandidateLookup.Status == FriendLookupStatus.DataFailure)
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Common.Unexpected;
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "fallo de datos");
            }
            else if (friendCandidateLookup.Status == FriendLookupStatus.NotFound)
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Friends.UserNotFound;
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "el nickname no existe");
            }
            else if (friendCandidateLookup.UserId == senderSession.UserId)
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Friends.CannotRequestSelf;
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "solicitud a sí mismo");
            }
            else
            {
                friendRequestNotifications = ApplyPairRules(
                    senderSession, friendCandidateLookup, operationSendFriendRequestResult);
            }

            return friendRequestNotifications;
        }

        private IReadOnlyList<Action> ApplyPairRules(
            PlayerSession senderSession,
            FriendCandidateLookup friendCandidate,
            OperationSendFriendRequestResult operationSendFriendRequestResult)
        {
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;
            FriendshipRecordLookup friendshipRecordLookup =
                _friendshipRepository.FindFriendshipRecord(senderSession.UserId, friendCandidate.UserId);

            if (friendshipRecordLookup.Status == FriendLookupStatus.DataFailure)
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Common.Unexpected;
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "fallo de datos");
            }
            else if (friendshipRecordLookup.Status == FriendLookupStatus.NotFound)
            {
                FriendshipWriteResult friendshipWriteResult =
                    _friendshipRepository.CreateFriendRequest(senderSession.UserId, friendCandidate.UserId);
                friendRequestNotifications = CompletePendingRequest(
                    senderSession,
                    friendCandidate,
                    friendshipWriteResult.Status,
                    friendshipWriteResult.FriendshipId,
                    operationSendFriendRequestResult);
            }
            else
            {
                friendRequestNotifications = ApplyExistingRecordRules(
                    senderSession, friendCandidate, friendshipRecordLookup.Record, operationSendFriendRequestResult);
            }

            return friendRequestNotifications;
        }

        private IReadOnlyList<Action> ApplyExistingRecordRules(
            PlayerSession senderSession,
            FriendCandidateLookup friendCandidate,
            FriendshipRecord friendshipRecord,
            OperationSendFriendRequestResult operationSendFriendRequestResult)
        {
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;

            if (friendshipRecord.State == FriendshipState.Accepted)
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Friends.AlreadyFriends;
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "ya son amigos");
            }
            else if (friendshipRecord.State == FriendshipState.Pending
                && friendshipRecord.SenderUserId == senderSession.UserId)
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Friends.RequestAlreadySent;
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "ya había enviado una solicitud");
            }
            else if (friendshipRecord.State == FriendshipState.Pending)
            {
                friendRequestNotifications = AcceptCrossedRequest(
                    senderSession, friendCandidate, friendshipRecord, operationSendFriendRequestResult);
            }
            else
            {
                FriendshipWriteStatus friendshipWriteStatus = _friendshipRepository.ReactivateFriendRequest(
                    friendshipRecord.FriendshipId, senderSession.UserId, friendCandidate.UserId);
                friendRequestNotifications = CompletePendingRequest(
                    senderSession,
                    friendCandidate,
                    friendshipWriteStatus,
                    friendshipRecord.FriendshipId,
                    operationSendFriendRequestResult);
            }

            return friendRequestNotifications;
        }

        private IReadOnlyList<Action> CompletePendingRequest(
            PlayerSession senderSession,
            FriendCandidateLookup friendCandidate,
            FriendshipWriteStatus friendshipWriteStatus,
            int friendshipId,
            OperationSendFriendRequestResult operationSendFriendRequestResult)
        {
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;

            if (friendshipWriteStatus == FriendshipWriteStatus.Applied)
            {
                operationSendFriendRequestResult.IsSuccess = true;
                operationSendFriendRequestResult.SentFriendRequest = new FriendRequest
                {
                    FriendRequestId = friendshipId,
                    UserId = friendCandidate.UserId,
                    Nickname = friendCandidate.Nickname,
                    IsIncoming = false
                };
                friendRequestNotifications = CreateRequestReceivedNotifications(
                    senderSession, friendCandidate.UserId, friendshipId);
                _log.InfoFormat(
                    "El jugador {0} envió una solicitud de amistad al jugador {1}.",
                    senderSession.UserId,
                    friendCandidate.UserId);
            }
            else
            {
                operationSendFriendRequestResult.ErrorKey = ResolveWriteErrorKey(friendshipWriteStatus);
                LogRejectedOperation("solicitar amistad", senderSession.UserId, friendshipWriteStatus.ToString());
            }

            return friendRequestNotifications;
        }

        private IReadOnlyList<Action> AcceptCrossedRequest(
            PlayerSession senderSession,
            FriendCandidateLookup friendCandidate,
            FriendshipRecord friendshipRecord,
            OperationSendFriendRequestResult operationSendFriendRequestResult)
        {
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;
            FriendshipWriteStatus friendshipWriteStatus = _friendshipRepository.AnswerFriendRequest(
                friendshipRecord.FriendshipId, senderSession.UserId, FriendshipState.Accepted);

            if (friendshipWriteStatus == FriendshipWriteStatus.Applied)
            {
                FriendsAddition friendsAddition = _sessionRegistry.AddFriendshipToOnlineChannels(
                    senderSession.UserId, friendCandidate.UserId);
                operationSendFriendRequestResult.IsSuccess = true;
                operationSendFriendRequestResult.NewFriend = new Friend
                {
                    UserId = friendCandidate.UserId,
                    Nickname = friendCandidate.Nickname,
                    IsOnline = friendsAddition.IsSecondOnline
                };
                friendRequestNotifications = CreateFriendAddedNotifications(
                    friendsAddition,
                    senderSession.UserId,
                    senderSession.Nickname,
                    friendCandidate.UserId,
                    friendCandidate.Nickname);
                _log.InfoFormat(
                    "La solicitud cruzada entre los jugadores {0} y {1} se aceptó sola.",
                    senderSession.UserId,
                    friendCandidate.UserId);
            }
            else
            {
                operationSendFriendRequestResult.ErrorKey = ResolveWriteErrorKey(friendshipWriteStatus);
                LogRejectedOperation("solicitar amistad", senderSession.UserId, friendshipWriteStatus.ToString());
            }

            return friendRequestNotifications;
        }

        private FriendRequestAnswer AnswerFriendRequest(
            string sessionToken,
            int friendRequestId,
            FriendshipState newState)
        {
            FriendRequestAnswer friendRequestAnswer = new FriendRequestAnswer();
            PlayerSession playerSession;

            if (!_sessionRegistry.TryFindSessionByToken(sessionToken, out playerSession))
            {
                friendRequestAnswer.ErrorKey = ErrorKeys.Common.InvalidSession;
                LogRejectedOperation(
                    "responder una solicitud de amistad", ValidationRules.NoUserId, "sesión no válida");
            }
            else
            {
                FriendshipRecordLookup friendshipRecordLookup =
                    _friendshipRepository.FindFriendshipRecordById(friendRequestId);

                if (friendshipRecordLookup.Status == FriendLookupStatus.DataFailure)
                {
                    friendRequestAnswer.ErrorKey = ErrorKeys.Common.Unexpected;
                    LogRejectedOperation("responder una solicitud de amistad", playerSession.UserId, "fallo de datos");
                }
                else if (friendshipRecordLookup.Status == FriendLookupStatus.NotFound
                    || !IsPendingRequestOfReceiver(friendshipRecordLookup.Record, playerSession.UserId))
                {
                    friendRequestAnswer.ErrorKey = ErrorKeys.Friends.RequestNotValid;
                    LogRejectedOperation(
                        "responder una solicitud de amistad", playerSession.UserId, "la solicitud no es válida");
                }
                else
                {
                    ApplyAnswer(playerSession, friendshipRecordLookup.Record, newState, friendRequestAnswer);
                }
            }

            return friendRequestAnswer;
        }

        private void ApplyAnswer(
            PlayerSession receiverSession,
            FriendshipRecord friendshipRecord,
            FriendshipState newState,
            FriendRequestAnswer friendRequestAnswer)
        {
            FriendshipWriteStatus friendshipWriteStatus = _friendshipRepository.AnswerFriendRequest(
                friendshipRecord.FriendshipId, receiverSession.UserId, newState);

            if (friendshipWriteStatus != FriendshipWriteStatus.Applied)
            {
                friendRequestAnswer.ErrorKey = ResolveWriteErrorKey(friendshipWriteStatus);
                LogRejectedOperation(
                    "responder una solicitud de amistad", receiverSession.UserId, friendshipWriteStatus.ToString());
            }
            else if (newState == FriendshipState.Accepted)
            {
                FriendsAddition friendsAddition = _sessionRegistry.AddFriendshipToOnlineChannels(
                    receiverSession.UserId, friendshipRecord.SenderUserId);
                friendRequestAnswer.NewFriend = new Friend
                {
                    UserId = friendshipRecord.SenderUserId,
                    Nickname = friendshipRecord.SenderNickname,
                    IsOnline = friendsAddition.IsSecondOnline
                };
                friendRequestAnswer.Notifications = CreateFriendAddedNotifications(
                    friendsAddition,
                    receiverSession.UserId,
                    receiverSession.Nickname,
                    friendshipRecord.SenderUserId,
                    friendshipRecord.SenderNickname);
                _log.InfoFormat(
                    "El jugador {0} aceptó la solicitud de amistad del jugador {1}.",
                    receiverSession.UserId,
                    friendshipRecord.SenderUserId);
            }
            else
            {
                _log.InfoFormat(
                    "El jugador {0} rechazó la solicitud de amistad del jugador {1}.",
                    receiverSession.UserId,
                    friendshipRecord.SenderUserId);
            }
        }

        private static bool IsPendingRequestOfReceiver(FriendshipRecord friendshipRecord, int receiverUserId)
        {
            bool isPendingRequestOfReceiver = friendshipRecord.State == FriendshipState.Pending
                && friendshipRecord.ReceiverUserId == receiverUserId;

            return isPendingRequestOfReceiver;
        }

        private IReadOnlyList<Action> CreateRequestReceivedNotifications(
            PlayerSession senderSession,
            int receiverUserId,
            int friendshipId)
        {
            List<Action> notifications = new List<Action>();
            IFriendsManagerCallback receiverCallback = _sessionRegistry.FindFriendsCallbackOfUser(receiverUserId);

            if (receiverCallback != null)
            {
                FriendRequest receivedRequest = new FriendRequest
                {
                    FriendRequestId = friendshipId,
                    UserId = senderSession.UserId,
                    Nickname = senderSession.Nickname,
                    IsIncoming = true
                };

                notifications.Add(() => receiverCallback.OnFriendRequestReceived(receivedRequest));
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        private static IReadOnlyList<Action> CreateFriendAddedNotifications(
            FriendsAddition friendsAddition,
            int firstUserId,
            string firstNickname,
            int secondUserId,
            string secondNickname)
        {
            List<Action> notifications = new List<Action>();

            if (friendsAddition.IsFirstOnline)
            {
                IFriendsManagerCallback firstCallback = friendsAddition.FirstCallback;
                Friend secondAsFriend = new Friend
                {
                    UserId = secondUserId,
                    Nickname = secondNickname,
                    IsOnline = friendsAddition.IsSecondOnline
                };

                notifications.Add(() => firstCallback.OnFriendAdded(secondAsFriend));
            }

            if (friendsAddition.IsSecondOnline)
            {
                IFriendsManagerCallback secondCallback = friendsAddition.SecondCallback;
                Friend firstAsFriend = new Friend
                {
                    UserId = firstUserId,
                    Nickname = firstNickname,
                    IsOnline = friendsAddition.IsFirstOnline
                };

                notifications.Add(() => secondCallback.OnFriendAdded(firstAsFriend));
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        private static string ResolveWriteErrorKey(FriendshipWriteStatus friendshipWriteStatus)
        {
            string errorKey = ErrorKeys.Common.Unexpected;

            if (friendshipWriteStatus == FriendshipWriteStatus.DuplicatePair)
            {
                errorKey = ErrorKeys.Friends.RequestAlreadySent;
            }
            else if (friendshipWriteStatus == FriendshipWriteStatus.StateChanged)
            {
                errorKey = ErrorKeys.Friends.RequestNotValid;
            }

            return errorKey;
        }

        private static List<FriendRequest> ConvertToFriendRequests(List<PendingFriendRequest> pendingRequests)
        {
            List<FriendRequest> friendRequests = new List<FriendRequest>();

            foreach (PendingFriendRequest pendingRequest in pendingRequests)
            {
                friendRequests.Add(new FriendRequest
                {
                    FriendRequestId = pendingRequest.FriendshipId,
                    UserId = pendingRequest.OtherUserId,
                    Nickname = pendingRequest.OtherNickname,
                    IsIncoming = pendingRequest.IsIncoming
                });
            }

            return friendRequests;
        }

        private sealed class FriendRequestAnswer
        {
            public string ErrorKey { get; set; }

            public Friend NewFriend { get; set; }

            public IReadOnlyList<Action> Notifications { get; set; } = _noNotifications;
        }

        private static string ResolveErrorKey(FriendsChangeStatus friendsChangeStatus)
        {
            string errorKey;

            if (!_errorKeysByStatus.TryGetValue(friendsChangeStatus, out errorKey))
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
        }

        private static void LogRejectedOperation(string operationDescription, int userId, string reason)
        {
            _log.WarnFormat("Se rechazó {0} del jugador {1}: {2}.", operationDescription, userId, reason);
        }

        private static IReadOnlyList<int> ExtractUserIds(List<AcceptedFriend> acceptedFriends)
        {
            List<int> userIds = new List<int>();

            foreach (AcceptedFriend acceptedFriend in acceptedFriends)
            {
                userIds.Add(acceptedFriend.UserId);
            }

            IReadOnlyList<int> readOnlyUserIds = userIds.AsReadOnly();

            return readOnlyUserIds;
        }

        private static List<Friend> ConvertToFriends(
            List<AcceptedFriend> acceptedFriends,
            IReadOnlyList<int> onlineFriendUserIds)
        {
            HashSet<int> onlineUserIds = new HashSet<int>(onlineFriendUserIds);
            List<Friend> friends = new List<Friend>();

            foreach (AcceptedFriend acceptedFriend in acceptedFriends)
            {
                friends.Add(new Friend
                {
                    UserId = acceptedFriend.UserId,
                    Nickname = acceptedFriend.Nickname,
                    IsOnline = onlineUserIds.Contains(acceptedFriend.UserId)
                });
            }

            return friends;
        }
    }
}