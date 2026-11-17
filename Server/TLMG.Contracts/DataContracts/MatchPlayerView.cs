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
    }
}
