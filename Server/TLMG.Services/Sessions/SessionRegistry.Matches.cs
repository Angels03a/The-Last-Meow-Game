using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Engine;

namespace TLMG.Services.Sessions
{
    public partial class SessionRegistry
    {
        /// <summary>
        /// Partidas autorizadas por su MatchId. Se conservan mientras su sala exista, aunque salgan miembros, y se
        /// liberan al eliminarse la sala (ReleaseMatchOfRemovedRoomInLock).
        /// </summary>
        private readonly Dictionary<Guid, MatchRecord> _matchesById = new Dictionary<Guid, MatchRecord>();

        /// <summary>Versión de la primera vista de una partida; una versión mayor siempre es más reciente.</summary>
        private const int InitialMatchVersion = 1;

        private static readonly IReadOnlyList<MatchViewDelivery> _noMatchViewDeliveries =
            new List<MatchViewDelivery>().AsReadOnly();

        /// <summary>
        /// Copia el estado básico de una partida si el usuario estaba entre sus participantes al iniciarla; el
        /// MatchState nunca sale del candado. Es pertenencia histórica, no autorización: quien atienda acciones de
        /// partida debe validar además el token de sesión, la identidad, la participación vigente y, cuando existan,
        /// el abandono definitivo y la ventana de reconexión.
        /// </summary>
        /// <param name="matchId">Partida consultada.</param>
        /// <param name="userId">Jugador que consulta.</param>
        /// <returns>La copia, o null si la partida no existe o el jugador no participa en ella.</returns>
        internal MatchSnapshot FindMatchOfParticipant(Guid matchId, int userId)
        {
            MatchSnapshot matchSnapshot = null;

            lock (_sessionsLock)
            {
                MatchRecord matchRecord;

                if (_matchesById.TryGetValue(matchId, out matchRecord)
                    && matchRecord.ParticipantUserIds.Contains(userId))
                {
                    matchSnapshot = CreateMatchSnapshotInLock(matchRecord);
                }
            }

            return matchSnapshot;
        }

        /// <summary>
        /// Libera la partida de una sala que se elimina, en la misma sección crítica. Es el único punto de liberación.
        /// Hoy la sala se elimina en cuanto sale su último miembro, incluso por perder el canal o terminar la sesión,
        /// porque aún no hay ventana de reconexión (AUD-002); cuando exista, solo cambia esta condición. Los callbacks
        /// de partida no necesitan descarte aquí: cada salida de la sala ya quitó el del jugador que salió.
        /// </summary>
        /// <param name="removedStart">Estado de inicio de la sala que se elimina.</param>
        private void ReleaseMatchOfRemovedRoomInLock(LobbyStartState removedStart)
        {
            if (removedStart.MatchId.HasValue)
            {
                _matchesById.Remove(removedStart.MatchId.Value);
            }
        }

        /// <summary>
        /// Revisa los plazos de la partida con la hora real y aplica los que ya vencieron. Hoy el único plazo es el de
        /// 10 s del castigo (ValidationRules.PunishmentChoiceSeconds): si venció, el motor elimina una carta al azar,
        /// con la misma probabilidad para cada carta del castigado. Es el ÚNICO punto que revisa plazos: se llama al
        /// inicio de cada operación de partida, de SubscribeToMatch y de la salida voluntaria; el servidor no usa
        /// temporizadores (D-54), así que un plazo vencido se aplica en cuanto llega la siguiente operación. El latido
        /// de I4 (BL-058) solo tendrá que llamar a esta función. Requiere _sessionsLock.
        /// </summary>
        /// <param name="matchRecord">Partida cuyos plazos se revisan.</param>
        /// <param name="nowUtc">Hora UTC real, leída una sola vez por operación.</param>
        /// <returns>true si venció un plazo y el estado de la partida cambió; en caso contrario, false.</returns>
        private bool ExpireMatchDeadlinesInLock(MatchRecord matchRecord, DateTime nowUtc)
        {
            PunishmentStatus punishmentStatus = matchRecord.MatchState.ExpirePunishment(nowUtc, _pickRandomIndex);
            bool wasExpired = punishmentStatus == PunishmentStatus.CardRemoved;

            if (wasExpired && matchRecord.LastChallenge != null)
            {
                matchRecord.LastChallenge.ResolvePunishment(MatchPunishmentResolution.ChosenAtRandom);
            }

            DiscardEndedBlindSlots(matchRecord);

            return wasExpired;
        }

