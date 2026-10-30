using System.Windows;

namespace TLMG.Client
{
    /// <summary>
    /// Traduce las claves de error (ErrorKey) que llegan del servidor con el diccionario de idioma activo.
    /// </summary>
    internal static class ErrorMessageHelper
    {
        private const string UnexpectedErrorKey = "Common_UnexpectedError";

        /// <summary>
        /// Busca el texto de una clave de error; si la clave falta o no existe en el diccionario, devuelve el mensaje
        /// genérico, para que el jugador nunca vea una clave ni un detalle técnico.
        /// </summary>
        /// <param name="resourceOwner">Elemento desde el que se buscan los recursos de idioma.</param>
        /// <param name="errorKey">Clave de error recibida del servidor; puede ser null.</param>
        /// <returns>El mensaje traducido al idioma activo.</returns>
        public static string FindErrorMessage(FrameworkElement resourceOwner, string errorKey)
        {
            string errorMessage = resourceOwner.TryFindResource(errorKey ?? UnexpectedErrorKey) as string;

            if (errorMessage == null)
            {
                errorMessage = resourceOwner.TryFindResource(UnexpectedErrorKey) as string;
            }

            return errorMessage;
        }
    }
}