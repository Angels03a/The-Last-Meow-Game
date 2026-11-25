using System;

namespace TLMG.Domain.Repositories
{
    public interface IVerificationCodeRepository
    {
        int? SavePendingRegistration(PendingRegistrationRecord pendingRegistration);
        PlayerExistenceLookupStatus FindPendingRegistrationByEmail(string email, out PendingRegistrationRecord record);
        void DeletePendingRegistration(int pendingId);
        bool SaveCode(VerificationCodeRecord verificationCodeRecord);
        VerificationCodeValidationResult ValidateCode(VerificationCodeRecord verificationCode, DateTime nowUtc);
    }
}
