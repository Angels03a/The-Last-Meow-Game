using TLMG.Domain.Constants;

namespace TLMG.Domain.Validators
{
    /// <summary>
    /// Valida la cantidad de cartas por jugador que se puede elegir para una sala: 4, 6 u 8 (D-04).
    /// </summary>
    public static class CardsPerPlayerValidator
    {
        /// <summary>
        /// Comprueba que la cantidad sea una de las modalidades permitidas.
        /// </summary>
        /// <param name="cardsPerPlayerCount">Cantidad de cartas por jugador pedida.</param>
        /// <returns>null si es válida; en otro caso, la clave de error.</returns>
        public static string ValidateCardsPerPlayerCount(int cardsPerPlayerCount)
        {
            string errorKey = null;
            bool isSupported = cardsPerPlayerCount == ValidationRules.FourCardsPerPlayerCount
                || cardsPerPlayerCount == ValidationRules.SixCardsPerPlayerCount
                || cardsPerPlayerCount == ValidationRules.EightCardsPerPlayerCount;

            if (!isSupported)
            {
                errorKey = ErrorKeys.Lobby.CardsPerPlayerNotValid;
            }

            return errorKey;
        }
    }
}
