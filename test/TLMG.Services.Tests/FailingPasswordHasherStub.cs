using System.Security.Cryptography;

using TLMG.Services.Accounts;

namespace TLMG.Services.Tests
{
    internal sealed class FailingPasswordHasherStub : IPasswordHasher
    {
        /// <inheritdoc />
        public string HashPassword(string password)
        {
            throw new CryptographicException("Fallo criptográfico sintético.");
        }

        /// <inheritdoc />
        public bool VerifyPassword(string password, string storedHash)
        {
            throw new CryptographicException("Fallo criptográfico sintético.");
        }
    }
}
