using System;

using log4net;

using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Helpers;

namespace TLMG.Services.Accounts
{
    /// <summary>
    /// Lógica del inicio de sesión; no conoce WCF.
    /// </summary>
    public class PlayerLogInService
    {
        private const string ActiveUserState = "USER_ACTIVE";
        private const string IncompleteFieldsErrorKey = "Login_IncompleteFieldsError";
        private const string EmailFormatErrorKey = "Login_EmailFormatError";
        private const string InvalidCredentialsErrorKey = "Login_InvalidCredentialsError";
        private const string DatabaseErrorKey = "Login_DataModelError";

        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerLogInService));

        private readonly IPlayerRepository _playerRepository;
        private readonly IPasswordHasher _passwordHasher;

        /// <summary>Crea el servicio de inicio de sesión con sus dependencias.</summary>
        /// <param name="playerRepository">Repositorio de cuentas.</param>
        /// <param name="passwordHasher">Verificador de contraseñas.</param>
        public PlayerLogInService(IPlayerRepository playerRepository, IPasswordHasher passwordHasher)
        {
            _playerRepository = playerRepository;
            _passwordHasher = passwordHasher;
        }

        /// <summary>Valida las credenciales y registra el acceso del jugador.</summary>
        /// <param name="operationLogInRequest">Credenciales recibidas del cliente.</param>
        /// <returns>Resultado con IsSuccess y, si falla, la clave de error.</returns>
        public OperationLogInResult LogIn(OperationLogInRequest operationLogInRequest)
        {
            string errorKey = ValidateLogInRequest(operationLogInRequest);

            if (errorKey == null)
            {
                errorKey = AuthenticatePlayer(operationLogInRequest);
            }

            OperationLogInResult operationLogInResult = new OperationLogInResult
            {
                IsSuccess = errorKey == null,
                ErrorKey = errorKey
            };

            return operationLogInResult;
        }

        private static string ValidateLogInRequest(OperationLogInRequest operationLogInRequest)
        {
            string errorKey = null;

            if (operationLogInRequest == null
                || string.IsNullOrWhiteSpace(operationLogInRequest.Email)
                || string.IsNullOrWhiteSpace(operationLogInRequest.Password))
            {
                errorKey = IncompleteFieldsErrorKey;
            }
            else if (PlayerAccountRegistrationValidationHelper.ValidateEmail(operationLogInRequest.Email) != null)
            {
                errorKey = EmailFormatErrorKey;
            }

            return errorKey;
        }

        private string AuthenticatePlayer(OperationLogInRequest operationLogInRequest)
        {
            string errorKey = InvalidCredentialsErrorKey;
            string email = operationLogInRequest.Email.ToLowerInvariant();
            PlayerCredentials playerCredentials = _playerRepository.FindCredentialsByEmail(email);

            if (playerCredentials.Status == CredentialsLookupStatus.DataFailure)
            {
                errorKey = DatabaseErrorKey;
            }
            else if (playerCredentials.Status == CredentialsLookupStatus.Found)
            {
                errorKey = VerifyKnownPlayer(playerCredentials, operationLogInRequest.Password);
            }

            return errorKey;
        }

        private string VerifyKnownPlayer(PlayerCredentials playerCredentials, string password)
        {
            string errorKey = InvalidCredentialsErrorKey;
            bool isPasswordValid = _passwordHasher.VerifyPassword(password, playerCredentials.PasswordHash);
            bool isAccountActive = playerCredentials.State == ActiveUserState;

            if (isPasswordValid && isAccountActive)
            {
                errorKey = RegisterLogInTime(playerCredentials.UserId);
            }
            else
            {
                _log.WarnFormat("Inicio de sesión rechazado para el jugador {0}.", playerCredentials.UserId);
            }

            return errorKey;
        }

        private string RegisterLogInTime(int userId)
        {
            string errorKey = DatabaseErrorKey;

            // El acceso se guarda en UTC: es una marca del servidor y no depende de la zona horaria de ninguna PC.
            bool wasUpdated = _playerRepository.UpdateLastLogInTime(userId, DateTime.UtcNow);

            if (wasUpdated)
            {
                errorKey = null;
                _log.InfoFormat("El jugador {0} inició sesión.", userId);
            }

            return errorKey;
        }
    }
}