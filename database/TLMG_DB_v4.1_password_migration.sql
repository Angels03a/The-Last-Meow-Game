/* Incremental password schema update for existing TLMG_DB installations.
   Review before execution. Existing user and game records are preserved. */
USE TLMG_DB;
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL OR OBJECT_ID(N'dbo.Verification_Code', N'U') IS NULL
        THROW 50000, 'Required tables are missing.', 1;
    IF DATABASE_PRINCIPAL_ID(N'role_tlmg_app') IS NULL
        THROW 50001, 'Required application role is missing.', 1;
    IF COL_LENGTH(N'dbo.[User]', N'password_changed_at') IS NULL
        ALTER TABLE dbo.[User] ADD password_changed_at DATETIME2(3) NOT NULL
            CONSTRAINT DF_User_password_changed DEFAULT (SYSUTCDATETIME()) WITH VALUES;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.[User]')
        AND name = N'password_changed_at' AND system_type_id = TYPE_ID(N'datetime2')
        AND scale = 3 AND is_nullable = 0)
        THROW 50002, 'Existing password_changed_at column is incompatible.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints AS dc
        INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id
            AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.[User]') AND c.name = N'password_changed_at')
        ALTER TABLE dbo.[User] ADD CONSTRAINT DF_User_password_changed
            DEFAULT (SYSUTCDATETIME()) FOR password_changed_at;
    EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE [dbo].[ChangePassword]
    @id_user      INT,
    @current_hash VARCHAR(255),
    @new_hash     VARCHAR(255),
    @now_utc      DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @status VARCHAR(20) = NULL;

    UPDATE dbo.[User]
    SET password_hash = @new_hash, password_changed_at = @now_utc
    WHERE id_user = @id_user AND password_hash = @current_hash;

    IF @@ROWCOUNT = 1
    BEGIN
        SET @status = ''Changed'';
    END
    ELSE IF EXISTS (SELECT 1 FROM dbo.[User] WHERE id_user = @id_user)
    BEGIN
        SET @status = ''StaleCredentials'';
    END
    ELSE
    BEGIN
        SET @status = ''UserNotFound'';
    END

    SELECT @status AS status;
END';
    EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE [dbo].[ResetPasswordWithVerifiedCode]
    @email             VARCHAR(254),
    @code_hash         VARCHAR(255),
    @new_password_hash VARCHAR(255),
    @now_utc           DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @reset_window_minutes INT = 10;
    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @id_user INT = NULL;
    DECLARE @id_code INT = NULL;

    BEGIN TRANSACTION;

    SELECT @id_user = id_user FROM dbo.[User] WITH (UPDLOCK) WHERE email = @email;

    IF @id_user IS NULL
    BEGIN
        SET @status = ''UserNotFound'';
    END
    ELSE
    BEGIN
        SELECT TOP (1) @id_code = id_code
        FROM dbo.Verification_Code WITH (UPDLOCK)
        WHERE id_user = @id_user
          AND type = ''CODE_PASSWORD_RESET''
          AND state = ''CODE_USED''
          AND code_hash = @code_hash
          AND DATEADD(MINUTE, @reset_window_minutes, generation_date) >= @now_utc
        ORDER BY generation_date DESC, id_code DESC;

        IF @id_code IS NULL
        BEGIN
            SET @status = ''NoVerifiedCode'';
        END
        ELSE
        BEGIN
            UPDATE dbo.[User]
            SET password_hash = @new_password_hash, password_changed_at = @now_utc
            WHERE id_user = @id_user;

            DELETE FROM dbo.Verification_Code
            WHERE id_user = @id_user AND type = ''CODE_PASSWORD_RESET'';

            SET @status = ''Reset'';
        END
    END

    COMMIT TRANSACTION;

    SELECT @status AS status;
END';
    GRANT EXECUTE ON dbo.ChangePassword TO role_tlmg_app;
    GRANT EXECUTE ON dbo.ResetPasswordWithVerifiedCode TO role_tlmg_app;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
