using System;

using log4net;

using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

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

        // Id que viaja en el resultado cuando el inicio de sesión falla.
        private const int NoUserId = 0;

        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerLogInService));

        private readonly IPlayerRepository _playerRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISessionRegistry _sessionRegistry;

        /// <summary>Crea el servicio de inicio de sesión con sus dependencias.</summary>
        /// <param name="playerRepository">Repositorio de cuentas.</param>
        /// <param name="passwordHasher">Verificador de contraseñas.</param>
        /// <param name="sessionRegistry">Registro de las sesiones iniciadas.</param>
        public PlayerLogInService(
            IPlayerRepository playerRepository,
            IPasswordHasher passwordHasher,
            ISessionRegistry sessionRegistry)
        {
            _playerRepository = playerRepository;
            _passwordHasher = passwordHasher;
            _sessionRegistry = sessionRegistry;
        }

        /// <summary>Valida las credenciales, registra el acceso del jugador y abre su sesión.</summary>
        /// <param name="operationLogInRequest">Credenciales recibidas del cliente.</param>
        /// <returns>
        /// El resultado con IsSuccess y, según el caso, la clave de error o el token de sesión con el id y el
        /// nickname del jugador; y los avisos para la sala y los amigos de la sesión anterior de la misma cuenta, si
        /// la había.
        /// </returns>
        public SessionOutcome<OperationLogInResult> LogIn(OperationLogInRequest operationLogInRequest)
        {
            string sessionToken = null;
            int userId = NoUserId;
            string nickname = null;
            SessionEnd replacedSession = null;
            string errorKey = ValidateLogInRequest(operationLogInRequest);

            if (errorKey == null)
            {
                PlayerCredentials playerCredentials;
                errorKey = AuthenticatePlayer(operationLogInRequest, out playerCredentials);

                if (errorKey == null)
                {
                    sessionToken = _sessionRegistry.StartSession(
                        playerCredentials.UserId,
                        playerCredentials.Nickname,
                        out replacedSession);
                    userId = playerCredentials.UserId;
                    nickname = playerCredentials.Nickname;
                }
            }

            OperationLogInResult operationLogInResult = new OperationLogInResult
            {
                IsSuccess = errorKey == null,
                ErrorKey = errorKey,
                SessionToken = sessionToken,
                UserId = userId,
                Nickname = nickname
            };

            SessionOutcome<OperationLogInResult> sessionOutcome = new SessionOutcome<OperationLogInResult>(
                operationLogInResult,
                SessionNotificationHelper.CreateSessionEndNotifications(replacedSession));

            return sessionOutcome;
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

        private string AuthenticatePlayer(
            OperationLogInRequest operationLogInRequest,
            out PlayerCredentials playerCredentials)
        {
            string errorKey = InvalidCredentialsErrorKey;
            string email = operationLogInRequest.Email.ToLowerInvariant();
            playerCredentials = _playerRepository.FindCredentialsByEmail(email);

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