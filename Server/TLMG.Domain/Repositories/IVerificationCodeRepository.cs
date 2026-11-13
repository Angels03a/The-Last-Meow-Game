using System;

namespace TLMG.Domain.Repositories
{
    public interface IVerificationCodeRepository
    {
        VerificationCodeRecord FindLatestCodeByEmail(string email, string type);
        void SaveCode(VerificationCodeRecord record);
        bool ConsumeCodeIfValid(string email, string codeHash, DateTime nowUtc);
        void DeleteExpiredCodes(DateTime nowUtc);
    }
}