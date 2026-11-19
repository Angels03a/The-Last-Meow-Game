using log4net;

using TLMG.Domain.Repositories;
using TLMG.Services.Utilities;

namespace TLMG.Services.Accounts
{
    /// <summary>
    /// Sends and verifies recovery codes by email. It hides whether the account exists so the recovery screen
    /// cannot be used to discover registered emails.
    /// </summary>
    public class PasswordResetDispatcher
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordResetDispatcher));

        private readonly VerificationCodeStore _verificationCodeStore;
        private readonly EmailUtility _emailUtility;
        private readonly IPlayerRepository _playerRepository;
        private readonly CodeGeneratorUtility _codeGeneratorUtility;

        public PasswordResetDispatcher(
            VerificationCodeStore verificationCodeStore,
            EmailUtility emailUtility,
            IPlayerRepository playerRepository)
        {
            _verificationCodeStore = verificationCodeStore;
            _emailUtility = emailUtility;
            _playerRepository = playerRepository;
            _codeGeneratorUtility = new CodeGeneratorUtility();
        }

        /// <summary>Sends a code when the account exists; true also when it does not, to hide that fact.</summary>
        /// <param name="normalizedEmail">Lowercase trimmed email.</param>
        /// <param name="cultureCode">Active client culture code.</param>
        /// <returns>False only when storing or sending failed.</returns>
        // TODO: Agregar el cooldown por cuenta y el Token Bucket del 2FA. (ADR-05).
        public bool SendResetCode(string normalizedEmail, string cultureCode)
        {
            bool wasHandled = true;
            PlayerCredentials credentials = _playerRepository.FindCredentialsByEmail(normalizedEmail);

            if (credentials.Status == CredentialsLookupStatus.DataFailure)
            {
                wasHandled = false;
            }
            else if (credentials.Status == CredentialsLookupStatus.Found)
            {
                wasHandled = SendCodeToUser(credentials.UserId, normalizedEmail, cultureCode);
            }

            return wasHandled;
        }

        /// <summary>Verifies the code of the account; an unknown email looks like a missing code.</summary>
        /// <param name="normalizedEmail">Lowercase trimmed email.</param>
        /// <param name="plainCode">Code typed by the player.</param>
        /// <returns>The validation status and failed attempts.</returns>
        public VerificationCodeValidationResult ValidateResetCode(string normalizedEmail, string plainCode)
        {
            VerificationCodeValidationResult validationResult = new VerificationCodeValidationResult
            {
                Status = VerificationCodeStatus.NoActiveCode
            };
            PlayerCredentials credentials = _playerRepository.FindCredentialsByEmail(normalizedEmail);

            if (credentials.Status == CredentialsLookupStatus.Found)
            {
                validationResult = _verificationCodeStore.ValidatePasswordResetCode(credentials.UserId, plainCode);
            }
            else if (credentials.Status == CredentialsLookupStatus.DataFailure)
            {
                validationResult.Status = VerificationCodeStatus.UnexpectedError;
            }

            return validationResult;
        }

        private bool SendCodeToUser(int userId, string normalizedEmail, string cultureCode)
        {
            bool wasDispatched = false;
            string plainCode = _codeGeneratorUtility.GenerateSixDigitCode();

            if (_verificationCodeStore.SavePasswordResetCode(userId, plainCode))
            {
                string subject = EmailTemplates.GetPasswordResetSubject(cultureCode);
                string body = EmailTemplates.GetPasswordResetBody(plainCode, cultureCode);
                wasDispatched = _emailUtility.SendEmail(normalizedEmail, subject, body);
                _log.InfoFormat("Código de recuperación enviado al jugador {0}: {1}.", userId, wasDispatched);
            }

            return wasDispatched;
        }
    }
}