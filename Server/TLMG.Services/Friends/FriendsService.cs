using System;
using System.Collections.Generic;

using log4net;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
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
        private const string NotInRoomErrorKey = "Friends_NotInRoomError";
        private const string CannotInviteErrorKey = "Friends_CannotInviteError";
        private const string InvitationNotValidErrorKey = "Friends_InvitationNotValidError";

        private static readonly ILog _log = LogManager.GetLogger(typeof(FriendsService));

        // CannotInvite cubre "no es tu amigo", "no está en línea" y "ya está en una sala": misma clave, sin filtrar
        // información. InvitationNotValid cubre "no existe", "no es tuya" y "la sala ya no existe".
        private static readonly Dictionary<FriendsChangeStatus, string> _errorKeysByStatus =
            new Dictionary<FriendsChangeStatus, string>
            {
                { FriendsChangeStatus.InvalidSession, CommonErrorKeyHelper.InvalidSessionErrorKey },
                { FriendsChangeStatus.NotInRoom, NotInRoomErrorKey },
                { FriendsChangeStatus.RoomFull, CommonErrorKeyHelper.RoomFullErrorKey },
                { FriendsChangeStatus.CannotInvite, CannotInviteErrorKey },
                { FriendsChangeStatus.InvitationNotValid, InvitationNotValidErrorKey }
            };

        private readonly IFriendshipRepository _friendshipRepository;
        private readonly ISessionRegistry _sessionRegistry;

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
        public SessionOutcome<OperationSubscribeToFriendsResult> SubscribeToFriends(
            string sessionToken,
            string channelId,
            IFriendsManagerCallback friendsCallback)
        {
            OperationSubscribeToFriendsResult operationSubscribeToFriendsResult =
                new OperationSubscribeToFriendsResult();
            PlayerSession playerSession;

            if (!_sessionRegistry.TryFindSessionByToken(sessionToken, out playerSession))
            {
                operationSubscribeToFriendsResult.ErrorKey = CommonErrorKeyHelper.InvalidSessionErrorKey;
                LogRejectedOperation("suscribirse a los amigos", LobbyMembershipChange.NoUserId, "sesión no válida");
            }
            else
            {
                operationSubscribeToFriendsResult = SubscribeFriendsChannel(
                    playerSession,
                    sessionToken,
                    channelId,
                    friendsCallback);
            }

            SessionOutcome<OperationSubscribeToFriendsResult> sessionOutcome =
                new SessionOutcome<OperationSubscribeToFriendsResult>(operationSubscribeToFriendsResult);

            return sessionOutcome;
        }

        /// <summary>Invita a un amigo en línea a la sala del jugador de la sesión.</summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <param name="friendUserId">Id de la cuenta del amigo al que se invita.</param>
        /// <returns>El resultado para quien invita y el aviso de invitación para el invitado.</returns>
        public SessionOutcome<OperationInviteFriendToRoomResult> InviteFriendToRoom(
            string sessionToken,
            int friendUserId)
        {
            OperationInviteFriendToRoomResult operationInviteFriendToRoomResult =
                new OperationInviteFriendToRoomResult();
            IReadOnlyList<Action> notifications = new List<Action>().AsReadOnly();
            PlayerSession playerSession;

            if (!_sessionRegistry.TryFindSessionByToken(sessionToken, out playerSession))
            {
                operationInviteFriendToRoomResult.ErrorKey = CommonErrorKeyHelper.InvalidSessionErrorKey;
                LogRejectedOperation("invitar a un amigo", LobbyMembershipChange.NoUserId, "sesión no válida");
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
                    notifications = CreateInvitationNotifications(roomInvitationCreation);
                }
                else
                {
                    operationInviteFriendToRoomResult.ErrorKey = errorKey;
                }
            }

            SessionOutcome<OperationInviteFriendToRoomResult> sessionOutcome =
                new SessionOutcome<OperationInviteFriendToRoomResult>(operationInviteFriendToRoomResult, notifications);

            return sessionOutcome;
        }

        /// <summary>
        /// Acepta una invitación del jugador de la sesión y lo autoriza a entrar a la sala; no lo mete en ella.
        /// </summary>
        /// <param name="sessionToken">Token de la sesión recibido del cliente.</param>
        /// <param name="invitationId">Id de la invitación recibida.</param>
        /// <returns>El resultado con el id de la sala o la clave de error.</returns>
        public SessionOutcome<OperationAcceptRoomInvitationResult> AcceptRoomInvitation(
            string sessionToken,
            int invitationId)
        {
            RoomInvitationAcceptance roomInvitationAcceptance =
                _sessionRegistry.AcceptRoomInvitation(sessionToken, invitationId);
            OperationAcceptRoomInvitationResult operationAcceptRoomInvitationResult =
                new OperationAcceptRoomInvitationResult();

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

            SessionOutcome<OperationAcceptRoomInvitationResult> sessionOutcome =
                new SessionOutcome<OperationAcceptRoomInvitationResult>(operationAcceptRoomInvitationResult);

            return sessionOutcome;
        }

        private OperationSubscribeToFriendsResult SubscribeFriendsChannel(
            PlayerSession playerSession,
            string sessionToken,
            string channelId,
            IFriendsManagerCallback friendsCallback)
        {
            OperationSubscribeToFriendsResult operationSubscribeToFriendsResult =
                new OperationSubscribeToFriendsResult();
            FriendListLookup friendListLookup = _friendshipRepository.FindAcceptedFriends(playerSession.UserId);

            if (!friendListLookup.WasSuccessful)
            {
                operationSubscribeToFriendsResult.ErrorKey = CommonErrorKeyHelper.UnexpectedErrorKey;
                LogRejectedOperation("suscribirse a los amigos", playerSession.UserId, "fallo de datos");
            }
            else
            {
                FriendsSubscription friendsSubscription = _sessionRegistry.SubscribeFriendsChannel(
                    sessionToken,
                    channelId,
                    friendsCallback,
                    ExtractUserIds(friendListLookup.Friends));

                if (friendsSubscription.Status == FriendsChangeStatus.Applied)
                {
                    operationSubscribeToFriendsResult.IsSuccess = true;
                    operationSubscribeToFriendsResult.Friends =
                        ConvertToFriends(friendListLookup.Friends, friendsSubscription.OnlineFriendUserIds);
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

            return operationSubscribeToFriendsResult;
        }

        // Devuelve la clave de error si la amistad no permite invitar, o null si se puede seguir con el registro.
        private string FindInvitationErrorKey(PlayerSession inviterSession, int friendUserId)
        {
            string errorKey = null;
            FriendshipLookupStatus friendshipLookupStatus =
                _friendshipRepository.FindFriendshipStatus(inviterSession.UserId, friendUserId);

            if (friendshipLookupStatus == FriendshipLookupStatus.DataFailure)
            {
                errorKey = CommonErrorKeyHelper.UnexpectedErrorKey;
                LogRejectedOperation("invitar a un amigo", inviterSession.UserId, "fallo de datos");
            }
            else if (friendshipLookupStatus == FriendshipLookupStatus.NotAccepted)
            {
                errorKey = CannotInviteErrorKey;
                LogRejectedOperation("invitar a un amigo", inviterSession.UserId, "no son amigos aceptados");
            }

            return errorKey;
        }

        private OperationInviteFriendToRoomResult CreateInviteResult(
            PlayerSession inviterSession,
            int friendUserId,
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
            List<Action> notifications = new List<Action>();

            if (roomInvitationCreation.Status == FriendsChangeStatus.Applied && roomInvitationCreation.IsNewInvitation)
            {
                IFriendsManagerCallback inviteeCallback = roomInvitationCreation.InviteeCallback;
                RoomInvitation roomInvitation = new RoomInvitation
                {
                    InvitationId = roomInvitationCreation.InvitationId,
                    InviterNickname = roomInvitationCreation.InviterNickname
                };

                notifications.Add(() => inviteeCallback.OnRoomInvitationReceived(roomInvitation));
            }

            IReadOnlyList<Action> invitationNotifications = notifications.AsReadOnly();

            return invitationNotifications;
        }

        private static string ResolveErrorKey(FriendsChangeStatus friendsChangeStatus)
        {
            string errorKey;

            if (!_errorKeysByStatus.TryGetValue(friendsChangeStatus, out errorKey))
            {
                errorKey = CommonErrorKeyHelper.UnexpectedErrorKey;
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