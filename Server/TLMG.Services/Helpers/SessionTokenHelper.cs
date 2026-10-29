using System;
using System.Security.Cryptography;

namespace TLMG.Services.Helpers
{
    /// <summary>
    /// Genera los tokens de sesión de los jugadores; no guarda estado.
    /// </summary>
    public static class SessionTokenHelper
    {
        private const int TokenSizeBytes = 32;

        /// <summary>Genera un token aleatorio con un generador criptográfico.</summary>
        /// <returns>Los bytes aleatorios codificados en Base64; nunca es null.</returns>
        public static string GenerateSessionToken()
        {
            byte[] tokenBytes = new byte[TokenSizeBytes];

            using (RandomNumberGenerator randomGenerator = RandomNumberGenerator.Create())
            {
                randomGenerator.GetBytes(tokenBytes);
            }

            string sessionToken = Convert.ToBase64String(tokenBytes);

            return sessionToken;
        }
    }
}