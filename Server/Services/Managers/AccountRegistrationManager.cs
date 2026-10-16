using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Security.Cryptography;

using log4net;

using TLMG.Contracts.Dtos;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Helpers;

namespace TLMG.Services.Managers
{
    /// <summary>
    /// Lógica del registro de cuentas; no conoce WCF.
    /// </summary>
    public class AccountRegistrationManager
    {
        private const int DuplicateIndexErrorNumber = 2601;
        private const int DuplicateKeyErrorNumber = 2627;
        private const string InvalidRequestErrorKey = "CreateAccount_IncompleteFieldsError";
        private const string DatabaseErrorKey = "CreateAccount_DataModelError";
        private const string InternalErrorKey = "CreateAccount_DataModelError";
        private const string EmailTakenErrorKey = "CreateAccount_EmailTakenError";
        private const string NicknameTakenErrorKey = "CreateAccount_NicknameTakenError";
        private const string AccountAlreadyExistsErrorKey = "CreateAccount_DuplicateDataError";

        private static readonly ILog _log = LogManager.GetLogger(typeof(AccountRegistrationManager));

        private readonly IPlayerRepository _playerRepository;

        /// <summary>Crea el manager con su repositorio.</summary>
        /// <param name="playerRepository">Repositorio de cuentas.</param>
        public AccountRegistrationManager(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        /// <summary>Valida los datos y crea la cuenta.</summary>
        /// <param name="registration">Datos recibidos del cliente.</param>
        /// <returns>Resultado con IsSuccess y, si falla, la clave de error.</returns>
        public OperationResultData Register(RegistrationData registration)
        {
            OperationResultData result = new OperationResultData();
            string validationErrorKey = ValidateRequest(registration);

            if (validationErrorKey != null)
            {
                result.ErrorKey = validationErrorKey;
            }
            else
            {
                result = TryCreateAccount(registration);
            }

            return result;
        }

        private static string ValidateRequest(RegistrationData registration)
        {
            string errorKey;

            if (registration == null)
            {
                errorKey = InvalidRequestErrorKey;
            }
            else
            {
                errorKey = AccountValidationHelper.ValidateRegistration(registration);
            }

            return errorKey;
        }

        private OperationResultData TryCreateAccount(RegistrationData registration)
        {
            OperationResultData result = new OperationResultData();
            string nickname = registration.Nickname;
            string email = registration.Email.ToLowerInvariant();

            try
            {
                string duplicateErrorKey = FindDuplicateErrorKey(email, nickname);

                if (duplicateErrorKey != null)
                {
                    result.ErrorKey = duplicateErrorKey;
                }
                else
                {
                    SavePlayer(nickname, email, registration.Password);
                    result.IsSuccess = true;
                }
            }
            catch (DbUpdateException ex)
            {
                _log.Error(string.Format("Error al guardar al jugador {0}.", nickname), ex);
                result.ErrorKey = ResolveSaveFailureErrorKey(ex);
            }
            catch (DbEntityValidationException ex)
            {
                _log.Error(string.Format("Datos inválidos para la base de datos del jugador {0}.", nickname), ex);
                result.ErrorKey = DatabaseErrorKey;
            }
            catch (EntityException ex)
            {
                _log.Error(string.Format("La base de datos no respondió al registrar a {0}.", nickname), ex);
                result.ErrorKey = DatabaseErrorKey;
            }
            catch (SqlException ex)
            {
                _log.Error(string.Format("Error de SQL Server al registrar a {0}.", nickname), ex);
                result.ErrorKey = DatabaseErrorKey;
            }
            catch (CryptographicException ex)
            {
                _log.Error(string.Format("Falló el hash de la contraseña de {0}.", nickname), ex);
                result.ErrorKey = InternalErrorKey;
            }

            return result;
        }

        private string FindDuplicateErrorKey(string email, string nickname)
        {
            string errorKey = null;

            if (_playerRepository.ExistsByEmail(email))
            {
                errorKey = EmailTakenErrorKey;
            }
            else if (_playerRepository.ExistsByNickname(nickname))
            {
                errorKey = NicknameTakenErrorKey;
            }

            return errorKey;
        }

        private void SavePlayer(string nickname, string email, string password)
        {
            // FIXME: Implementar el hash de la contraseña; hoy se guarda en claro.
            _playerRepository.CreatePlayer(nickname, email, password);
        }

        private static string ResolveSaveFailureErrorKey(DbUpdateException exception)
        {
            string errorKey = DatabaseErrorKey;
            Exception rootException = exception.GetBaseException();

            if (rootException is SqlException sqlException && IsDuplicateKeyError(sqlException))
            {
                errorKey = AccountAlreadyExistsErrorKey;
            }

            return errorKey;
        }

        private static bool IsDuplicateKeyError(SqlException sqlException)
        {
            bool isDuplicateIndex = sqlException.Number == DuplicateIndexErrorNumber;
            bool isDuplicateKey = sqlException.Number == DuplicateKeyErrorNumber;
            bool isDuplicate = isDuplicateIndex || isDuplicateKey;

            return isDuplicate;
        }
    }
}