using TLMG.Domain.Constants;

namespace TLMG.Domain.Validators
{
    /// <summary>
    /// Valida el nickname que el jugador escribe para buscar a otro (RC-IA-01 a RC-IA-03): de 3 a 12 caracteres y
    /// sin aceptar un correo.
    /// </summary>
    public static class FriendNicknameValidator
    {
        private const char EmailMarker = '@';

        /// <summary>
        /// Recorta los espacios y valida la longitud y que no sea un correo.
        /// </summary>
        /// <param name="nickname">Nickname recibido; puede ser null.</param>
        /// <param name="normalizedNickname">Nickname sin espacios en los extremos, o null si fue inválido.</param>
        /// <returns>null si es válido; en otro caso, la clave de error.</returns>
        public static string ValidateNickname(string nickname, out string normalizedNickname)
        {
            normalizedNickname = null;
            string errorKey = ValidatePresenceAndTrim(nickname, out string trimmedNickname);

            if (errorKey == null)
            {
                errorKey = ValidateLength(trimmedNickname);
            }

            if (errorKey == null)
            {
                errorKey = ValidateIsNotEmail(trimmedNickname);
            }

            if (errorKey == null)
            {
                normalizedNickname = trimmedNickname;
            }

            return errorKey;
        }

        private static string ValidatePresenceAndTrim(string nickname, out string trimmedNickname)
        {
            trimmedNickname = string.Empty;
            string errorKey = null;

            if (nickname == null)
            {
                errorKey = ErrorKeys.Friends.NicknameFormat;
            }
            else
            {
                trimmedNickname = nickname.Trim();
            }

            return errorKey;
        }

        private static string ValidateLength(string nickname)
        {
            string errorKey = null;

            if (nickname.Length < ValidationRules.NicknameMinimumLength
                || nickname.Length > ValidationRules.NicknameMaximumLength)
            {
                errorKey = ErrorKeys.Friends.NicknameFormat;
            }

            return errorKey;
        }

        private static string ValidateIsNotEmail(string nickname)
        {
            string errorKey = null;

            if (nickname.IndexOf(EmailMarker) != ValidationRules.InvalidCharacterIndex)
            {
                errorKey = ErrorKeys.Friends.NicknameFormat;
            }

            return errorKey;
        }
    }
}
