using System;
using log4net;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Entities;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;
using TLMG.Services.Helpers;
using TLMG.Services.Moderation;
using TLMG.Services.Sessions;

namespace TLMG.Services.Accounts
{
    public class PlayerLogInService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerLogInService));

        private readonly IPlayerRepository _playerRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISessionRegistry _sessionRegistry;
        private readonly ReportService _reportService;

        public PlayerLogInService(
            IPlayerRepository playerRepository,
            IPasswordHasher passwordHasher,
            ISessionRegistry sessionRegistry, ReportService reportService)
        {
            _playerRepository = playerRepository;
            _passwordHasher = passwordHasher;
            _sessionRegistry = sessionRegistry;
            _reportService = reportService;
        }

        public ResultWithNotification<OperationLogInResult> LogIn(OperationLogInRequest operationLogInRequest)
        {
            OperationLogInResult operationLogInResult = new OperationLogInResult
            {
                ErrorKey = ErrorKeys.Login.IncompleteFields,
                UserId = ValidationRules.NoUserId,
                SessionToken = string.Empty,
                Nickname = string.Empty
            };
            SessionEnd replacedSession = null;
            PlayerLogin playerLogin = new PlayerLogin();

            if (operationLogInRequest != null)
            {
                playerLogin.Email = AccountFieldValidator.NormalizeEmail(operationLogInRequest.Email);
                playerLogin.Password = operationLogInRequest.Password;
            }

            string validationErrorKey;
            if (PlayerLogInValidator.TryValidateLogIn(playerLogin, out validationErrorKey))
            {
                lock (_reportService.AccountStateSynchronizationRoot)
                {
                    PlayerCredentials credentials = _playerRepository.FindCredentialsByEmail(playerLogin.Email);
                    string authenticationErrorKey;

                    if (TryAuthenticatePlayer(credentials, playerLogin.Password, out authenticationErrorKey))
                    {
                        string sessionToken = _sessionRegistry.StartRegisteredSession(
                            credentials.UserId, credentials.Nickname, out replacedSession);

                        if (!string.IsNullOrWhiteSpace(sessionToken))
                        {
                            operationLogInResult.IsSuccess = true;
                            operationLogInResult.ErrorKey = string.Empty;
                            operationLogInResult.SessionToken = sessionToken;
                            operationLogInResult.UserId = credentials.UserId;
                            operationLogInResult.Nickname = credentials.Nickname;
                            _log.InfoFormat("El jugador {0} inició sesión.", credentials.UserId);
                        }
                        else
                        {
                            operationLogInResult.ErrorKey = ErrorKeys.Common.Unexpected;
                            _log.Error("No se obtuvo un token válido al iniciar la sesión.");
                        }
                    }
                    else
                    {
                        operationLogInResult.ErrorKey = authenticationErrorKey;
                    }
                }
            }
            else
            {
                operationLogInResult.ErrorKey = validationErrorKey;
            }

            ResultWithNotification<OperationLogInResult> loginResult =
                new ResultWithNotification<OperationLogInResult>(operationLogInResult,
                    SessionNotificationHelper.CreateReplacedSessionNotifications(replacedSession));

            return loginResult;
        }

        private bool TryAuthenticatePlayer(PlayerCredentials credentials, string password, out string errorKey)
        {
            bool isAuthenticated = false;
            errorKey = ErrorKeys.Login.DataModel;

            if (credentials == null)
            {
                _log.Warn("La consulta de credenciales no devolvió resultado.");
            }
            else if (credentials.Status == CredentialsLookupStatus.NotFound)
            {
                errorKey = ErrorKeys.Login.InvalidCredentials;
            }
            else if (credentials.Status == CredentialsLookupStatus.Found)
            {
                if (credentials.UserId > ValidationRules.NoUserId
                    && !string.IsNullOrWhiteSpace(credentials.Nickname)
                    && !string.IsNullOrWhiteSpace(credentials.PasswordHash))
                {
                    isAuthenticated = TryVerifyKnownPlayer(credentials, password, out errorKey);
                }
                else
                {
                    _log.Warn("La consulta de credenciales devolvió datos incompletos.");
                }
            }
            else if (credentials.Status != CredentialsLookupStatus.DataFailure)
            {
                _log.Warn("La consulta de credenciales devolvió un estado desconocido.");
            }

            return isAuthenticated;
        }

        private bool TryVerifyKnownPlayer(PlayerCredentials credentials, string password, out string errorKey)
        {
            bool isAuthenticated = false;
            errorKey = ErrorKeys.Login.InvalidCredentials;
            bool isPasswordValid = _passwordHasher.VerifyPassword(password, credentials.PasswordHash);
            if (isPasswordValid)
            {
                SuspensionLiftStatus status = _reportService.CheckAccountState(credentials.UserId);

                if (status == SuspensionLiftStatus.NotSuspended || status == SuspensionLiftStatus.Lifted)
                {
                    bool wasLogInTimeUpdated = _playerRepository.UpdateLastLogInTime(credentials.UserId, DateTime.UtcNow);
                    if (wasLogInTimeUpdated)
                    {
                        isAuthenticated = true;
                        errorKey = string.Empty;
                    }
                    else
                    {
                        errorKey = ErrorKeys.Login.DataModel;
                    }
                }
                else
                {
                    errorKey = FindSanctionErrorKey(status);
                }
            }
            else
            {
                _log.WarnFormat("Inicio de sesión rechazado para el jugador {0}.", credentials.UserId);
            }

            return isAuthenticated;
        }

        private static string FindSanctionErrorKey(SuspensionLiftStatus status)
        {
            const string accountSuspendedErrorKey = "Login_AccountSuspendedError";
            const string accountBannedErrorKey = "Login_AccountBannedError";
            string errorKey = ErrorKeys.Login.DataModel;

            if (status == SuspensionLiftStatus.StillSuspended)
            {
                errorKey = accountSuspendedErrorKey;
            }
            else if (status == SuspensionLiftStatus.Banned)
            {
                errorKey = accountBannedErrorKey;
            }

            return errorKey;
        }
    }
}
