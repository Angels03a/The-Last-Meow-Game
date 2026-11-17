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
    }
}
