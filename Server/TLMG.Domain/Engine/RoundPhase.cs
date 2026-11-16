namespace TLMG.Domain.Engine
{
    /// <summary>Fase en la que está la ronda actual de una partida.</summary>
    public enum RoundPhase
    {
        /// <summary>Los jugadores colocan cartas por turnos; quien tiene el turno puede abrir una apuesta.</summary>
        PlacingCards,

        /// <summary>Hay una apuesta en curso; nadie coloca y cada jugador sube o se retira en su turno.</summary>
        Betting,

        /// <summary>Queda un solo apostador: el reto está listo para resolverse (etapa E3).</summary>
        ChallengeReady
    }
}