        /// <summary>
        /// Saca al jugador del motor de partida por una salida voluntaria inmediata (Reglas No. 3 y 4.0.6): sale por
        /// completo, la ronda en curso se reinicia y la apuesta se anula; un castigo se cancela o se resuelve al
        /// azar según quién salga; si queda un solo jugador activo, gana. Requiere _sessionsLock.
        /// </summary>
        /// <param name="userId">Jugador que sale.</param>
        /// <param name="matchId">Partida de la sala que deja.</param>
        /// <returns>Una vista por cada suscrito si el estado cambió; vacía en otro caso.</returns>
        private IReadOnlyList<MatchViewDelivery> ApplyVoluntaryMatchExitInLock(int userId, Guid matchId)
        {
            IReadOnlyList<MatchViewDelivery> viewDeliveries = _noMatchViewDeliveries;
            MatchRecord matchRecord;

            if (_matchesById.TryGetValue(matchId, out matchRecord))
            {
                DateTime nowUtc = _readUtcNow();
                bool hasChanged = ExpireMatchDeadlinesInLock(matchRecord, nowUtc);
                PlayerExitStatus exitStatus = matchRecord.MatchState.RemovePlayer(
                    userId,
                    PlayerExit.Immediate(nowUtc),
                    _pickRandomIndex);
                hasChanged = hasChanged || HasExitChangedMatch(exitStatus);
                UpdateChallengeAfterExit(matchRecord, exitStatus);
                DiscardEndedBlindSlots(matchRecord);

                if (hasChanged)
                {
                    matchRecord.Version++;
                    viewDeliveries = CreateViewDeliveriesInLock(matchRecord);
                }
            }

            return viewDeliveries;
        }

        /// <summary>Olvida la permutación del castigo a ciegas en cuanto ya no hay un castigo pendiente.</summary>
        private static void DiscardEndedBlindSlots(MatchRecord matchRecord)
        {
            if (matchRecord.MatchState.PendingPunishment == null)
            {
                matchRecord.BlindSlotOrder = null;
            }
        }

        private static bool HasExitChangedMatch(PlayerExitStatus exitStatus)
        {
            bool hasChanged = true;

            switch (exitStatus)
            {
                case PlayerExitStatus.EliminatedPlayerLeft:
                case PlayerExitStatus.MatchAlreadyFinished:
                case PlayerExitStatus.PlayerNotInMatch:
                case PlayerExitStatus.PlayerAlreadyLeft:
                    hasChanged = false;
                    break;
            }

            return hasChanged;
        }

        /// <summary>
        /// Ajusta el desenlace del último reto a lo que hizo la salida: un reto en curso desaparece con la ronda y un
        /// castigo pendiente se anota como cancelado o resuelto al azar.
        /// </summary>
        private static void UpdateChallengeAfterExit(MatchRecord matchRecord, PlayerExitStatus exitStatus)
        {
            MatchChallengeRecord challenge = matchRecord.LastChallenge;
            bool endsRound = exitStatus == PlayerExitStatus.RoundRestarted
                || exitStatus == PlayerExitStatus.MatchFinished;

            if (challenge != null)
            {
                if (challenge.Outcome == MatchChallengeOutcome.InProgress && endsRound)
                {
                    matchRecord.LastChallenge = null;
                }
                else if (challenge.PunishmentResolution == MatchPunishmentResolution.Pending)
                {
                    ResolvePendingPunishmentAfterExit(challenge, exitStatus);
                }
            }
        }

        private static void ResolvePendingPunishmentAfterExit(
            MatchChallengeRecord challenge,
            PlayerExitStatus exitStatus)
        {
            if (exitStatus == PlayerExitStatus.PunishmentResolvedAtRandom)
            {
                challenge.ResolvePunishment(MatchPunishmentResolution.ChosenAtRandom);
            }
            else if (exitStatus == PlayerExitStatus.PunishmentCancelled
                || exitStatus == PlayerExitStatus.MatchFinished)
            {
                challenge.ResolvePunishment(MatchPunishmentResolution.Cancelled);
            }
        }

