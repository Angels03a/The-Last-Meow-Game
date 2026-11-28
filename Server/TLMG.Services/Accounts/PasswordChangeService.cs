using log4net;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Authorization;
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
        private readonly ISessionAuthorizer _sessionAuthorizer;

        public PasswordChangeService(
            IPasswordRepository passwordRepository,
            IPasswordHasher passwordHasher,
            ISessionAuthorizer sessionAuthorizer)
        {
            _passwordRepository = passwordRepository;
            _passwordHasher = passwordHasher;
            _sessionAuthorizer = sessionAuthorizer;
        }

        /// <summary>
        /// Validates the password change request and enforces account permissions.
        /// </summary>
        /// <param name="changePasswordRequest">Session token, current and new password.</param>
        /// <returns>Success or the corresponding error key.</returns>
        public OperationResult ChangePassword(
            OperationChangePasswordRequest changePasswordRequest)
        {
            string errorKey = ErrorKeys.Common.Unexpected;

            if (changePasswordRequest != null)
            {
                string validationErrorKey = PasswordChangeValidator.ValidateNewPassword(
                    changePasswordRequest.NewPassword);

                if (validationErrorKey == string.Empty)
                {
                    errorKey = ChangeForSession(changePasswordRequest);
                }
                else
                {
                    errorKey = validationErrorKey;
                }
            }

            OperationResult changeResult = new OperationResult
            {
                IsSuccess = errorKey == string.Empty,
                ErrorKey = errorKey
            };

            return changeResult;
        }

        private string ChangeForSession(
            OperationChangePasswordRequest changePasswordRequest)
        {
            SessionAuthorization authorization = _sessionAuthorizer.Authorize(
                changePasswordRequest.SessionToken,
                SystemPermission.ChangePassword);

            string errorKey;

            if (authorization.Status == AuthorizationStatus.Granted)
            {
                PasswordHashLookup hashLookup =
                    _passwordRepository.FindPasswordHashByUserId(
                        authorization.UserId);

                if (hashLookup == null)
                {
                    _log.Warn("La consulta de credenciales devolvió un resultado nulo.");
                    errorKey = ErrorKeys.Password.DataModel;
                }
                else if (hashLookup.Status == CredentialsLookupStatus.Found
                    && !string.IsNullOrWhiteSpace(hashLookup.PasswordHash))
                {
                    errorKey = VerifyAndChange(
                        authorization.UserId,
                        hashLookup.PasswordHash,
                        changePasswordRequest);
                }
                else
                {
                    errorKey = ResolveLookupErrorKey(hashLookup.Status);

                    if (hashLookup.Status != CredentialsLookupStatus.NotFound
                        && hashLookup.Status != CredentialsLookupStatus.DataFailure)
                    {
                        _log.Warn("La consulta de credenciales devolvió un resultado inválido.");
                    }
                }
            }
            else
            {
                errorKey = authorization.ErrorKey;
            }

            return errorKey;
        }

        private string VerifyAndChange(
            int userId,
            string storedHash,
            OperationChangePasswordRequest changePasswordRequest)
        {
            string errorKey = ErrorKeys.Password.CurrentPasswordIncorrect;

            if (!string.IsNullOrEmpty(changePasswordRequest.CurrentPassword)
                && _passwordHasher.VerifyPassword(
                    changePasswordRequest.CurrentPassword,
                    storedHash))
            {
                if (_passwordHasher.VerifyPassword(
                    changePasswordRequest.NewPassword,
                    storedHash))
                {
                    errorKey = ErrorKeys.Password.SamePassword;
                }
                else
                {
                    string newHash = _passwordHasher.HashPassword(
                        changePasswordRequest.NewPassword);

                    PasswordChangeStatus changeStatus =
                        _passwordRepository.ChangePassword(
                            userId,
                            storedHash,
                            newHash);

                    errorKey = ResolveChangeErrorKey(changeStatus);

                    if (changeStatus == PasswordChangeStatus.Changed)
                    {
                        _log.InfoFormat(
                            "El jugador {0} cambió su contraseña.",
                            userId);
                    }
                }
            }
            else
            {
                _log.WarnFormat(
                    "Cambio de contraseña rechazado para el jugador {0}.",
                    userId);
            }

            return errorKey;
        }

        private static string ResolveLookupErrorKey(
            CredentialsLookupStatus lookupStatus)
        {
            string errorKey = ErrorKeys.Password.DataModel;

            if (lookupStatus == CredentialsLookupStatus.NotFound)
            {
                errorKey = ErrorKeys.Common.InvalidSession;
            }

            return errorKey;
        }

        private static string ResolveChangeErrorKey(
            PasswordChangeStatus changeStatus)
        {
            string errorKey;

            switch (changeStatus)
            {
                case PasswordChangeStatus.Changed:
                    errorKey = string.Empty;
                    break;

                case PasswordChangeStatus.StaleCredentials:
                    errorKey = ErrorKeys.Password.CurrentPasswordIncorrect;
                    break;

                case PasswordChangeStatus.UserNotFound:
                    errorKey = ErrorKeys.Common.InvalidSession;
                    break;

                default:
                    if (changeStatus != PasswordChangeStatus.DataFailure)
                    {
                        _log.Warn(
                            "La escritura de credenciales devolvió un estado desconocido.");
                    }

                    errorKey = ErrorKeys.Password.DataModel;
                    break;
            }

            return errorKey;
        }
    }
}
