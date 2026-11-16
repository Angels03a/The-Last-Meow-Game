namespace TLMG.Domain.Engine
{
    /// <summary>Resultado de elegir o vencer un castigo pendiente; Services lo traduce a ErrorKey.</summary>
    public enum PunishmentStatus
    {
        /// <summary>Se eliminó la carta del castigado; Phase indica cómo sigue la partida.</summary>
        CardRemoved,

        /// <summary>El castigo quedó suspendido: su plazo se detuvo y se guardó el tiempo restante.</summary>
        Suspended,

        /// <summary>El castigo se reanudó con el tiempo que le quedaba, sin reiniciar el plazo.</summary>
        Resumed,

        /// <summary>El jugador que pide elegir no forma parte de la partida.</summary>
        PlayerNotInMatch,

        /// <summary>No hay un castigo pendiente: ya se resolvió o no hubo reto fallido.</summary>
        NoPunishmentPending,

        /// <summary>Solo el jugador que debe elegir puede elegir la carta que se pierde.</summary>
        NotTheChooser,

        /// <summary>El índice no corresponde a ninguna carta del castigado.</summary>
        CardIndexNotValid,

        /// <summary>El plazo para elegir ya venció: la carta la elige el sistema al azar.</summary>
        DeadlineExpired,

        /// <summary>El plazo para elegir todavía no vence: aún no puede elegir el sistema.</summary>
        DeadlineNotReached,

        /// <summary>El castigo está suspendido: no se puede elegir, vencer ni volver a suspender.</summary>
        PunishmentSuspended,

        /// <summary>El castigo no está suspendido: no hay nada que reanudar.</summary>
        PunishmentNotSuspended,

        /// <summary>Solo el jugador castigado puede suspender o reanudar su castigo.</summary>
        NotThePunished
    }
}
