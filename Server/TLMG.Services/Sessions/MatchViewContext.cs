using System;
using System.Collections.Generic;

using TLMG.Domain.Engine;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Lo que hace falta para armar la vista de un jugador: el estado del motor y los datos que el registro guarda
    /// fuera de él. Solo se usa bajo _sessionsLock y nunca sale del registro.
    /// </summary>
    internal sealed class MatchViewContext
    {
        /// <summary>Crea el contexto.</summary>
        /// <param name="matchId">Partida de la vista.</param>
        /// <param name="version">Versión de la partida.</param>
        /// <param name="matchState">Estado del motor; solo se lee.</param>
        /// <param name="nicknamesByUserId">Nicknames congelados al iniciar la partida.</param>
        /// <param name="lastChallenge">Desenlace del último reto; null si no hay uno vigente.</param>
        internal MatchViewContext(
            Guid matchId,
            int version,
            MatchState matchState,
            IReadOnlyDictionary<int, string> nicknamesByUserId,
            MatchChallengeRecord lastChallenge)
        {
            MatchId = matchId;
            Version = version;
            MatchState = matchState;
            NicknamesByUserId = nicknamesByUserId;
            LastChallenge = lastChallenge;
        }

        /// <summary>Obtiene la partida de la vista.</summary>
        internal Guid MatchId { get; }

        /// <summary>Obtiene la versión de la partida.</summary>
        internal int Version { get; }

        /// <summary>Obtiene el estado del motor.</summary>
        internal MatchState MatchState { get; }

        /// <summary>Obtiene los nicknames congelados al iniciar la partida.</summary>
        internal IReadOnlyDictionary<int, string> NicknamesByUserId { get; }

        /// <summary>Obtiene el desenlace del último reto; null si no hay uno vigente.</summary>
        internal MatchChallengeRecord LastChallenge { get; }
    }
}
