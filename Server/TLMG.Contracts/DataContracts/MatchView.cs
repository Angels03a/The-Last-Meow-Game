using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Vista privada de una partida para un jugador: el estado visible para todos y, solo del destinatario, su mano y
    /// su pila. Una vista con versión menor o igual a la ya mostrada es obsoleta y el cliente la descarta.
    /// </summary>
    [DataContract]
    public class MatchView
    {
        /// <summary>Partida a la que pertenece la vista.</summary>
        [DataMember]
        public Guid MatchId { get; set; }

        /// <summary>Versión de la partida; sube con cada acción aceptada.</summary>
        [DataMember]
        public int Version { get; set; }

        /// <summary>Ronda en curso.</summary>
        [DataMember]
        public int RoundNumber { get; set; }

        /// <summary>Fase de la ronda en curso.</summary>
        [DataMember]
        public MatchRoundPhase Phase { get; set; }

        /// <summary>Cuenta del jugador que tiene el turno.</summary>
        [DataMember]
        public int PlayerInTurnUserId { get; set; }

        /// <summary>Jugadores en su orden fijo de turnos, solo con cantidades.</summary>
        [DataMember]
        public List<MatchPlayerView> Players { get; set; }

        /// <summary>Mano del destinatario; ningún otro jugador la recibe.</summary>
        [DataMember]
        public List<MatchCardKind> OwnHandCards { get; set; }

        /// <summary>
        /// Pila del destinatario, de la carta más antigua a la de arriba; ningún otro jugador la recibe.
        /// </summary>
        [DataMember]
        public List<MatchCardKind> OwnPileCards { get; set; }
    }
}
