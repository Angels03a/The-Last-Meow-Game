using System;
using System.Collections.Generic;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    internal sealed class FailingSessionRegistryStub : ISessionRegistry
    {
        private const string FailureMessage = "Fallo simulado del registro de sesiones.";

        public string StartRegisteredSession(int userId, string nickname, out SessionEnd replacedSession) =>
            throw new InvalidOperationException(FailureMessage);

        public bool TryFindSessionByToken(string sessionToken, out PlayerSession playerSession) =>
            throw new InvalidOperationException(FailureMessage);

        public bool EndSession(string sessionToken, out SessionEnd sessionEnd) =>
            throw new InvalidOperationException(FailureMessage);

        public bool TryLogOut(string sessionToken, out SessionEnd sessionEnd, out bool hasActiveParticipation) =>
            throw new InvalidOperationException(FailureMessage);

        public ResultWithNotification<OperationRecoveryContextResult> GetRecoveryContext(string sessionToken) =>
            throw new InvalidOperationException(FailureMessage);

        public IReadOnlyList<Action> EvaluateConnectionDeadlines(bool force = true) => Array.Empty<Action>();

        public IReadOnlyList<Action> ProcessHeartbeat(string token, string channelId, HeartbeatChannel channel,
            int sequence, int confirmedSequence, Action<int, string> echo) =>
            throw new InvalidOperationException(FailureMessage);

        public bool TryEndSessionByFriendsChannel(string channelId, out SessionEnd sessionEnd) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange CreateRoom(
            LobbyCaller caller,
            ILobbyManagerCallback lobbyCallback) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange EnterRoom(
            LobbyCaller caller,
            int roomId,
            ILobbyManagerCallback lobbyCallback) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange LeaveRoom(
            string sessionToken,
            out IReadOnlyList<MatchViewDelivery> matchViewDeliveries) =>
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
            LobbyCaller caller,
            int roomId,
            bool isReady) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange ChangeCardsPerPlayer(
            LobbyCaller caller,
            int roomId,
            int cardsPerPlayerCount) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange RequestMatchStart(
            LobbyCaller caller,
            int roomId,
            long countdownId) =>
            throw new InvalidOperationException(FailureMessage);

        public string SubscribeToChat(LobbyCaller caller, IChatManagerCallback callback) =>
            throw new InvalidOperationException(FailureMessage);

        public string AcceptChatMessage(
            LobbyCaller caller,
            ChatSubmission submission,
            out ChatDelivery chatDelivery) =>
            throw new InvalidOperationException(FailureMessage);

        public void RemoveChatChannel(string channelId) =>
            throw new InvalidOperationException(FailureMessage);

        public MatchDelivery SubscribeToMatch(LobbyCaller caller, Guid matchId, IMatchManagerCallback callback) =>
            throw new InvalidOperationException(FailureMessage);

        public MatchDelivery TryCatchFugitiveKitten(LobbyCaller caller) { throw new System.NotImplementedException(); }

        public MatchDelivery CheckFugitiveKittenDeadline(LobbyCaller caller) { throw new System.NotImplementedException(); }

        public MatchDelivery PlaceCard(LobbyCaller caller, MatchCardKind card) =>
            throw new InvalidOperationException(FailureMessage);

        public MatchDelivery PlaceBet(LobbyCaller caller, int betValue) =>
            throw new InvalidOperationException(FailureMessage);

        public MatchDelivery WithdrawFromBet(LobbyCaller caller) =>
            throw new InvalidOperationException(FailureMessage);

        public MatchDelivery RevealCard(LobbyCaller caller, int pileOwnerUserId) =>
            throw new InvalidOperationException(FailureMessage);

        public MatchDelivery ChoosePunishmentCard(LobbyCaller caller, int cardIndex) =>
            throw new InvalidOperationException(FailureMessage);

        public bool RemoveMatchChannel(string channelId, IMatchManagerCallback closedCallback,
            out IReadOnlyList<MatchViewDelivery> deliveries) =>
            throw new InvalidOperationException(FailureMessage);
    }
}