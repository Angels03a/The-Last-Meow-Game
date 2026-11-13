using System;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Utilities
{
    public class VerificationCodeStore
    {
        private readonly IVerificationCodeRepository _verificationCodeRepository;
        private const int ExpirationTimeMinutes = 5;
        private const string DefaultPendingState = "PENDING";

        public VerificationCodeStore(IVerificationCodeRepository verificationCodeRepository)
        {
            _verificationCodeRepository = verificationCodeRepository;
        }

        public void SaveCode(string email, string codeHash, string type)
        {
            DateTime nowUtc = DateTime.UtcNow;
            DateTime expiresAtUtc = nowUtc.AddMinutes(ExpirationTimeMinutes);

            VerificationCodeRecord record = new VerificationCodeRecord
            {
                Email = email,
                CodeHash = codeHash,
                Type = type,
                State = DefaultPendingState,
                GeneratedAtUtc = nowUtc,
                ExpiresAtUtc = expiresAtUtc,
                AttemptsFailed = 0
            };

            _verificationCodeRepository.SaveCode(record);
        }

        public bool ValidateCode(string email, string codeHash)
        {
            DateTime nowUtc = DateTime.UtcNow;
            bool isValid = _verificationCodeRepository.ConsumeCodeIfValid(email, codeHash, nowUtc);
            return isValid;
        }
    }
}