        /// <summary>
        /// Prepara la partida de la sala sin publicarla: modalidad de la sala y participantes congelados en el orden
        /// de ingreso. Requiere _sessionsLock; puede lanzar si el motor rechaza la configuración.
        /// </summary>
        private MatchRecord CreateMatchRecordInLock(Guid matchId, int roomId)
        {
            int cardsPerPlayerCount = _startsByRoomId[roomId].CardsPerPlayerCount;
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);
            IReadOnlyList<int> participantUserIds =
                _membersByRoomId[roomId].Select(member => member.UserId).ToList().AsReadOnly();
            MatchState matchState = new MatchState(modeRules, participantUserIds);
            MatchRecord matchRecord = new MatchRecord(
                matchId,
                roomId,
                matchState,
                participantUserIds,
                cardsPerPlayerCount,
                CopyNicknamesInLock(participantUserIds));

            return matchRecord;
        }

        /// <summary>Copia los nicknames de los participantes para congelarlos; requiere _sessionsLock.</summary>
        private Dictionary<int, string> CopyNicknamesInLock(IReadOnlyList<int> participantUserIds)
        {
            Dictionary<int, string> nicknamesByUserId = new Dictionary<int, string>();

            foreach (int userId in participantUserIds)
            {
                nicknamesByUserId[userId] = _sessionsByToken[_tokensByUserId[userId]].Nickname;
            }

            return nicknamesByUserId;
        }

        /// <summary>Copia inmutable de la partida; requiere _sessionsLock.</summary>
        private static MatchSnapshot CreateMatchSnapshotInLock(MatchRecord matchRecord)
        {
            MatchState matchState = matchRecord.MatchState;
            MatchSnapshot matchSnapshot = new MatchSnapshot(
                matchRecord.MatchId,
                matchRecord.RoomId,
                matchRecord.ParticipantUserIds,
                matchState.ModeRules.CardsPerPlayerCount,
                matchState.RoundNumber,
                matchState.Phase,
                matchState.PlayerInTurn.PlayerId);

            return matchSnapshot;
        }

        /// <summary>Partida autorizada; mutable solo a través de su MatchState y solo bajo _sessionsLock.</summary>
        private sealed class MatchRecord
        {
            public MatchRecord(
                Guid matchId,
                int roomId,
                MatchState matchState,
                IReadOnlyList<int> participantUserIds,
                int cardsPerPlayerCount,
                IReadOnlyDictionary<int, string> participantNicknames)
            {
                MatchId = matchId;
                RoomId = roomId;
                MatchState = matchState;
                ParticipantUserIds = participantUserIds;
                CardsPerPlayerCount = cardsPerPlayerCount;
                ParticipantNicknames = participantNicknames;
                Version = InitialMatchVersion;
            }

            /// <summary>Identificador de la partida, el mismo que recibe la sala.</summary>
            public Guid MatchId { get; }

            /// <summary>Sala que autorizó la partida.</summary>
            public int RoomId { get; }

            /// <summary>Estado autoritativo del motor; nunca sale del candado.</summary>
            public MatchState MatchState { get; }

            /// <summary>Participantes congelados al autorizar, en el orden de ingreso a la sala.</summary>
            public IReadOnlyList<int> ParticipantUserIds { get; }

            /// <summary>Cartas por jugador de la sala al autorizar.</summary>
            public int CardsPerPlayerCount { get; }

            /// <summary>Nicknames de los participantes al autorizar; las vistas los usan aunque salgan.</summary>
            public IReadOnlyDictionary<int, string> ParticipantNicknames { get; }

            /// <summary>Versión de la partida; sube con cada acción aceptada y solo bajo _sessionsLock.</summary>
            public int Version { get; set; }

            /// <summary>
            /// Desenlace del último reto; null hasta que haya uno y otra vez null cuando se abre la siguiente apuesta
            /// o se coloca la siguiente carta. Solo cambia bajo _sessionsLock.
            /// </summary>
            public MatchChallengeRecord LastChallenge { get; set; }

            /// <summary>
            /// Mientras hay un castigo a ciegas, la permutación lugar → índice real de la mano del castigado, hecha por
            /// el servidor; null si no hay castigo a ciegas pendiente. Nunca sale del registro.
            /// </summary>
            public int[] BlindSlotOrder { get; set; }
        }
    }
}
