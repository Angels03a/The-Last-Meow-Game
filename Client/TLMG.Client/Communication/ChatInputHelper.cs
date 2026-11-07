namespace TLMG.Client.Communication
{
    /// <summary>Valida el alfabeto del chat sin reemplazar la validación del servidor.</summary>
    internal static class ChatInputHelper
    {
        /// <summary>Límite de caracteres por mensaje.</summary>
        public const int MaximumMessageCharacters = 160;
        public const int zeroCaracterCount = 0;
        private const string AllowedCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚáéíóúÑñ0123456789 ,?¿";

        /// <summary>Comprueba longitud y caracteres, deteniéndose ante el primero no permitido.</summary>
        /// <param name="messageText">Texto que ocuparía la caja.</param>
        /// <returns>Indica si puede introducirse el texto, incluso vacío durante la edición.</returns>
        public static bool CanEnterText(string messageText)
        {
            bool isTextAllowed = messageText != null && messageText.Length <= MaximumMessageCharacters;
            if (isTextAllowed)
            {
                foreach (char character in messageText)
                {
                    if (AllowedCharacters.IndexOf(character) < zeroCaracterCount)
                    {
                        isTextAllowed = false;
                        break;
                    }
                }
            }
            return isTextAllowed;
        }
    }
}
