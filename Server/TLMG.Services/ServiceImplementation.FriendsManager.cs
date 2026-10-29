using System;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Friends;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IFriendsManager
    {
        // Se asignan en el constructor estático de ServiceImplementation.cs porque dependen del registro de sesiones.
        private static readonly IFriendshipRepository _friendshipRepository;
        private static readonly FriendsService _friendsService;

        private bool _isFriendsChannelObserved;
        private string _friendsChannelId;

        /// <summary>Delega la suscripción al servicio de amigos y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        public void SubscribeToFriends(string sessionToken)
        {
            IFriendsManagerCallback callback = OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();
            ObserveFriendsChannel();
            SessionOutcome<OperationSubscribeToFriendsResult> sessionOutcome;

            try
            {
                sessionOutcome = _friendsService.SubscribeToFriends(sessionToken, _friendsChannelId, callback);
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al suscribirse a los amigos.", ex);
                sessionOutcome = new SessionOutcome<OperationSubscribeToFriendsResult>(
                    new OperationSubscribeToFriendsResult { ErrorKey = CommonErrorKeyHelper.UnexpectedErrorKey });
            }

            SafeNotify(() => callback.SubscribeToFriendsResult(sessionOutcome.Result));
            SafeNotifyAll(sessionOutcome.Notifications);
        }

        /// <summary>Delega la invitación a la sala al servicio de amigos y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador que invita.</param>
        /// <param name="friendUserId">Id de la cuenta del amigo al que se invita.</param>
        public void InviteFriendToRoom(string sessionToken, int friendUserId)
        {
            IFriendsManagerCallback callback = OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();
            SessionOutcome<OperationInviteFriendToRoomResult> sessionOutcome;

            try
            {
                sessionOutcome = _friendsService.InviteFriendToRoom(sessionToken, friendUserId);
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al invitar a un amigo a la sala.", ex);
                sessionOutcome = new SessionOutcome<OperationInviteFriendToRoomResult>(
                    new OperationInviteFriendToRoomResult { ErrorKey = CommonErrorKeyHelper.UnexpectedErrorKey });
            }

            SafeNotify(() => callback.InviteFriendToRoomResult(sessionOutcome.Result));
            SafeNotifyAll(sessionOutcome.Notifications);
        }

        /// <summary>Delega la aceptación de la invitación al servicio de amigos y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador que acepta.</param>
        /// <param name="invitationId">Id de la invitación recibida.</param>
        public void AcceptRoomInvitation(string sessionToken, int invitationId)
        {
            IFriendsManagerCallback callback = OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();
            SessionOutcome<OperationAcceptRoomInvitationResult> sessionOutcome;

            try
            {
                sessionOutcome = _friendsService.AcceptRoomInvitation(sessionToken, invitationId);
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al aceptar una invitación a la sala.", ex);
                sessionOutcome = new SessionOutcome<OperationAcceptRoomInvitationResult>(
                    new OperationAcceptRoomInvitationResult { ErrorKey = CommonErrorKeyHelper.UnexpectedErrorKey });
            }

            SafeNotify(() => callback.AcceptRoomInvitationResult(sessionOutcome.Result));
            SafeNotifyAll(sessionOutcome.Notifications);
        }

        // El canal vive tanto como esta instancia (PerSession), así que sus eventos no se desuscriben: el canal que
        // los publica termina antes que cualquier otro objeto que pudiera quedar referenciado.
        private void ObserveFriendsChannel()
        {
            if (!_isFriendsChannelObserved)
            {
                IContextChannel friendsChannel = OperationContext.Current.Channel;
                _friendsChannelId = friendsChannel.SessionId;
                friendsChannel.Closed += OnFriendsChannelClosed;
                friendsChannel.Faulted += OnFriendsChannelFaulted;
                _isFriendsChannelObserved = true;
            }
        }

        private void OnFriendsChannelClosed(object sender, EventArgs eventArguments)
        {
            EndSessionOfFriendsChannel();
        }

        private void OnFriendsChannelFaulted(object sender, EventArgs eventArguments)
        {
            EndSessionOfFriendsChannel();
        }

        // Se ejecuta en un hilo del canal y nadie recibe un resultado: una excepción sin capturar aquí tumbaría al
        // servidor, así que este límite del sistema registra el fallo y no lo relanza.
        private void EndSessionOfFriendsChannel()
        {
            try
            {
                SafeNotifyAll(_friendsService.EndSessionByFriendsChannel(_friendsChannelId));
            }
            catch (Exception ex)
            {
                _log.Error("Fallo inesperado al terminar la sesión de un canal de amigos cerrado.", ex);
            }
        }
    }
}