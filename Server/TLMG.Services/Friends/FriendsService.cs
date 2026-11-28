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
using TLMG.Services.Moderation;
using TLMG.Services.Sessions;

namespace TLMG.Services.Friends
{
    public class FriendsService
    {
        private const int NoFriendshipId = 0;

        private static readonly ILog _log = LogManager.GetLogger(typeof(FriendsService));

        private static readonly IReadOnlyList<Action> _noNotifications = new List<Action>().AsReadOnly();

        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IFriendsChannelRegistry _sessionRegistry;
        private readonly IFriendsSessionAccess _sessionAccess;
        private readonly BlockPolicy _blockPolicy;

        public FriendsService(
            IFriendshipRepository friendshipRepository,
            IFriendsChannelRegistry sessionRegistry,
            IFriendsSessionAccess sessionAccess,
            BlockPolicy blockPolicy)
        {
            _friendshipRepository = friendshipRepository;
            _sessionRegistry = sessionRegistry;
            _sessionAccess = sessionAccess;
            _blockPolicy = blockPolicy;
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

        public ResultWithNotification<OperationSendFriendRequestResult> SendFriendRequest(
            string sessionToken,
            string friendNickname)
        {
            ResultWithNotification<OperationSendFriendRequestResult> outcome;
            FriendRequestValidation validationResult = ValidateSendRequest(sessionToken, friendNickname);

            if (validationResult.IsValid)
            {
                outcome = ApplySendFriendRequest(validationResult.PlayerSession, validationResult.NormalizedNickname);
            }
            else
            {
                outcome = RejectFriendRequest(validationResult.ErrorKey);
            }

            return outcome;
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
            string errorKey = ErrorKeys.Common.Unexpected;

            if (guestFriendshipChange != null)
            {
                if (guestFriendshipChange.Status == GuestFriendshipChangeStatus.GuestNotFound)
                {
                    errorKey = ErrorKeys.Friends.UserNotFound;
                }
                else if (guestFriendshipChange.Status == GuestFriendshipChangeStatus.AlreadyExists
                    && guestFriendshipChange.Friendship != null)
                {
                    errorKey = ErrorKeys.Friends.RequestAlreadySent;

                    if (guestFriendshipChange.Friendship.State == GuestFriendshipState.Accepted)
                    {
                        errorKey = ErrorKeys.Friends.AlreadyFriends;
                    }
                }
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
                int otherUserId = friendship.SenderUserId;
                string otherNickname = friendship.SenderNickname;

                if (isSender)
                {
                    otherUserId = friendship.ReceiverUserId;
                    otherNickname = friendship.ReceiverNickname;
                }

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

        private FriendsSnapshot LoadFriendsSnapshot(int userId)
        {
            FriendsSnapshot snapshot = new FriendsSnapshot
            {
                Friends = new List<AcceptedFriend>(),
                Requests = new List<PendingFriendRequest>()
            };

            bool loaded = GuestIdentity.IsGuestUserId(userId) || TryAddAccountFriendships(snapshot, userId);
            if (loaded)
            {
                AddGuestFriendships(snapshot, _sessionAccess.FindGuestFriendships(userId), userId);
            }

            snapshot.WasSuccessful = loaded;
            return snapshot;
        }

        private bool TryAddAccountFriendships(FriendsSnapshot snapshot, int userId)
        {
            FriendListLookup friends = _friendshipRepository.FindAcceptedFriends(userId);
            PendingFriendRequestsLookup pending = _friendshipRepository.FindPendingFriendRequests(userId);
            bool loaded = false;

            if (friends.WasFriendListLookupSuccessful && pending.WasLookupSuccessful)
            {
                BlockListLookup blocks = _blockPolicy.FindBlocks(userId);
                if (blocks.WasLookupSuccessful)
                {
                    snapshot.Friends.AddRange(BlockPolicy.FilterVisibleFriends(blocks, userId, friends.Friends));
                    snapshot.Requests.AddRange(pending.Requests);
                    loaded = true;
                }
            }

            return loaded;
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
                    Friend friend = new Friend
                    {
                        UserId = friendsSubscription.UserId,
                        Nickname = friendsSubscription.Nickname,
                        IsOnline = true
                    };
                    subscribeToFriendsNotifications = SessionNotificationHelper.CreatePresenceNotifications(
                        friendsSubscription.PresenceRecipientCallbacks, friend);
                    _log.InfoFormat("El jugador {0} se suscribió a sus amigos.", friendsSubscription.UserId);
                }
                else
                {
                    operationSubscribeToFriendsResult.ErrorKey = ErrorKeys.Common.Unexpected;
                    if (friendsSubscription.Status == FriendsChangeStatus.InvalidSession)
                    {
                        operationSubscribeToFriendsResult.ErrorKey = ErrorKeys.Common.InvalidSession;
                    }
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

        private FriendRequestValidation ValidateSendRequest(string sessionToken, string friendNickname)
        {
            FriendRequestValidation validationResult = new FriendRequestValidation();
            string normalizedNickname;
            bool isNicknameValid = FriendNicknameValidator.TryNormalizeNickname(friendNickname, out normalizedNickname);

            if (!isNicknameValid)
            {
                validationResult.ErrorKey = ErrorKeys.Friends.NicknameFormat;
                LogRejectedOperation("solicitar amistad", ValidationRules.NoUserId, "nickname con formato no válido");
            }
            else
            {
                SessionAuthorization authorization = _sessionAccess.Authorize(
                    sessionToken,
                    SystemPermission.SendFriendRequest);
                PlayerSession playerSession;

                if (authorization.Status != AuthorizationStatus.Granted)
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
                    validationResult.IsValid = true;
                    validationResult.ErrorKey = string.Empty;
                    validationResult.PlayerSession = playerSession;
                    validationResult.NormalizedNickname = normalizedNickname;
                }
            }

            return validationResult;
        }

        private static ResultWithNotification<OperationSendFriendRequestResult> RejectFriendRequest(string errorKey)
        {
            OperationSendFriendRequestResult result = new OperationSendFriendRequestResult { ErrorKey = errorKey };
            return new ResultWithNotification<OperationSendFriendRequestResult>(result);
        }

        private ResultWithNotification<OperationSendFriendRequestResult> ApplySendFriendRequest(
            PlayerSession senderSession,
            string nickname)
        {
            ResultWithNotification<OperationSendFriendRequestResult> outcome;
            int guestUserId;
            string guestNickname;

            if (_sessionAccess.TryFindGuestByNickname(nickname, out guestUserId, out guestNickname))
            {
                outcome = ApplyGuestFriendRequest(
                    senderSession,
                    guestUserId);
            }
            else
            {
                outcome = ApplyDatabaseFriendRequest(
                    senderSession,
                    nickname);
            }

            return outcome;
        }

        private ResultWithNotification<OperationSendFriendRequestResult> ApplyGuestFriendRequest(
            PlayerSession senderSession,
            int guestUserId)
        {
            OperationSendFriendRequestResult result = new OperationSendFriendRequestResult();
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;
            GuestFriendshipChange guestFriendshipChange =
                _sessionAccess.CreateGuestFriendRequest(senderSession.UserId, guestUserId);

            if (guestFriendshipChange == null)
            {
                result.ErrorKey = ErrorKeys.Common.Unexpected;
                _log.Error("No se obtuvo el resultado de la solicitud de amistad temporal.");
            }
            else if (guestFriendshipChange.Status == GuestFriendshipChangeStatus.Applied
                && guestFriendshipChange.Friendship == null)
            {
                result.ErrorKey = ErrorKeys.Common.Unexpected;
                _log.Error("La solicitud de amistad temporal aplicada no contiene la amistad.");
            }
            else if (guestFriendshipChange.Status == GuestFriendshipChangeStatus.Applied)
            {
                GuestFriendship friendship = guestFriendshipChange.Friendship;
                result.IsSuccess = true;
                result.ErrorKey = string.Empty;
                result.SentFriendRequest = new FriendRequest
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
                result.ErrorKey = ResolveGuestErrorKey(guestFriendshipChange);
                LogRejectedOperation(
                    "solicitar amistad a un invitado",
                    senderSession.UserId,
                    guestFriendshipChange.Status.ToString());
            }

            return new ResultWithNotification<OperationSendFriendRequestResult>(result, friendRequestNotifications);
        }

        private ResultWithNotification<OperationSendFriendRequestResult> ApplyDatabaseFriendRequest(
            PlayerSession senderSession,
            string nickname)
        {
            ResultWithNotification<OperationSendFriendRequestResult> outcome;
            FriendCandidateLookup friendCandidateLookup = _friendshipRepository.FindFriendCandidateByNickname(nickname);

            bool found = friendCandidateLookup?.Status == FriendLookupStatus.Found;
            bool missing = friendCandidateLookup?.Status == FriendLookupStatus.NotFound;
            if (!found && !missing)
            {
                outcome = RejectFriendRequest(ErrorKeys.Common.Unexpected);
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "fallo de datos");
            }
            else if (missing)
            {
                outcome = RejectFriendRequest(ErrorKeys.Friends.UserNotFound);
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "el nickname no existe");
            }
            else if (friendCandidateLookup.UserId <= ValidationRules.NoUserId
                || string.IsNullOrWhiteSpace(friendCandidateLookup.Nickname))
            {
                outcome = RejectFriendRequest(ErrorKeys.Common.Unexpected);
            }
            else if (friendCandidateLookup.UserId == senderSession.UserId)
            {
                outcome = RejectFriendRequest(ErrorKeys.Friends.CannotRequestSelf);
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "solicitud a sí mismo");
            }
            else
            {
                outcome = ApplyPairRules(
                    senderSession, friendCandidateLookup);
            }

            return outcome;
        }

