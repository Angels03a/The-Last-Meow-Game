namespace TLMG.Domain.Engine
{
    /// <summary>Resultado de calcular la tabla final de una partida; los demás valores son rechazos.</summary>
    public enum FinalStandingsStatus
    {
        /// <summary>La tabla se calculó: cada jugador tiene posición y puntos.</summary>
        Calculated,

        /// <summary>La partida no ha terminado: todavía no hay tabla final.</summary>
        MatchNotFinished
    }
}
