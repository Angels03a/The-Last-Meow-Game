using System;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Friends;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IFriendsManager
    {
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

            SessionOutcome<OperationSubscribeToFriendsResult> sessionOutcome =
                _friendsService.SubscribeToFriends(sessionToken, _friendsChannelId, callback);

            SafeNotify(() => callback.SubscribeToFriendsResult(sessionOutcome.Result));
            SafeNotifyAll(sessionOutcome.Notifications);
        }

        /// <summary>Delega la invitación a la sala al servicio de amigos y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador que invita.</param>
        /// <param name="friendUserId">Identificador de la cuenta del amigo al que se invita.</param>
        public void InviteFriendToRoom(string sessionToken, int friendUserId)
        {
            IFriendsManagerCallback callback = OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

            SessionOutcome<OperationInviteFriendToRoomResult> sessionOutcome =
                _friendsService.InviteFriendToRoom(sessionToken, friendUserId);

            SafeNotify(() => callback.InviteFriendToRoomResult(sessionOutcome.Result));
            SafeNotifyAll(sessionOutcome.Notifications);
        }

        /// <summary>Delega la aceptación de la invitación al servicio de amigos y responde por callback.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador que acepta.</param>
        /// <param name="invitationId">Identificador de la invitación recibida.</param>
        public void AcceptRoomInvitation(string sessionToken, int invitationId)
        {
            IFriendsManagerCallback callback = OperationContext.Current.GetCallbackChannel<IFriendsManagerCallback>();

            SessionOutcome<OperationAcceptRoomInvitationResult> sessionOutcome =
                _friendsService.AcceptRoomInvitation(sessionToken, invitationId);

            SafeNotify(() => callback.AcceptRoomInvitationResult(sessionOutcome.Result));
            SafeNotifyAll(sessionOutcome.Notifications);
        }

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

        private void EndSessionOfFriendsChannel()
        {
            SafeNotifyAll(_friendsService.EndSessionByFriendsChannel(_friendsChannelId));
        }
    }
}