using TLMG.Domain.Constants;

namespace TLMG.Domain.Validators
{
    /// <summary>
    /// Checks the room's cards per player against the allowed values of 4, 6 and 8 (D-04).
    /// </summary>
    public static class CardsPerPlayerValidator
    {
        /// <summary>
        /// Checks whether the card count matches an allowed mode.
        /// </summary>
        /// <param name="cardsPerPlayerCount">Requested number of cards per player.</param>
        /// <returns>Null for a valid value, otherwise the error key.</returns>
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
