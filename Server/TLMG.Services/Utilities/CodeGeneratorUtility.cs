using System;
using System.Security.Cryptography;

namespace TLMG.Services.Utilities
{
    public class CodeGeneratorUtility
    {
        private const int CodeByteCount = 4;
        private const uint CodeRange = 1000000;
        private const string CodeFormattingPattern = "D6";

        public string GenerateSixDigitCode()
        {
            byte[] randomBytes = new byte[CodeByteCount];

            using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(randomBytes);
            }

            uint randomValue = BitConverter.ToUInt32(randomBytes, 0);
            uint codeValue = randomValue % CodeRange;
            string formattedCode = codeValue.ToString(CodeFormattingPattern);

            return formattedCode;
        }
    }
}