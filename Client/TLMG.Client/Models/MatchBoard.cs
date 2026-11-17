using System.Collections.Generic;

namespace TLMG.Client.Models
{
    /// <summary>
    /// Lo que la pantalla de partida dibuja a partir de una vista privada del servidor: ronda, fase, jugadores por
    /// lugar de la mesa, y la mano y la pila del jugador local. Se arma completo en cada vista y no se modifica.
    /// </summary>
    public sealed class MatchBoard
    {
        /// <summary>Indica si ya llegó una vista del servidor; antes de eso el tablero está vacío.</summary>
        public bool HasMatchView { get; set; }

        /// <summary>Ronda en curso.</summary>
        public int RoundNumber { get; set; }

        /// <summary>Clave de recurso del nombre de la fase en curso.</summary>
        public string PhaseLabelKey { get; set; }

        /// <summary>Indica si el turno es del jugador local.</summary>
        public bool IsLocalPlayerTurn { get; set; }

        /// <summary>Indica si el jugador local puede pedir colocar una carta; el servidor decide si procede.</summary>
        public bool CanPlaceCards { get; set; }

        /// <summary>Todos los jugadores, en su orden fijo de turnos.</summary>
        public IReadOnlyList<MatchPlayerEntry> Players { get; set; }

        /// <summary>El jugador local, o null si todavía no llega la vista.</summary>
        public MatchPlayerEntry LocalPlayer { get; set; }

        /// <summary>Rival en el lugar izquierdo de la mesa, o null si no hay.</summary>
        public MatchPlayerEntry OpponentLeft { get; set; }

        /// <summary>Rival en el lugar superior izquierdo, o null si no hay.</summary>
        public MatchPlayerEntry OpponentTopLeft { get; set; }

        /// <summary>Rival en el lugar superior, o null si no hay.</summary>
        public MatchPlayerEntry OpponentTop { get; set; }

        /// <summary>Rival en el lugar superior derecho, o null si no hay.</summary>
        public MatchPlayerEntry OpponentTopRight { get; set; }

        /// <summary>Rival en el lugar derecho, o null si no hay.</summary>
        public MatchPlayerEntry OpponentRight { get; set; }

        /// <summary>Mano del jugador local.</summary>
        public IReadOnlyList<HandCardEntry> HandCards { get; set; }

        /// <summary>Pila del jugador local, de la carta más antigua a la de arriba.</summary>
        public IReadOnlyList<PileCardEntry> OwnPileCards { get; set; }
    }
}
