using System;
using System.Security.Cryptography;
using System.Text;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Utilities
{
    public class VerificationCodeStore
    {
        private const int CodeExpirationMinutes = 5;
        private const int PendingRegistrationMinutes = 15;
        private const string HexByteFormat = "x2";

        private readonly IVerificationCodeRepository _verificationCodeRepository;

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

        public PlayerExistenceLookupStatus FindValidPendingRegistration(string email, out PendingRegistrationRecord pendingRecord)
        {
            PlayerExistenceLookupStatus status =
                _verificationCodeRepository.FindPendingRegistrationByEmail(email, out pendingRecord);

            if (status == PlayerExistenceLookupStatus.Exists && pendingRecord == null)
            {
                status = PlayerExistenceLookupStatus.DataFailure;
            }
            else if (status == PlayerExistenceLookupStatus.Exists && pendingRecord.ExpiresAtUtc < DateTime.UtcNow)
            {
                _verificationCodeRepository.DeletePendingRegistration(pendingRecord.PendingId);
                pendingRecord = null;
                status = PlayerExistenceLookupStatus.NotFound;
            }

            return status;
        }

        public void DeletePendingRegistration(int pendingId)
        {
            _verificationCodeRepository.DeletePendingRegistration(pendingId);
        }

        public bool SaveRegistrationCode(int pendingId, string plainCode)
        {
            DateTime nowUtc = DateTime.UtcNow;
            VerificationCodeRecord record = new VerificationCodeRecord
            {
                UserId = null,
                PendingId = pendingId,
                CodeHash = ComputeCodeHash(plainCode),
                Type = VerificationCodeType.Registration,
                GeneratedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.AddMinutes(CodeExpirationMinutes)
            };

            bool wasCodeSaved = _verificationCodeRepository.SaveCode(record);

            return wasCodeSaved;
        }

        public VerificationCodeValidationResult ValidateRegistrationCode(int pendingId, string plainCode)
        {
            VerificationCodeRecord recordToValidate = new VerificationCodeRecord
            {
                UserId = null,
                PendingId = pendingId,
                Type = VerificationCodeType.Registration,
                CodeHash = ComputeCodeHash(plainCode)
            };

            VerificationCodeValidationResult result =
                _verificationCodeRepository.ValidateCode(recordToValidate, DateTime.UtcNow);

            return result;
        }

        public static string ComputeCodeHash(string plainCode)
        {
            string computedHash;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(plainCode);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                StringBuilder hashBuilder = new StringBuilder();

                foreach (byte hashByte in hashBytes)
                {
                    hashBuilder.Append(hashByte.ToString(HexByteFormat));
                }

                computedHash = hashBuilder.ToString();
            }

            return computedHash;
        }

        public bool SavePasswordResetCode(int userId, string plainCode)
        {
            DateTime nowUtc = DateTime.UtcNow;
            VerificationCodeRecord record = new VerificationCodeRecord
            {
                UserId = userId,
                PendingId = null,
                CodeHash = ComputeCodeHash(plainCode),
                Type = VerificationCodeType.PasswordReset,
                GeneratedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.AddMinutes(CodeExpirationMinutes)
            };

            bool wasCodeSaved = _verificationCodeRepository.SaveCode(record);

            return wasCodeSaved;
        }

        public VerificationCodeValidationResult ValidatePasswordResetCode(int userId, string plainCode)
        {
            VerificationCodeRecord recordToValidate = new VerificationCodeRecord
            {
                UserId = userId,
                PendingId = null,
                Type = VerificationCodeType.PasswordReset,
                CodeHash = ComputeCodeHash(plainCode)
            };

            VerificationCodeValidationResult validationResult =
                _verificationCodeRepository.ValidateCode(recordToValidate, DateTime.UtcNow);

            return validationResult;
        }
    }
}
