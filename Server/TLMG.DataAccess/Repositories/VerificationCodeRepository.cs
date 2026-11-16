using System;
using System.Data;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    public class VerificationCodeRepository : IVerificationCodeRepository
    {
        private const int EmailLength = 254;
        private const int NicknameLength = 12;
        private const int HashLength = 255;
        private const int CodeTypeLength = 30;
        private const int NoFailedAttempts = 0;
        private const string SavedStatus = "Saved";
        private const string CreatedStatus = "Created";

        private static readonly ILog _log = LogManager.GetLogger(typeof(VerificationCodeRepository));

        private class SavePendingRegistrationSpResult
        {
            public string Status { get; set; }
            public int? PendingId { get; set; }
        }

        private class CreateVerificationCodeSpResult
        {
            public string status { get; set; }
            public int? id_code { get; set; }
        }

        private class ValidateVerificationCodeSpResult
        {
            public string status { get; set; }
            public int? attempts_failed { get; set; }
        }

        public int? SavePendingRegistration(PendingRegistrationRecord pendingRegistration)
        {
            int? pendingId = null;
            const string storedProcedureCommand =
                "EXEC dbo.SavePendingRegistration @email, @nickname, @password_hash, @created_at_utc, @expires_at_utc";

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    SqlParameter emailParameter =
                        CreateVarCharParameter("@email", pendingRegistration.Email, EmailLength);
                    SqlParameter nicknameParameter =
                        CreateVarCharParameter("@nickname", pendingRegistration.Nickname, NicknameLength);
                    SqlParameter passwordHashParameter =
                        CreateVarCharParameter("@password_hash", pendingRegistration.PasswordHash, HashLength);
                    SqlParameter createdAtParameter =
                        CreateDateTimeParameter("@created_at_utc", pendingRegistration.CreatedAtUtc);
                    SqlParameter expiresAtParameter =
                        CreateDateTimeParameter("@expires_at_utc", pendingRegistration.ExpiresAtUtc);

                    SavePendingRegistrationSpResult spResult = context.Database
                        .SqlQuery<SavePendingRegistrationSpResult>(
                            storedProcedureCommand,
                            emailParameter,
                            nicknameParameter,
                            passwordHashParameter,
                            createdAtParameter,
                            expiresAtParameter)
                        .FirstOrDefault();

                    if (spResult != null && spResult.Status == SavedStatus)
                    {
                        pendingId = spResult.PendingId;
                    }
                    else
                    {
                        string failedStatus = spResult != null ? spResult.Status : null;
                        _log.WarnFormat("dbo.SavePendingRegistration no guardó el registro. Estado: {0}.", failedStatus);
                    }
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error SQL al guardar el registro pendiente.", sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error("Error de entidad al guardar el registro pendiente.", entityException);
            }

            return pendingId;
        }

        public PendingRegistrationRecord FindPendingRegistrationByEmail(string email)
        {
            PendingRegistrationRecord pendingRecord = null;
            const string storedProcedureCommand = "EXEC dbo.FindPendingRegistration @email";

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    SqlParameter emailParameter = CreateVarCharParameter("@email", email, EmailLength);

                    pendingRecord = context.Database
                        .SqlQuery<PendingRegistrationRecord>(storedProcedureCommand, emailParameter)
                        .FirstOrDefault();
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error SQL al consultar el registro pendiente.", sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error("Error de entidad al consultar el registro pendiente.", entityException);
            }

            return pendingRecord;
        }

        public void DeletePendingRegistration(int pendingId)
        {
            const string storedProcedureCommand = "EXEC dbo.DeletePendingRegistration @id_pending";

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    SqlParameter pendingIdParameter =
                        new SqlParameter("@id_pending", SqlDbType.Int) { Value = pendingId };

                    context.Database.ExecuteSqlCommand(storedProcedureCommand, pendingIdParameter);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error SQL al eliminar el registro pendiente.", sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error("Error de entidad al eliminar el registro pendiente.", entityException);
            }
        }

        public bool SaveCode(VerificationCodeRecord verificationCodeRecord)
        {
            bool wasCodeSaved = false;
            const string storedProcedureCommand =
                "EXEC dbo.CreateVerificationCode @id_user, @id_pending, @type, @code_hash, @expiration_utc";

            string codeTypeText = ConvertToCodeTypeText(verificationCodeRecord.Type);

            if (codeTypeText != null)
            {
                try
                {
                    using (TLMG_DBEntities context = new TLMG_DBEntities())
                    {
                        SqlParameter userIdParameter =
                            CreateNullableIntParameter("@id_user", verificationCodeRecord.UserId);
                        SqlParameter pendingIdParameter =
                            CreateNullableIntParameter("@id_pending", verificationCodeRecord.PendingId);
                        SqlParameter typeParameter =
                            CreateVarCharParameter("@type", codeTypeText, CodeTypeLength);
                        SqlParameter codeHashParameter =
                            CreateVarCharParameter("@code_hash", verificationCodeRecord.CodeHash, HashLength);
                        SqlParameter expirationParameter =
                            CreateDateTimeParameter("@expiration_utc", verificationCodeRecord.ExpiresAtUtc);

                        CreateVerificationCodeSpResult spResult = context.Database
                            .SqlQuery<CreateVerificationCodeSpResult>(
                                storedProcedureCommand,
                                userIdParameter,
                                pendingIdParameter,
                                typeParameter,
                                codeHashParameter,
                                expirationParameter)
                            .FirstOrDefault();

                        wasCodeSaved = spResult != null && spResult.status == CreatedStatus;
                    }
                }
                catch (SqlException sqlException)
                {
                    _log.Error("Error SQL al ejecutar dbo.CreateVerificationCode.", sqlException);
                }
                catch (EntityException entityException)
                {
                    _log.Error("Error de entidad al ejecutar dbo.CreateVerificationCode.", entityException);
                }
            }

            return wasCodeSaved;
        }

        public VerificationCodeValidationResult ValidateCode(
            VerificationCodeRecord verificationCode,
            DateTime nowUtc)
        {
            VerificationCodeValidationResult validationResult = new VerificationCodeValidationResult
            {
                Status = VerificationCodeStatus.UnexpectedError,
                AttemptsFailed = NoFailedAttempts
            };

            const string storedProcedureCommand =
                "EXEC dbo.ValidateVerificationCode @id_user, @id_pending, @type, @code_hash, @now_utc";

            string codeTypeText = ConvertToCodeTypeText(verificationCode.Type);

            if (codeTypeText != null)
            {
                try
                {
                    using (TLMG_DBEntities context = new TLMG_DBEntities())
                    {
                        SqlParameter userIdParameter =
                            CreateNullableIntParameter("@id_user", verificationCode.UserId);
                        SqlParameter pendingIdParameter =
                            CreateNullableIntParameter("@id_pending", verificationCode.PendingId);
                        SqlParameter typeParameter =
                            CreateVarCharParameter("@type", codeTypeText, CodeTypeLength);
                        SqlParameter codeHashParameter =
                            CreateVarCharParameter("@code_hash", verificationCode.CodeHash, HashLength);
                        SqlParameter nowUtcParameter = CreateDateTimeParameter("@now_utc", nowUtc);

                        ValidateVerificationCodeSpResult spResult = context.Database
                            .SqlQuery<ValidateVerificationCodeSpResult>(
                                storedProcedureCommand,
                                userIdParameter,
                                pendingIdParameter,
                                typeParameter,
                                codeHashParameter,
                                nowUtcParameter)
                            .FirstOrDefault();

                        if (spResult != null)
                        {
                            validationResult.Status = ConvertToStatus(spResult.status);
                            validationResult.AttemptsFailed = spResult.attempts_failed ?? NoFailedAttempts;
                        }
                        else
                        {
                            _log.Error("dbo.ValidateVerificationCode no devolvió ninguna fila.");
                        }
                    }
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    _log.Error(
                        "Resultado inesperado al ejecutar dbo.ValidateVerificationCode.",
                        invalidOperationException);
                }
                catch (SqlException sqlException)
                {
                    _log.Error("Error SQL al ejecutar dbo.ValidateVerificationCode.", sqlException);
                }
                catch (EntityException entityException)
                {
                    _log.Error("Error de entidad al ejecutar dbo.ValidateVerificationCode.", entityException);
                }
            }

            return validationResult;
        }

        private static VerificationCodeStatus ConvertToStatus(string storedProcedureStatus)
        {
            const string validStatus = "Valid";
            const string invalidStatus = "Invalid";
            const string tooManyAttemptsStatus = "TooManyAttempts";
            const string expiredStatus = "Expired";
            const string noActiveCodeStatus = "NoActiveCode";

            VerificationCodeStatus verificationCodeStatus;

            switch (storedProcedureStatus)
            {
                case validStatus:
                    verificationCodeStatus = VerificationCodeStatus.Valid;
                    break;
                case invalidStatus:
                    verificationCodeStatus = VerificationCodeStatus.Invalid;
                    break;
                case tooManyAttemptsStatus:
                    verificationCodeStatus = VerificationCodeStatus.TooManyAttempts;
                    break;
                case expiredStatus:
                    verificationCodeStatus = VerificationCodeStatus.Expired;
                    break;
                case noActiveCodeStatus:
                    verificationCodeStatus = VerificationCodeStatus.NoActiveCode;
                    break;
                default:
                    verificationCodeStatus = VerificationCodeStatus.UnexpectedError;
                    break;
            }

            return verificationCodeStatus;
        }

        private static string ConvertToCodeTypeText(VerificationCodeType verificationCodeType)
        {
            const string registrationText = "CODE_REGISTRATION";
            const string logInText = "CODE_LOGIN";
            const string passwordChangeText = "CODE_PASSWORD_CHANGE";
            const string passwordResetText = "CODE_PASSWORD_RESET";

            string codeTypeText = null;

            switch (verificationCodeType)
            {
                case VerificationCodeType.Registration:
                    codeTypeText = registrationText;
                    break;
                case VerificationCodeType.LogIn:
                    codeTypeText = logInText;
                    break;
                case VerificationCodeType.PasswordChange:
                    codeTypeText = passwordChangeText;
                    break;
                case VerificationCodeType.PasswordReset:
                    codeTypeText = passwordResetText;
                    break;
                default:
                    _log.ErrorFormat("Tipo de código de verificación no soportado: {0}.", verificationCodeType);
                    break;
            }

            return codeTypeText;
        }

        private static SqlParameter CreateVarCharParameter(string parameterName, string value, int length)
        {
            return new SqlParameter(parameterName, SqlDbType.VarChar, length)
            {
                Value = (object)value ?? DBNull.Value
            };
        }

        private static SqlParameter CreateDateTimeParameter(string parameterName, DateTime value)
        {
            return new SqlParameter(parameterName, SqlDbType.DateTime2)
            {
                Value = value
            };
        }

        private static SqlParameter CreateNullableIntParameter(string parameterName, int? value)
        {
            return new SqlParameter(parameterName, SqlDbType.Int)
            {
                Value = (object)value ?? DBNull.Value
            };
        }
    }
}