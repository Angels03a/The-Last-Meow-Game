namespace TLMG.Domain.Engine
{
    /// <summary>Resultado de voltear una carta al resolver un reto; Services lo traduce a ErrorKey.</summary>
    public enum ChallengeRevealStatus
    {
        /// <summary>Se volteó una carta y el reto continúa.</summary>
        Revealed,

        /// <summary>Se volteó una carta y con ella se alcanzó el número prometido: el reto fue exitoso.</summary>
        ChallengeSucceeded,

        /// <summary>Se volteó un Gato con Maullido Fatal sin vida extra que lo absorbiera: el reto falló.</summary>
        ChallengeFailed,

        /// <summary>El jugador que pide voltear no forma parte de la partida.</summary>
        PlayerNotInMatch,

        /// <summary>No hay un reto en resolución: no se puede voltear ninguna carta.</summary>
        NoChallengeInProgress,

        /// <summary>Solo el apostador con la apuesta más alta puede voltear cartas.</summary>
        NotTheBettor,

        /// <summary>El dueño de la pila elegida no forma parte de la partida.</summary>
        PileOwnerNotInMatch,

        /// <summary>El apostador debe voltear primero las cartas de su propia pila.</summary>
        OwnPileFirst,

        /// <summary>La pila elegida no tiene cartas por voltear.</summary>
        PileFullyRevealed
    }
}
