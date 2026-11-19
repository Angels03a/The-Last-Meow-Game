using log4net;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;
using TLMG.Services.Sessions;

namespace TLMG.Services.Accounts
{
    public class PasswordChangeService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordChangeService));

        private readonly IPasswordRepository _passwordRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISessionIdentityProvider _sessionIdentityProvider;

        public PasswordChangeService(
            IPasswordRepository passwordRepository,
            IPasswordHasher passwordHasher,
            ISessionIdentityProvider sessionIdentityProvider)
        {
            _passwordRepository = passwordRepository;
            _passwordHasher = passwordHasher;
            _sessionIdentityProvider = sessionIdentityProvider;
        }

        /// <summary>Validates the format first and the session second, then verifies the current password.</summary>
        /// <param name="changePasswordRequest">Session token, current and new password.</param>
        /// <returns>Success or the error key.</returns>
        public OperationResult ChangePassword(OperationChangePasswordRequest changePasswordRequest)
        {
            string errorKey = ErrorKeys.Common.Unexpected;

            if (changePasswordRequest != null)
            {
                errorKey = PasswordChangeValidator.ValidateNewPassword(changePasswordRequest.NewPassword);

                if (errorKey == null)
                {
                    errorKey = ChangeForSession(changePasswordRequest);
                }
            }

            OperationResult changeResult = new OperationResult
            {
                IsSuccess = errorKey == null,
                ErrorKey = errorKey
            };

            return changeResult;
        }

        private string ChangeForSession(OperationChangePasswordRequest changePasswordRequest)
        {
            string errorKey = ErrorKeys.Common.InvalidSession;

            if (_sessionIdentityProvider.TryGetUserId(changePasswordRequest.SessionToken, out int userId))
            {
                PasswordHashLookup hashLookup = _passwordRepository.FindPasswordHashByUserId(userId);
                errorKey = ResolveLookupErrorKey(hashLookup.Status);

                if (errorKey == null)
                {
                    errorKey = VerifyAndChange(userId, hashLookup.PasswordHash, changePasswordRequest);
                }
            }

            return errorKey;
        }

        private string VerifyAndChange(
            int userId,
            string storedHash,
            OperationChangePasswordRequest changePasswordRequest)
        {
            string errorKey = ErrorKeys.Password.CurrentPasswordIncorrect;

            if (_passwordHasher.VerifyPassword(changePasswordRequest.CurrentPassword, storedHash))
            {
                if (_passwordHasher.VerifyPassword(changePasswordRequest.NewPassword, storedHash))
                {
                    errorKey = ErrorKeys.Password.SamePassword;
                }
                else
                {
                    string newHash = _passwordHasher.HashPassword(changePasswordRequest.NewPassword);
                    PasswordChangeStatus changeStatus = _passwordRepository.ChangePassword(
                        userId,
                        storedHash,
                        newHash);
                    errorKey = ResolveChangeErrorKey(changeStatus);

                    if (errorKey == null)
                    {
                        _log.InfoFormat("El jugador {0} cambió su contraseña.", userId);
                    }
                }
            }
            else
            {
                _log.WarnFormat("Cambio de contraseña rechazado para el jugador {0}.", userId);
            }

            return errorKey;
        }

        private static string ResolveLookupErrorKey(CredentialsLookupStatus lookupStatus)
        {
            string errorKey = null;

            if (lookupStatus == CredentialsLookupStatus.NotFound)
            {
                errorKey = ErrorKeys.Common.InvalidSession;
            }
            else if (lookupStatus == CredentialsLookupStatus.DataFailure)
            {
                errorKey = ErrorKeys.Password.DataModel;
            }

            return errorKey;
        }

        private static string ResolveChangeErrorKey(PasswordChangeStatus changeStatus)
        {
            string errorKey;

            switch (changeStatus)
            {
                case PasswordChangeStatus.Changed:
                    errorKey = null;
                    break;
                case PasswordChangeStatus.StaleCredentials:
                    errorKey = ErrorKeys.Password.CurrentPasswordIncorrect;
                    break;
                case PasswordChangeStatus.UserNotFound:
                    errorKey = ErrorKeys.Common.InvalidSession;
                    break;
                default:
                    errorKey = ErrorKeys.Password.DataModel;
                    break;
            }

            return errorKey;
        }
    }
}