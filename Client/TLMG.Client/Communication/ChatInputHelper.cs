namespace TLMG.Client.Communication
{
    /// <summary>Valida el alfabeto del chat sin reemplazar la validación del servidor.</summary>
    internal static class ChatInputHelper
    {
        /// <summary>Límite de caracteres por mensaje.</summary>
        public const int MaximumMessageCharacters = 160;
        public const int FirstValidIndex = 0;
        private const string AllowedCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚáéíóúÑñ0123456789 ,?¿";

        /// <summary>Recorta los espacios del texto escrito en el chat.</summary>
        /// <param name="typedText">Texto de la caja de chat; puede ser null.</param>
        /// <returns>El texto recortado; cadena vacía si era null.</returns>
        internal static string TrimMessageText(string typedText)
        {
            string trimmedText = (typedText ?? string.Empty).Trim();

            return trimmedText;
        }

        /// <summary>Indica si el texto ya recortado no trae ningún carácter.</summary>
        /// <param name="trimmedText">Texto devuelto por <see cref="TrimMessageText"/>.</param>
        /// <returns>true si el mensaje está vacío; en otro caso, false.</returns>
        internal static bool IsEmptyMessage(string trimmedText)
        {
            bool isEmptyMessage = string.IsNullOrEmpty(trimmedText);

            return isEmptyMessage;
        }

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
                    if (AllowedCharacters.IndexOf(character) < FirstValidIndex)
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
