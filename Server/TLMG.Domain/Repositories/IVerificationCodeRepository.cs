using System;

namespace TLMG.Domain.Repositories
{
    public interface IVerificationCodeRepository
    {
        int? SavePendingRegistration(PendingRegistrationRecord pendingRegistration);
        PendingRegistrationRecord FindPendingRegistrationByEmail(string email);
        void DeletePendingRegistration(int pendingId);
        bool SaveCode(VerificationCodeRecord verificationCodeRecord);
        VerificationCodeValidationResult ValidateCode(VerificationCodeRecord verificationCode, DateTime nowUtc);
    }
}