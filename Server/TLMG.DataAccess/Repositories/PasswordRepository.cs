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
    public class PasswordRepository : IPasswordRepository
    {
        private const int EmailLength = 254;
        private const int HashLength = 255;
        private const string ChangedStatus = "Changed";
        private const string StaleCredentialsStatus = "StaleCredentials";
        private const string ResetStatus = "Reset";
        private const string NoVerifiedCodeStatus = "NoVerifiedCode";
        private const string UserNotFoundStatus = "UserNotFound";

        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordRepository));

        private class PasswordProcedureRow
        {
            public string status { get; set; }
        }

        public PasswordHashLookup FindPasswordHashByUserId(int userId)
        {
            PasswordHashLookup hashLookup = new PasswordHashLookup { Status = CredentialsLookupStatus.NotFound };

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    string storedHash = (from user in context.User.AsNoTracking()
                                         where user.id_user == userId
                                         select user.password_hash).FirstOrDefault();

                    if (storedHash != null)
                    {
                        hashLookup.Status = CredentialsLookupStatus.Found;
                        hashLookup.PasswordHash = storedHash;
                    }
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(
                    string.Format("Error de conexión al leer la contraseña del jugador {0}.", userId),
                    sqlException);
                hashLookup.Status = CredentialsLookupStatus.DataFailure;
            }
            catch (EntityException entityException)
            {
                _log.Error(
                    string.Format("La base de datos falló al leer la contraseña del jugador {0}.", userId),
                    entityException);
                hashLookup.Status = CredentialsLookupStatus.DataFailure;
            }

            return hashLookup;
        }

        public PasswordChangeStatus ChangePassword(int userId, string currentHash, string newHash)
        {
            PasswordChangeStatus changeStatus = PasswordChangeStatus.DataFailure;
            const string command = "EXEC dbo.ChangePassword @id_user, @current_hash, @new_hash, @now_utc";

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    PasswordProcedureRow row = context.Database
                        .SqlQuery<PasswordProcedureRow>(
                            command,
                            new SqlParameter("@id_user", SqlDbType.Int) { Value = userId },
                            CreateHashParameter("@current_hash", currentHash),
                            CreateHashParameter("@new_hash", newHash),
                            CreateNowParameter())
                        .FirstOrDefault();

                    changeStatus = ConvertChangeStatus(row);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(
                    string.Format("Error SQL al cambiar la contraseña del jugador {0}.", userId),
                    sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error(
                    string.Format("La base de datos falló al cambiar la contraseña del jugador {0}.", userId),
                    entityException);
            }

            return changeStatus;
        }

        public PasswordResetStatus ResetPasswordWithVerifiedCode(
            string email,
            string codeHash,
            string newPasswordHash)
        {
            PasswordResetStatus resetStatus = PasswordResetStatus.DataFailure;
            const string command =
                "EXEC dbo.ResetPasswordWithVerifiedCode @email, @code_hash, @new_password_hash, @now_utc";

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    PasswordProcedureRow row = context.Database
                        .SqlQuery<PasswordProcedureRow>(
                            command,
                            new SqlParameter("@email", SqlDbType.VarChar, EmailLength) { Value = email },
                            CreateHashParameter("@code_hash", codeHash),
                            CreateHashParameter("@new_password_hash", newPasswordHash),
                            CreateNowParameter())
                        .FirstOrDefault();

                    resetStatus = ConvertResetStatus(row);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error("Error SQL al restablecer una contraseña con código.", sqlException);
            }
            catch (EntityException entityException)
            {
                _log.Error("La base de datos falló al restablecer una contraseña con código.", entityException);
            }

            return resetStatus;
        }

        private static PasswordChangeStatus ConvertChangeStatus(PasswordProcedureRow row)
        {
            PasswordChangeStatus changeStatus = PasswordChangeStatus.DataFailure;

            if (row != null)
            {
                switch (row.status)
                {
                    case ChangedStatus:
                        changeStatus = PasswordChangeStatus.Changed;
                        break;
                    case StaleCredentialsStatus:
                        changeStatus = PasswordChangeStatus.StaleCredentials;
                        break;
                    case UserNotFoundStatus:
                        changeStatus = PasswordChangeStatus.UserNotFound;
                        break;
                    default:
                        _log.ErrorFormat("dbo.ChangePassword devolvió un estado desconocido: {0}.", row.status);
                        break;
                }
            }

            return changeStatus;
        }

        private static PasswordResetStatus ConvertResetStatus(PasswordProcedureRow row)
        {
            PasswordResetStatus resetStatus = PasswordResetStatus.DataFailure;

            if (row != null)
            {
                switch (row.status)
                {
                    case ResetStatus:
                        resetStatus = PasswordResetStatus.Reset;
                        break;
                    case NoVerifiedCodeStatus:
                        resetStatus = PasswordResetStatus.NoVerifiedCode;
                        break;
                    case UserNotFoundStatus:
                        resetStatus = PasswordResetStatus.UserNotFound;
                        break;
                    default:
                        _log.ErrorFormat(
                            "dbo.ResetPasswordWithVerifiedCode devolvió un estado desconocido: {0}.",
                            row.status);
                        break;
                }
            }

            return resetStatus;
        }

        private static SqlParameter CreateHashParameter(string parameterName, string value)
        {
            SqlParameter hashParameter = new SqlParameter(parameterName, SqlDbType.VarChar, HashLength)
            {
                Value = value
            };

            return hashParameter;
        }

        private static SqlParameter CreateNowParameter()
        {
            SqlParameter nowParameter = new SqlParameter("@now_utc", SqlDbType.DateTime2)
            {
                Value = DateTime.UtcNow
            };

            return nowParameter;
        }
    }
}