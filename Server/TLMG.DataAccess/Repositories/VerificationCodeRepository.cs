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
        private static readonly ILog _log = LogManager.GetLogger(typeof(VerificationCodeRepository));
        private const string CreatedStatus = "Created";
        private const string UnexpectedErrorStatus = "Error";

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
            const string upsertCommand =
                "DELETE vc " +
                "FROM dbo.Verification_Code vc " +
                "INNER JOIN dbo.Pending_Registration pr ON vc.id_pending = pr.id_pending " +
                "WHERE pr.email = @Email; " +
                "DELETE FROM dbo.Pending_Registration WHERE email = @Email; " +
                "INSERT INTO dbo.Pending_Registration (email, nickname, password_hash, created_at, expires_at) " +
                "OUTPUT INSERTED.id_pending " +
                "VALUES (@Email, @Nickname, @PasswordHash, @CreatedAt, @ExpiresAt);";

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    SqlParameter emailParameter =
                        new SqlParameter("@Email", SqlDbType.VarChar, 254) { Value = pendingRegistration.Email };
                    SqlParameter nicknameParameter =
                        new SqlParameter("@Nickname", SqlDbType.VarChar, 12) { Value = pendingRegistration.Nickname };
                    SqlParameter passwordHashParameter =
                        new SqlParameter("@PasswordHash", SqlDbType.VarChar, 255) { Value = pendingRegistration.PasswordHash };
                    SqlParameter createdAtParameter =
                        new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = pendingRegistration.CreatedAtUtc };
                    SqlParameter expiresAtParameter =
                        new SqlParameter("@ExpiresAt", SqlDbType.DateTime2) { Value = pendingRegistration.ExpiresAtUtc };

                    int insertedId = context.Database.SqlQuery<int>(
                        upsertCommand,
                        emailParameter,
                        nicknameParameter,
                        passwordHashParameter,
                        createdAtParameter,
                        expiresAtParameter).SingleOrDefault();

                    if (insertedId > 0)
                    {
                        pendingId = insertedId;
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
            const string selectQuery =
                "SELECT TOP (1) id_pending AS PendingId, email AS Email, nickname AS Nickname, " +
                "password_hash AS PasswordHash, created_at AS CreatedAtUtc, expires_at AS ExpiresAtUtc " +
                "FROM dbo.Pending_Registration WHERE email = @Email ORDER BY id_pending DESC;";

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    SqlParameter emailParameter =
                        new SqlParameter("@Email", SqlDbType.VarChar, 254) { Value = email };

                    pendingRecord = context.Database.SqlQuery<PendingRegistrationRecord>(
                        selectQuery,
                        emailParameter).FirstOrDefault();
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
            const string deleteCommand =
                "DELETE FROM dbo.Verification_Code WHERE id_pending = @PendingId; " +
                "DELETE FROM dbo.Pending_Registration WHERE id_pending = @PendingId;";


            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    SqlParameter pendingIdParameter =
                        new SqlParameter("@PendingId", SqlDbType.Int) { Value = pendingId };

                    context.Database.ExecuteSqlCommand(deleteCommand, pendingIdParameter);
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

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    SqlParameter userIdParameter =
                        CreateNullableIntParameter("@id_user", verificationCodeRecord.UserId);
                    SqlParameter pendingIdParameter =
                        CreateNullableIntParameter("@id_pending", verificationCodeRecord.PendingId);
                    SqlParameter typeParameter =
                        new SqlParameter("@type", SqlDbType.VarChar, 30) { Value = verificationCodeRecord.Type };
                    SqlParameter codeHashParameter =
                        new SqlParameter("@code_hash", SqlDbType.VarChar, 255) { Value = verificationCodeRecord.CodeHash };
                    SqlParameter expirationParameter =
                        new SqlParameter("@expiration_utc", SqlDbType.DateTime2) { Value = verificationCodeRecord.ExpiresAtUtc };

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

            return wasCodeSaved;
        }

        public VerificationCodeValidationResult ValidateCode(
            VerificationCodeRecord verificationCode,
            DateTime nowUtc)
        {
            VerificationCodeValidationResult validationResult = new VerificationCodeValidationResult
            {
                Status = UnexpectedErrorStatus,
                AttemptsFailed = 0
            };

            const string storedProcedureCommand =
                "EXEC dbo.ValidateVerificationCode @id_user, @id_pending, @type, @code_hash, @now_utc";

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    SqlParameter userIdParameter =
                        CreateNullableIntParameter("@id_user", verificationCode.UserId);
                    SqlParameter pendingIdParameter =
                        CreateNullableIntParameter("@id_pending", verificationCode.PendingId);
                    SqlParameter typeParameter =
                        new SqlParameter("@type", SqlDbType.VarChar, 30) { Value = verificationCode.Type };
                    SqlParameter codeHashParameter =
                        new SqlParameter("@code_hash", SqlDbType.VarChar, 255) { Value = verificationCode.CodeHash };
                    SqlParameter nowUtcParameter =
                        new SqlParameter("@now_utc", SqlDbType.DateTime2) { Value = nowUtc };

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
                        validationResult.Status = spResult.status;
                        validationResult.AttemptsFailed = spResult.attempts_failed ?? 0;
                    }
                    else
                    {
                        _log.Error("dbo.ValidateVerificationCode no devolvió ninguna fila.");
                    }
                }
            }
            catch (InvalidOperationException invalidOperationException)
            {
                _log.Error("Resultado inesperado al ejecutar dbo.ValidateVerificationCode.", invalidOperationException);
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error SQL al ejecutar dbo.ValidateVerificationCode.", sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error("Error de entidad al ejecutar dbo.ValidateVerificationCode.", entityException);
            }


            return validationResult;
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