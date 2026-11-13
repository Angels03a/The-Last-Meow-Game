using System;
using System.Security.Cryptography;
using System.Text;
using log4net;

namespace TLMG.Services.Accounts
{
    public class PasswordHasher : IPasswordHasher
    {
        private const string FormatVersion = "v1";
        private const string FormatSeparator = "$";
        private const char FormatSeparatorChar = '$';
        private const int FormatPartCount = 3;
        private const int VersionPartIndex = 0;
        private const int SaltPartIndex = 1;
        private const int HashPartIndex = 2;
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;
        private const int StartOffset = 0;
        private const int NoDifference = 0;

        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordHasher));

        public string HashPassword(string password)
        {
            string storedHash = null;

            if (!string.IsNullOrEmpty(password))
            {
                byte[] salt = GenerateSalt();
                byte[] hash = ComputeHash(password, salt);
                storedHash = string.Join(
                    FormatSeparator,
                    FormatVersion,
                    Convert.ToBase64String(salt),
                    Convert.ToBase64String(hash));
            }

            return storedHash;
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            bool isValid = false;
            byte[] salt;
            byte[] expectedHash;

            if (password != null && TryParseStoredHash(storedHash, out salt, out expectedHash))
            {
                byte[] actualHash = ComputeHash(password, salt);
                isValid = AreEqualInConstantTime(expectedHash, actualHash);
            }

            return isValid;
        }

        private static byte[] GenerateSalt()
        {
            byte[] salt = new byte[SaltSizeBytes];

            using (RandomNumberGenerator randomGenerator = RandomNumberGenerator.Create())
            {
                randomGenerator.GetBytes(salt);
            }

            return salt;
        }

        private static byte[] ComputeHash(string password, byte[] salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
            Buffer.BlockCopy(salt, StartOffset, saltedPassword, StartOffset, salt.Length);
            Buffer.BlockCopy(passwordBytes, StartOffset, saltedPassword, salt.Length, passwordBytes.Length);

            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(saltedPassword);
            }

            return hash;
        }

        private static bool TryParseStoredHash(string storedHash, out byte[] salt, out byte[] hash)
        {
            bool isParsed = false;
            salt = null;
            hash = null;

            if (!string.IsNullOrEmpty(storedHash))
            {
                string[] parts = storedHash.Split(FormatSeparatorChar);

                if (parts.Length == FormatPartCount && parts[VersionPartIndex] == FormatVersion)
                {
                    isParsed = TryDecodeParts(parts, out salt, out hash);
                }
            }

            return isParsed;
        }

        private static bool TryDecodeParts(string[] parts, out byte[] salt, out byte[] hash)
        {
            bool isDecoded = false;
            salt = null;
            hash = null;

            try
            {
                salt = Convert.FromBase64String(parts[SaltPartIndex]);
                hash = Convert.FromBase64String(parts[HashPartIndex]);
                isDecoded = salt.Length == SaltSizeBytes && hash.Length == HashSizeBytes;
            }
            catch (FormatException formatException)
            {
                _log.Warn("Un hash guardado tiene la sal o el hash con formato Base64 inválido.", formatException);
            }

            return isDecoded;
        }

        private static bool AreEqualInConstantTime(byte[] expected, byte[] actual)
        {
            int difference = expected.Length ^ actual.Length;
            int sharedLength = Math.Min(expected.Length, actual.Length);

            for (int byteIndex = StartOffset; byteIndex < sharedLength; byteIndex++)
            {
                difference |= expected[byteIndex] ^ actual[byteIndex];
            }

            bool areEqual = difference == NoDifference;

            return areEqual;
        }
    }
}