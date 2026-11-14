using System.Security.Cryptography;
using log4net;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Entities;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;

namespace TLMG.Services.Accounts
{
    public class PlayerAccountRegistrationService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerAccountRegistrationService));

        private readonly IPlayerRepository _playerRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly AccountVerificationDispatcher _verificationDispatcher;

        public PlayerAccountRegistrationService(
            IPlayerRepository playerRepository,
            IPasswordHasher passwordHasher)
            : this(playerRepository, passwordHasher, null)
        {
        }

        public PlayerAccountRegistrationService(
            IPlayerRepository playerRepository,
            IPasswordHasher passwordHasher,
            AccountVerificationDispatcher verificationDispatcher)
        {
            _playerRepository = playerRepository;
            _passwordHasher = passwordHasher;
            _verificationDispatcher = verificationDispatcher;
        }

        public OperationResult RegisterPlayerAccount(PlayerAccountRegistration playerAccountRegistration)
        {
            OperationResult operationRegisterPlayerAccountResult = new OperationResult();
            string validationErrorKey = ValidateRegisterPlayerAccountRequest(
                playerAccountRegistration,
                out PlayerRegistration playerRegistration);

            if (validationErrorKey != null)
            {
                operationRegisterPlayerAccountResult.ErrorKey = validationErrorKey;
            }
            else
            {
                operationRegisterPlayerAccountResult = CreateAccount(playerRegistration);
            }

            return operationRegisterPlayerAccountResult;
        }

        public OperationResult RequestVerificationCode(
            PlayerAccountRegistration playerAccountRegistration,
            string cultureCode)
        {
            OperationResult operationResult = new OperationResult();
            string errorKey = ValidateRegisterPlayerAccountRequest(
                playerAccountRegistration,
                out PlayerRegistration playerRegistration);

            if (errorKey == null)
            {
                string normalizedEmail = playerRegistration.Email.ToLowerInvariant();
                errorKey = FindDuplicateAccountErrorKey(normalizedEmail, playerRegistration.Nickname);

                if (errorKey == null)
                {
                    errorKey = CreatePendingAndSendCode(playerRegistration, normalizedEmail, cultureCode);
                }
            }

            operationResult.IsSuccess = errorKey == null;
            operationResult.ErrorKey = errorKey;

            return operationResult;
        }

        public OperationResult CompleteRegistration(
            PlayerAccountRegistration playerAccountRegistration,
            string verificationCode)
        {
            OperationResult operationResult = new OperationResult();
            string errorKey = null;

            if (playerAccountRegistration == null || string.IsNullOrWhiteSpace(playerAccountRegistration.Email))
            {
                errorKey = ErrorKeys.CreateAccount.IncompleteFields;
            }
            else if (string.IsNullOrWhiteSpace(verificationCode))
            {
                errorKey = ErrorKeys.Verification.IncorrectCode;
            }
            else if (_verificationDispatcher == null)
            {
                _log.Error("El despachador de códigos de verificación no está configurado.");
                errorKey = ErrorKeys.Common.Unexpected;
            }

            if (errorKey == null)
            {
                string normalizedEmail = playerAccountRegistration.Email.Trim().ToLowerInvariant();
                PendingRegistrationRecord pendingRecord =
                    _verificationDispatcher.FindValidPendingRegistration(normalizedEmail);

                if (pendingRecord == null)
                {
                    errorKey = ErrorKeys.Verification.ExpiredCode;
                }
                else
                {
                    errorKey = VerifyCodeAndFinalizePlayer(pendingRecord, verificationCode.Trim());
                }
            }

            operationResult.IsSuccess = errorKey == null;
            operationResult.ErrorKey = errorKey;

            return operationResult;
        }

        public OperationResult ResendVerificationCode(string email, string cultureCode)
        {
            OperationResult operationResult = new OperationResult();
            string errorKey = null;

            if (string.IsNullOrWhiteSpace(email))
            {
                errorKey = ErrorKeys.CreateAccount.EmailFormat;
            }
            else if (_verificationDispatcher == null)
            {
                _log.Error("El despachador de códigos de verificación no está configurado.");
                errorKey = ErrorKeys.Common.Unexpected;
            }
            else
            {
                string normalizedEmail = email.Trim().ToLowerInvariant();
                PendingRegistrationRecord pendingRecord =
                    _verificationDispatcher.FindValidPendingRegistration(normalizedEmail);

                if (pendingRecord == null)
                {
                    errorKey = ErrorKeys.Verification.ExpiredCode;
                }
                else if (!_verificationDispatcher.SendRegistrationCode(
                    pendingRecord.PendingId,
                    normalizedEmail,
                    cultureCode))
                {
                    errorKey = ErrorKeys.CreateAccount.DataModel;
                }
            }

            operationResult.IsSuccess = errorKey == null;
            operationResult.ErrorKey = errorKey;

            return operationResult;
        }

        private string CreatePendingAndSendCode(
            PlayerRegistration playerRegistration,
            string normalizedEmail,
            string cultureCode)
        {
            string errorKey = null;

            if (_verificationDispatcher == null)
            {
                _log.Error("El despachador de códigos de verificación no está configurado.");
                errorKey = ErrorKeys.Common.Unexpected;
            }
            else
            {
                string passwordHash = null;

                try
                {
                    passwordHash = _passwordHasher.HashPassword(playerRegistration.Password);
                }
                catch (CryptographicException cryptographicException)
                {
                    _log.Error("Falló el hash de la contraseña al crear el registro pendiente.", cryptographicException);
                    errorKey = ErrorKeys.Common.Unexpected;
                }

                if (errorKey == null)
                {
                    int? pendingId = _verificationDispatcher.SavePendingRegistration(
                        playerRegistration.Nickname,
                        normalizedEmail,
                        passwordHash);

                    if (!pendingId.HasValue)
                    {
                        errorKey = ErrorKeys.CreateAccount.DataModel;
                    }
                    else if (!_verificationDispatcher.SendRegistrationCode(
                        pendingId.Value,
                        normalizedEmail,
                        cultureCode))
                    {
                        errorKey = ErrorKeys.CreateAccount.DataModel;
                    }
                }
            }

            return errorKey;
        }

        private string VerifyCodeAndFinalizePlayer(
            PendingRegistrationRecord pendingRecord,
            string plainCode)
        {
            string errorKey = null;
            VerificationCodeValidationResult validationResult =
                _verificationDispatcher.ValidateRegistrationCode(pendingRecord.PendingId, plainCode);

            if (validationResult.Status == "Valid")
            {
                errorKey = FindDuplicateAccountErrorKey(pendingRecord.Email, pendingRecord.Nickname);

                if (errorKey == null)
                {
                    bool wasCreated = _playerRepository.CreatePlayer(
                        pendingRecord.Nickname,
                        pendingRecord.Email,
                        pendingRecord.PasswordHash);

                    if (wasCreated)
                    {
                        _verificationDispatcher.DeletePendingRegistration(pendingRecord.PendingId);
                    }
                    else
                    {
                        errorKey = ErrorKeys.CreateAccount.DataModel;
                    }
                }
            }
            else if (validationResult.Status == "Invalid")
            {
                errorKey = ErrorKeys.Verification.IncorrectCode;
            }
            else if (validationResult.Status == "TooManyAttempts")
            {
                errorKey = ErrorKeys.Verification.AttemptsExhausted;
            }
            else if (validationResult.Status == "Expired" || validationResult.Status == "NoActiveCode")
            {
                errorKey = ErrorKeys.Verification.ExpiredCode;
            }
            else
            {
                _log.ErrorFormat("Estado de validación no reconocido: {0}.", validationResult.Status);
                errorKey = ErrorKeys.CreateAccount.DataModel;
            }

            return errorKey;
        }

        private static string ValidateRegisterPlayerAccountRequest(
            PlayerAccountRegistration playerAccountRegistration,
            out PlayerRegistration playerRegistration)
        {
            string errorKey = null;
            playerRegistration = null;

            if (playerAccountRegistration == null)
            {
                errorKey = ErrorKeys.CreateAccount.IncompleteFields;
            }
            else
            {
                playerRegistration = new PlayerRegistration
                {
                    Nickname = playerAccountRegistration.Nickname,
                    Email = playerAccountRegistration.Email,
                    Password = playerAccountRegistration.Password,
                    HasAcceptedTerms = playerAccountRegistration.HasAcceptedTerms
                };

                errorKey = PlayerAccountRegistrationValidator.ValidateAccountPlayerRegistration(playerRegistration);
            }

            return errorKey;
        }

        private OperationResult CreateAccount(PlayerRegistration playerRegistration)
        {
            OperationResult operationRegisterPlayerAccountResult = new OperationResult();

            string nickname = playerRegistration.Nickname;
            string email = playerRegistration.Email.ToLowerInvariant();
            string errorKey = FindDuplicateAccountErrorKey(email, nickname);

            if (errorKey == null)
            {
                errorKey = SavePlayer(nickname, email, playerRegistration.Password);
            }

            if (errorKey == null)
            {
                operationRegisterPlayerAccountResult.IsSuccess = true;
            }
            else
            {
                operationRegisterPlayerAccountResult.ErrorKey = errorKey;
            }

            return operationRegisterPlayerAccountResult;
        }

        private string FindDuplicateAccountErrorKey(string email, string nickname)
        {
            string errorKey = ResolveExistenceErrorKey(
                _playerRepository.FindEmailExistenceStatus(email),
                ErrorKeys.CreateAccount.EmailTaken);

            if (errorKey == null)
            {
                errorKey = ResolveExistenceErrorKey(
                    _playerRepository.FindNicknameExistenceStatus(nickname),
                    ErrorKeys.CreateAccount.NicknameTaken);
            }

            return errorKey;
        }

        private static string ResolveExistenceErrorKey(
            PlayerExistenceLookupStatus playerExistenceLookupStatus,
            string takenErrorKey)
        {
            string errorKey = null;

            if (playerExistenceLookupStatus == PlayerExistenceLookupStatus.Exists)
            {
                errorKey = takenErrorKey;
            }
            else if (playerExistenceLookupStatus == PlayerExistenceLookupStatus.DataFailure)
            {
                errorKey = ErrorKeys.CreateAccount.DataModel;
            }

            return errorKey;
        }

        private string SavePlayer(string nickname, string email, string password)
        {
            string errorKey = null;
            string passwordHash = null;

            try
            {
                passwordHash = _passwordHasher.HashPassword(password);
            }
            catch (CryptographicException cryptographicException)
            {
                _log.Error(string.Format("Falló el hash de la contraseña de {0}.", nickname), cryptographicException);
                errorKey = ErrorKeys.Common.Unexpected;
            }

            if (errorKey == null && string.IsNullOrEmpty(passwordHash))
            {
                errorKey = ErrorKeys.CreateAccount.IncompleteFields;
            }

            if (errorKey == null && !_playerRepository.CreatePlayer(nickname, email, passwordHash))
            {
                errorKey = ErrorKeys.CreateAccount.DataModel;
            }

            return errorKey;
        }
    }
}