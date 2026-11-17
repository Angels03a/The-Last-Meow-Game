using System;
using System.Collections.Generic;
using System.Linq;

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
        }
    }
}
