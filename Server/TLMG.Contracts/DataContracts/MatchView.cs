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

        /// <summary>Hora del servidor cuando se construyó esta vista.</summary>
        [DataMember]
        public DateTime ServerTimeUtc { get; set; }

        /// <summary>Plazos UTC de los jugadores que suspenden la partida por desconexión.</summary>
        [DataMember]
        public Dictionary<int, DateTime> ReconnectDeadlinesUtc { get; set; }

        /// <summary>El minijuego espera las primeras vinculaciones antes de iniciar su reloj.</summary>
        [DataMember]
        public bool IsWaitingForInitialLinks { get; set; }

        /// <summary>Minijuego público; null en las modalidades de cuatro y seis cartas.</summary>
        [DataMember]
        public FugitiveKittenView FugitiveKitten { get; set; }

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

        /// <summary>Apuesta actual en cartas a voltear; 0 si no hay apuesta.</summary>
        [DataMember]
        public int CurrentBet { get; set; }

        /// <summary>Cuenta de quien tiene la apuesta más alta; 0 si no hay apuesta.</summary>
        [DataMember]
        public int BetLeaderUserId { get; set; }

        /// <summary>Retos exitosos que se necesitan para ganar la partida.</summary>
        [DataMember]
        public int ChallengesToWin { get; set; }

        /// <summary>Cuenta del ganador; 0 mientras la partida sigue.</summary>
        [DataMember]
        public int WinnerUserId { get; set; }

        /// <summary>Clasificación pública final; null si no está disponible.</summary>
        [DataMember]
        public List<MatchStandingView> FinalStandings { get; set; }

        /// <summary>Cartas boca abajo en la mesa: la suma de las pilas.</summary>
        [DataMember]
        public int CardsOnTable { get; set; }

        /// <summary>Desenlace del último reto, público; null si no hay uno vigente.</summary>
        [DataMember]
        public MatchChallengeView Challenge { get; set; }

        /// <summary>Castigo pendiente, público; null si no hay ninguno.</summary>
        [DataMember]
        public MatchPunishmentView Punishment { get; set; }

        /// <summary>Indica si el destinatario puede colocar una carta ahora.</summary>
        [DataMember]
        public bool CanPlaceCard { get; set; }

        /// <summary>Indica si el destinatario puede abrir o subir una apuesta ahora.</summary>
        [DataMember]
        public bool CanPlaceBet { get; set; }

        /// <summary>Menor apuesta que el destinatario puede declarar; solo vale si CanPlaceBet.</summary>
        [DataMember]
        public int MinimumBetValue { get; set; }

        /// <summary>Mayor apuesta que el destinatario puede declarar; solo vale si CanPlaceBet.</summary>
        [DataMember]
        public int MaximumBetValue { get; set; }

        /// <summary>Indica si el destinatario puede retirarse de la apuesta ahora.</summary>
        [DataMember]
        public bool CanWithdrawFromBet { get; set; }

        /// <summary>Indica si el destinatario puede elegir una pila para voltear ahora.</summary>
        [DataMember]
        public bool CanRevealCard { get; set; }

        /// <summary>Indica si el destinatario debe elegir ahora la carta que se pierde.</summary>
        [DataMember]
        public bool CanChoosePunishmentCard { get; set; }
    }
}
