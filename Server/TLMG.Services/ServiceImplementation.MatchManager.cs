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
            IMatchManagerCallback callback = GetMatchCallback();

            ResultWithNotification<OperationResult> subscribeToMatchResult =
                _matchService.SubscribeToMatch(CreateMatchCaller(sessionToken), matchId, callback);

            SafeNotify(() => callback.SubscribeToMatchResult(subscribeToMatchResult.OperationResult));
            SafeNotifyAll(subscribeToMatchResult.Notifications);
        }

        public void PlaceCard(string sessionToken, MatchCardKind cardKind)
        {
            IMatchManagerCallback callback = GetMatchCallback();

            ResultWithNotification<OperationResult> placeCardResult =
                _matchService.PlaceCard(CreateMatchCaller(sessionToken), cardKind);

            SafeNotify(() => callback.PlaceCardResult(placeCardResult.OperationResult));
            SafeNotifyAll(placeCardResult.Notifications);
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
                matchChannel.Closed += OnMatchChannelEnded;
                matchChannel.Faulted += OnMatchChannelEnded;
                _isMatchChannelObserved = true;
            }
        }

        /// <summary>El cierre o fallo del canal solo quita su callback; no abandona ni finaliza la partida.</summary>
        private void OnMatchChannelEnded(object sender, EventArgs eventArguments)
        {
            _matchService.RemoveMatchChannel(_matchChannelId, _matchCallback);
        }
    }
}
