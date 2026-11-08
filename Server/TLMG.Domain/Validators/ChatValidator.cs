using System.Text;
using TLMG.Domain.Constants;

namespace TLMG.Domain.Validators
{
    /// <summary>Valida el texto del chat mediante una lista cerrada y estructura plana de baja complejidad.</summary>
    public static class ChatValidator
    {
        /// <summary>
        /// Recorta espacios, valida reglas de forma modular y normaliza a Form C.
        /// </summary>
        /// <param name="text">Texto recibido; puede ser null.</param>
        /// <param name="normalizedText">Texto normalizado, o null si fue inválido.</param>
        /// <returns>null si es válido; en otro caso, la clave de error.</returns>
        public static string ValidateText(string text, out string normalizedText)
        {
            normalizedText = null;
            string errorKey = ValidatePresenceAndTrim(text, out string trimmedText);

            if (errorKey == null)
            {
                errorKey = ValidateLength(trimmedText);
            }

            if (errorKey == null)
            {
                errorKey = ValidateCharacters(trimmedText);
            }

            if (errorKey == null)
            {
                normalizedText = trimmedText.Normalize(NormalizationForm.FormC);
            }

            return errorKey;
        }

        private static string ValidatePresenceAndTrim(string text, out string trimmedText)
        {
            trimmedText = string.Empty;
            string errorKey = null;

            if (text == null)
            {
                errorKey = ErrorKeys.Lobby.ChatEmpty;
            }

            if (errorKey == null)
            {
                trimmedText = text.Trim(' ');
                if (trimmedText.Length == ValidationRules.EmptyTextLength)
                {
                    errorKey = ErrorKeys.Lobby.ChatEmpty;
                }
            }

            return errorKey;
        }

        private static string ValidateLength(string text)
        {
            string errorKey = null;

            if (text.Length > ValidationRules.ChatMaximumMessageLengthCharacters)
            {
                errorKey = ErrorKeys.Lobby.ChatTooLong;
            }

            return errorKey;
        }

        private static string ValidateCharacters(string text)
        {
            string errorKey = null;
            int currentIndex = 0;
            int textLength = text.Length;
            bool hasInvalidCharacter = false;

            while (currentIndex < textLength && !hasInvalidCharacter)
            {
                char character = text[currentIndex];
                if (ValidationRules.ChatAllowedCharacters.IndexOf(character) 
                    == ValidationRules.InvalidCharacterIndex)
                {
                    hasInvalidCharacter = true;
                }
                currentIndex++;
            }

            if (hasInvalidCharacter)
            {
                errorKey = ErrorKeys.Lobby.ChatCharacters;
            }

            return errorKey;
        }
    }
}