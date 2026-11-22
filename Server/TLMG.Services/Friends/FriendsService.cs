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

        private readonly IFriendshipRepository _friendshipRepository;
        private readonly ISessionRegistry _sessionRegistry;

        public FriendsService(IFriendshipRepository friendshipRepository, ISessionRegistry sessionRegistry)
        {
            _friendshipRepository = friendshipRepository;
            _sessionRegistry = sessionRegistry;
        }

        public ResultWithNotification<OperationSubscribeToFriendsResult> SubscribeToFriends(
            string sessionToken,
            string channelId,
            IFriendsManagerCallback friendsCallback)
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
                SubscriptionChannelRequest subscriptionRequest = new SubscriptionChannelRequest
                {
                    PlayerSession = playerSession,
                    SessionToken = sessionToken,
                    ChannelId = channelId,
                    FriendsCallback = friendsCallback
                };
                subscribeToFriendsResult = SubscribeFriendsChannel(subscriptionRequest);
            }

            return subscribeToFriendsResult;
        }

        public ResultWithNotification<OperationResult> InviteFriendToRoom(
            string sessionToken,
            int friendUserId)
        {
            OperationResult operationInviteFriendToRoomResult = new OperationResult();
            IReadOnlyList<Action> createInvitationNotifications = _noNotifications;
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
                    if (roomInvitationCreation == null)
                    {
                        operationInviteFriendToRoomResult.ErrorKey = ErrorKeys.Common.Unexpected;
                    }
                    else
                    {
                        operationInviteFriendToRoomResult =
                            CreateInviteResult(playerSession, friendUserId, roomInvitationCreation);
                        createInvitationNotifications = CreateInvitationNotifications(roomInvitationCreation);
                    }
                }
                else
                {
                    operationInviteFriendToRoomResult.ErrorKey = errorKey;
                }
            }

            ResultWithNotification<OperationResult> inviteFriendToRoomResult =
                new ResultWithNotification<OperationResult>(
                    operationInviteFriendToRoomResult,
                    createInvitationNotifications);

            return inviteFriendToRoomResult;
        }

        public ResultWithNotification<OperationAcceptRoomInvitationResult> AcceptRoomInvitation(
            string sessionToken,
            int invitationId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance =
                _sessionRegistry.AcceptRoomInvitation(sessionToken, invitationId);
            OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult =
                new OperationAcceptRoomInvitationResult();

            if (roomInvitationAcceptance == null)
            {
                operationAcceptRoomInvitationResult.ErrorKey = ErrorKeys.Common.Unexpected;
            }
            else if (roomInvitationAcceptance.Status == FriendsChangeStatus.Applied)
            {
                operationAcceptRoomInvitationResult.IsSuccess = true;
                operationAcceptRoomInvitationResult.ErrorKey = string.Empty;
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

        public ResultWithNotification<OperationSendFriendRequestResult> SendFriendRequest(
            string sessionToken,
            string friendNickname)
        {
            OperationSendFriendRequestResult operationSendFriendRequestResult =
                new OperationSendFriendRequestResult();
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;
            FriendRequestValidation validationResult = ValidateSendRequest(sessionToken, friendNickname);

            if (validationResult.ErrorKey == null)
            {
                friendRequestNotifications = ApplySendFriendRequest(
                    validationResult.PlayerSession,
                    validationResult.NormalizedNickname,
                    operationSendFriendRequestResult);
            }
            else
            {
                operationSendFriendRequestResult.ErrorKey = validationResult.ErrorKey;
            }

            ResultWithNotification<OperationSendFriendRequestResult> sendFriendRequestResult =
                new ResultWithNotification<OperationSendFriendRequestResult>(
                    operationSendFriendRequestResult,
                    friendRequestNotifications);

            return sendFriendRequestResult;
        }

        public ResultWithNotification<OperationAcceptFriendRequestResult> AcceptFriendRequest(
            string sessionToken,
            int friendRequestId)
        {
            OperationAcceptFriendRequestResult operationAcceptFriendRequestResult =
                new OperationAcceptFriendRequestResult();
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;

            FriendRequestAnswer friendRequestAnswer =
                AnswerFriendRequest(sessionToken, friendRequestId, FriendshipState.Accepted);
            operationAcceptFriendRequestResult.ErrorKey = friendRequestAnswer.ErrorKey;

            if (friendRequestAnswer.IsSuccess)
            {
                operationAcceptFriendRequestResult.IsSuccess = true;
                operationAcceptFriendRequestResult.ErrorKey = string.Empty;
                operationAcceptFriendRequestResult.FriendRequestId = friendRequestId;
                operationAcceptFriendRequestResult.NewFriend = friendRequestAnswer.NewFriend;
                friendRequestNotifications = friendRequestAnswer.Notifications;
            }

            ResultWithNotification<OperationAcceptFriendRequestResult> acceptFriendRequestResult =
                new ResultWithNotification<OperationAcceptFriendRequestResult>(
                    operationAcceptFriendRequestResult,
                    friendRequestNotifications);

            return acceptFriendRequestResult;
        }

        public ResultWithNotification<OperationDeclineFriendRequestResult> DeclineFriendRequest(
            string sessionToken,
            int friendRequestId)
        {
            OperationDeclineFriendRequestResult operationDeclineFriendRequestResult =
                new OperationDeclineFriendRequestResult();

            FriendRequestAnswer friendRequestAnswer =
                AnswerFriendRequest(sessionToken, friendRequestId, FriendshipState.Rejected);
            operationDeclineFriendRequestResult.ErrorKey = friendRequestAnswer.ErrorKey;

            if (friendRequestAnswer.IsSuccess)
            {
                operationDeclineFriendRequestResult.IsSuccess = true;
                operationDeclineFriendRequestResult.ErrorKey = string.Empty;
                operationDeclineFriendRequestResult.FriendRequestId = friendRequestId;
            }

            ResultWithNotification<OperationDeclineFriendRequestResult> declineFriendRequestResult =
                new ResultWithNotification<OperationDeclineFriendRequestResult>(operationDeclineFriendRequestResult);

            return declineFriendRequestResult;
        }

        public IReadOnlyList<Action> EndSessionByFriendsChannel(string channelId)
        {
            SessionEnd sessionEnd;
            _sessionRegistry.TryEndSessionByFriendsChannel(channelId, out sessionEnd);
            IReadOnlyList<Action> createSessionEndNotifications =
                SessionNotificationHelper.CreateSessionEndNotifications(sessionEnd);

            return createSessionEndNotifications;
        }

        private ResultWithNotification<OperationSubscribeToFriendsResult> SubscribeFriendsChannel(
            SubscriptionChannelRequest request)
        {
            OperationSubscribeToFriendsResult operationSubscribeToFriendsResult =
                new OperationSubscribeToFriendsResult();
            IReadOnlyList<Action> subscribeToFriendsNotifications = _noNotifications;
            FriendListLookup friendListLookup = _friendshipRepository.FindAcceptedFriends(request.PlayerSession.UserId);
            PendingFriendRequestsLookup pendingFriendRequestsLookup =
                _friendshipRepository.FindPendingFriendRequests(request.PlayerSession.UserId);

            if (friendListLookup == null || pendingFriendRequestsLookup == null
                || !friendListLookup.WasFriendListLookupSuccessful || !pendingFriendRequestsLookup.WasLookupSuccessful
                || friendListLookup.Friends == null || pendingFriendRequestsLookup.Requests == null)
            {
                operationSubscribeToFriendsResult.ErrorKey = ErrorKeys.Common.Unexpected;
                LogRejectedOperation("suscribirse a los amigos", request.PlayerSession.UserId, "fallo de datos");
            }
            else
            {
                FriendsSubscription friendsSubscription = _sessionRegistry.SubscribeFriendsChannel(
                    request.SessionToken,
                    request.ChannelId,
                    request.FriendsCallback,
                    ExtractUserIds(friendListLookup.Friends));

                if (friendsSubscription == null)
                {
                    operationSubscribeToFriendsResult.ErrorKey = ErrorKeys.Common.Unexpected;
                }
                else if (friendsSubscription.Status == FriendsChangeStatus.Applied)
                {
                    operationSubscribeToFriendsResult.IsSuccess = true;
                    operationSubscribeToFriendsResult.ErrorKey = string.Empty;
                    operationSubscribeToFriendsResult.Friends = ConvertToFriends(
                        friendListLookup.Friends,
                        friendsSubscription.OnlineFriendUserIds);
                    operationSubscribeToFriendsResult.PendingFriendRequests =
                        ConvertToFriendRequests(pendingFriendRequestsLookup.Requests);
                    subscribeToFriendsNotifications = SessionNotificationHelper.CreatePresenceNotifications(
                        friendsSubscription.PresenceRecipientCallbacks,
                        friendsSubscription.UserId,
                        friendsSubscription.Nickname,
                        true);
                    _log.InfoFormat("El jugador {0} se suscribió a sus amigos.", friendsSubscription.UserId);
                }
                else
                {
                    operationSubscribeToFriendsResult.ErrorKey = ResolveErrorKey(friendsSubscription.Status);
                    LogRejectedOperation(
                        "suscribirse a los amigos",
                        request.PlayerSession.UserId,
                        friendsSubscription.Status.ToString());
                }
            }

            ResultWithNotification<OperationSubscribeToFriendsResult> subscribeToFriendsResult =
                new ResultWithNotification<OperationSubscribeToFriendsResult>(
                    operationSubscribeToFriendsResult,
                    subscribeToFriendsNotifications);

            return subscribeToFriendsResult;
        }

        private string FindInvitationErrorKey(PlayerSession inviterSession, int friendUserId)
        {
            string errorKey = null;
            FriendshipLookupStatus friendshipLookupStatus = _friendshipRepository.FindFriendshipStatus(
                inviterSession.UserId,
                friendUserId);

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
            else if (friendshipLookupStatus != FriendshipLookupStatus.Accepted)
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
        }

        private OperationResult CreateInviteResult(
            PlayerSession inviterSession,
            int friendUserId,
            RoomInvitationCreation roomInvitationCreation)
        {
            OperationResult operationInviteFriendToRoomResult = new OperationResult();

            if (roomInvitationCreation.Status == FriendsChangeStatus.Applied)
            {
                operationInviteFriendToRoomResult.IsSuccess = true;
                operationInviteFriendToRoomResult.ErrorKey = string.Empty;
                _log.InfoFormat(
                    "El jugador {0} invitó al jugador {1} a su sala.",
                    inviterSession.UserId,
                    friendUserId);
            }
            else
            {
                operationInviteFriendToRoomResult.ErrorKey = ResolveErrorKey(roomInvitationCreation.Status);
                LogRejectedOperation(
                    "invitar a un amigo",
                    inviterSession.UserId,
                    roomInvitationCreation.Status.ToString());
            }

            return operationInviteFriendToRoomResult;
        }

        private static IReadOnlyList<Action> CreateInvitationNotifications(
            RoomInvitationCreation roomInvitationCreation)
        {
            List<Action> roomInvitationNotifications = new List<Action>();

            if (roomInvitationCreation.Status == FriendsChangeStatus.Applied
                && roomInvitationCreation.ShouldNotifyInvitee)
            {
                IFriendsManagerCallback inviteCallback = roomInvitationCreation.InviteeCallback;
                RoomInvitation roomInvitation = new RoomInvitation
                {
                    InvitationId = roomInvitationCreation.InvitationId,
                    InviterNickname = roomInvitationCreation.InviterNickname
                };

                roomInvitationNotifications.Add(
                    () => inviteCallback.OnRoomInvitationReceived(roomInvitation));
            }

            IReadOnlyList<Action> invitationNotifications = roomInvitationNotifications.AsReadOnly();

            return invitationNotifications;
        }

        private FriendRequestValidation ValidateSendRequest(string sessionToken, string friendNickname)
        {
            FriendRequestValidation validationResult = new FriendRequestValidation();
            string normalizedNickname;
            string errorKey = FriendNicknameValidator.ValidateNickname(friendNickname, out normalizedNickname);

            if (errorKey != null)
            {
                validationResult.ErrorKey = errorKey;
                LogRejectedOperation("solicitar amistad", ValidationRules.NoUserId, "nickname con formato no válido");
            }
            else
            {
                PlayerSession playerSession;

                if (!_sessionRegistry.TryFindSessionByToken(sessionToken, out playerSession))
                {
                    validationResult.ErrorKey = ErrorKeys.Common.InvalidSession;
                    LogRejectedOperation("solicitar amistad", ValidationRules.NoUserId, "sesión no válida");
                }
                else
                {
                    validationResult.PlayerSession = playerSession;
                    validationResult.NormalizedNickname = normalizedNickname;
                }
            }

            return validationResult;
        }

        private IReadOnlyList<Action> ApplySendFriendRequest(
            PlayerSession senderSession,
            string nickname,
            OperationSendFriendRequestResult operationSendFriendRequestResult)
        {
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;
            FriendCandidateLookup friendCandidateLookup = _friendshipRepository.FindFriendCandidateByNickname(nickname);

            if (friendCandidateLookup == null || (friendCandidateLookup.Status != FriendLookupStatus.Found
                && friendCandidateLookup.Status != FriendLookupStatus.NotFound))
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Common.Unexpected;
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "fallo de datos");
            }
            else if (friendCandidateLookup.Status == FriendLookupStatus.NotFound)
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Friends.UserNotFound;
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "el nickname no existe");
            }
            else if (friendCandidateLookup.UserId <= ValidationRules.NoUserId
                || string.IsNullOrWhiteSpace(friendCandidateLookup.Nickname))
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Common.Unexpected;
            }
            else if (friendCandidateLookup.UserId == senderSession.UserId)
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Friends.CannotRequestSelf;
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "solicitud a sí mismo");
            }
            else
            {
                friendRequestNotifications = ApplyPairRules(
                    senderSession,
                    friendCandidateLookup,
                    operationSendFriendRequestResult);
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

            if (friendshipRecordLookup == null || (friendshipRecordLookup.Status != FriendLookupStatus.Found
                && friendshipRecordLookup.Status != FriendLookupStatus.NotFound)
                || (friendshipRecordLookup.Status == FriendLookupStatus.Found && friendshipRecordLookup.Record == null))
            {
                operationSendFriendRequestResult.ErrorKey = ErrorKeys.Common.Unexpected;
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "fallo de datos");
            }
            else if (friendshipRecordLookup.Status == FriendLookupStatus.NotFound)
            {
                FriendshipWriteResult friendshipWriteResult =
                    _friendshipRepository.CreateFriendRequest(senderSession.UserId, friendCandidate.UserId);
                if (friendshipWriteResult == null)
                {
                    operationSendFriendRequestResult.ErrorKey = ErrorKeys.Common.Unexpected;
                }
                else
                {
                    PendingRequestCompletion completionContext = new PendingRequestCompletion
                    {
                        SenderSession = senderSession,
                        FriendCandidate = friendCandidate,
                        WriteStatus = friendshipWriteResult.Status,
                        FriendshipId = friendshipWriteResult.FriendshipId,
                        Result = operationSendFriendRequestResult
                    };
                    friendRequestNotifications = CompletePendingRequest(completionContext);
                }
            }
            else
            {
                ExistingRecordRuleContext ruleContext = new ExistingRecordRuleContext
                {
                    SenderSession = senderSession,
                    FriendCandidate = friendCandidate,
                    FriendshipRecord = friendshipRecordLookup.Record,
                    Result = operationSendFriendRequestResult
                };
                friendRequestNotifications = ApplyExistingRecordRules(ruleContext);
            }

            return friendRequestNotifications;
        }

        private IReadOnlyList<Action> ApplyExistingRecordRules(ExistingRecordRuleContext context)
        {
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;

            if (context.FriendshipRecord.State == FriendshipState.Accepted)
            {
                context.Result.ErrorKey = ErrorKeys.Friends.AlreadyFriends;
                LogRejectedOperation("solicitar amistad", context.SenderSession.UserId, "ya son amigos");
            }
            else if (context.FriendshipRecord.State == FriendshipState.Pending
                && context.FriendshipRecord.SenderUserId == context.SenderSession.UserId)
            {
                context.Result.ErrorKey = ErrorKeys.Friends.RequestAlreadySent;
                LogRejectedOperation("solicitar amistad", context.SenderSession.UserId, "ya había enviado una solicitud");
            }
            else if (context.FriendshipRecord.State == FriendshipState.Pending)
            {
                friendRequestNotifications = AcceptCrossedRequest(context);
            }
            else if (context.FriendshipRecord.State == FriendshipState.Rejected)
            {
                FriendshipWriteStatus friendshipWriteStatus = _friendshipRepository.ReactivateFriendRequest(
                    context.FriendshipRecord.FriendshipId,
                    context.SenderSession.UserId,
                    context.FriendCandidate.UserId);
                PendingRequestCompletion completionContext = new PendingRequestCompletion
                {
                    SenderSession = context.SenderSession,
                    FriendCandidate = context.FriendCandidate,
                    WriteStatus = friendshipWriteStatus,
                    FriendshipId = context.FriendshipRecord.FriendshipId,
                    Result = context.Result
                };
                friendRequestNotifications = CompletePendingRequest(completionContext);
            }
            else
            {
                context.Result.ErrorKey = ErrorKeys.Common.Unexpected;
            }

            return friendRequestNotifications;
        }

        private IReadOnlyList<Action> CompletePendingRequest(PendingRequestCompletion context)
        {
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;

            if (context.WriteStatus == FriendshipWriteStatus.Applied)
            {
                context.Result.IsSuccess = true;
                context.Result.ErrorKey = string.Empty;
                context.Result.SentFriendRequest = new FriendRequest
                {
                    FriendRequestId = context.FriendshipId,
                    UserId = context.FriendCandidate.UserId,
                    Nickname = context.FriendCandidate.Nickname,
                    IsIncoming = false
                };
                friendRequestNotifications = CreateRequestReceivedNotifications(
                    context.SenderSession,
                    context.FriendCandidate.UserId,
                    context.FriendshipId);
                _log.InfoFormat(
                    "El jugador {0} envió una solicitud de amistad al jugador {1}.",
                    context.SenderSession.UserId,
                    context.FriendCandidate.UserId);
            }
            else
            {
                context.Result.ErrorKey = ResolveWriteErrorKey(context.WriteStatus);
                LogRejectedOperation(
                    "solicitar amistad",
                    context.SenderSession.UserId,
                    context.WriteStatus.ToString());
            }

            return friendRequestNotifications;
        }

        private IReadOnlyList<Action> AcceptCrossedRequest(ExistingRecordRuleContext context)
        {
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;
            FriendshipWriteStatus friendshipWriteStatus = _friendshipRepository.AnswerFriendRequest(
                context.FriendshipRecord.FriendshipId,
                context.SenderSession.UserId,
                FriendshipState.Accepted);

            if (friendshipWriteStatus == FriendshipWriteStatus.Applied)
            {
                Friend newFriend;
                friendRequestNotifications = PrepareFriendshipNotifications(
                    context.SenderSession, context.FriendCandidate.UserId,
                    context.FriendCandidate.Nickname, out newFriend);
                context.Result.IsSuccess = true;
                context.Result.ErrorKey = string.Empty;
                context.Result.NewFriend = newFriend;
                _log.InfoFormat(
                    "La solicitud cruzada entre los jugadores {0} y {1} se aceptó sola.",
                    context.SenderSession.UserId,
                    context.FriendCandidate.UserId);
            }
            else
            {
                context.Result.ErrorKey = ResolveWriteErrorKey(friendshipWriteStatus);
                LogRejectedOperation(
                    "solicitar amistad",
                    context.SenderSession.UserId,
                    friendshipWriteStatus.ToString());
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

                if (friendshipRecordLookup == null || (friendshipRecordLookup.Status != FriendLookupStatus.Found
                    && friendshipRecordLookup.Status != FriendLookupStatus.NotFound)
                    || (friendshipRecordLookup.Status == FriendLookupStatus.Found && friendshipRecordLookup.Record == null))
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
                    FriendRequestAnswerContext answerContext = new FriendRequestAnswerContext
                    {
                        ReceiverSession = playerSession,
                        FriendshipRecord = friendshipRecordLookup.Record,
                        NewState = newState,
                        Answer = friendRequestAnswer
                    };
                    ApplyAnswer(answerContext);
                }
            }

            return friendRequestAnswer;
        }

        private void ApplyAnswer(FriendRequestAnswerContext context)
        {
            FriendshipWriteStatus friendshipWriteStatus = _friendshipRepository.AnswerFriendRequest(
                context.FriendshipRecord.FriendshipId,
                context.ReceiverSession.UserId,
                context.NewState);

            if (friendshipWriteStatus != FriendshipWriteStatus.Applied)
            {
                context.Answer.ErrorKey = ResolveWriteErrorKey(friendshipWriteStatus);
                LogRejectedOperation(
                    "responder una solicitud de amistad",
                    context.ReceiverSession.UserId,
                    friendshipWriteStatus.ToString());
            }
            else if (context.NewState == FriendshipState.Accepted)
            {
                context.Answer.IsSuccess = true;
                Friend newFriend;
                context.Answer.Notifications = PrepareFriendshipNotifications(
                    context.ReceiverSession, context.FriendshipRecord.SenderUserId,
                    context.FriendshipRecord.SenderNickname, out newFriend);
                context.Answer.NewFriend = newFriend;
                _log.InfoFormat(
                    "El jugador {0} aceptó la solicitud de amistad del jugador {1}.",
                    context.ReceiverSession.UserId,
                    context.FriendshipRecord.SenderUserId);
            }
            else
            {
                context.Answer.IsSuccess = true;
                _log.InfoFormat(
                    "El jugador {0} rechazó la solicitud de amistad del jugador {1}.",
                    context.ReceiverSession.UserId,
                    context.FriendshipRecord.SenderUserId);
            }
        }

        private IReadOnlyList<Action> PrepareFriendshipNotifications(
            PlayerSession playerSession, int friendUserId, string friendNickname, out Friend newFriend)
        {
            FriendsAddition friendsAddition = _sessionRegistry.AddFriendshipToOnlineChannels(
                playerSession.UserId, friendUserId);
            newFriend = new Friend
            {
                UserId = friendUserId,
                Nickname = friendNickname,
                IsOnline = friendsAddition.IsSecondOnline
            };
            FriendshipNotificationPair notificationPair = new FriendshipNotificationPair
            {
                FriendsAddition = friendsAddition,
                FirstUserId = playerSession.UserId,
                FirstNickname = playerSession.Nickname,
                SecondUserId = friendUserId,
                SecondNickname = friendNickname
            };

            return CreateFriendAddedNotifications(notificationPair);
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

                notifications.Add(
                    () => receiverCallback.OnFriendRequestReceived(receivedRequest));
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        private static IReadOnlyList<Action> CreateFriendAddedNotifications(FriendshipNotificationPair pair)
        {
            List<Action> notifications = new List<Action>();

            if (pair.FriendsAddition.IsFirstOnline)
            {
                IFriendsManagerCallback firstCallback = pair.FriendsAddition.FirstCallback;
                Friend secondAsFriend = new Friend
                {
                    UserId = pair.SecondUserId,
                    Nickname = pair.SecondNickname,
                    IsOnline = pair.FriendsAddition.IsSecondOnline
                };

                notifications.Add(
                    () => firstCallback.OnFriendAdded(secondAsFriend));
            }

            if (pair.FriendsAddition.IsSecondOnline)
            {
                IFriendsManagerCallback secondCallback = pair.FriendsAddition.SecondCallback;
                Friend firstAsFriend = new Friend
                {
                    UserId = pair.FirstUserId,
                    Nickname = pair.FirstNickname,
                    IsOnline = pair.FriendsAddition.IsFirstOnline
                };

                notifications.Add(
                    () => secondCallback.OnFriendAdded(firstAsFriend));
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

        private sealed class SubscriptionChannelRequest
        {
            public PlayerSession PlayerSession { get; set; }

            public string SessionToken { get; set; }

            public string ChannelId { get; set; }

            public IFriendsManagerCallback FriendsCallback { get; set; }
        }

        private sealed class FriendRequestValidation
        {
            public string ErrorKey { get; set; }

            public PlayerSession PlayerSession { get; set; }

            public string NormalizedNickname { get; set; }
        }

        private sealed class PendingRequestCompletion
        {
            public PlayerSession SenderSession { get; set; }

            public FriendCandidateLookup FriendCandidate { get; set; }

            public FriendshipWriteStatus WriteStatus { get; set; }

            public int FriendshipId { get; set; }

            public OperationSendFriendRequestResult Result { get; set; }
        }

        private sealed class ExistingRecordRuleContext
        {
            public PlayerSession SenderSession { get; set; }

            public FriendCandidateLookup FriendCandidate { get; set; }

            public FriendshipRecord FriendshipRecord { get; set; }

            public OperationSendFriendRequestResult Result { get; set; }
        }

        private sealed class FriendshipNotificationPair
        {
            public FriendsAddition FriendsAddition { get; set; }

            public int FirstUserId { get; set; }

            public string FirstNickname { get; set; }

            public int SecondUserId { get; set; }

            public string SecondNickname { get; set; }
        }

        private sealed class FriendRequestAnswerContext
        {
            public PlayerSession ReceiverSession { get; set; }

            public FriendshipRecord FriendshipRecord { get; set; }

            public FriendshipState NewState { get; set; }

            public FriendRequestAnswer Answer { get; set; }
        }

        private sealed class FriendRequestAnswer
        {
            public bool IsSuccess { get; set; }
            public string ErrorKey { get; set; }

            public Friend NewFriend { get; set; }

            public IReadOnlyList<Action> Notifications { get; set; } = _noNotifications;
        }
    }
}
