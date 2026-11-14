using System;
using System.Security.Cryptography;
using System.Text;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Utilities
{
    public class VerificationCodeStore
    {
        private readonly IVerificationCodeRepository _verificationCodeRepository;
        private const int CodeExpirationMinutes = 5;
        private const int PendingRegistrationMinutes = 15;

        public VerificationCodeStore(IVerificationCodeRepository verificationCodeRepository)
        {
            _verificationCodeRepository = verificationCodeRepository;
        }

        public int? SavePendingRegistration(string nickname, string email, string passwordHash)
        {
            DateTime nowUtc = DateTime.UtcNow;
            PendingRegistrationRecord record = new PendingRegistrationRecord
            {
                Nickname = nickname,
                Email = email,
                PasswordHash = passwordHash,
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.AddMinutes(PendingRegistrationMinutes)
            };

            int? pendingId = _verificationCodeRepository.SavePendingRegistration(record);

            return pendingId;
        }

        public PendingRegistrationRecord FindValidPendingRegistration(string email)
        {
            PendingRegistrationRecord pendingRecord =
                _verificationCodeRepository.FindPendingRegistrationByEmail(email);

            if (pendingRecord != null && pendingRecord.ExpiresAtUtc < DateTime.UtcNow)
            {
                _verificationCodeRepository.DeletePendingRegistration(pendingRecord.PendingId);
                pendingRecord = null;
            }

            return pendingRecord;
        }

        public void DeletePendingRegistration(int pendingId)
        {
            _verificationCodeRepository.DeletePendingRegistration(pendingId);
        }

        public bool SaveRegistrationCode(int pendingId, string plainCode, string type)
        {
            DateTime nowUtc = DateTime.UtcNow;
            VerificationCodeRecord record = new VerificationCodeRecord
            {
                UserId = null,
                PendingId = pendingId,
                CodeHash = ComputeCodeHash(plainCode),
                Type = type,
                GeneratedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.AddMinutes(CodeExpirationMinutes)
            };

            bool wasCodeSaved = _verificationCodeRepository.SaveCode(record);

            return wasCodeSaved;
        }

        public VerificationCodeValidationResult ValidateRegistrationCode(
            int pendingId,
            string plainCode,
            string type)
        {
            VerificationCodeRecord recordToValidate = new VerificationCodeRecord
            {
                UserId = null,
                PendingId = pendingId,
                Type = type,
                CodeHash = ComputeCodeHash(plainCode)
            };

            VerificationCodeValidationResult result =
                _verificationCodeRepository.ValidateCode(recordToValidate, DateTime.UtcNow);

            return result;
        }

        private static string ComputeCodeHash(string plainCode)
        {
            string computedHash;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(plainCode);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                StringBuilder builder = new StringBuilder();

                for (int byteIndex = 0; byteIndex < hashBytes.Length; byteIndex++)
                {
                    builder.Append(hashBytes[byteIndex].ToString("x2"));
                }

                computedHash = builder.ToString();
            }

            return computedHash;
        }
    }
}