        private ResultWithNotification<OperationSendFriendRequestResult> ApplyPairRules(
            PlayerSession senderSession,
            FriendCandidateLookup friendCandidate)
        {
            ResultWithNotification<OperationSendFriendRequestResult> outcome;

            lock (_blockPolicy.SynchronizationRoot)
            {
                BlockListLookup blocks = _blockPolicy.FindBlocks(senderSession.UserId);

                if (!blocks.WasLookupSuccessful)
                {
                    outcome = RejectFriendRequest(ErrorKeys.Common.Unexpected);
                }
                else if (BlockPolicy.CanInteract(blocks, senderSession.UserId, friendCandidate.UserId))
                {
                    outcome = ApplyUnblockedPairRules(
                        senderSession, friendCandidate);
                }
                else
                {
                    outcome = RejectFriendRequest(ErrorKeys.Friends.RequestNotValid);
                }
            }

            return outcome;
        }

        private ResultWithNotification<OperationSendFriendRequestResult> ApplyUnblockedPairRules(
            PlayerSession senderSession,
            FriendCandidateLookup friendCandidate)
        {
            ResultWithNotification<OperationSendFriendRequestResult> outcome;
            FriendshipRecordLookup friendshipRecordLookup =
                _friendshipRepository.FindFriendshipRecord(senderSession.UserId, friendCandidate.UserId);

            bool missing = friendshipRecordLookup?.Status == FriendLookupStatus.NotFound;
            bool found = friendshipRecordLookup?.Status == FriendLookupStatus.Found
                && friendshipRecordLookup.Record != null;
            if (!missing && !found)
            {
                outcome = RejectFriendRequest(ErrorKeys.Common.Unexpected);
                LogRejectedOperation("solicitar amistad", senderSession.UserId, "fallo de datos");
            }
            else if (missing)
            {
                FriendshipWriteResult friendshipWriteResult =
                    _friendshipRepository.CreateFriendRequest(senderSession.UserId, friendCandidate.UserId);
                if (friendshipWriteResult == null)
                {
                    outcome = RejectFriendRequest(ErrorKeys.Common.Unexpected);
                }
                else
                {
                    outcome = CompletePendingRequest(senderSession, friendCandidate, friendshipWriteResult);
                }
            }
            else
            {
                outcome = ApplyExistingRecordRules(senderSession, friendCandidate, friendshipRecordLookup.Record);
            }

            return outcome;
        }

