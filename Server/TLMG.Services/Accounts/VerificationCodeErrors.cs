using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

namespace TLMG.Services.Accounts
{
    internal static class VerificationCodeErrors
    {
        internal static string Resolve(VerificationCodeStatus status, string dataFailureKey)
        {
            string errorKey = dataFailureKey;
            if (status == VerificationCodeStatus.Valid)
            {
                errorKey = string.Empty;
            }
            else if (status == VerificationCodeStatus.Invalid)
            {
                errorKey = ErrorKeys.Verification.IncorrectCode;
            }
            else if (status == VerificationCodeStatus.TooManyAttempts)
            {
                errorKey = ErrorKeys.Verification.AttemptsExhausted;
            }
            else if (status == VerificationCodeStatus.Expired || status == VerificationCodeStatus.NoActiveCode)
            {
                errorKey = ErrorKeys.Verification.ExpiredCode;
            }

            return errorKey;
        }
    }
}
