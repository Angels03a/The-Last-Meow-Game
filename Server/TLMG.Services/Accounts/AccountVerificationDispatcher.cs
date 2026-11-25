using log4net;
using TLMG.Domain.Repositories;
using TLMG.Services.Utilities;

namespace TLMG.Services.Accounts
{
    public class AccountVerificationDispatcher
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(AccountVerificationDispatcher));
        private readonly VerificationCodeStore _verificationCodeStore;
        private readonly EmailUtility _emailUtility;
        private readonly VerificationCodeGenerator _codeGeneratorUtility;

        public AccountVerificationDispatcher(
            VerificationCodeStore verificationCodeStore,
            EmailUtility emailUtility)
        {
            _verificationCodeStore = verificationCodeStore;
            _emailUtility = emailUtility;
            _codeGeneratorUtility = new VerificationCodeGenerator();
        }

        public int? SavePendingRegistration(string nickname, string normalizedEmail, string passwordHash)
        {
            return _verificationCodeStore.SavePendingRegistration(nickname, normalizedEmail, passwordHash);
        }

        public PlayerExistenceLookupStatus FindValidPendingRegistration(string normalizedEmail, out PendingRegistrationRecord record)
        {
            return _verificationCodeStore.FindValidPendingRegistration(normalizedEmail, out record);
        }

        public void DeletePendingRegistration(int pendingId)
        {
            _verificationCodeStore.DeletePendingRegistration(pendingId);
        }

        public bool SendRegistrationCode(int pendingId, string normalizedEmail, string cultureCode)
        {
            bool wasDispatched = false;
            string plainCode = _codeGeneratorUtility.GenerateSixDigitCode();

            bool wasCodeSaved = _verificationCodeStore.SaveRegistrationCode(pendingId, plainCode);

            if (wasCodeSaved)
            {
                string subject = EmailTemplates.GetAccountVerificationSubject(cultureCode);
                string body = EmailTemplates.GetAccountVerificationBody(plainCode, cultureCode);
                _log.InfoFormat("Enviando código de registro al correo del pendiente {0}.", pendingId);
                wasDispatched = _emailUtility.SendEmail(normalizedEmail, subject, body);
                _log.InfoFormat("Resultado del envío del correo del pendiente {0}: {1}.", pendingId, wasDispatched);
            }

            return wasDispatched;
        }

        public VerificationCodeValidationResult ValidateRegistrationCode(int pendingId, string plainCode)
        {
            return _verificationCodeStore.ValidateRegistrationCode(pendingId, plainCode);
        }
    }
}
