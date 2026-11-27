/* TLMG_DB v4.2 - Incremental match history and leaderboard migration.
   Requires the v4.1 schema. Execute as a database administrator with the server stopped.
   Existing game records, player totals and scoring timestamps are preserved.
   Password procedures are maintained by the separate password migration. */
USE [TLMG_DB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Game', N'U') IS NULL
        OR OBJECT_ID(N'dbo.Game_Player', N'U') IS NULL
        OR OBJECT_ID(N'dbo.Player', N'U') IS NULL
        OR OBJECT_ID(N'dbo.[User]', N'U') IS NULL
        THROW 50010, 'Required TLMG tables are missing.', 1;

    IF DATABASE_PRINCIPAL_ID(N'role_tlmg_app') IS NULL
        THROW 50011, 'Required application role is missing.', 1;

    IF COL_LENGTH(N'dbo.Game', N'match_id') IS NULL
        ALTER TABLE dbo.Game ADD match_id UNIQUEIDENTIFIER NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Game') AND name = N'match_id'
          AND system_type_id = TYPE_ID(N'uniqueidentifier') AND is_nullable = 1
    )
        THROW 50012, 'Existing match_id column is incompatible.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Game') AND name = N'UX_Game_match_id'
    )
        EXEC sys.sp_executesql N'
            CREATE UNIQUE NONCLUSTERED INDEX UX_Game_match_id
            ON dbo.Game (match_id) WHERE match_id IS NOT NULL;';

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes AS i
        INNER JOIN sys.index_columns AS ic
            ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN sys.columns AS c
            ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE i.object_id = OBJECT_ID(N'dbo.Game') AND i.name = N'UX_Game_match_id'
          AND i.is_unique = 1 AND i.is_disabled = 0 AND i.has_filter = 1
          AND REPLACE(REPLACE(REPLACE(REPLACE(UPPER(i.filter_definition), '[', ''), ']', ''), '(', ''), ')', '') = N'MATCH_ID IS NOT NULL'
          AND ic.key_ordinal = 1 AND c.name = N'match_id'
          AND NOT EXISTS (SELECT 1 FROM sys.index_columns AS extra
              WHERE extra.object_id = i.object_id AND extra.index_id = i.index_id AND extra.key_ordinal > 1)
    )
        THROW 50013, 'Existing match identity index is incompatible.', 1;

    IF COL_LENGTH(N'dbo.Player', N'points_reached_at_utc') IS NULL
    BEGIN
        ALTER TABLE dbo.Player ADD points_reached_at_utc DATETIME2(7) NOT NULL
            CONSTRAINT DF_Player_points_reached_at_utc DEFAULT (SYSUTCDATETIME()) WITH VALUES;

        DECLARE @migration_utc DATETIME2(7) = SYSUTCDATETIME();

        EXEC sys.sp_executesql N'
            UPDATE player
            SET points_reached_at_utc = COALESCE(history.last_scoring_game, @migration_utc)
            FROM dbo.Player AS player
            OUTER APPLY
            (
                SELECT MAX(game.game_ending_date) AS last_scoring_game
                FROM dbo.Game_Player AS participant
                INNER JOIN dbo.Game AS game ON game.id_game = participant.id_game
                WHERE participant.id_player = player.id_player
                  AND participant.points_earned > 0
                  AND game.state = ''GAME_FINISHED''
                HAVING SUM(CONVERT(BIGINT, participant.points_earned)) = player.total_points
                   AND COUNT(*) = COUNT(game.game_ending_date)
            ) AS history;',
            N'@migration_utc DATETIME2(7)', @migration_utc = @migration_utc;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Player') AND name = N'points_reached_at_utc'
          AND system_type_id = TYPE_ID(N'datetime2') AND scale = 7 AND is_nullable = 0
    )
        THROW 50014, 'Existing points_reached_at_utc column is incompatible.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Player') AND name = N'points_reached_at_utc'
          AND default_object_id <> 0
    )
        ALTER TABLE dbo.Player ADD CONSTRAINT DF_Player_points_reached_at_utc
            DEFAULT (SYSUTCDATETIME()) FOR points_reached_at_utc;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Player') AND name = N'IX_Player_points'
    )
        EXEC sys.sp_executesql N'
            CREATE NONCLUSTERED INDEX IX_Player_points
            ON dbo.Player (total_points DESC, points_reached_at_utc ASC, id_player ASC)
            INCLUDE (id_user, total_wins, total_games_played);';
    ELSE IF NOT EXISTS (
        SELECT 1 FROM sys.indexes AS i
        INNER JOIN sys.index_columns AS ic
            ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN sys.columns AS c
            ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE i.object_id = OBJECT_ID(N'dbo.Player') AND i.name = N'IX_Player_points'
          AND i.is_disabled = 0 AND ic.key_ordinal = 2
          AND c.name = N'points_reached_at_utc' AND ic.is_descending_key = 0
    )
        EXEC sys.sp_executesql N'
            CREATE NONCLUSTERED INDEX IX_Player_points
            ON dbo.Player (total_points DESC, points_reached_at_utc ASC, id_player ASC)
            INCLUDE (id_user, total_wins, total_games_played)
            WITH (DROP_EXISTING = ON);';

    EXEC sys.sp_executesql N'
        CREATE OR ALTER VIEW dbo.vw_Leaderboard
        AS
        SELECT
            ROW_NUMBER() OVER (
                ORDER BY player.total_points DESC,
                         player.points_reached_at_utc ASC,
                         player.id_player ASC
            ) AS ranking_position,
            account.id_user,
            account.nickname,
            player.total_points,
            player.total_wins,
            player.total_games_played
        FROM dbo.Player AS player
        INNER JOIN dbo.[User] AS account ON account.id_user = player.id_user
        WHERE account.state = ''USER_ACTIVE'';';

    EXEC sys.sp_executesql N'
        CREATE OR ALTER PROCEDURE dbo.GetPlayerStanding
            @id_user INT
        AS
        BEGIN
            SET NOCOUNT ON;

            SELECT CONVERT(INT, ranking_position) AS ranking_position,
                   total_points, total_wins, total_games_played
            FROM dbo.vw_Leaderboard
            WHERE id_user = @id_user;
        END;';

    GRANT SELECT, INSERT, UPDATE ON dbo.Game TO role_tlmg_app;
    GRANT SELECT, INSERT, UPDATE ON dbo.Game_Player TO role_tlmg_app;
    GRANT SELECT, INSERT, UPDATE ON dbo.Player TO role_tlmg_app;
    GRANT UPDATE (nickname, profile_image) ON dbo.[User] TO role_tlmg_app;
    GRANT SELECT ON dbo.vw_Leaderboard TO role_tlmg_app;
    GRANT EXECUTE ON dbo.GetPlayerStanding TO role_tlmg_app;

    COMMIT TRANSACTION;
    PRINT N'TLMG_DB v4.2 match history migration completed.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
