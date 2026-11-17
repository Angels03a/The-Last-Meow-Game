using System.Collections.Generic;

namespace TLMG.Client.Models
{
    /// <summary>
    /// Jugador de la partida tal como lo muestra la pantalla: nickname, cantidades y estado. Nunca guarda los tipos
    /// de las cartas de un rival; solo el servidor los conoce.
    /// </summary>
    public sealed class MatchPlayerEntry
    {
        /// <summary>Cuenta del jugador.</summary>
        public int UserId { get; set; }

        /// <summary>Nickname que tenía el jugador al iniciar la partida.</summary>
        public string Nickname { get; set; }

        /// <summary>Cartas que conserva en la mano.</summary>
        public int HandCardCount { get; set; }

        /// <summary>Cartas que tiene boca abajo en su pila.</summary>
        public int PileCardCount { get; set; }

        /// <summary>Vidas del jugador: las cartas de la mano más las de la pila.</summary>
        public int LivesCount { get; set; }

        /// <summary>Vidas con las que empezó: las cartas por jugador de la modalidad.</summary>
        public int MaximumLivesCount { get; set; }

        /// <summary>Indica si el jugador quedó eliminado.</summary>
        public bool IsEliminated { get; set; }

        /// <summary>Indica si es el jugador que tiene el turno.</summary>
        public bool IsInTurn { get; set; }

        /// <summary>Cartas de la pila en miniatura, una por carta colocada.</summary>
        public IReadOnlyList<PileCardEntry> PileCards { get; set; }
    }
}
