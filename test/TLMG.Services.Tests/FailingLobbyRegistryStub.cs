using System;
using System.Collections.Generic;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services.Tests
{
    internal sealed class FailingLobbyRegistryStub : ILobbyRegistry
    {
        private const string FailureMessage = "Fallo simulado del registro de sesiones.";

        internal LobbyMembershipChange Creation { get; set; }

        public LobbyMembershipChange CreateRoom(
            SessionChannelCaller caller,
            ILobbyManagerCallback lobbyCallback)
        {
            if (Creation == null)
            {
                throw new InvalidOperationException(FailureMessage);
            }

            return Creation;
        }

        public LobbyMembershipChange EnterRoom(
            SessionChannelCaller caller,
            int roomId,
            ILobbyManagerCallback lobbyCallback) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange LeaveRoom(
            string sessionToken,
            out IReadOnlyList<MatchViewDelivery> matchViewDeliveries) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange LeaveRoomByChannel(string channelId) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange ChangeCardsPerPlayer(
            SessionChannelCaller caller,
            int roomId,
            int cardsPerPlayerCount) =>
            throw new InvalidOperationException(FailureMessage);

        public LobbyMembershipChange ChangePlayerReadiness(
            SessionChannelCaller caller,
            int roomId,
            bool isReady) =>
            throw new InvalidOperationException(FailureMessage);
    }
}
