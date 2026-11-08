using System;
using log4net;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Domain.Entities;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;
using TLMG.Services.Helpers;
using TLMG.Services.Sessions;

namespace TLMG.Services.Accounts
{
    /// <summary>
    /// Lógica del inicio de sesión; no conoce WCF.
    /// </summary>
    public class PlayerLogInService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerLogInService));

        private IPlayerRepository _playerRepository;
        private IPasswordHasher _passwordHasher;
        private ISessionRegistry _sessionRegistry;

        public PlayerLogInService(
            IPlayerRepository playerRepository,
            IPasswordHasher passwordHasher,
            ISessionRegistry sessionRegistry)
        {
            _playerRepository = playerRepository;
            _passwordHasher = passwordHasher;
            _sessionRegistry = sessionRegistry;
        }

        public ResultWithNotification<OperationLogInResult> LogIn(OperationLogInRequest operationLogInRequest)
        {
            string sessionToken = null;
            int userId = ValidationRules.NoUserId;
            string nickname = null;
            SessionEnd replacedSession = null;

            string errorKey = ValidateLogInRequest(operationLogInRequest, out PlayerLogin playerLogin);

            if (errorKey == null)
            {
                PlayerCredentials playerCredentials;
                errorKey = AuthenticatePlayer(playerLogin, out playerCredentials);

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

            ResultWithNotification<OperationLogInResult> loginResult = 
                new ResultWithNotification<OperationLogInResult>(operationLogInResult,
                SessionNotificationHelper.CreateSessionEndNotifications(replacedSession));

            return loginResult;
        }

        private static string ValidateLogInRequest(OperationLogInRequest operationLogInRequest, 
            out PlayerLogin playerLogin)
        {
            string errorKey = null;
            playerLogin = null;

            if (operationLogInRequest == null
                || string.IsNullOrWhiteSpace(operationLogInRequest.Email)
                || string.IsNullOrWhiteSpace(operationLogInRequest.Password))
            {
                errorKey = ErrorKeys.Login.IncompleteFields;
            }
            else
            {
                playerLogin = new PlayerLogin
                {
                    Email = operationLogInRequest.Email,
                    Password = operationLogInRequest.Password
                };

                if (PlayerAccountRegistrationValidator.ValidateEmail(playerLogin.Email) != null)
                {
                    errorKey = ErrorKeys.Login.EmailFormat;
                }
            }

            return errorKey;
        }

        private string AuthenticatePlayer(PlayerLogin playerLogin, out PlayerCredentials playerCredentials)
        {
            string errorKey = ErrorKeys.Login.InvalidCredentials;
            string email = playerLogin.Email.ToLowerInvariant();
            playerCredentials = _playerRepository.FindCredentialsByEmail(email);

            if (playerCredentials.Status == CredentialsLookupStatus.DataFailure)
            {
                errorKey = ErrorKeys.Login.DataModel;
            }
            else if (playerCredentials.Status == CredentialsLookupStatus.Found)
            {
                errorKey = VerifyKnownPlayer(playerCredentials, playerLogin.Password);
            }

            return errorKey;
        }

        private string VerifyKnownPlayer(PlayerCredentials playerCredentials, string password)
        {
            string errorKey = ErrorKeys.Login.InvalidCredentials;
            bool isPasswordValid = _passwordHasher.VerifyPassword(password, playerCredentials.PasswordHash);
            bool isAccountActive = playerCredentials.State == ValidationRules.ActiveUserState;

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
            string errorKey = ErrorKeys.Login.DataModel;
            bool wasLastLogInTimeUpdated = _playerRepository.UpdateLastLogInTime(userId, DateTime.UtcNow);

            if (wasLastLogInTimeUpdated)
            {
                errorKey = null;
                _log.InfoFormat("El jugador {0} inició sesión.", userId);
            }

            return errorKey;
        }
    }
}