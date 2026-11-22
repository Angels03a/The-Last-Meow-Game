using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;
using TLMG.Services.Utilities;

namespace TLMG.Services.Accounts
{
    public class PasswordRecoveryService
    {
        private readonly IPasswordRepository _passwordRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly PasswordResetDispatcher _resetDispatcher;

        public PasswordRecoveryService(
            IPasswordRepository passwordRepository,
            IPasswordHasher passwordHasher,
            PasswordResetDispatcher resetDispatcher)
        {
            _passwordRepository = passwordRepository;
            _passwordHasher = passwordHasher;
            _resetDispatcher = resetDispatcher;
        }

        /// <summary>Sends a code if the account exists, without revealing whether it does.</summary>
        /// <param name="email">Email typed by the player.</param>
        /// <param name="cultureCode">Active client culture code.</param>
        /// <returns>Success or the error key.</returns>
        public OperationResult RequestResetCode(string email, string cultureCode)
        {
            string normalizedEmail = AccountFieldValidator.NormalizeEmail(email);
            string errorKey = null;

            if (!AccountFieldValidator.IsEmailValid(normalizedEmail))
            {
                errorKey = ErrorKeys.Password.EmailFormat;
            }
            else if (!_resetDispatcher.SendResetCode(normalizedEmail, cultureCode))
            {
                errorKey = ErrorKeys.Password.DataModel;
            }

            OperationResult result = CreateResult(errorKey);

            if (result.IsSuccess)
            {
                result.ErrorKey = string.Empty;
            }

            return result;
        }

        /// <summary>Verifies the code; the database marks it as used so the reset step can rely on it.</summary>
        /// <param name="email">Email typed by the player.</param>
        /// <param name="verificationCode">Code typed by the player.</param>
        /// <returns>Success or the error key.</returns>
        public OperationResult VerifyResetCode(string email, string verificationCode)
        {
            string errorKey = ErrorKeys.Verification.IncorrectCode;
            bool isSuccess = false;

            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(verificationCode))
            {
                VerificationCodeValidationResult validationResult = _resetDispatcher.ValidateResetCode(
                    AccountFieldValidator.NormalizeEmail(email),
                    verificationCode.Trim());
                if (validationResult != null)
                {
                    errorKey = ResolveVerificationErrorKey(validationResult.Status);
                    isSuccess = validationResult.Status == VerificationCodeStatus.Valid;
                }
                else
                {
                    errorKey = ErrorKeys.Password.DataModel;
                }
            }

            return new OperationResult { IsSuccess = isSuccess, ErrorKey = errorKey };
        }

        /// <summary>Validates the new password, then lets the database check the verified code and apply it.</summary>
        /// <param name="resetPasswordRequest">Email, verified code and new password.</param>
        /// <returns>Success or the error key.</returns>
        public OperationResult ResetPassword(OperationResetPasswordRequest resetPasswordRequest)
        {
            string errorKey = ErrorKeys.Common.Unexpected;

            if (resetPasswordRequest != null)
            {
                errorKey = PasswordChangeValidator.ValidateNewPassword(resetPasswordRequest.NewPassword);

                if (errorKey == null)
                {
                    errorKey = ApplyReset(resetPasswordRequest);
                }
            }

            return CreateResult(errorKey);
        }

        private string ApplyReset(OperationResetPasswordRequest resetPasswordRequest)
        {
            string errorKey = ErrorKeys.Verification.ExpiredCode;

            if (!string.IsNullOrWhiteSpace(resetPasswordRequest.Email)
                && !string.IsNullOrWhiteSpace(resetPasswordRequest.VerificationCode))
            {
                string newHash = _passwordHasher.HashPassword(resetPasswordRequest.NewPassword);
                PasswordResetStatus resetStatus = _passwordRepository.ResetPasswordWithVerifiedCode(
                    AccountFieldValidator.NormalizeEmail(resetPasswordRequest.Email),
                    VerificationCodeStore.ComputeCodeHash(resetPasswordRequest.VerificationCode.Trim()),
                    newHash);
                errorKey = ResolveResetErrorKey(resetStatus);
            }

            return errorKey;
        }

        private static string ResolveVerificationErrorKey(VerificationCodeStatus status)
        {
            string errorKey;

            switch (status)
            {
                case VerificationCodeStatus.Valid:
                    errorKey = string.Empty;
                    break;
                case VerificationCodeStatus.Invalid:
                    errorKey = ErrorKeys.Verification.IncorrectCode;
                    break;
                case VerificationCodeStatus.TooManyAttempts:
                    errorKey = ErrorKeys.Verification.AttemptsExhausted;
                    break;
                case VerificationCodeStatus.Expired:
                case VerificationCodeStatus.NoActiveCode:
                    errorKey = ErrorKeys.Verification.ExpiredCode;
                    break;
                default:
                    errorKey = ErrorKeys.Password.DataModel;
                    break;
            }

            return errorKey;
        }

        private static string ResolveResetErrorKey(PasswordResetStatus resetStatus)
        {
            string errorKey;

            switch (resetStatus)
            {
                case PasswordResetStatus.Reset:
                    errorKey = null;
                    break;
                case PasswordResetStatus.NoVerifiedCode:
                case PasswordResetStatus.UserNotFound:
                    errorKey = ErrorKeys.Verification.ExpiredCode;
                    break;
                default:
                    errorKey = ErrorKeys.Password.DataModel;
                    break;
            }

            return errorKey;
        }

        private static OperationResult CreateResult(string errorKey)
        {
            OperationResult operationResult = new OperationResult
            {
                IsSuccess = errorKey == null,
                ErrorKey = errorKey
            };

            return operationResult;
        }
    }
}
