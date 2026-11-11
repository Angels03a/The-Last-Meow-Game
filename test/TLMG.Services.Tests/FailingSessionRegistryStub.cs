using System;
using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    /// <summary>Registro de sesiones de prueba cuyas operaciones fallan, para probar errores inesperados.</summary>
    internal sealed class FailingSessionRegistryStub : ISessionRegistry
    {
        private const string FailureMessage = "Fallo simulado del registro de sesiones.";

        public string StartSession(int userId, string nickname, out SessionEnd replacedSession) =>
            throw new InvalidOperationException(FailureMessage);

        public bool TryFindSessionByToken(string sessionToken, out PlayerSession playerSession) =>
            throw new InvalidOperationException(FailureMessage);

        public bool EndSession(string sessionToken, out SessionEnd sessionEnd) =>
            throw new InvalidOperationException(FailureMessage);

        public bool TryEndSessionByFriendsChannel(string channelId, out SessionEnd sessionEnd) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange CreateRoom(
            string sessionToken,
            string channelId,
            ILobbyManagerCallback lobbyCallback) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange EnterRoom(
            string sessionToken,
            int roomId,
            string channelId,
            ILobbyManagerCallback lobbyCallback) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange LeaveRoom(string sessionToken) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange LeaveRoomByChannel(string channelId) =>
            throw new InvalidOperationException(FailureMessage);

        public FriendsSubscription SubscribeFriendsChannel(
            string sessionToken,
            string channelId,
            IFriendsManagerCallback friendsCallback,
            IReadOnlyList<int> friendUserIds) =>
            throw new InvalidOperationException(FailureMessage);

        public RoomInvitationCreation CreateRoomInvitation(string sessionToken, int friendUserId) =>
            throw new InvalidOperationException(FailureMessage);

        public RoomInvitationAcceptance AcceptRoomInvitation(string sessionToken, int invitationId) =>
            throw new InvalidOperationException(FailureMessage);

        public IFriendsManagerCallback FindFriendsCallbackOfUser(int userId) =>
            throw new InvalidOperationException(FailureMessage);

        public FriendsAddition AddFriendshipToOnlineChannels(int firstUserId, int secondUserId) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange ChangePlayerReadiness(
            string sessionToken,
            string channelId,
            int roomId,
            bool isReady) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange ChangeCardsPerPlayer(
            string sessionToken,
            string channelId,
            int roomId,
            int cardsPerPlayerCount) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange RequestMatchStart(
            string sessionToken,
            string channelId,
            int roomId,
            long countdownId) =>
            throw new InvalidOperationException(FailureMessage);

        public string SubscribeToChat(string sessionToken, string channelId, IChatManagerCallback callback) =>
            throw new InvalidOperationException(FailureMessage);

        public string AcceptChatMessage(
            string sessionToken,
            string channelId,
            int roomId,
            Guid clientMessageId,
            string normalizedText,
            string validationErrorKey,
            out ChatDelivery chatDelivery) =>
            throw new InvalidOperationException(FailureMessage);

        public void RemoveChatChannel(string channelId) =>
            throw new InvalidOperationException(FailureMessage);
    }
}
