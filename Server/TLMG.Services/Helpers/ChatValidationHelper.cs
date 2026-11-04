using System;
using System.Text;

namespace TLMG.Services.Helpers
{
    /// <summary>Valida el texto del chat mediante una lista cerrada, sin guardar mensajes.</summary>
    public static class ChatValidationHelper
    {
        /// <summary>Clave para texto vacío, incluida una petición sin texto.</summary>
        public const string EmptyErrorKey = "Lobby_ChatEmptyError";

        /// <summary>Clave para texto que supera la longitud permitida.</summary>
        public const string TooLongErrorKey = "Lobby_ChatTooLongError";

        /// <summary>Clave para caracteres no permitidos o Unicode mal formado.</summary>
        public const string CharactersErrorKey = "Lobby_ChatCharactersError";

        /// <summary>Clave para un envío antes de cumplir el intervalo.</summary>
        public const string RateLimitErrorKey = "Lobby_ChatRateLimitError";

        /// <summary>Clave para un canal que requiere suscripción.</summary>
        public const string NotSubscribedErrorKey = "Lobby_ChatNotSubscribedError";

        private const int MaximumChatMessageLengthCharacters = 160;
        private const string AllowedChatCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚáéíóúÑñ0123456789 ,?¿";

        /// <summary>
        /// Recorta espacios permitidos, normaliza a Form C y valida sin lanzar por entradas inválidas.
        /// </summary>
        /// <param name="text">Texto recibido; puede ser null.</param>
        /// <param name="normalizedText">Texto normalizado, o null si no se pudo normalizar.</param>
        /// <returns>null si es válido; en otro caso, la clave de error.</returns>
        public static string ValidateText(string text, out string normalizedText)
        {
            string errorKey = null;
            normalizedText = null;

            if (text == null)
            {
                errorKey = EmptyErrorKey;
            }
            else
            {
                try
                {
                    normalizedText = text.Trim(' ').Normalize(NormalizationForm.FormC);
                }
                catch (ArgumentException)
                {
                    errorKey = CharactersErrorKey;
                }

                if (normalizedText != null)
                {
                    if (normalizedText.Length == 0)
                    {
                        errorKey = EmptyErrorKey;
                    }
                    else if (normalizedText.Length > MaximumChatMessageLengthCharacters)
                    {
                        errorKey = TooLongErrorKey;
                    }
                    else
                    {
                        foreach (char character in normalizedText)
                        {
                            if (AllowedChatCharacters.IndexOf(character) < 0)
                            {
                                errorKey = CharactersErrorKey;
                                break;
                            }
                        }
                    }
                }
            }

            return errorKey;
        }
    }
}
