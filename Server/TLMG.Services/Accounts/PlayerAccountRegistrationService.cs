using System.Security.Cryptography;
using log4net;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Entities;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;

namespace TLMG.Services.Accounts
{
    /// <summary>
    /// Lógica del registro de cuentas; orquesta validadores del dominio y persistencia.
    /// </summary>
    public class PlayerAccountRegistrationService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerAccountRegistrationService));

        private readonly IPlayerRepository _playerRepository;
        private readonly IPasswordHasher _passwordHasher;

        public PlayerAccountRegistrationService(IPlayerRepository playerRepository, IPasswordHasher passwordHasher)
        {
            _playerRepository = playerRepository;
            _passwordHasher = passwordHasher;
        }

        public OperationRegisterPlayerAccountResult RegisterPlayerAccount(PlayerAccountRegistration 
            playerAccountRegistration)
        {
            OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult = 
                new OperationRegisterPlayerAccountResult();
            string validationErrorKey = ValidateRegisterPlayerAccountRequest(playerAccountRegistration, 
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

        private static string ValidateRegisterPlayerAccountRequest(PlayerAccountRegistration playerAccountRegistration, 
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

        private OperationRegisterPlayerAccountResult CreateAccount(PlayerRegistration playerRegistration)
        {
            OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult = 
                new OperationRegisterPlayerAccountResult();

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

        private static string ResolveExistenceErrorKey(PlayerExistenceLookupStatus playerExistenceLookupStatus, 
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