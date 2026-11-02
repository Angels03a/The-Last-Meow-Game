using System.Security.Cryptography;

using log4net;

using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Helpers;

namespace TLMG.Services.Accounts
{
    /// <summary>
    /// Lógica del registro de cuentas; no conoce WCF ni Entity Framework.
    /// </summary>
    public class PlayerAccountRegistrationService
    {
        private const string InvalidRequestErrorKey = "CreateAccount_IncompleteFieldsError";
        private const string DatabaseErrorKey = "CreateAccount_DataModelError";
        private const string EmailTakenErrorKey = "CreateAccount_EmailTakenError";
        private const string NicknameTakenErrorKey = "CreateAccount_NicknameTakenError";

        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerAccountRegistrationService));

        private readonly IPlayerRepository _playerRepository;
        private readonly IPasswordHasher _passwordHasher;

        /// <summary>Crea el servicio de registro con sus dependencias.</summary>
        /// <param name="playerRepository">Repositorio de cuentas.</param>
        /// <param name="passwordHasher">Calculador del hash de contraseñas.</param>
        public PlayerAccountRegistrationService(IPlayerRepository playerRepository, IPasswordHasher passwordHasher)
        {
            _playerRepository = playerRepository;
            _passwordHasher = passwordHasher;
        }

        /// <summary>Valida los datos y crea la cuenta.</summary>
        /// <param name="playerAccountRegistration">Datos recibidos del cliente.</param>
        /// <returns>Resultado con IsSuccess y, si falla, la clave de error.</returns>
        public OperationRegisterPlayerAccountResult RegisterPlayerAccount(
            PlayerAccountRegistration playerAccountRegistration)
        {
            OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult =
                new OperationRegisterPlayerAccountResult();

            string validationErrorKey = ValidateRegisterPlayerAccountRequest(playerAccountRegistration);

            if (validationErrorKey != null)
            {
                operationRegisterPlayerAccountResult.ErrorKey = validationErrorKey;
            }
            else
            {
                operationRegisterPlayerAccountResult = CreateAccount(playerAccountRegistration);
            }

            return operationRegisterPlayerAccountResult;
        }

        private static string ValidateRegisterPlayerAccountRequest(PlayerAccountRegistration playerAccountRegistration)
        {
            string errorKey;

            if (playerAccountRegistration == null)
            {
                errorKey = InvalidRequestErrorKey;
            }
            else
            {
                errorKey = PlayerAccountRegistrationValidationHelper.ValidateAccountPlayerRegistration(
                    playerAccountRegistration);
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
                errorKey = DatabaseErrorKey;
            }

            return errorKey;
        }

        private OperationRegisterPlayerAccountResult CreateAccount(PlayerAccountRegistration playerAccountRegistration)
        {
            OperationRegisterPlayerAccountResult operationRegisterPlayerAccountResult =
                new OperationRegisterPlayerAccountResult();

            string nickname = playerAccountRegistration.Nickname;
            string email = playerAccountRegistration.Email.ToLowerInvariant();
            string errorKey = FindDuplicateAccountErrorKey(email, nickname);

            if (errorKey == null)
            {
                errorKey = SavePlayer(nickname, email, playerAccountRegistration.Password);
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
                EmailTakenErrorKey);

            if (errorKey == null)
            {
                errorKey = ResolveExistenceErrorKey(
                    _playerRepository.FindNicknameExistenceStatus(nickname),
                    NicknameTakenErrorKey);
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
                _log.Error(
                    string.Format("Falló el hash de la contraseña de {0}.", nickname),
                    cryptographicException);
                errorKey = CommonErrorKeyHelper.UnexpectedErrorKey;
            }

            if (errorKey == null && !_playerRepository.CreatePlayer(nickname, email, passwordHash))
            {
                errorKey = DatabaseErrorKey;
            }

            return errorKey;
        }
    }
}