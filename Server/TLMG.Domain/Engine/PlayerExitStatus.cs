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
        /// Rechazo transitorio: hay un castigo pendiente y la salida durante el castigo todavía no se resuelve en el
        /// motor; nada cambia. E6b retira este valor al resolver o cancelar el castigo (2.0.3.1 y H-07).
        /// </summary>
        ExitDuringPunishmentNotYetSupported
    }
}
