using System;
using System.Data;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Implementación del repositorio alineada con la entidad Verification_Code del EDMX.
    /// </summary>
    public class VerificationCodeRepository : IVerificationCodeRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(VerificationCodeRepository));

        public VerificationCodeRecord FindLatestCodeByEmail(string email, string type)
        {
            VerificationCodeRecord verificationCodeRecord = null;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    IQueryable<VerificationCodeRecord> query = from verificationEntry in context.Verification_Code
                                                               join userEntry in context.User on verificationEntry.id_user equals userEntry.id_user into userGroup
                                                               from subUser in userGroup.DefaultIfEmpty()
                                                               where (subUser != null && subUser.email == email) && verificationEntry.type == type
                                                               orderby verificationEntry.generation_date descending
                                                               select new VerificationCodeRecord
                                                               {
                                                                   CodeId = verificationEntry.id_code,
                                                                   UserId = verificationEntry.id_user,
                                                                   PendingId = verificationEntry.id_pending,
                                                                   CodeHash = verificationEntry.code_hash,
                                                                   Type = verificationEntry.type,
                                                                   State = verificationEntry.state,
                                                                   GeneratedAtUtc = verificationEntry.generation_date,
                                                                   ExpiresAtUtc = verificationEntry.expiration_date,
                                                                   AttemptsFailed = verificationEntry.attempts_failed
                                                               };

                    verificationCodeRecord = query.FirstOrDefault();
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexión al buscar el código de verificación por correo.", sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos no respondió al consultar el código de verificación.", entityException);
            }

            return verificationCodeRecord;
        }

        public void SaveCode(VerificationCodeRecord verificationCodeRecord)
        {
            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    Verification_Code newVerificationCode = new Verification_Code
                    {
                        id_user = verificationCodeRecord.UserId,
                        id_pending = verificationCodeRecord.PendingId,
                        code_hash = verificationCodeRecord.CodeHash,
                        type = verificationCodeRecord.Type,
                        state = verificationCodeRecord.State,
                        generation_date = verificationCodeRecord.GeneratedAtUtc,
                        expiration_date = verificationCodeRecord.ExpiresAtUtc,
                        attempts_failed = verificationCodeRecord.AttemptsFailed
                    };

                    context.Verification_Code.Add(newVerificationCode);
                    context.SaveChanges();
                }
            }
            catch (DbUpdateException dbUpdateException)
            {
                _log.Error("Error al guardar el registro del código de verificación.", dbUpdateException);
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos no respondió al insertar el código de verificación.", entityException);
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexión al insertar el código de verificación.", sqlException);
            }
        }

        public bool ConsumeCodeIfValid(string email, string codeHash, DateTime nowUtc)
        {
            bool isValidAndConsumed = false;
            const string storedProcedureCommand = 
                "EXEC sp_ConsumeVerificationCodeIfValid @Email, @CodeHash, @NowUtc, @IsValid OUTPUT";

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    SqlParameter emailParameter =
                        new SqlParameter("@Email", SqlDbType.NVarChar) { Value = email };
                    SqlParameter codeHashParameter = 
                        new SqlParameter("@CodeHash", SqlDbType.VarChar) { Value = codeHash };
                    SqlParameter nowUtcParameter = 
                        new SqlParameter("@NowUtc", SqlDbType.DateTime2) { Value = nowUtc };
                    SqlParameter isValidOutputParameter = 
                        new SqlParameter("@IsValid", SqlDbType.Bit)
                    {
                        Direction = ParameterDirection.Output
                    };

                    context.Database.ExecuteSqlCommand(storedProcedureCommand, 
                        emailParameter, codeHashParameter, nowUtcParameter, isValidOutputParameter);

                    if (isValidOutputParameter.Value != DBNull.Value)
                    {
                        isValidAndConsumed = Convert.ToBoolean(isValidOutputParameter.Value);
                    }
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexión al validar y consumir el código de verificación.", sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos no respondió al validar el código de verificación.", entityException);
            }

            return isValidAndConsumed;
        }

        public void DeleteExpiredCodes(DateTime nowUtc)
        {
            const string deleteCommand = 
                "DELETE FROM Verification_Code WHERE expiration_date <= @NowUtc";

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    SqlParameter nowUtcParameter = 
                        new SqlParameter("@NowUtc", SqlDbType.DateTime2) { Value = nowUtc };
                    context.Database.ExecuteSqlCommand(deleteCommand, nowUtcParameter);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error de conexión al eliminar los códigos de verificación expirados.", sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos no respondió al depurar códigos expirados.", entityException);
            }
        }
    }
}