        private ResultWithNotification<OperationSendFriendRequestResult> ApplyExistingRecordRules(
            PlayerSession sender, FriendCandidateLookup candidate, FriendshipRecord record)
        {
            ResultWithNotification<OperationSendFriendRequestResult> outcome;

            if (record.State == FriendshipState.Accepted)
            {
                outcome = RejectFriendRequest(ErrorKeys.Friends.AlreadyFriends);
                LogRejectedOperation("solicitar amistad", sender.UserId, "ya son amigos");
            }
            else if (record.State == FriendshipState.Pending
                && record.SenderUserId == sender.UserId)
            {
                outcome = RejectFriendRequest(ErrorKeys.Friends.RequestAlreadySent);
                LogRejectedOperation(
                    "solicitar amistad",
                    sender.UserId,
                    "ya había enviado una solicitud");
            }
            else if (record.State == FriendshipState.Pending)
            {
                outcome = AcceptCrossedRequest(sender, candidate, record);
            }
            else if (record.State == FriendshipState.Rejected)
            {
                FriendshipWriteStatus friendshipWriteStatus = _friendshipRepository.ReactivateFriendRequest(
                    record.FriendshipId,
                    sender.UserId,
                    candidate.UserId);
                FriendshipWriteResult write = new FriendshipWriteResult
                {
                    Status = friendshipWriteStatus,
                    FriendshipId = record.FriendshipId
                };
                outcome = CompletePendingRequest(sender, candidate, write);
            }
            else
            {
                outcome = RejectFriendRequest(ErrorKeys.Common.Unexpected);
            }

            return outcome;
        }

