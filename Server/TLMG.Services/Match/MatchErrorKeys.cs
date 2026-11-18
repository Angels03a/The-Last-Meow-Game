namespace TLMG.Services.Match
{
    /// <summary>
    /// Claves de error de las operaciones de partida que el cliente traduce. Viven aquí porque este incremento no
    /// modifica Domain; al integrarlas en ErrorKeys.Match solo cambia dónde se declaran, no su valor.
    /// </summary>
    public static class MatchErrorKeys
    {
        /// <summary>Partida inexistente, ajena o a la que el jugador ya no pertenece.</summary>
        public const string NotInMatch = "Match_NotInMatchError";

        /// <summary>El canal no está suscrito a la partida.</summary>
        public const string NotSubscribed = "Match_NotSubscribedError";

        /// <summary>No es el turno del jugador.</summary>
        public const string NotYourTurn = "Match_NotYourTurnError";

        /// <summary>Hay una apuesta en curso o un reto listo: no se pueden colocar cartas.</summary>
        public const string BetInProgress = "Match_BetInProgressError";

        /// <summary>El jugador no tiene cartas en la mano.</summary>
        public const string HandEmpty = "Match_HandEmptyError";

        /// <summary>La mano del jugador no contiene una carta de ese tipo.</summary>
        public const string CardNotInHand = "Match_CardNotInHandError";

        /// <summary>Hay un castigo pendiente: no se admite ninguna acción.</summary>
        public const string PunishmentPending = "Match_PunishmentPendingError";

        /// <summary>La partida terminó.</summary>
        public const string MatchFinished = "Match_FinishedError";

        /// <summary>El tipo de carta recibido no existe.</summary>
        public const string CardNotValid = "Match_CardNotValidError";

        /// <summary>No hay una apuesta en curso: no se puede subir ni retirarse.</summary>
        public const string NoBetInProgress = "Match_NoBetInProgressError";

        /// <summary>No hay cartas boca abajo en la mesa: no se puede apostar.</summary>
        public const string TableEmpty = "Match_TableEmptyError";

        /// <summary>Con la mano vacía solo se puede apostar exactamente 1.</summary>
        public const string EmptyHandBet = "Match_EmptyHandBetError";

        /// <summary>La apuesta está fuera del rango permitido o no supera la actual.</summary>
        public const string BetValueNotValid = "Match_BetValueNotValidError";

        /// <summary>Para abrir una apuesta hay que haber colocado antes una carta propia en la ronda.</summary>
        public const string NoCardPlaced = "Match_NoCardPlacedError";

        /// <summary>No hay un reto listo para voltear cartas.</summary>
        public const string NoChallenge = "Match_NoChallengeError";

        /// <summary>Solo el apostador con la apuesta más alta voltea cartas.</summary>
        public const string NotTheBettor = "Match_NotTheBettorError";

        /// <summary>El dueño de la pila elegida ya no está en la partida.</summary>
        public const string PileOwnerNotInMatch = "Match_PileOwnerNotInMatchError";

        /// <summary>Primero hay que voltear la pila propia.</summary>
        public const string OwnPileFirst = "Match_OwnPileFirstError";

        /// <summary>La pila elegida no tiene cartas por voltear.</summary>
        public const string PileEmpty = "Match_PileEmptyError";

        /// <summary>No hay un castigo pendiente.</summary>
        public const string NoPunishmentPending = "Match_NoPunishmentPendingError";

        /// <summary>Solo quien debe elegir puede elegir la carta del castigo.</summary>
        public const string NotTheChooser = "Match_NotTheChooserError";

        /// <summary>El lugar o la posición de la carta elegida no existe.</summary>
        public const string CardIndexNotValid = "Match_CardIndexNotValidError";
    }
}
