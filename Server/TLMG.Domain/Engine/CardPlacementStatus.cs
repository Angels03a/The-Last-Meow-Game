namespace TLMG.Domain.Engine
{
    /// <summary>Resultado de pedir al motor que un jugador coloque una carta; Services lo traduce a ErrorKey.</summary>
    public enum CardPlacementStatus
    {
        /// <summary>La carta quedó arriba de la pila del jugador y el turno pasó al siguiente.</summary>
        Placed,

        /// <summary>El jugador no forma parte de la partida.</summary>
        PlayerNotInMatch,

        /// <summary>Hay una apuesta en curso o un reto listo para resolver: nadie puede colocar cartas.</summary>
        BetInProgress,

        /// <summary>Juega otro jugador: solo puede actuar quien tiene el turno.</summary>
        NotPlayersTurn,

        /// <summary>El jugador no tiene cartas en la mano, así que no puede colocar.</summary>
        HandEmpty,

        /// <summary>La mano del jugador no contiene una carta de ese tipo.</summary>
        CardNotInHand
    }
}
