using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Services.Sessions;

namespace TLMG.Services.Friends
{
    public sealed class RoomInvitationService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RoomInvitationService));

        private static readonly Dictionary<FriendsChangeStatus, string> _errorKeysByStatus =
            new Dictionary<FriendsChangeStatus, string>
            {
                { FriendsChangeStatus.InvalidSession, ErrorKeys.Common.InvalidSession },
                { FriendsChangeStatus.NotInRoom, ErrorKeys.Lobby.NotInRoom },
                { FriendsChangeStatus.RoomFull, ErrorKeys.Common.RoomFull },
                { FriendsChangeStatus.CannotInvite, ErrorKeys.Friends.CannotInvite },
                { FriendsChangeStatus.InvitationNotValid, ErrorKeys.Friends.InvitationNotValid },
                { FriendsChangeStatus.DataFailure, ErrorKeys.Common.Unexpected },
                { FriendsChangeStatus.RoomAccessDenied, ErrorKeys.Lobby.CannotJoinRoom },
                { FriendsChangeStatus.AlreadyInRoom, ErrorKeys.Lobby.AlreadyInRoom },
                { FriendsChangeStatus.MatchAlreadyStarted, ErrorKeys.Lobby.MatchAlreadyStarted }
            };

        private static readonly IReadOnlyList<Action> _noNotifications = new List<Action>().AsReadOnly();

        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IRoomInvitationRegistry _sessionRegistry;
        private readonly IFriendsSessionAccess _sessionAccess;

        public RoomInvitationService(
            IFriendshipRepository friendshipRepository,
            IRoomInvitationRegistry sessionRegistry,
            IFriendsSessionAccess sessionAccess)
        {
            _friendshipRepository = friendshipRepository;
            _sessionRegistry = sessionRegistry;
            _sessionAccess = sessionAccess;
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

            if (authorization.Status != AuthorizationStatus.Granted)
            {
                operationInviteFriendToRoomResult.ErrorKey = authorization.ErrorKey;
                LogRejectedOperation("invitar a un amigo", authorization.UserId, authorization.Status.ToString());
            }
            else
            {
                OperationResult invitationPermission = CheckInvitationPermission(authorization.UserId, friendUserId);

                if (invitationPermission.IsSuccess)
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
                    operationInviteFriendToRoomResult.ErrorKey = invitationPermission.ErrorKey;
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

            if (authorization.Status != AuthorizationStatus.Granted)
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

        private OperationResult CheckInvitationPermission(int inviterUserId, int friendUserId)
        {
            OperationResult permission = new OperationResult { ErrorKey = ErrorKeys.Friends.CannotInvite };
            bool involvesGuest = GuestIdentity.IsGuestUserId(inviterUserId)
                || GuestIdentity.IsGuestUserId(friendUserId);

            if (involvesGuest)
            {
                permission.IsSuccess = _sessionAccess.AreGuestFriends(inviterUserId, friendUserId);

                if (permission.IsSuccess)
                {
                    permission.ErrorKey = string.Empty;
                }
            }
            else
            {
                permission = CheckDatabaseInvitationPermission(inviterUserId, friendUserId);
            }

            if (!permission.IsSuccess)
            {
                LogRejectedOperation("invitar a un amigo", inviterUserId, permission.ErrorKey);
            }

            return permission;
        }

        private OperationResult CheckDatabaseInvitationPermission(int inviterUserId, int friendUserId)
        {
            FriendshipLookupStatus status = _friendshipRepository.FindFriendshipStatus(inviterUserId, friendUserId);
            OperationResult permission = new OperationResult
            {
                IsSuccess = status == FriendshipLookupStatus.Accepted,
                ErrorKey = ErrorKeys.Common.Unexpected
            };

            if (permission.IsSuccess)
            {
                permission.ErrorKey = string.Empty;
            }
            else if (status == FriendshipLookupStatus.NotAccepted)
            {
                permission.ErrorKey = ErrorKeys.Friends.CannotInvite;
            }

            return permission;
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
    }
}
