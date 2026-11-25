using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Friends
{
    public class FriendsService
    {
        private const int NoFriendshipId = 0;

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
        private readonly IFriendsSessionAccess _sessionAccess;

        public FriendsService(
            IFriendshipRepository friendshipRepository,
            ISessionRegistry sessionRegistry,
            IFriendsSessionAccess sessionAccess)
        {
            _friendshipRepository = friendshipRepository;
            _sessionRegistry = sessionRegistry;
            _sessionAccess = sessionAccess;
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
            SessionAuthorization authorization = _sessionAccess.Authorize(
                sessionToken,
                SystemPermission.InviteFriendToRoom);

            if (authorization.ErrorKey != null)
            {
                operationInviteFriendToRoomResult.ErrorKey = authorization.ErrorKey;
                LogRejectedOperation("invitar a un amigo", authorization.UserId, authorization.Status.ToString());
            }
            else
            {
                string errorKey = FindInvitationErrorKey(authorization.UserId, friendUserId);

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
                            CreateInviteResult(authorization.UserId, friendUserId, roomInvitationCreation);
                        createInvitationNotifications =
                            CreateInvitationNotifications(roomInvitationCreation, authorization.UserId);
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
            OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult =
                new OperationAcceptRoomInvitationResult();
            SessionAuthorization authorization = _sessionAccess.Authorize(
                sessionToken,
                SystemPermission.AcceptRoomInvitation);

            if (authorization.ErrorKey != null)
            {
                operationAcceptRoomInvitationResult.ErrorKey = authorization.ErrorKey;
                LogRejectedOperation(
                    "aceptar una invitación",
                    authorization.UserId,
                    authorization.Status.ToString());
            }
            else
            {
                ApplyRoomInvitationAcceptance(sessionToken, invitationId, operationAcceptRoomInvitationResult);
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

        private static IReadOnlyList<Action> CreateInvitationNotifications(
            RoomInvitationCreation roomInvitationCreation,
            int inviterUserId)
        {
            List<Action> roomInvitationNotifications = new List<Action>();

            if (roomInvitationCreation.Status == FriendsChangeStatus.Applied && roomInvitationCreation.ShouldNotifyInvitee)
            {
                RoomInvitation roomInvitation = new RoomInvitation
                {
                    InvitationId = roomInvitationCreation.InvitationId,
                    InviterNickname = roomInvitationCreation.InviterNickname,
                    InviterUserId = inviterUserId
                };
                InvitationNotification notification =
                    new InvitationNotification(roomInvitationCreation.InviteeCallback, roomInvitation);

                roomInvitationNotifications.Add(notification.Send);
            }

            IReadOnlyList<Action> invitationNotifications = roomInvitationNotifications.AsReadOnly();

            return invitationNotifications;
        }

        private static bool IsPendingRequestOfReceiver(FriendshipRecord friendshipRecord, int receiverUserId)
        {
            bool isPendingRequestOfReceiver = friendshipRecord.State == FriendshipState.Pending
                && friendshipRecord.ReceiverUserId == receiverUserId;

            return isPendingRequestOfReceiver;
        }

        private static IReadOnlyList<Action> CreateFriendAddedNotifications(FriendshipNotificationPair pair)
        {
            List<Action> notifications = new List<Action>();

            if (pair.FriendsAddition.IsFirstOnline)
            {
                Friend secondAsFriend = new Friend
                {
                    UserId = pair.SecondUserId,
                    Nickname = pair.SecondNickname,
                    IsOnline = pair.FriendsAddition.IsSecondOnline
                };
                FriendAddedNotification firstNotification =
                    new FriendAddedNotification(pair.FriendsAddition.FirstCallback, secondAsFriend);

                notifications.Add(firstNotification.Send);
            }

            if (pair.FriendsAddition.IsSecondOnline)
            {
                Friend firstAsFriend = new Friend
                {
                    UserId = pair.FirstUserId,
                    Nickname = pair.FirstNickname,
                    IsOnline = pair.FriendsAddition.IsFirstOnline
                };
                FriendAddedNotification secondNotification =
                    new FriendAddedNotification(pair.FriendsAddition.SecondCallback, firstAsFriend);

                notifications.Add(secondNotification.Send);
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

        private static string ResolveGuestErrorKey(GuestFriendshipChange guestFriendshipChange)
        {
            string errorKey;

            switch (guestFriendshipChange.Status)
            {
                case GuestFriendshipChangeStatus.GuestNotFound:
                    errorKey = ErrorKeys.Friends.UserNotFound;
                    break;
                case GuestFriendshipChangeStatus.AlreadyExists:
                    errorKey = ErrorKeys.Friends.RequestAlreadySent;

                    if (guestFriendshipChange.Friendship.State == GuestFriendshipState.Accepted)
                    {
                        errorKey = ErrorKeys.Friends.AlreadyFriends;
                    }

                    break;
                default:
                    errorKey = ErrorKeys.Common.Unexpected;
                    break;
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

        private static void AddGuestFriendships(FriendsSnapshot snapshot, IReadOnlyList<GuestFriendship> friendships,
            int userId)
        {
            foreach (GuestFriendship friendship in friendships)
            {
                bool isSender = friendship.SenderUserId == userId;
                int otherUserId = isSender ? friendship.ReceiverUserId : friendship.SenderUserId;
                string otherNickname = isSender ? friendship.ReceiverNickname : friendship.SenderNickname;

                if (friendship.State == GuestFriendshipState.Accepted)
                {
                    snapshot.Friends.Add(new AcceptedFriend { UserId = otherUserId, Nickname = otherNickname });
                }
                else
                {
                    snapshot.Requests.Add(new PendingFriendRequest
                    {
                        FriendshipId = friendship.FriendshipId,
                        OtherUserId = otherUserId,
                        OtherNickname = otherNickname,
                        IsIncoming = !isSender
                    });
                }
            }
        }

        private void ApplyRoomInvitationAcceptance(
            string sessionToken,
            int invitationId,
            OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult)
        {
            RoomInvitationAcceptance roomInvitationAcceptance =
                _sessionRegistry.AcceptRoomInvitation(sessionToken, invitationId);

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
        }

        private FriendsSnapshot LoadFriendsSnapshot(int userId)
        {
            FriendsSnapshot snapshot = new FriendsSnapshot
            {
                WasSuccessful = true,
                Friends = new List<AcceptedFriend>(),
                Requests = new List<PendingFriendRequest>()
            };

            if (!GuestIdentity.IsGuestUserId(userId))
            {
                FriendListLookup friendListLookup = _friendshipRepository.FindAcceptedFriends(userId);
                PendingFriendRequestsLookup pendingLookup = _friendshipRepository.FindPendingFriendRequests(userId);

                snapshot.WasSuccessful = friendListLookup != null
                    && pendingLookup != null
                    && friendListLookup.WasFriendListLookupSuccessful
                    && pendingLookup.WasLookupSuccessful
                    && friendListLookup.Friends != null
                    && pendingLookup.Requests != null;

                if (snapshot.WasSuccessful)
                {
                    snapshot.Friends.AddRange(friendListLookup.Friends);
                    snapshot.Requests.AddRange(pendingLookup.Requests);
                }
            }

            if (snapshot.WasSuccessful)
            {
                AddGuestFriendships(snapshot, _sessionAccess.FindGuestFriendships(userId), userId);
            }

            return snapshot;
        }

        private ResultWithNotification<OperationSubscribeToFriendsResult> SubscribeFriendsChannel(
            SubscriptionChannelRequest request)
        {
            OperationSubscribeToFriendsResult operationSubscribeToFriendsResult =
                new OperationSubscribeToFriendsResult();
            IReadOnlyList<Action> subscribeToFriendsNotifications = _noNotifications;
            FriendsSnapshot snapshot = LoadFriendsSnapshot(request.PlayerSession.UserId);

            if (!snapshot.WasSuccessful)
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
                    ExtractUserIds(snapshot.Friends));

                if (friendsSubscription == null)
                {
                    operationSubscribeToFriendsResult.ErrorKey = ErrorKeys.Common.Unexpected;
                }
                else if (friendsSubscription.Status == FriendsChangeStatus.Applied)
                {
                    operationSubscribeToFriendsResult.IsSuccess = true;
                    operationSubscribeToFriendsResult.ErrorKey = string.Empty;
                    operationSubscribeToFriendsResult.Friends = ConvertToFriends(
                        snapshot.Friends,
                        friendsSubscription.OnlineFriendUserIds);
                    operationSubscribeToFriendsResult.PendingFriendRequests =
                        ConvertToFriendRequests(snapshot.Requests);
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

        private string FindInvitationErrorKey(int inviterUserId, int friendUserId)
        {
            string errorKey = null;
            bool involvesGuest = GuestIdentity.IsGuestUserId(inviterUserId)
                || GuestIdentity.IsGuestUserId(friendUserId);

            if (involvesGuest)
            {
                if (!_sessionAccess.AreGuestFriends(inviterUserId, friendUserId))
                {
                    errorKey = ErrorKeys.Friends.CannotInvite;
                    LogRejectedOperation("invitar a un amigo", inviterUserId, "no son amigos aceptados");
                }
            }
            else
            {
                errorKey = FindDatabaseInvitationErrorKey(inviterUserId, friendUserId);
            }

            return errorKey;
        }

        private string FindDatabaseInvitationErrorKey(int inviterUserId, int friendUserId)
        {
            string errorKey = null;
            FriendshipLookupStatus friendshipLookupStatus = _friendshipRepository.FindFriendshipStatus(
                inviterUserId,
                friendUserId);

            if (friendshipLookupStatus == FriendshipLookupStatus.DataFailure)
            {
                errorKey = ErrorKeys.Common.Unexpected;
                LogRejectedOperation("invitar a un amigo", inviterUserId, "fallo de datos");
            }
            else if (friendshipLookupStatus == FriendshipLookupStatus.NotAccepted)
            {
                errorKey = ErrorKeys.Friends.CannotInvite;
                LogRejectedOperation("invitar a un amigo", inviterUserId, "no son amigos aceptados");
            }
            else if (friendshipLookupStatus != FriendshipLookupStatus.Accepted)
            {
                errorKey = ErrorKeys.Common.Unexpected;
            }

            return errorKey;
        }

        private OperationResult CreateInviteResult(
            int inviterUserId,
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
                    inviterUserId,
                    friendUserId);
            }
            else
            {
                operationInviteFriendToRoomResult.ErrorKey = ResolveErrorKey(roomInvitationCreation.Status);
                LogRejectedOperation(
                    "invitar a un amigo",
                    inviterUserId,
                    roomInvitationCreation.Status.ToString());
            }

            return operationInviteFriendToRoomResult;
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
                SessionAuthorization authorization = _sessionAccess.Authorize(
                    sessionToken,
                    SystemPermission.SendFriendRequest);
                PlayerSession playerSession;

                if (authorization.ErrorKey != null)
                {
                    validationResult.ErrorKey = authorization.ErrorKey;
                    LogRejectedOperation("solicitar amistad", authorization.UserId, authorization.Status.ToString());
                }
                else if (!_sessionRegistry.TryFindSessionByToken(sessionToken, out playerSession))
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
            IReadOnlyList<Action> friendRequestNotifications;
            int guestUserId;
            string guestNickname;

            if (_sessionAccess.TryFindGuestByNickname(nickname, out guestUserId, out guestNickname))
            {
                friendRequestNotifications = ApplyGuestFriendRequest(
                    senderSession,
                    guestUserId,
                    operationSendFriendRequestResult);
            }
            else
            {
                friendRequestNotifications = ApplyDatabaseFriendRequest(
                    senderSession,
                    nickname,
                    operationSendFriendRequestResult);
            }

            return friendRequestNotifications;
        }

        private IReadOnlyList<Action> ApplyGuestFriendRequest(
            PlayerSession senderSession,
            int guestUserId,
            OperationSendFriendRequestResult operationSendFriendRequestResult)
        {
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;
            GuestFriendshipChange guestFriendshipChange =
                _sessionAccess.CreateGuestFriendRequest(senderSession.UserId, guestUserId);

            if (guestFriendshipChange.Status == GuestFriendshipChangeStatus.Applied)
            {
                GuestFriendship friendship = guestFriendshipChange.Friendship;
                operationSendFriendRequestResult.IsSuccess = true;
                operationSendFriendRequestResult.ErrorKey = string.Empty;
                operationSendFriendRequestResult.SentFriendRequest = new FriendRequest
                {
                    FriendRequestId = friendship.FriendshipId,
                    UserId = guestUserId,
                    Nickname = friendship.ReceiverNickname,
                    IsIncoming = false
                };
                friendRequestNotifications = CreateRequestReceivedNotifications(
                    senderSession,
                    guestUserId,
                    friendship.FriendshipId);
                _log.InfoFormat(
                    "El jugador {0} envió una solicitud de amistad al invitado {1}.",
                    senderSession.UserId,
                    guestUserId);
            }
            else
            {
                operationSendFriendRequestResult.ErrorKey = ResolveGuestErrorKey(guestFriendshipChange);
                LogRejectedOperation(
                    "solicitar amistad a un invitado",
                    senderSession.UserId,
                    guestFriendshipChange.Status.ToString());
            }

            return friendRequestNotifications;
        }

        private IReadOnlyList<Action> ApplyDatabaseFriendRequest(
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
                LogRejectedOperation(
                    "solicitar amistad",
                    context.SenderSession.UserId,
                    "ya había enviado una solicitud");
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
                FriendshipNotificationPair notificationPair = CreateFriendshipNotificationPair(
                    context.SenderSession, context.FriendCandidate.UserId, context.FriendCandidate.Nickname);
                friendRequestNotifications = CreateFriendAddedNotifications(notificationPair);
                context.Result.IsSuccess = true;
                context.Result.ErrorKey = string.Empty;
                context.Result.NewFriend = CreateFriendFromPair(notificationPair);
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
            SessionAuthorization authorization = _sessionAccess.Authorize(
                sessionToken,
                SystemPermission.RespondToFriendRequest);
            PlayerSession playerSession;

            if (authorization.ErrorKey != null)
            {
                friendRequestAnswer.ErrorKey = authorization.ErrorKey;
                LogRejectedOperation(
                    "responder una solicitud de amistad",
                    authorization.UserId,
                    authorization.Status.ToString());
            }
            else if (!_sessionRegistry.TryFindSessionByToken(sessionToken, out playerSession))
            {
                friendRequestAnswer.ErrorKey = ErrorKeys.Common.InvalidSession;
                LogRejectedOperation(
                    "responder una solicitud de amistad", ValidationRules.NoUserId, "sesión no válida");
            }
            else
            {
                FriendRequestAnswerContext answerContext = new FriendRequestAnswerContext
                {
                    ReceiverSession = playerSession,
                    NewState = newState,
                    Answer = friendRequestAnswer
                };

                if (friendRequestId < NoFriendshipId)
                {
                    ApplyGuestAnswer(answerContext, friendRequestId);
                }
                else
                {
                    ApplyDatabaseAnswer(answerContext, friendRequestId);
                }
            }

            return friendRequestAnswer;
        }

        private void ApplyDatabaseAnswer(FriendRequestAnswerContext context, int friendRequestId)
        {
            FriendshipRecordLookup friendshipRecordLookup =
                _friendshipRepository.FindFriendshipRecordById(friendRequestId);

            if (friendshipRecordLookup == null
                || (friendshipRecordLookup.Status != FriendLookupStatus.Found
                    && friendshipRecordLookup.Status != FriendLookupStatus.NotFound)
                || (friendshipRecordLookup.Status == FriendLookupStatus.Found
                    && friendshipRecordLookup.Record == null))
            {
                context.Answer.ErrorKey = ErrorKeys.Common.Unexpected;
                LogRejectedOperation("responder una solicitud de amistad", context.ReceiverSession.UserId,
                    "fallo de datos");
            }
            else if (friendshipRecordLookup.Status == FriendLookupStatus.NotFound
                || !IsPendingRequestOfReceiver(friendshipRecordLookup.Record, context.ReceiverSession.UserId))
            {
                context.Answer.ErrorKey = ErrorKeys.Friends.RequestNotValid;
                LogRejectedOperation("responder una solicitud de amistad", context.ReceiverSession.UserId,
                    "la solicitud no es válida");
            }
            else
            {
                context.FriendshipRecord = friendshipRecordLookup.Record;
                ApplyAnswer(context);
            }
        }

        private void ApplyGuestAnswer(FriendRequestAnswerContext context, int friendRequestId)
        {
            bool isAccepted = context.NewState == FriendshipState.Accepted;
            GuestFriendshipChange guestFriendshipChange = _sessionAccess.AnswerGuestFriendRequest(
                friendRequestId,
                context.ReceiverSession.UserId,
                isAccepted);

            if (guestFriendshipChange.Status != GuestFriendshipChangeStatus.Applied)
            {
                context.Answer.ErrorKey = ErrorKeys.Friends.RequestNotValid;
                LogRejectedOperation("responder una solicitud de amistad", context.ReceiverSession.UserId,
                    "la solicitud no es válida");
            }
            else if (isAccepted)
            {
                GuestFriendship friendship = guestFriendshipChange.Friendship;
                context.Answer.IsSuccess = true;
                context.Answer.ErrorKey = string.Empty;
                FriendshipNotificationPair notificationPair = CreateFriendshipNotificationPair(
                    context.ReceiverSession, friendship.SenderUserId, friendship.SenderNickname);
                context.Answer.NewFriend = CreateFriendFromPair(notificationPair);
                context.Answer.Notifications = CreateFriendAddedNotifications(notificationPair);
                _log.InfoFormat(
                    "El invitado {0} aceptó la solicitud de amistad del jugador {1}.",
                    context.ReceiverSession.UserId,
                    friendship.SenderUserId);
            }
            else
            {
                context.Answer.IsSuccess = true;
                context.Answer.ErrorKey = string.Empty;
                _log.InfoFormat(
                    "El invitado {0} rechazó una solicitud de amistad.",
                    context.ReceiverSession.UserId);
            }
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
                context.Answer.ErrorKey = string.Empty;
                FriendshipNotificationPair notificationPair = CreateFriendshipNotificationPair(
                    context.ReceiverSession, context.FriendshipRecord.SenderUserId,
                    context.FriendshipRecord.SenderNickname);
                context.Answer.Notifications = CreateFriendAddedNotifications(notificationPair);
                context.Answer.NewFriend = CreateFriendFromPair(notificationPair);
                _log.InfoFormat(
                    "El jugador {0} aceptó la solicitud de amistad del jugador {1}.",
                    context.ReceiverSession.UserId,
                    context.FriendshipRecord.SenderUserId);
            }
            else
            {
                context.Answer.IsSuccess = true;
                context.Answer.ErrorKey = string.Empty;
                _log.InfoFormat(
                    "El jugador {0} rechazó la solicitud de amistad del jugador {1}.",
                    context.ReceiverSession.UserId,
                    context.FriendshipRecord.SenderUserId);
            }
        }

        private FriendshipNotificationPair CreateFriendshipNotificationPair(
            PlayerSession playerSession, int friendUserId, string friendNickname)
        {
            FriendsAddition friendsAddition = _sessionRegistry.AddFriendshipToOnlineChannels(
                playerSession.UserId, friendUserId);

            FriendshipNotificationPair notificationPair = new FriendshipNotificationPair
            {
                FriendsAddition = friendsAddition,
                FirstUserId = playerSession.UserId,
                FirstNickname = playerSession.Nickname,
                SecondUserId = friendUserId,
                SecondNickname = friendNickname
            };

            return notificationPair;
        }

        private static Friend CreateFriendFromPair(FriendshipNotificationPair pair)
        {
            Friend friend = new Friend
            {
                UserId = pair.SecondUserId,
                Nickname = pair.SecondNickname,
                IsOnline = pair.FriendsAddition.IsSecondOnline
            };

            return friend;
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
                FriendRequestNotification notification =
                    new FriendRequestNotification(receiverCallback, receivedRequest);

                notifications.Add(notification.Send);
            }

            IReadOnlyList<Action> readOnlyNotifications = notifications.AsReadOnly();

            return readOnlyNotifications;
        }

        private sealed class InvitationNotification
        {
            private readonly IFriendsManagerCallback _callback;
            private readonly RoomInvitation _roomInvitation;

            public InvitationNotification(IFriendsManagerCallback callback, RoomInvitation roomInvitation)
            {
                _callback = callback;
                _roomInvitation = roomInvitation;
            }

            public void Send()
            {
                _callback.OnRoomInvitationReceived(_roomInvitation);
            }
        }

        private sealed class FriendRequestNotification
        {
            private readonly IFriendsManagerCallback _callback;
            private readonly FriendRequest _friendRequest;

            public FriendRequestNotification(IFriendsManagerCallback callback, FriendRequest friendRequest)
            {
                _callback = callback;
                _friendRequest = friendRequest;
            }

            public void Send()
            {
                _callback.OnFriendRequestReceived(_friendRequest);
            }
        }

        private sealed class FriendAddedNotification
        {
            private readonly IFriendsManagerCallback _callback;
            private readonly Friend _friend;

            public FriendAddedNotification(IFriendsManagerCallback callback, Friend friend)
            {
                _callback = callback;
                _friend = friend;
            }

            public void Send()
            {
                _callback.OnFriendAdded(_friend);
            }
        }

        private sealed class FriendsSnapshot
        {
            public bool WasSuccessful { get; set; }

            public List<AcceptedFriend> Friends { get; set; }

            public List<PendingFriendRequest> Requests { get; set; }
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
