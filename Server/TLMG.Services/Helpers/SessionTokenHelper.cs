using System;
using System.Security.Cryptography;

namespace TLMG.Services.Helpers
{
    public static class SessionTokenHelper
    {
        private const int TokenSizeBytes = 32;

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