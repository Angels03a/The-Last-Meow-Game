using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using log4net;

using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    public sealed class KickVotingRepository : IKickVotingRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(KickVotingRepository));
        private const string RecordVotingCommand = "EXEC dbo.RecordCompletedKickVoting @voting_id, "
            + "@room_scope_id, @match_id, @id_user_receiver, @guest_receiver_session_id, @completed_at_utc, @voters";
        private const string VoterListTypeName = "dbo.KickVoterList";
        private const byte TimestampScale = 3;
        private static readonly IReadOnlyDictionary<string, KickVotingWriteStatus> _statuses =
            new Dictionary<string, KickVotingWriteStatus>(StringComparer.Ordinal)
            {
                { nameof(KickVotingWriteStatus.Recorded), KickVotingWriteStatus.Recorded },
                { nameof(KickVotingWriteStatus.AlreadyRecorded), KickVotingWriteStatus.AlreadyRecorded },
                { nameof(KickVotingWriteStatus.VotingConflict), KickVotingWriteStatus.VotingConflict },
                { nameof(KickVotingWriteStatus.InvalidVoting), KickVotingWriteStatus.InvalidVoting },
                { nameof(KickVotingWriteStatus.MatchNotFound), KickVotingWriteStatus.MatchNotFound },
                { nameof(KickVotingWriteStatus.UserNotFound), KickVotingWriteStatus.UserNotFound }
            };

        public KickVotingWriteStatus RecordCompletedVoting(CompletedKickVoting voting)
        {
            KickVotingWriteStatus status = KickVotingWriteStatus.InvalidVoting;

            if (voting != null && voting.IsValid)
            {
                status = KickVotingWriteStatus.DataFailure;
                try
                {
                    using (DataTable voters = CreateVoterTable(voting.Voters))
                    using (TLMG_DBEntities context = new TLMG_DBEntities())
                    {
                        SqlParameter[] parameters = CreateParameters(voting, voters);
                        string procedureStatus = context.Database.SqlQuery<string>(RecordVotingCommand, parameters)
                            .FirstOrDefault();
                        status = ResolveStatus(procedureStatus);
                    }
                }
                catch (SqlException sqlException)
                {
                    _log.Error("SQL Server no pudo registrar el resultado de la votación de expulsión.", sqlException);
                }
                catch (DataException dataException)
                {
                    _log.Error("No se pudo guardar el resultado de la votación de expulsión.", dataException);
                }
            }

            return status;
        }

        private static KickVotingWriteStatus ResolveStatus(string procedureStatus)
        {
            KickVotingWriteStatus status = KickVotingWriteStatus.DataFailure;
            if (string.IsNullOrWhiteSpace(procedureStatus) || !_statuses.TryGetValue(procedureStatus, out status))
            {
                status = KickVotingWriteStatus.DataFailure;
                _log.ErrorFormat("RecordCompletedKickVoting devolvió un estado no admitido: {0}.", procedureStatus);
            }

            return status;
        }

        private static DataTable CreateVoterTable(IReadOnlyList<KickParticipantIdentity> identities)
        {
            DataTable voters = new DataTable();
            voters.Columns.Add("id_user", typeof(int));
            voters.Columns.Add("guest_session_id", typeof(Guid));
            foreach (KickParticipantIdentity identity in identities)
            {
                voters.Rows.Add(ToDatabaseValue(identity.UserId), ToDatabaseValue(identity.GuestSessionId));
            }

            return voters;
        }

        private static SqlParameter[] CreateParameters(CompletedKickVoting voting, DataTable voters)
        {
            SqlParameter[] parameters =
            {
                new SqlParameter("@voting_id", SqlDbType.UniqueIdentifier) { Value = voting.VotingId },
                new SqlParameter("@room_scope_id", SqlDbType.UniqueIdentifier) { Value = voting.RoomScopeId },
                new SqlParameter("@match_id", SqlDbType.UniqueIdentifier) { Value = ToDatabaseValue(voting.MatchId) },
                new SqlParameter("@id_user_receiver", SqlDbType.Int) { Value = ToDatabaseValue(voting.Target.UserId) },
                new SqlParameter("@guest_receiver_session_id", SqlDbType.UniqueIdentifier)
                {
                    Value = ToDatabaseValue(voting.Target.GuestSessionId)
                },
                new SqlParameter("@completed_at_utc", SqlDbType.DateTime2)
                {
                    Scale = TimestampScale,
                    Value = voting.CompletedAtUtc
                },
                new SqlParameter("@voters", SqlDbType.Structured) { TypeName = VoterListTypeName, Value = voters }
            };

            return parameters;
        }

        private static object ToDatabaseValue<T>(T? value) where T : struct
        {
            object databaseValue = DBNull.Value;
            if (value.HasValue)
            {
                databaseValue = value.Value;
            }

            return databaseValue;
        }
    }
}