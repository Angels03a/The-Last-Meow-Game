using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Engine;
using TLMG.Services.Helpers;
using TLMG.Services.Match;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        private readonly Dictionary<int, MatchChannel> _matchChannelsByUserId = new Dictionary<int, MatchChannel>();
        private readonly Dictionary<string, int> _matchUserIdsByChannelId = new Dictionary<string, int>();

        /// <summary>
        /// Acción de partida que se aplica al motor bajo _sessionsLock, ya autorizada y con los plazos revisados.
        /// Devuelve null si el motor la aceptó o la ErrorKey del rechazo.
        /// </summary>
        private delegate string MatchAction(MatchRecord matchRecord, int userId, DateTime nowUtc);

        /// <summary>
        /// Autoriza y registra la suscripción del jugador a una partida y arma su vista; todo bajo _sessionsLock.
        /// Reemplaza la suscripción anterior del jugador. Antes de armar la vista revisa los plazos vencidos.
        /// </summary>
        /// <param name="caller">Token de cuenta registrada y canal de partida.</param>
        /// <param name="matchId">Partida solicitada por el cliente.</param>
        /// <param name="callback">Callback del canal de partida.</param>
        /// <returns>La vista del jugador si se aceptó; la clave de error si se rechazó.</returns>
        public MatchDelivery SubscribeToMatch(LobbyCaller caller, Guid matchId, IMatchManagerCallback callback)
        {
            MatchDelivery matchDelivery;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                MatchRecord matchRecord;
                string errorKey = ValidateMatchSubscriptionInLock(
                    caller, matchId, callback, out playerSession, out matchRecord);

                if (errorKey == null)
                {
                    matchDelivery = SubscribeInLock(caller, playerSession.UserId, matchRecord, callback);
                }
                else
                {
                    matchDelivery = MatchDelivery.CreateRejected(errorKey);
                }
            }

            return matchDelivery;
        }

        /// <summary>
        /// Autoriza y coloca una carta en la partida a la que está suscrito el canal; el userId sale de la sesión.
        /// Si el motor la acepta, sube la versión y arma una vista por cada suscrito; si la rechaza, nada cambia.
        /// </summary>
        /// <param name="caller">Token vigente y canal de partida ya suscrito.</param>
        /// <param name="card">Tipo de carta que el jugador quiere colocar.</param>
        /// <returns>Las vistas de todos los suscritos si se aceptó; la clave de error si se rechazó.</returns>
        public MatchDelivery PlaceCard(LobbyCaller caller, MatchCardKind card)
        {
            MatchAction placeCardAction =
                (matchRecord, userId, nowUtc) => PlaceCardInEngineInLock(matchRecord, userId, card);
            MatchDelivery matchDelivery = RunMatchAction(caller, placeCardAction);

            return matchDelivery;
        }

        /// <summary>
        /// Autoriza y abre o sube una apuesta: si ya hay una en curso la sube, y si no, intenta abrirla. Abrir borra
        /// el desenlace del reto anterior.
        /// </summary>
        /// <param name="caller">Token vigente y canal de partida ya suscrito.</param>
        /// <param name="betValue">Cartas que el jugador declara poder voltear.</param>
        /// <returns>Las vistas de todos los suscritos si se aceptó; la clave de error si se rechazó.</returns>
        public MatchDelivery PlaceBet(LobbyCaller caller, int betValue)
        {
            MatchAction placeBetAction =
                (matchRecord, userId, nowUtc) => PlaceBetInEngineInLock(matchRecord, userId, betValue);
            MatchDelivery matchDelivery = RunMatchAction(caller, placeBetAction);

            return matchDelivery;
        }

        /// <summary>
        /// Autoriza y retira al jugador de la apuesta. Si queda un solo apostador, el reto queda listo y el registro
        /// empieza a anotar su desenlace.
        /// </summary>
        /// <param name="caller">Token vigente y canal de partida ya suscrito.</param>
        /// <returns>Las vistas de todos los suscritos si se aceptó; la clave de error si se rechazó.</returns>
        public MatchDelivery WithdrawFromBet(LobbyCaller caller)
        {
            MatchAction withdrawAction =
                (matchRecord, userId, nowUtc) => WithdrawFromBetInEngineInLock(matchRecord, userId);
            MatchDelivery matchDelivery = RunMatchAction(caller, withdrawAction);

            return matchDelivery;
        }

        /// <summary>
        /// Autoriza y voltea la carta de arriba de la pila elegida. Anota cada carta volteada y, al terminar el reto,
        /// su resultado y el castigo resultante, porque el motor pasa de inmediato a la siguiente ronda o termina.
        /// </summary>
        /// <param name="caller">Token vigente y canal de partida ya suscrito.</param>
        /// <param name="pileOwnerUserId">Cuenta del dueño de la pila de la que se voltea.</param>
        /// <returns>Las vistas de todos los suscritos si se aceptó; la clave de error si se rechazó.</returns>
        public MatchDelivery RevealCard(LobbyCaller caller, int pileOwnerUserId)
        {
            MatchAction revealAction = (matchRecord, userId, nowUtc) =>
                RevealCardInEngineInLock(matchRecord, userId, pileOwnerUserId, nowUtc);
            MatchDelivery matchDelivery = RunMatchAction(caller, revealAction);

            return matchDelivery;
        }

        /// <summary>
        /// Autoriza y elimina la carta elegida del castigo; en el castigo a ciegas traduce el lugar con la
        /// permutación del servidor. Si el plazo ya venció, la acción lo aplica primero y la elección se rechaza.
        /// </summary>
        /// <param name="caller">Token vigente y canal de partida ya suscrito.</param>
        /// <param name="cardIndex">Lugar mostrado (a ciegas) o posición en la mano (castigo propio).</param>
        /// <returns>Las vistas de todos los suscritos si se aceptó; la clave de error si se rechazó.</returns>
        public MatchDelivery ChoosePunishmentCard(LobbyCaller caller, int cardIndex)
        {
            MatchAction chooseAction = (matchRecord, userId, nowUtc) =>
                ChoosePunishmentCardInEngineInLock(matchRecord, userId, cardIndex, nowUtc);
            MatchDelivery matchDelivery = RunMatchAction(caller, chooseAction);

            return matchDelivery;
        }

        /// <summary>
        /// Único camino de las acciones de juego: autoriza, revisa los plazos vencidos con la hora real y aplica la
        /// acción al motor, todo bajo _sessionsLock. Si el estado cambió (por la acción o por un plazo vencido) sube
        /// la versión una sola vez y arma una vista por cada suscrito; el rechazo de la acción no deshace un plazo
        /// vencido, así que sus vistas viajan igual.
        /// </summary>
        private MatchDelivery RunMatchAction(LobbyCaller caller, MatchAction matchAction)
        {
            MatchDelivery matchDelivery;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                MatchRecord matchRecord;
                string errorKey = ValidateMatchActionInLock(caller, out playerSession, out matchRecord);

                if (errorKey == null)
                {
                    matchDelivery = ApplyMatchActionInLock(matchRecord, playerSession.UserId, matchAction);
                }
                else
                {
                    matchDelivery = MatchDelivery.CreateRejected(errorKey);
                }
            }

            return matchDelivery;
        }

        private MatchDelivery ApplyMatchActionInLock(MatchRecord matchRecord, int userId, MatchAction matchAction)
        {
            DateTime nowUtc = _readUtcNow();
            bool hasExpired = ExpireMatchDeadlinesInLock(matchRecord, nowUtc);
            string errorKey;
            if (matchRecord.Disconnections.Count != 0
                && matchRecord.MatchState.Phase != RoundPhase.MatchFinished
                && matchRecord.MatchState.PendingPunishment?.ChooserPlayerId != userId)
            {
                errorKey = MatchErrorKeys.ReconnectionPending;
            }
            else if (matchRecord.Kitten != null && !matchRecord.Kitten.IsResolved)
            {
                errorKey = MatchErrorKeys.FugitiveKittenInProgress;
            }
            else
            {
                errorKey = matchAction(matchRecord, userId, nowUtc);
            }
            IReadOnlyList<MatchViewDelivery> viewDeliveries = _noMatchViewDeliveries;

            if (hasExpired || errorKey == null)
            {
                matchRecord.Version++;
                viewDeliveries = CreateViewDeliveriesInLock(matchRecord);
            }

            MatchDelivery matchDelivery;

            if (errorKey == null)
            {
                matchDelivery = MatchDelivery.CreateAccepted(viewDeliveries);
            }
            else
            {
                matchDelivery = MatchDelivery.CreateRejected(errorKey, viewDeliveries);
            }

            return matchDelivery;
        }

        private MatchDelivery SubscribeInLock(
            LobbyCaller caller,
            int userId,
            MatchRecord matchRecord,
            IMatchManagerCallback callback)
        {
            DateTime nowUtc = _readUtcNow();
            bool hasExpired = ExpireMatchDeadlinesInLock(matchRecord, nowUtc);
            if (matchRecord.MatchState.Players.Any(player => player.PlayerId == userId && player.HasLeftMatch))
            {
                if (hasExpired)
                {
                    matchRecord.Version++;
                }
                return MatchDelivery.CreateRejected(MatchErrorKeys.NotInMatch,
                    hasExpired ? CreateViewDeliveriesInLock(matchRecord) : _noMatchViewDeliveries);
            }

            matchRecord.AwaitingFirstLink.Remove(userId);
            if (matchRecord.Disconnections.Remove(userId))
            {
                if (matchRecord.MatchState.Phase != RoundPhase.MatchFinished
                    && matchRecord.MatchState.ResumePunishment(userId, nowUtc, _pickRandomIndex)
                        == PunishmentStatus.CardRemoved)
                {
                    matchRecord.LastChallenge?.ResolvePunishment(MatchPunishmentResolution.ChosenAtRandom);
                    DiscardEndedBlindSlots(matchRecord);
                }
                hasExpired = true;
            }

            if (hasExpired)
            {
                matchRecord.Version++;
            }

            if (UpdateKittenClockInLock(matchRecord, nowUtc) && !hasExpired)
            {
                matchRecord.Version++;
                hasExpired = true;
            }

            // Primero se prepara todo lo que puede fallar; después solo se sustituye la suscripción.
            MatchViewDelivery ownViewDelivery = CreateViewDeliveryInLock(matchRecord, userId, callback);
            RemoveMatchChannelOfUserInLock(userId);
            _matchChannelsByUserId[userId] =
                new MatchChannel(caller.SessionToken, caller.ChannelId, matchRecord.MatchId, callback);
            _matchUserIdsByChannelId[caller.ChannelId] = userId;
            TrackLinkInLock(_sessionsByToken[caller.SessionToken], HeartbeatChannel.Match, caller.ChannelId);
            IReadOnlyList<MatchViewDelivery> viewDeliveries = new List<MatchViewDelivery> { ownViewDelivery }
                .AsReadOnly();

            if (hasExpired)
            {
                viewDeliveries = CreateViewDeliveriesInLock(matchRecord);
            }

            return MatchDelivery.CreateAccepted(viewDeliveries);
        }

        /// <summary>
        /// Quita la suscripción de un canal de partida que se cerró o falló, solo si sigue siendo la registrada: se
        /// compara el callback por referencia para no borrar una suscripción más nueva. No toca la partida ni la
        /// sala.
        /// </summary>
        /// <param name="channelId">Canal que se cerró o falló.</param>
        /// <param name="closedCallback">Callback que tenía ese canal.</param>
        /// <returns>true si se quitó la suscripción; en caso contrario, false.</returns>
        public bool RemoveMatchChannel(string channelId, IMatchManagerCallback closedCallback)
        {
            return RemoveMatchChannel(channelId, closedCallback, out _);
        }

        public bool RemoveMatchChannel(string channelId, IMatchManagerCallback closedCallback,
            out IReadOnlyList<MatchViewDelivery> deliveries)
        {
            bool wasRemoved = false;
            deliveries = _noMatchViewDeliveries;

            lock (_sessionsLock)
            {
                int userId = 0;

                if (!string.IsNullOrEmpty(channelId) && _matchUserIdsByChannelId.TryGetValue(channelId, out userId))
                {
                    wasRemoved = ReferenceEquals(_matchChannelsByUserId[userId].Callback, closedCallback);
                }

                if (wasRemoved)
                {
                    MatchChannel channel = _matchChannelsByUserId[userId];
                    RemoveMatchChannelOfUserInLock(userId);
                    if (_matchesById.TryGetValue(channel.MatchId, out MatchRecord match))
                    {
                        DateTime nowUtc = _readUtcNow();
                        bool changed = ExpireMatchDeadlinesInLock(match, nowUtc);
                        changed |= OpenMatchGraceInLock(match, userId, nowUtc);
                        if (changed)
                        {
                            match.Version++;
                            deliveries = CreateViewDeliveriesInLock(match);
                        }
                    }
                }
            }

            return wasRemoved;
        }

        private string ValidateMatchSubscriptionInLock(
            LobbyCaller caller,
            Guid matchId,
            IMatchManagerCallback callback,
            out PlayerSession playerSession,
            out MatchRecord matchRecord)
        {
            string errorKey = ValidateMatchIdentityInLock(caller, out playerSession);
            matchRecord = null;

            if (errorKey == null && callback == null)
            {
                errorKey = ErrorKeys.Common.InvalidSession;
            }

            if (errorKey == null && !_matchesById.TryGetValue(matchId, out matchRecord))
            {
                errorKey = MatchErrorKeys.NotInMatch;
            }

            if (errorKey == null)
            {
                errorKey = ValidateMatchParticipationInLock(playerSession.UserId, matchRecord);
            }

            return errorKey;
        }

        private string ValidateMatchActionInLock(
            LobbyCaller caller,
            out PlayerSession playerSession,
            out MatchRecord matchRecord)
        {
            string errorKey = ValidateMatchIdentityInLock(caller, out playerSession);
            matchRecord = null;
            MatchChannel matchChannel = null;

            if (errorKey == null && !_matchChannelsByUserId.TryGetValue(playerSession.UserId, out matchChannel))
            {
                errorKey = MatchErrorKeys.NotSubscribed;
            }

            if (errorKey == null && !IsMatchChannelOfCaller(matchChannel, caller))
            {
                errorKey = ErrorKeys.Common.InvalidSession;
            }

            if (errorKey == null && !_matchesById.TryGetValue(matchChannel.MatchId, out matchRecord))
            {
                errorKey = MatchErrorKeys.NotInMatch;
            }

            if (errorKey == null)
            {
                errorKey = ValidateMatchParticipationInLock(playerSession.UserId, matchRecord);
            }

            return errorKey;
        }

        /// <summary>
        /// Valida token vigente, cuenta registrada y que el canal no pertenezca a otro jugador; el mismo criterio del
        /// chat, con el mapa de canales de partida.
        /// </summary>
        private string ValidateMatchIdentityInLock(LobbyCaller caller, out PlayerSession playerSession)
        {
            string errorKey = ErrorKeys.Common.InvalidSession;
            playerSession = null;
            int channelUserId;

            if (caller.HasSessionAndChannel && _sessionsByToken.TryGetValue(caller.SessionToken, out playerSession))
            {
                bool hasRegisteredAccount = playerSession.UserId > LobbyMembershipChange.NoUserId;
                bool isChannelOfOtherUser = _matchUserIdsByChannelId.TryGetValue(caller.ChannelId, out channelUserId)
                    && channelUserId != playerSession.UserId;

                if (hasRegisteredAccount && !isChannelOfOtherUser)
                {
                    errorKey = null;
                }
            }

            return errorKey;
        }

        /// <summary>
        /// Participación vigente: el jugador es de los participantes congelados y sigue en la sala cuyo MatchId
        /// coincide con la partida (AUD-002).
        /// </summary>
        private string ValidateMatchParticipationInLock(int userId, MatchRecord matchRecord)
        {
            string errorKey = MatchErrorKeys.NotInMatch;
            int currentRoomId;

            bool isParticipant = matchRecord.ParticipantUserIds.Contains(userId);

            if (isParticipant && _roomIdsByUserId.TryGetValue(userId, out currentRoomId)
                && currentRoomId == matchRecord.RoomId)
            {
                bool isRoomOfMatch = _startsByRoomId[currentRoomId].MatchId == matchRecord.MatchId;

                if (isRoomOfMatch)
                {
                    errorKey = null;
                }
            }

            return errorKey;
        }

        private static bool IsMatchChannelOfCaller(MatchChannel matchChannel, LobbyCaller caller)
        {
            bool isSameSession = matchChannel.SessionToken == caller.SessionToken;
            bool isSameChannel = matchChannel.ChannelId == caller.ChannelId;

            return isSameSession && isSameChannel;
        }

        private string PlaceCardInEngineInLock(MatchRecord matchRecord, int userId, MatchCardKind card)
        {
            string errorKey = MatchErrorKeys.CardNotValid;
            CardKind engineCard;

            if (MatchViewHelper.TryConvertToEngineCard(card, out engineCard))
            {
                CardPlacementStatus placementStatus = matchRecord.MatchState.PlaceCard(userId, engineCard);
                errorKey = MatchViewHelper.ConvertToErrorKey(placementStatus);

                if (errorKey == null)
                {
                    matchRecord.LastChallenge = null;
                }
            }

            return errorKey;
        }

        private string PlaceBetInEngineInLock(MatchRecord matchRecord, int userId, int betValue)
        {
            MatchState matchState = matchRecord.MatchState;
            bool isOpening = matchState.Phase != RoundPhase.Betting;
            BetActionStatus betStatus;

            if (isOpening)
            {
                betStatus = matchState.OpenBet(userId, betValue);
            }
            else
            {
                betStatus = matchState.RaiseBet(userId, betValue);
            }

            string errorKey = MatchViewHelper.ConvertToErrorKey(betStatus);

            if (errorKey == null && isOpening)
            {
                matchRecord.LastChallenge = null;
            }

            return errorKey;
        }

        private string WithdrawFromBetInEngineInLock(MatchRecord matchRecord, int userId)
        {
            MatchState matchState = matchRecord.MatchState;
            string errorKey = MatchViewHelper.ConvertToErrorKey(matchState.WithdrawFromBet(userId));

            if (errorKey == null && matchState.Phase == RoundPhase.ChallengeReady)
            {
                matchRecord.LastChallenge =
                    MatchChallengeRecord.Begin(matchState.BetLeader.PlayerId, matchState.CurrentBet);
            }

            return errorKey;
        }

        private string RevealCardInEngineInLock(
            MatchRecord matchRecord,
            int userId,
            int pileOwnerUserId,
            DateTime nowUtc)
        {
            ChallengeRevealStatus revealStatus = matchRecord.MatchState.RevealCard(userId, pileOwnerUserId, nowUtc);
            string errorKey = MatchViewHelper.ConvertToErrorKey(revealStatus);

            if (errorKey == null && matchRecord.LastChallenge != null)
            {
                RecordRevealInLock(matchRecord, revealStatus);
            }

            return errorKey;
        }

        /// <summary>Anota la carta volteada y, si el reto terminó, su resultado y el castigo que dejó.</summary>
        private void RecordRevealInLock(MatchRecord matchRecord, ChallengeRevealStatus revealStatus)
        {
            ChallengeResolution resolution = matchRecord.MatchState.Resolution;
            MatchChallengeRecord challenge = matchRecord.LastChallenge;
            challenge.RecordReveal(
                resolution.LastRevealedPlayerId.Value,
                MatchViewHelper.ConvertToContractCard(resolution.LastRevealedCard.Value));

            if (revealStatus == ChallengeRevealStatus.ChallengeSucceeded)
            {
                challenge.MarkSucceeded();
            }
            else if (revealStatus == ChallengeRevealStatus.ChallengeFailed)
            {
                RecordFailedChallengeInLock(matchRecord, challenge);
            }
        }

        /// <summary>
        /// Anota el castigo de un reto fallido. Con una sola vida el motor ya eliminó la carta sin pedir elección; en
        /// otro caso queda pendiente y, si es a ciegas, el servidor baraja los lugares.
        /// </summary>
        private void RecordFailedChallengeInLock(MatchRecord matchRecord, MatchChallengeRecord challenge)
        {
            MatchState matchState = matchRecord.MatchState;
            PendingPunishment pendingPunishment = matchState.PendingPunishment;

            if (pendingPunishment != null)
            {
                challenge.MarkFailed(
                    pendingPunishment.ChooserPlayerId,
                    pendingPunishment.PunishedPlayerId,
                    MatchPunishmentResolution.Pending);
                matchRecord.BlindSlotOrder = null;

                if (pendingPunishment.IsBlindChoice)
                {
                    int punishedCardCount = FindHandCardCount(matchState, pendingPunishment.PunishedPlayerId);
                    matchRecord.BlindSlotOrder = CreateBlindSlotOrder(punishedCardCount);
                }
            }
            else
            {
                PendingPunishment appliedPunishment = matchState.LastPunishment;
                challenge.MarkFailed(
                    appliedPunishment.ChooserPlayerId,
                    appliedPunishment.PunishedPlayerId,
                    MatchPunishmentResolution.SingleLifeNoChoice);
                matchRecord.BlindSlotOrder = null;
            }
        }

        private static int FindHandCardCount(MatchState matchState, int playerId)
        {
            return matchState.Players.First(player => player.PlayerId == playerId).HandCards.Count;
        }

        /// <summary>
        /// Baraja los lugares del castigo a ciegas (Fisher-Yates con el azar inyectado): el lugar mostrado al cliente
        /// es la posición en el arreglo y el valor es el índice real de la carta en la mano del castigado.
        /// </summary>
        private int[] CreateBlindSlotOrder(int cardCount)
        {
            int[] slotOrder = Enumerable.Range(0, cardCount).ToArray();

            for (int slotNumber = cardCount - 1; slotNumber > 0; slotNumber--)
            {
                int swapNumber = _pickRandomIndex(slotNumber + 1);
                int swappedIndex = slotOrder[slotNumber];
                slotOrder[slotNumber] = slotOrder[swapNumber];
                slotOrder[swapNumber] = swappedIndex;
            }

            return slotOrder;
        }

        private string ChoosePunishmentCardInEngineInLock(
            MatchRecord matchRecord,
            int userId,
            int cardIndex,
            DateTime nowUtc)
        {
            int engineIndex = cardIndex;
            string errorKey = null;

            if (IsBlindChooser(matchRecord, userId))
            {
                errorKey = TranslateBlindSlot(matchRecord.BlindSlotOrder, cardIndex, out engineIndex);
            }

            if (errorKey == null)
            {
                PunishmentStatus punishmentStatus =
                    matchRecord.MatchState.ChoosePunishmentCard(userId, engineIndex, nowUtc);
                errorKey = MatchViewHelper.ConvertToErrorKey(punishmentStatus);
            }

            if (errorKey == null)
            {
                matchRecord.LastChallenge?.ResolvePunishment(MatchPunishmentResolution.ChosenByChooser);
                DiscardEndedBlindSlots(matchRecord);
            }

            return errorKey;
        }

        /// <summary>Indica si el jugador es quien elige a ciegas y hay una permutación vigente que traducir.</summary>
        private static bool IsBlindChooser(MatchRecord matchRecord, int userId)
        {
            PendingPunishment pendingPunishment = matchRecord.MatchState.PendingPunishment;
            bool hasBlindSlots = pendingPunishment != null && matchRecord.BlindSlotOrder != null;

            return hasBlindSlots && pendingPunishment.ChooserPlayerId == userId;
        }

        private static string TranslateBlindSlot(int[] blindSlotOrder, int slot, out int engineIndex)
        {
            string errorKey = null;
            engineIndex = slot;

            if (slot < 0 || slot >= blindSlotOrder.Length)
            {
                errorKey = MatchErrorKeys.CardIndexNotValid;
            }
            else
            {
                engineIndex = blindSlotOrder[slot];
            }

            return errorKey;
        }

        private IReadOnlyList<MatchViewDelivery> CreateViewDeliveriesInLock(MatchRecord matchRecord)
        {
            List<MatchViewDelivery> viewDeliveries = new List<MatchViewDelivery>();

            foreach (int userId in matchRecord.ParticipantUserIds)
            {
                MatchChannel matchChannel;

                if (_matchChannelsByUserId.TryGetValue(userId, out matchChannel)
                    && matchChannel.MatchId == matchRecord.MatchId)
                {
                    viewDeliveries.Add(CreateViewDeliveryInLock(matchRecord, userId, matchChannel.Callback));
                }
            }

            IReadOnlyList<MatchViewDelivery> readOnlyViewDeliveries = viewDeliveries.AsReadOnly();

            return readOnlyViewDeliveries;
        }

        private MatchViewDelivery CreateViewDeliveryInLock(
            MatchRecord matchRecord,
            int userId,
            IMatchManagerCallback callback)
        {
            MatchViewContext viewContext = new MatchViewContext(
                matchRecord.MatchId,
                matchRecord.Version,
                matchRecord.MatchState,
                matchRecord.ParticipantNicknames,
                matchRecord.LastChallenge,
                matchRecord.AllParticipantsRegistered);
            MatchView matchView = MatchViewHelper.CreateMatchView(viewContext, userId);
            DateTime nowUtc = _readUtcNow();
            matchView.ServerTimeUtc = nowUtc;
            if (matchView.Phase != MatchRoundPhase.MatchFinished)
            {
                matchView.ReconnectDeadlinesUtc = matchRecord.Disconnections
                    .ToDictionary(entry => entry.Key, entry => entry.Value.EffectiveAtUtc);
                matchView.IsWaitingForInitialLinks = matchRecord.Kitten != null
                    && !matchRecord.Kitten.IsResolved && matchRecord.AwaitingFirstLink.Count != 0
                    && matchRecord.Disconnections.Count == 0;
            }

            if (matchRecord.Disconnections.Count != 0)
            {
                matchView.CanPlaceCard = false;
                matchView.CanPlaceBet = false;
                matchView.CanWithdrawFromBet = false;
                matchView.CanRevealCard = false;
            }
            matchView.FugitiveKitten = CreateFugitiveKittenViewInLock(matchRecord, nowUtc);
            MatchViewDelivery viewDelivery = new MatchViewDelivery(callback, matchView);

            return viewDelivery;
        }

        private void RemoveMatchChannelOfUserInLock(int userId)
        {
            MatchChannel matchChannel;

            if (_matchChannelsByUserId.TryGetValue(userId, out matchChannel))
            {
                _matchUserIdsByChannelId.Remove(matchChannel.ChannelId);
                _matchChannelsByUserId.Remove(userId);
            }
        }
    }
}
