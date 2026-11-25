using System;
using System.Diagnostics;
using System.ServiceModel;

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

        /// <inheritdoc />
        public bool CanSendGameplayAction => IsMatchSubscribed
            && !IsMatchRecoveryPending
            && !IsMatchRecoveryExhausted
            && CurrentMatchView != null
            && CurrentRoom?.MatchId == CurrentMatchView.MatchId;

        /// <inheritdoc />
        public MatchView CurrentMatchView { get; private set; }

        /// <inheritdoc />
        public TimeSpan CurrentMatchViewAge => _matchViewAge.Elapsed;

        /// <inheritdoc />
        public bool SubscribeToCurrentMatch()
        {
            bool wasSubscriptionSent = false;

            if (HasAuthorizedMatch())
            {
                if (IsMatchSubscribed || _matchSubscriptionUntil.HasValue)
                {
                    wasSubscriptionSent = true;
                }
                else if (OpenMatchChannel())
                {
                    Guid matchId = CurrentRoom.MatchId.Value;
                    _matchSubscriptionUntil = _heartbeatClock.ElapsedMilliseconds + RecoveryReplyMilliseconds;
                    wasSubscriptionSent = TrySendToMatchChannel(
                        () => SendConnectionRequest(
                            _matchChannel,
                            () => _matchClient.SubscribeToMatch(_sessionToken, matchId)),
                        "suscribirse a la partida");
                }
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
                    () => _matchClient.TryCatchFugitiveKitten(_sessionToken),
                    "intentar atrapar al gatito");
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
                    () => _matchClient.CheckFugitiveKittenDeadline(_sessionToken),
                    "comprobar el gatito");
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
                    () => _matchClient.PlaceBet(_sessionToken, betValue),
                    "apostar");
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
                    () => _matchClient.WithdrawFromBet(_sessionToken),
                    "retirarse de la apuesta");
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
                    () => _matchClient.RevealCard(_sessionToken, pileOwnerUserId),
                    "revelar una carta");
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
                    () => _matchClient.ChoosePunishmentCard(_sessionToken, cardIndex),
                    "elegir una carta");
            }

            return wasSent;
        }

        internal void NotifyMatchSubscriptionResult(ServerCallback callback, OperationResult subscriptionResult)
        {
            if (IsCurrentMatchCallback(callback) && IsMatchSubscriptionPending())
            {
                subscriptionResult = NormalizeOperationResult(subscriptionResult);
                if (subscriptionResult.IsSuccess)
                {
                    _matchSubscriptionAccepted = true;
                    ConfirmMatchLink(callback);
                }
                else
                {
                    HandleRejectedMatchSubscription(subscriptionResult.ErrorKey);
                }
            }
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
            if (IsMatchViewForCurrentMatch(callback, matchView))
            {
                if (IsMatchSubscribed)
                {
                    PublishNewMatchView(matchView);
                }
                else if (ShouldBufferPendingMatchView(matchView))
                {
                    _pendingMatchView = matchView;
                    ConfirmMatchLink(callback);
                }
            }
        }

        private bool HasAuthorizedMatch()
        {
            bool hasRoomWithMatch = CurrentRoom != null && CurrentRoom.MatchId.HasValue;
            bool hasAuthorizedMatch = HasSession && hasRoomWithMatch;

            return hasAuthorizedMatch;
        }

        private void HandleRejectedMatchSubscription(string errorKey)
        {
            CloseMatchChannel();
            if (errorKey == "Match_NotInMatchError")
            {
                EndRecoveryContext();
            }
            else
            {
                if (errorKey == InvalidSessionErrorKey)
                {
                    _recoveryContext = null;
                }
                MatchErrorReceived?.Invoke(this, new ServerEventArgs<string>(errorKey));
            }
        }

        private bool IsMatchViewForCurrentMatch(ServerCallback callback, MatchView matchView)
        {
            bool isForCurrentMatch = false;
            bool hasValidCallbackAndView = IsCurrentMatchCallback(callback) && matchView != null;

            if (hasValidCallbackAndView && CurrentRoom != null)
            {
                isForCurrentMatch = CurrentRoom.MatchId == matchView.MatchId;
            }

            return isForCurrentMatch;
        }

        private bool ShouldBufferPendingMatchView(MatchView matchView)
        {
            bool isVersionCompatible = CurrentMatchView == null
                || matchView.Version >= CurrentMatchView.Version;
            bool isNewerThanPending = MatchViewVersionHelper.ShouldDisplay(_pendingMatchView, matchView);
            bool shouldBuffer = IsMatchSubscriptionPending() && isVersionCompatible && isNewerThanPending;

            return shouldBuffer;
        }

        private bool IsMatchSubscriptionPending()
        {
            bool isPending = _matchSubscriptionUntil.HasValue
                && _heartbeatClock.ElapsedMilliseconds < _matchSubscriptionUntil.Value;

            return isPending;
        }

        private void ConfirmMatchLink(ServerCallback callback)
        {
            if (_matchSubscriptionAccepted && _pendingMatchView != null)
            {
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
            bool wasSent = TrySend(sendOperation, operationDescription, CloseMatchChannel);

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
