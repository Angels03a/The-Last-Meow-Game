using System;
using System.Collections.Generic;

using TLMG.Domain.Engine;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Copia inmutable del estado básico de una partida autorizada, tomada bajo _sessionsLock; no guarda referencias
    /// al MatchState.
    /// </summary>
    internal sealed class MatchSnapshot
    {
        /// <summary>Crea la copia.</summary>
        /// <param name="matchId">Identificador de la partida.</param>
        /// <param name="roomId">Sala que la autorizó.</param>
        /// <param name="participantUserIds">Participantes en orden de ingreso; lista ya congelada.</param>
        /// <param name="cardsPerPlayerCount">Cartas por jugador de la modalidad del motor.</param>
        /// <param name="roundNumber">Ronda en curso del motor.</param>
        /// <param name="phase">Fase en curso del motor.</param>
        /// <param name="playerInTurnUserId">Jugador en turno.</param>
        /// <param name="guestUserIds">Invitados congelados al autorizar; no se persisten en Game_Player.</param>
        internal MatchSnapshot(
            Guid matchId,
            int roomId,
            IReadOnlyList<int> participantUserIds,
            int cardsPerPlayerCount,
            int roundNumber,
            RoundPhase phase,
            int playerInTurnUserId,
            IReadOnlyList<int> guestUserIds = null)
        {
            MatchId = matchId;
            RoomId = roomId;
            ParticipantUserIds = participantUserIds;
            GuestUserIds = guestUserIds ?? Array.Empty<int>();
            CardsPerPlayerCount = cardsPerPlayerCount;
            RoundNumber = roundNumber;
            Phase = phase;
            PlayerInTurnUserId = playerInTurnUserId;
        }

        /// <summary>Obtiene el identificador de la partida.</summary>
        internal Guid MatchId { get; }

        /// <summary>Obtiene la sala que autorizó la partida.</summary>
        internal int RoomId { get; }

        /// <summary>Obtiene los participantes en el orden de ingreso a la sala.</summary>
        internal IReadOnlyList<int> ParticipantUserIds { get; }

        /// <summary>Obtiene los identificadores de los invitados congelados al autorizar la partida.</summary>
        internal IReadOnlyList<int> GuestUserIds { get; }

        /// <summary>Obtiene las cartas por jugador de la modalidad con la que se creó el motor.</summary>
        internal int CardsPerPlayerCount { get; }

        /// <summary>Obtiene la ronda en curso.</summary>
        internal int RoundNumber { get; }

        /// <summary>Obtiene la fase en curso.</summary>
        internal RoundPhase Phase { get; }

        /// <summary>Obtiene el jugador en turno.</summary>
        internal int PlayerInTurnUserId { get; }
    }
}