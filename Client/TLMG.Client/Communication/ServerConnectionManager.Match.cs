using System;
using System.ServiceModel;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Parte del manager que atiende el canal de partida. Cerrar o fallar este canal solo lo limpia: no saca al
    /// jugador de la sala ni termina su sesión.
    /// </summary>
    internal sealed partial class ServerConnectionManager : IMatchConnection
    {
        private MatchManagerClient _matchClient;
        private IClientChannel _matchChannel;
        private ServerCallback _matchCallback;

        /// <inheritdoc />
        public event EventHandler<ServerEventArgs<MatchView>> MatchViewChanged;

        /// <inheritdoc />
        public event EventHandler<ServerEventArgs<string>> MatchErrorReceived;

        /// <inheritdoc />
        public event EventHandler MatchAvailabilityChanged;

        /// <inheritdoc />
        public bool IsMatchSubscribed { get; private set; }

        /// <inheritdoc />
        public MatchView CurrentMatchView { get; private set; }

        /// <inheritdoc />
        public bool SubscribeToCurrentMatch()
        {
            bool wasSubscriptionSent = false;
            bool hasAuthorizedMatch = HasRegisteredSession && CurrentRoom != null && CurrentRoom.MatchId.HasValue;

            if (hasAuthorizedMatch && OpenMatchChannel())
            {
                Guid matchId = CurrentRoom.MatchId.Value;
                wasSubscriptionSent = TrySendToMatchChannel(
                    () => _matchClient.SubscribeToMatch(_sessionToken, matchId),
                    "suscribirse a la partida");
            }

            return wasSubscriptionSent;
        }

        /// <inheritdoc />
        public bool PlaceCard(MatchCardKind cardKind)
        {
            bool wasCardSent = false;

            if (_matchClient != null && IsMatchSubscribed)
            {
                wasCardSent = TrySendToMatchChannel(
                    () => _matchClient.PlaceCard(_sessionToken, cardKind),
                    "colocar una carta");
            }

            return wasCardSent;
        }

        /// <inheritdoc />
        public bool PlaceBet(int betValue)
        {
            bool wasSent = false;
            if (_matchClient != null && IsMatchSubscribed)
            {
                wasSent = TrySendToMatchChannel(
                    () => _matchClient.PlaceBet(_sessionToken, betValue), "apostar");
            }

            return wasSent;
        }

        /// <inheritdoc />
        public bool WithdrawFromBet()
        {
            bool wasSent = false;
            if (_matchClient != null && IsMatchSubscribed)
            {
                wasSent = TrySendToMatchChannel(
                    () => _matchClient.WithdrawFromBet(_sessionToken), "retirarse de la apuesta");
            }

            return wasSent;
        }

        /// <inheritdoc />
        public bool RevealCard(int pileOwnerUserId)
        {
            bool wasSent = false;
            if (_matchClient != null && IsMatchSubscribed)
            {
                wasSent = TrySendToMatchChannel(
                    () => _matchClient.RevealCard(_sessionToken, pileOwnerUserId), "revelar una carta");
            }

            return wasSent;
        }

        /// <inheritdoc />
        public bool ChoosePunishmentCard(int cardIndex)
        {
            bool wasSent = false;
            if (_matchClient != null && IsMatchSubscribed)
            {
                wasSent = TrySendToMatchChannel(
                    () => _matchClient.ChoosePunishmentCard(_sessionToken, cardIndex), "elegir una carta");
            }

            return wasSent;
        }

        internal void NotifyMatchSubscriptionResult(ServerCallback callback, OperationResult subscriptionResult)
        {
            if (IsCurrentMatchCallback(callback) && subscriptionResult != null)
            {
                IsMatchSubscribed = subscriptionResult.IsSuccess;
                MatchAvailabilityChanged?.Invoke(this, EventArgs.Empty);

                if (!subscriptionResult.IsSuccess)
                {
                    MatchErrorReceived?.Invoke(this, new ServerEventArgs<string>(subscriptionResult.ErrorKey));
                }
            }
        }

        internal void NotifyMatchActionResult(ServerCallback callback, OperationResult actionResult)
        {
            if (IsCurrentMatchCallback(callback) && actionResult != null && !actionResult.IsSuccess)
            {
                MatchErrorReceived?.Invoke(this, new ServerEventArgs<string>(actionResult.ErrorKey));
            }
        }

        internal void NotifyMatchViewReceived(ServerCallback callback, MatchView matchView)
        {
            bool isFreshView = IsCurrentMatchCallback(callback)
                && MatchViewVersionHelper.ShouldDisplay(CurrentMatchView, matchView);

            if (isFreshView)
            {
                CurrentMatchView = matchView;
                MatchViewChanged?.Invoke(this, new ServerEventArgs<MatchView>(matchView));
            }
        }

        private bool IsCurrentMatchCallback(ServerCallback callback)
        {
            bool isCurrentCallback = ReferenceEquals(callback, _matchCallback) && _matchClient != null;

            return isCurrentCallback;
        }

        private bool OpenMatchChannel()
        {
            bool isMatchChannelOpen = _matchClient != null;

            if (!isMatchChannelOpen)
            {
                _matchCallback = new ServerCallback(this);
                _matchClient = new MatchManagerClient(new InstanceContext(_matchCallback));
                _matchChannel = _matchClient.InnerChannel;
                WatchMatchChannel(_matchChannel);
                isMatchChannelOpen = TrySendToMatchChannel(_matchClient.Open, "abrir el canal de partida");
            }

            return isMatchChannelOpen;
        }

        /// <summary>
        /// Envía por el canal de partida y, si falla, limpia solo ese canal. A diferencia de TrySend no desconecta la
        /// sesión ni avisa ConnectionLost: el jugador sigue en la sala y en la sesión, sin tablero jugable. No hay
        /// reintentos ni reconexión; el detalle técnico va solo al log.
        /// </summary>
        private bool TrySendToMatchChannel(Action sendOperation, string operationDescription)
        {
            bool wasSent = false;

            try
            {
                sendOperation();
                wasSent = true;
            }
            catch (CommunicationException ex)
            {
                _log.Error(string.Format("Error de comunicación al {0} (canal de partida).", operationDescription), ex);
                CloseMatchChannel();
            }
            catch (TimeoutException ex)
            {
                _log.Error(string.Format("Se agotó el tiempo al {0} (canal de partida).", operationDescription), ex);
                CloseMatchChannel();
            }

            return wasSent;
        }

        private void CloseMatchChannel()
        {
            MatchManagerClient previousClient = _matchClient;
            IClientChannel previousChannel = _matchChannel;
            _matchClient = null;
            _matchChannel = null;
            _matchCallback = null;
            IsMatchSubscribed = false;
            CurrentMatchView = null;

            if (previousClient != null)
            {
                UnwatchMatchChannel(previousChannel);
                CloseClientSafely(previousClient);
                MatchAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void WatchMatchChannel(ICommunicationObject channel)
        {
            channel.Closed += OnMatchChannelEnded;
            channel.Faulted += OnMatchChannelEnded;
        }

        private void UnwatchMatchChannel(ICommunicationObject channel)
        {
            if (channel != null)
            {
                channel.Closed -= OnMatchChannelEnded;
                channel.Faulted -= OnMatchChannelEnded;
            }
        }

        private void OnMatchChannelEnded(object sender, EventArgs eventArguments)
        {
            RunOnInterface(() => CloseMatchChannelIfCurrent(sender));
        }

        /// <summary>
        /// Limpia solo el canal de partida si el que terminó sigue siendo el vigente. A diferencia de los canales de
        /// amigos y de lobby, no desconecta la sesión ni avisa ConnectionLost.
        /// </summary>
        private void CloseMatchChannelIfCurrent(object channel)
        {
            if (_matchChannel != null && ReferenceEquals(channel, _matchChannel))
            {
                CommunicationState channelState = ((ICommunicationObject)channel).State;
                _log.WarnFormat(
                    "El canal de partida terminó sin que la aplicación lo pidiera. Estado: {0}.",
                    channelState);
                CloseMatchChannel();
            }
        }
    }
}
