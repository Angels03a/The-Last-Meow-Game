using System;
using System.Data;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;

using log4net;

using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Implementación de la persistencia del chat con Entity Framework 6 y el procedimiento InsertChatMessage.
    /// </summary>
    public class ChatMessageRepository : IChatMessageRepository
    {
        private const string InsertChatMessageCommand =
            "EXEC dbo.InsertChatMessage @id_user, @scope, @id_game, @room_number, @text_message, @sent_at_utc";

        private const string UserIdParameterName = "@id_user";
        private const string ScopeParameterName = "@scope";
        private const string GameIdParameterName = "@id_game";
        private const string RoomNumberParameterName = "@room_number";
        private const string TextParameterName = "@text_message";
        private const string SentAtParameterName = "@sent_at_utc";

        private const int ScopeMaximumLength = 10;
        private const string LobbyScopeText = "LOBBY";
        private const string MatchScopeText = "MATCH";

        private const string SavedStatusText = "Saved";
        private const string InvalidTextStatusText = "InvalidText";
        private const string UserNotFoundStatusText = "UserNotFound";
        private const string InvalidContextStatusText = "InvalidContext";

        private static readonly ILog _log = LogManager.GetLogger(typeof(ChatMessageRepository));

        public ChatMessageSaveStatus SaveChatMessage(ChatMessageRecord chatMessageRecord)
        {
            ChatMessageSaveStatus chatMessageSaveStatus;

            try
            {
                chatMessageSaveStatus = ExecuteInsert(chatMessageRecord);
            }
            catch (SqlException sqlException)
            {
                _log.Error(
                    string.Format(
                        "Error de conexión al guardar un mensaje de chat del jugador {0}.",
                        chatMessageRecord.UserId),
                    sqlException);
                chatMessageSaveStatus = ChatMessageSaveStatus.DataFailure;
            }
            catch (EntityException entityException)
            {
                _log.Error(
                    string.Format(
                        "La base de datos falló al guardar un mensaje de chat del jugador {0}.",
                        chatMessageRecord.UserId),
                    entityException);
                chatMessageSaveStatus = ChatMessageSaveStatus.DataFailure;
            }

            return chatMessageSaveStatus;
        }

        private static ChatMessageSaveStatus ExecuteInsert(ChatMessageRecord chatMessageRecord)
        {
            string procedureStatus = null;

            using (TLMG_DBEntities context = new TLMG_DBEntities())
            {
                ChatInsertOutcome chatInsertOutcome = context.Database
                    .SqlQuery<ChatInsertOutcome>(InsertChatMessageCommand, CreateInsertParameters(chatMessageRecord))
                    .FirstOrDefault();

                if (chatInsertOutcome != null)
                {
                    procedureStatus = chatInsertOutcome.Status;
                }
            }

            ChatMessageSaveStatus chatMessageSaveStatus = ConvertToSaveStatus(procedureStatus);

            return chatMessageSaveStatus;
        }

        private static SqlParameter[] CreateInsertParameters(ChatMessageRecord chatMessageRecord)
        {
            SqlParameter scopeParameter = CreateParameter(
                ScopeParameterName, SqlDbType.VarChar, ConvertToScopeText(chatMessageRecord.Scope));
            scopeParameter.Size = ScopeMaximumLength;

            SqlParameter textParameter = CreateParameter(
                TextParameterName, SqlDbType.NVarChar, chatMessageRecord.Text);
            textParameter.Size = ValidationRules.ChatMaximumMessageLengthCharacters;

            SqlParameter[] insertParameters = new SqlParameter[]
            {
                CreateParameter(UserIdParameterName, SqlDbType.Int, chatMessageRecord.UserId),
                scopeParameter,
                CreateParameter(GameIdParameterName, SqlDbType.Int, ToDatabaseValue(chatMessageRecord.GameId)),
                CreateParameter(
                    RoomNumberParameterName, SqlDbType.Int, ToDatabaseValue(chatMessageRecord.RoomNumber)),
                textParameter,
                CreateParameter(SentAtParameterName, SqlDbType.DateTime2, chatMessageRecord.SentAtUtc)
            };

            return insertParameters;
        }

        private static SqlParameter CreateParameter(string parameterName, SqlDbType parameterType, object value)
        {
            SqlParameter sqlParameter = new SqlParameter(parameterName, parameterType)
            {
                Value = value
            };

            return sqlParameter;
        }

        private static object ToDatabaseValue(int? value)
        {
            object databaseValue = DBNull.Value;

            if (value.HasValue)
            {
                databaseValue = value.Value;
            }

            return databaseValue;
        }

        private static string ConvertToScopeText(ChatMessageScope chatMessageScope)
        {
            string scopeText;

            switch (chatMessageScope)
            {
                case ChatMessageScope.Match:
                    scopeText = MatchScopeText;
                    break;
                default:
                    scopeText = LobbyScopeText;
                    break;
            }

            return scopeText;
        }

        private static ChatMessageSaveStatus ConvertToSaveStatus(string procedureStatus)
        {
            ChatMessageSaveStatus chatMessageSaveStatus;

            switch (procedureStatus)
            {
                case SavedStatusText:
                    chatMessageSaveStatus = ChatMessageSaveStatus.Saved;
                    break;
                case InvalidTextStatusText:
                    chatMessageSaveStatus = ChatMessageSaveStatus.InvalidText;
                    break;
                case UserNotFoundStatusText:
                    chatMessageSaveStatus = ChatMessageSaveStatus.UserNotFound;
                    break;
                case InvalidContextStatusText:
                    chatMessageSaveStatus = ChatMessageSaveStatus.InvalidContext;
                    break;
                default:
                    _log.WarnFormat(
                        "El procedimiento InsertChatMessage devolvió un estado en blanco: {0}.",
                        procedureStatus);
                    chatMessageSaveStatus = ChatMessageSaveStatus.DataFailure;
                    break;
            }

            return chatMessageSaveStatus;
        }
    }
}