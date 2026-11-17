namespace TLMG.Domain.Engine
{
    /// <summary>Resultado de la salida de un jugador de la partida.</summary>
    public enum PlayerExitStatus
    {
        /// <summary>El jugador salió y la ronda en curso se reinició; RoundNumber subió una vez.</summary>
        RoundRestarted,

        /// <summary>El jugador salió y quedó un solo jugador en juego, que gana; no empieza otra ronda.</summary>
        MatchFinished,

        /// <summary>El jugador ya estaba eliminado: conserva posición y puntos y nada cambia (H-12 y H-16).</summary>
        EliminatedPlayerLeft,

        /// <summary>La partida ya terminó: la salida no cambia nada.</summary>
        MatchAlreadyFinished,

        /// <summary>El jugador no pertenece a la partida.</summary>
        PlayerNotInMatch,

        /// <summary>El jugador ya había salido de la partida.</summary>
        PlayerAlreadyLeft,

        /// <summary>
        /// Salió el castigado durante el castigo pendiente: el castigo se cancela sin pérdida de vida y empieza la
        /// siguiente ronda (Reglas 2.0.3.1 y H-07).
        /// </summary>
        PunishmentCancelled,

        /// <summary>
        /// Salió quien elige con el castigo activo: el sistema eligió al azar la carta del castigado y el castigo se
        /// completó (Reglas 2.0.3.1).
        /// </summary>
        PunishmentResolvedAtRandom,

        /// <summary>
        /// Se registró la salida durante el castigo pendiente sin tocarlo: salió un tercero, o quien elige con el
        /// castigo suspendido; el castigo sigue y la ronda no cambia.
        /// </summary>
        ExitRecorded
    }
}
