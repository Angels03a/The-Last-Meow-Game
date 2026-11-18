using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Lo que todos pueden ver de un jugador de la partida: solo cantidades, nunca los tipos de sus cartas.
    /// </summary>
    [DataContract]
    public class MatchPlayerView
    {
        /// <summary>Cuenta del jugador.</summary>
        [DataMember]
        public int UserId { get; set; }

        /// <summary>Nickname que tenía el jugador al iniciar la partida.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Cartas que conserva en la mano.</summary>
        [DataMember]
        public int HandCardCount { get; set; }

        /// <summary>Cartas que tiene boca abajo en su pila.</summary>
        [DataMember]
        public int PileCardCount { get; set; }

        /// <summary>Indica si el jugador quedó eliminado.</summary>
        [DataMember]
        public bool IsEliminated { get; set; }

        /// <summary>Retos exitosos del jugador; nunca se reinician durante la partida.</summary>
        [DataMember]
        public int SuccessfulChallengeCount { get; set; }

        /// <summary>Indica si el jugador se retiró de la apuesta en curso.</summary>
        [DataMember]
        public bool HasWithdrawnFromBet { get; set; }

        /// <summary>Indica si el jugador salió de la partida por completo.</summary>
        [DataMember]
        public bool HasLeftMatch { get; set; }

        /// <summary>
        /// Cartas de su pila ya volteadas en el reto en curso, de la de arriba hacia abajo; es información pública.
        /// Vacía fuera de un reto.
        /// </summary>
        [DataMember]
        public List<MatchCardKind> RevealedPileCards { get; set; }
    }
}
