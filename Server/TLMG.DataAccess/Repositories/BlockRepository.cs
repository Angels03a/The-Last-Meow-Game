using System.Data;
using System.Data.SqlClient;
using System.Linq;

using log4net;

using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    public sealed class BlockRepository : IBlockRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BlockRepository));

        public BlockWriteStatus BlockPlayer(int blockerUserId, int blockedUserId)
        {
            const string command = "EXEC dbo.BlockPlayer @id_user_blocker, @id_user_blocked";
            BlockWriteStatus status = ExecuteBlockProcedure(command, blockerUserId, blockedUserId);

            return status;
        }

        public BlockWriteStatus UnblockPlayer(int blockerUserId, int blockedUserId)
        {
            const string command = "EXEC dbo.UnblockPlayer @id_user_blocker, @id_user_blocked";
            BlockWriteStatus status = ExecuteBlockProcedure(command, blockerUserId, blockedUserId);

            return status;
        }

        public BlockListLookup FindBlocksInvolvingUser(int userId)
        {
            BlockListLookup blockListLookup = new BlockListLookup();

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    IQueryable<BlockRelationship> blocksQuery =
                        from block in context.Set<Block_List>().AsNoTracking()
                        join blockedUser in context.User.AsNoTracking()
                            on block.id_user_blocked equals blockedUser.id_user
                        where block.id_user_blocker == userId || block.id_user_blocked == userId
                        select new BlockRelationship
                        {
                            BlockerUserId = block.id_user_blocker,
                            BlockedUserId = block.id_user_blocked,
                            BlockedNickname = blockedUser.nickname
                        };

                    blockListLookup.Relationships = blocksQuery.ToList();
                    blockListLookup.WasLookupSuccessful = true;
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(
                    string.Format("Error SQL al consultar los bloqueos del jugador {0}.", userId),
                    sqlException);
            }
            catch (DataException dataException)
            {
                _log.Error(
                    string.Format("La base de datos falló al consultar los bloqueos del jugador {0}.", userId),
                    dataException);
            }

            return blockListLookup;
        }

        private static BlockWriteStatus ExecuteBlockProcedure(
            string command,
            int blockerUserId,
            int blockedUserId)
        {
            BlockWriteStatus status = BlockWriteStatus.DataFailure;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                {
                    BlockProcedureRow row = context.Database.SqlQuery<BlockProcedureRow>(
                        command,
                        new SqlParameter("@id_user_blocker", SqlDbType.Int) { Value = blockerUserId },
                        new SqlParameter("@id_user_blocked", SqlDbType.Int) { Value = blockedUserId })
                        .FirstOrDefault();

                    status = ConvertWriteStatus(row);
                }
            }
            catch (SqlException sqlException)
            {
                _log.Error(
                    string.Format("Error SQL al modificar el bloqueo del jugador {0} al jugador {1}.",
                        blockerUserId, blockedUserId), sqlException);
            }
            catch (DataException dataException)
            {
                _log.Error(
                    string.Format("La base de datos falló al modificar el bloqueo del jugador {0} al jugador {1}.",
                        blockerUserId, blockedUserId), dataException);
            }

            return status;
        }

        private static BlockWriteStatus ConvertWriteStatus(BlockProcedureRow row)
        {
            BlockWriteStatus status = BlockWriteStatus.DataFailure;

            if (row == null)
            {
                _log.Error("El procedimiento de bloqueo no devolvió un resultado.");
            }
            else if (row.Status == nameof(BlockWriteStatus.Blocked))
            {
                status = BlockWriteStatus.Blocked;
            }
            else if (row.Status == nameof(BlockWriteStatus.AlreadyBlocked))
            {
                status = BlockWriteStatus.AlreadyBlocked;
            }
            else if (row.Status == nameof(BlockWriteStatus.Unblocked))
            {
                status = BlockWriteStatus.Unblocked;
            }
            else if (row.Status == nameof(BlockWriteStatus.NotBlocked))
            {
                status = BlockWriteStatus.NotBlocked;
            }
            else if (row.Status == nameof(BlockWriteStatus.InvalidTarget))
            {
                status = BlockWriteStatus.InvalidTarget;
            }
            else if (row.Status == nameof(BlockWriteStatus.UserNotFound))
            {
                status = BlockWriteStatus.UserNotFound;
            }
            else
            {
                _log.ErrorFormat("El procedimiento de bloqueo devolvió un estado desconocido: {0}.", row.Status);
            }

            return status;
        }

        private sealed class BlockProcedureRow
        {
            public string Status { get; set; }
        }
    }
}
