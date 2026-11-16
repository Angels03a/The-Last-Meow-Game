namespace TLMG.Domain.Engine
{
    /// <summary>Fila de la tabla final de una partida: jugador, posición sin empates y puntos ganados.</summary>
    public sealed class PlayerStanding
    {
        /// <summary>Crea la fila de un jugador.</summary>
        /// <param name="playerId">Identificador del jugador en la partida.</param>
        /// <param name="position">Posición final, empezando en 1.</param>
        /// <param name="points">Puntos ganados en la partida; nunca negativos.</param>
        internal PlayerStanding(int playerId, int position, int points)
        {
            PlayerId = playerId;
            Position = position;
            Points = points;
        }

        /// <summary>Obtiene el identificador del jugador en la partida.</summary>
        public int PlayerId { get; }

        /// <summary>Obtiene la posición final del jugador; 1 es el ganador.</summary>
        public int Position { get; }

        /// <summary>Obtiene los puntos que ganó el jugador; un invitado siempre tiene 0.</summary>
        public int Points { get; }
    }
}