        private ResultWithNotification<OperationSendFriendRequestResult> CompletePendingRequest(
            PlayerSession senderSession, FriendCandidateLookup friendCandidate, FriendshipWriteResult write)
        {
            OperationSendFriendRequestResult result = new OperationSendFriendRequestResult();
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;

            if (write.Status == FriendshipWriteStatus.Applied)
            {
                result.IsSuccess = true;
                result.ErrorKey = string.Empty;
                result.SentFriendRequest = new FriendRequest
                {
                    FriendRequestId = write.FriendshipId,
                    UserId = friendCandidate.UserId,
                    Nickname = friendCandidate.Nickname,
                    IsIncoming = false
                };
                friendRequestNotifications = CreateRequestReceivedNotifications(
                    senderSession,
                    friendCandidate.UserId,
                    write.FriendshipId);
                _log.InfoFormat(
                    "El jugador {0} envió una solicitud de amistad al jugador {1}.",
                    senderSession.UserId,
                    friendCandidate.UserId);
            }
            else
            {
                result.ErrorKey = ResolveWriteErrorKey(write.Status);
                LogRejectedOperation(
                    "solicitar amistad",
                    senderSession.UserId,
                    write.Status.ToString());
            }

            return new ResultWithNotification<OperationSendFriendRequestResult>(result, friendRequestNotifications);
        }

        private ResultWithNotification<OperationSendFriendRequestResult> AcceptCrossedRequest(
            PlayerSession sender, FriendCandidateLookup candidate, FriendshipRecord record)
        {
            OperationSendFriendRequestResult result = new OperationSendFriendRequestResult();
            IReadOnlyList<Action> friendRequestNotifications = _noNotifications;
            FriendshipWriteStatus friendshipWriteStatus = _friendshipRepository.AnswerFriendRequest(
                record.FriendshipId,
                sender.UserId,
                FriendshipState.Accepted);

            if (friendshipWriteStatus == FriendshipWriteStatus.Applied)
            {
                FriendshipNotificationPair notificationPair = CreateFriendshipNotificationPair(
                    sender, candidate.UserId, candidate.Nickname);
                friendRequestNotifications = CreateFriendAddedNotifications(notificationPair);
                result.IsSuccess = true;
                result.ErrorKey = string.Empty;
                result.NewFriend = CreateFriendFromPair(notificationPair);
                _log.InfoFormat(
                    "La solicitud cruzada entre los jugadores {0} y {1} se aceptó sola.",
                    sender.UserId,
                    candidate.UserId);
            }
            else
            {
                result.ErrorKey = ResolveWriteErrorKey(friendshipWriteStatus);
                LogRejectedOperation(
                    "solicitar amistad",
                    sender.UserId,
                    friendshipWriteStatus.ToString());
            }

            return new ResultWithNotification<OperationSendFriendRequestResult>(result, friendRequestNotifications);
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

            if (authorization.Status != AuthorizationStatus.Granted)
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

            if (guestFriendshipChange == null)
            {
                context.Answer.ErrorKey = ErrorKeys.Common.Unexpected;
                _log.Error("No se obtuvo el resultado de responder la solicitud de amistad temporal.");
            }
            else if (guestFriendshipChange.Status == GuestFriendshipChangeStatus.Applied
                && isAccepted && guestFriendshipChange.Friendship == null)
            {
                context.Answer.ErrorKey = ErrorKeys.Common.Unexpected;
                _log.Error("La aceptación de amistad temporal no contiene la amistad.");
            }
            else if (guestFriendshipChange.Status != GuestFriendshipChangeStatus.Applied)
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
            public bool IsValid { get; set; }

            public string ErrorKey { get; set; }

            public PlayerSession PlayerSession { get; set; }

            public string NormalizedNickname { get; set; }
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