using System;
using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Services.Sessions;

namespace TLMG.Services
{
    public partial class ServiceImplementation : IMatchManager
    {
        private bool _isMatchChannelObserved;
        private string _matchChannelId;
        private IMatchManagerCallback _matchCallback;

        public void SubscribeToMatch(string sessionToken, Guid matchId)
        {
            LogChannelOperation("Match", "SubscribeToMatch", sessionToken);
            IMatchManagerCallback callback = GetMatchCallback();

            ResultWithNotification<OperationResult> subscribeToMatchResult =
                _matchService.SubscribeToMatch(CreateMatchCaller(sessionToken), matchId, callback);

            SafeNotify(() => callback.SubscribeToMatchResult(subscribeToMatchResult.OperationResult));
            SafeNotifyAll(subscribeToMatchResult.Notifications);
        }

        public void PlaceCard(string sessionToken, MatchCardKind cardKind)
        {
            LogChannelOperation("Match", "PlaceCard", sessionToken);
            IMatchManagerCallback callback = GetMatchCallback();

            ResultWithNotification<OperationResult> placeCardResult =
                _matchService.PlaceCard(CreateMatchCaller(sessionToken), cardKind);

            SafeNotify(() => callback.PlaceCardResult(placeCardResult.OperationResult));
            SafeNotifyAll(placeCardResult.Notifications);
        }

        public void PlaceBet(string sessionToken, int betValue)
        {
            LogChannelOperation("Match", "PlaceBet", sessionToken);
            IMatchManagerCallback callback = GetMatchCallback();

            ResultWithNotification<OperationResult> placeBetResult =
                _matchService.PlaceBet(CreateMatchCaller(sessionToken), betValue);

            SafeNotify(() => callback.PlaceBetResult(placeBetResult.OperationResult));
            SafeNotifyAll(placeBetResult.Notifications);
        }

        public void WithdrawFromBet(string sessionToken)
        {
            LogChannelOperation("Match", "WithdrawFromBet", sessionToken);
            IMatchManagerCallback callback = GetMatchCallback();

            ResultWithNotification<OperationResult> withdrawFromBetResult =
                _matchService.WithdrawFromBet(CreateMatchCaller(sessionToken));

            SafeNotify(() => callback.WithdrawFromBetResult(withdrawFromBetResult.OperationResult));
            SafeNotifyAll(withdrawFromBetResult.Notifications);
        }

        public void RevealCard(string sessionToken, int pileOwnerUserId)
        {
            LogChannelOperation("Match", "RevealCard", sessionToken);
            IMatchManagerCallback callback = GetMatchCallback();

            ResultWithNotification<OperationResult> revealCardResult =
                _matchService.RevealCard(CreateMatchCaller(sessionToken), pileOwnerUserId);

            SafeNotify(() => callback.RevealCardResult(revealCardResult.OperationResult));
            SafeNotifyAll(revealCardResult.Notifications);
        }

        public void ChoosePunishmentCard(string sessionToken, int cardIndex)
        {
            LogChannelOperation("Match", "ChoosePunishmentCard", sessionToken);
            IMatchManagerCallback callback = GetMatchCallback();

            ResultWithNotification<OperationResult> choosePunishmentCardResult =
                _matchService.ChoosePunishmentCard(CreateMatchCaller(sessionToken), cardIndex);

            SafeNotify(() => callback.ChoosePunishmentCardResult(choosePunishmentCardResult.OperationResult));
            SafeNotifyAll(choosePunishmentCardResult.Notifications);
        }

        private IMatchManagerCallback GetMatchCallback()
        {
            ObserveMatchChannel();

            return _matchCallback;
        }

        private LobbyCaller CreateMatchCaller(string sessionToken)
        {
            LobbyCaller matchCaller = new LobbyCaller(sessionToken, _matchChannelId);

            return matchCaller;
        }

        /// <summary>
        /// En la primera operación fija el callback de la sesión; todas las operaciones y la limpieza usan ese mismo
        /// objeto, así la comparación por referencia al cerrar el canal no depende de lo que entregue WCF después.
        /// </summary>
        private void ObserveMatchChannel()
        {
            if (!_isMatchChannelObserved)
            {
                IContextChannel matchChannel = OperationContext.Current.Channel;
                _matchChannelId = matchChannel.SessionId;
                _matchCallback = OperationContext.Current.GetCallbackChannel<IMatchManagerCallback>();
                matchChannel.Closed += OnMatchChannelClosed;
                matchChannel.Faulted += OnMatchChannelFaulted;
                _isMatchChannelObserved = true;
            }
        }

        /// <summary>El cierre o fallo del canal solo quita su callback; no abandona ni finaliza la partida.</summary>
        private void OnMatchChannelClosed(object sender, EventArgs eventArguments)
        {
            LogChannelEnd("Match", "Closed", sender);
            OnMatchChannelEnded(sender, eventArguments);
        }

        private void OnMatchChannelFaulted(object sender, EventArgs eventArguments)
        {
            LogChannelEnd("Match", "Faulted", sender);
            OnMatchChannelEnded(sender, eventArguments);
        }

        private void OnMatchChannelEnded(object sender, EventArgs eventArguments)
        {
            _matchService.RemoveMatchChannel(_matchChannelId, _matchCallback);
        }
    }
}
