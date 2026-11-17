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
        /// Autoriza y registra la suscripción del jugador a una partida y arma su vista; todo bajo _sessionsLock.
        /// Reemplaza la suscripción anterior del jugador.
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
                    // Primero se prepara todo lo que puede fallar; después solo se sustituye la suscripción.
                    MatchViewDelivery viewDelivery =
                        CreateViewDeliveryInLock(matchRecord, playerSession.UserId, callback);
                    List<MatchViewDelivery> viewDeliveries = new List<MatchViewDelivery> { viewDelivery };
                    matchDelivery = MatchDelivery.CreateAccepted(viewDeliveries.AsReadOnly());
                    RemoveMatchChannelOfUserInLock(playerSession.UserId);
                    _matchChannelsByUserId[playerSession.UserId] =
                        new MatchChannel(caller.SessionToken, caller.ChannelId, matchId, callback);
                    _matchUserIdsByChannelId[caller.ChannelId] = playerSession.UserId;
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
            MatchDelivery matchDelivery;

            lock (_sessionsLock)
            {
                PlayerSession playerSession;
                MatchRecord matchRecord;
                string errorKey = ValidateMatchActionInLock(caller, out playerSession, out matchRecord);

                if (errorKey == null)
                {
                    errorKey = PlaceCardInEngineInLock(matchRecord, playerSession.UserId, card);
                }

                if (errorKey == null)
                {
                    matchRecord.Version++;
                    matchDelivery = MatchDelivery.CreateAccepted(CreateViewDeliveriesInLock(matchRecord));
                }
                else
                {
                    matchDelivery = MatchDelivery.CreateRejected(errorKey);
                }
            }

            return matchDelivery;
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
            bool wasRemoved = false;

            lock (_sessionsLock)
            {
                int userId;

                if (!string.IsNullOrEmpty(channelId) && _matchUserIdsByChannelId.TryGetValue(channelId, out userId))
                {
                    wasRemoved = ReferenceEquals(_matchChannelsByUserId[userId].Callback, closedCallback);
                }

                if (wasRemoved)
                {
                    RemoveMatchChannelOfUserInLock(_matchUserIdsByChannelId[channelId]);
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

        private static MatchViewDelivery CreateViewDeliveryInLock(
            MatchRecord matchRecord,
            int userId,
            IMatchManagerCallback callback)
        {
            MatchView matchView = MatchViewHelper.CreateMatchView(
                matchRecord.MatchId,
                matchRecord.Version,
                matchRecord.MatchState,
                matchRecord.ParticipantNicknames,
                userId);
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
