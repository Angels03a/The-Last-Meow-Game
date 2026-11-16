namespace TLMG.Domain.Engine
{
    /// <summary>Resultado de abrir, subir o retirarse de una apuesta; Services lo traduce a ErrorKey.</summary>
    public enum BetActionStatus
    {
        /// <summary>La acción se aplicó.</summary>
        Accepted,

        /// <summary>El jugador no forma parte de la partida.</summary>
        PlayerNotInMatch,

        /// <summary>Juega otro jugador: solo puede actuar quien tiene el turno.</summary>
        NotPlayersTurn,

        /// <summary>No se puede abrir porque ya hay una apuesta abierta o un reto listo para resolver.</summary>
        BetAlreadyOpen,

        /// <summary>No se puede subir ni retirarse porque no hay una apuesta abierta.</summary>
        BetNotOpen,

        /// <summary>No se puede abrir porque no hay cartas boca abajo en la mesa.</summary>
        TableEmpty,

        /// <summary>Con la mano vacía solo se puede abrir una apuesta de exactamente 1.</summary>
        EmptyHandRequiresBetOfOne,

        /// <summary>El valor está fuera del rango permitido o no supera la apuesta actual.</summary>
        BetValueNotValid
    }
}
