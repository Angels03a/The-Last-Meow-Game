using System;
using System.ServiceModel;
using System.Diagnostics;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Manages the match channel. Closing it or losing it clears only that channel. The player stays in the room and
    /// keeps the session.
    /// </summary>
    internal sealed partial class ServerConnectionManager : IMatchConnection
    {
        private MatchManagerClient _matchClient;
        private IClientChannel _matchChannel;
        private ServerCallback _matchCallback;
        private long? _matchSubscriptionUntil;
        private bool _matchSubscriptionAccepted;
        private MatchView _pendingMatchView;
        private readonly Stopwatch _matchViewAge = new Stopwatch();

        /// <inheritdoc />
        public event EventHandler<ServerEventArgs<MatchView>> MatchViewChanged;

        /// <inheritdoc />
        public event EventHandler<ServerEventArgs<string>> MatchErrorReceived;

        /// <inheritdoc />
        public event EventHandler<ServerEventArgs<OperationResult>> FugitiveKittenCatchResultReceived;

        /// <inheritdoc />
        public event EventHandler<ServerEventArgs<OperationResult>> FugitiveKittenDeadlineResultReceived;

        /// <inheritdoc />
        public event EventHandler MatchAvailabilityChanged;

        /// <inheritdoc />
        public bool IsMatchSubscribed { get; private set; }

        public bool CanSendGameplayAction => IsMatchSubscribed && !IsMatchRecoveryPending
            && !IsMatchRecoveryExhausted && CurrentMatchView != null
            && CurrentRoom?.MatchId == CurrentMatchView.MatchId;

        /// <inheritdoc />
        public MatchView CurrentMatchView { get; private set; }

        /// <inheritdoc />
        public TimeSpan CurrentMatchViewAge => _matchViewAge.Elapsed;

        /// <inheritdoc />
        public bool SubscribeToCurrentMatch()
        {
            bool wasSubscriptionSent = false;
            bool hasAuthorizedMatch = HasRegisteredSession && CurrentRoom != null && CurrentRoom.MatchId.HasValue;

            if (hasAuthorizedMatch && (IsMatchSubscribed || _matchSubscriptionUntil.HasValue))
            {
                wasSubscriptionSent = true;
            }
            else if (hasAuthorizedMatch && OpenMatchChannel())
            {
                Guid matchId = CurrentRoom.MatchId.Value;
                _matchSubscriptionUntil = _heartbeatClock.ElapsedMilliseconds + RecoveryReplyMilliseconds;
                wasSubscriptionSent = TrySendToMatchChannel(
                    () => SendConnectionRequest(
                        _matchChannel,
                        () => _matchClient.SubscribeToMatch(_sessionToken, matchId)),
                    "suscribirse a la partida");
            }

            return wasSubscriptionSent;
        }

        /// <inheritdoc />
        public bool TryCatchFugitiveKitten()
        {
            bool wasSent = false;
            if (_matchClient != null && IsMatchSubscribed)
            {
                wasSent = TrySendToMatchChannel(
                    () => _matchClient.TryCatchFugitiveKitten(_sessionToken), "intentar atrapar al gatito");
            }
            return wasSent;
        }

        /// <inheritdoc />
        public bool CheckFugitiveKittenDeadline()
        {
            bool wasSent = false;
            if (_matchClient != null && IsMatchSubscribed)
            {
                wasSent = TrySendToMatchChannel(
                    () => _matchClient.CheckFugitiveKittenDeadline(_sessionToken), "comprobar el gatito");
            }
            return wasSent;
        }

        /// <inheritdoc />
        public bool PlaceCard(MatchCardKind cardKind)
        {
            bool wasCardSent = false;

            if (_matchClient != null && CanSendGameplayAction)
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
            if (_matchClient != null && CanSendGameplayAction)
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
            if (_matchClient != null && CanSendGameplayAction)
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
            if (_matchClient != null && CanSendGameplayAction)
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
            if (_matchClient != null && CanSendGameplayAction)
            {
                wasSent = TrySendToMatchChannel(
                    () => _matchClient.ChoosePunishmentCard(_sessionToken, cardIndex), "elegir una carta");
            }

            return wasSent;
        }

        internal void NotifyMatchSubscriptionResult(ServerCallback callback, OperationResult subscriptionResult)
        {
            if (!IsCurrentMatchCallback(callback) || !IsMatchSubscriptionPending())
            {
                return;
            }

            subscriptionResult = NormalizeOperationResult(subscriptionResult);
            if (subscriptionResult.IsSuccess)
            {
                _matchSubscriptionAccepted = true;
                ConfirmMatchLink(callback);
                return;
            }

            CloseMatchChannel();
            if (subscriptionResult.ErrorKey == "Match_NotInMatchError")
            {
                EndRecoveryContext();
                return;
            }
            else if (subscriptionResult.ErrorKey == InvalidSessionErrorKey)
            {
                _recoveryContext = null;
            }

            MatchErrorReceived?.Invoke(this, new ServerEventArgs<string>(subscriptionResult.ErrorKey));
        }

        internal void NotifyMatchActionResult(ServerCallback callback, OperationResult actionResult)
        {
            if (IsCurrentMatchCallback(callback))
            {
                actionResult = NormalizeOperationResult(actionResult);
                if (!actionResult.IsSuccess)
                {
                    MatchErrorReceived?.Invoke(this, new ServerEventArgs<string>(actionResult.ErrorKey));
                }
            }
        }

        internal void NotifyFugitiveKittenResult(ServerCallback callback, OperationResult result, bool isCatch)
        {
            if (IsCurrentMatchCallback(callback))
            {
                result = NormalizeOperationResult(result);
                ServerEventArgs<OperationResult> eventArgs = new ServerEventArgs<OperationResult>(result);
                if (isCatch)
                {
                    FugitiveKittenCatchResultReceived?.Invoke(this, eventArgs);
                }
                else
                {
                    FugitiveKittenDeadlineResultReceived?.Invoke(this, eventArgs);
                }
                NotifyMatchActionResult(callback, result);
            }
        }

        internal void NotifyMatchViewReceived(ServerCallback callback, MatchView matchView)
        {
            if (!IsCurrentMatchCallback(callback) || matchView == null || CurrentRoom?.MatchId != matchView.MatchId)
            {
                return;
            }

            if (IsMatchSubscribed)
            {
                PublishNewMatchView(matchView);
                return;
            }

            if (IsMatchSubscriptionPending()
                && (CurrentMatchView == null || matchView.Version >= CurrentMatchView.Version)
                && MatchViewVersionHelper.ShouldDisplay(_pendingMatchView, matchView))
            {
                _pendingMatchView = matchView;
                ConfirmMatchLink(callback);
            }
        }

        private bool IsMatchSubscriptionPending()
        {
            return _matchSubscriptionUntil.HasValue
                && _heartbeatClock.ElapsedMilliseconds < _matchSubscriptionUntil.Value;
        }

        private void ConfirmMatchLink(ServerCallback callback)
        {
            if (!_matchSubscriptionAccepted || _pendingMatchView == null)
            {
                return;
            }

            MatchView view = _pendingMatchView;
            _pendingMatchView = null;
            _matchSubscriptionUntil = null;
            IsMatchSubscribed = true;
            PublishNewMatchView(view);
            if (IsCurrentMatchCallback(callback))
            {
                MatchAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void PublishNewMatchView(MatchView view)
        {
            if (MatchViewVersionHelper.ShouldDisplay(CurrentMatchView, view))
            {
                CurrentMatchView = view;
                _matchViewAge.Restart();
                MatchViewChanged?.Invoke(this, new ServerEventArgs<MatchView>(view));
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
                isMatchChannelOpen = TrySendToMatchChannel(
                    () => OpenConnection(_matchClient),
                    "abrir el canal de partida");
            }

            return isMatchChannelOpen;
        }

        private bool TrySendToMatchChannel(Action sendOperation, string operationDescription)
        {
            return TrySend(sendOperation, operationDescription, CloseMatchChannel);
        }


        private void CloseMatchChannel()
        {
            MatchManagerClient previousClient = _matchClient;
            IClientChannel previousChannel = _matchChannel;
            _matchClient = null;
            _matchChannel = null;
            _matchCallback = null;
            IsMatchSubscribed = false;
            _matchSubscriptionUntil = null;
            _matchSubscriptionAccepted = false;
            _pendingMatchView = null;

            if (previousClient != null)
            {
                UnwatchMatchChannel(previousChannel);
                previousClient.Abort();
                if (CurrentRoom?.MatchId.HasValue == true)
                {
                    RequestRecovery();
                }
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
        /// Clears the match channel only if the channel that ended is still current. Unlike the friends and lobby
        /// channels, this leaves the session open and does not raise ConnectionLost.
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
