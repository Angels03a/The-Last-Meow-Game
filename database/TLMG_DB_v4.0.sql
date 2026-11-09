/* ============================================================================
   The Last Meow Game — TLMG_DB
   Script de creación v4.0 · Microsoft SQL Server 2022 (nivel de compatibilidad 160)
   ----------------------------------------------------------------------------
   HISTORIAL DE VERSIONES (Estándar §5.3)
     v4.0 · 2026-11-08 · Refactor del esquema y de los permisos (ver abajo).
     v3.1 · (fecha por completar) · Procedimientos sin prefijo sp_.
     v3   · (fecha por completar) · Pending_Registration y 2FA (D-13).
     v2   · (fecha por completar) · Guest_Session, Game y Game_Player desde el
                                    lobby (D-15, D-16).
     v1   · (fecha por completar) · Esquema inicial.

   QUÉ CAMBIA RESPECTO A LA v3.1
     1. Sin rutas locales, sin datos de demostración y sin el trigger
        INSTEAD OF (chocaba con EF6). Los datos viven en database/seed/ (D-59).
     2. Todas las fechas son DATETIME2(3) en UTC (SYSUTCDATETIME).
     3. [User]: correo VARCHAR(254) y nickname VARCHAR(12) (reglas RC), con
        CHECK de longitud. Player: CHECK en todos los contadores.
     4. Message: pertenece a una cuenta (id_user), no a un asiento; texto
        NVARCHAR de 1 a 160 (D-63); retención máxima de 24 h por purga.
     5. Game_Player conserva su fila al salir del lobby: joined_at y left_at.
        Game.mode admite NULL mientras la partida está en el lobby.
     6. Ban_Record con motivo y número de suspensión (D-24).
     7. Friendship: una sola fila por pareja sin importar el sentido (BL-061).
     8. Índices en las claves foráneas y en el ranking; vista vw_Leaderboard.
     9. Procedimientos del 2FA, del chat, del lobby, de moderación y del
        ranking. Cada uno devuelve UNA fila con un código de estado.
    10. Rol role_tlmg_app de mínimo privilegio (D-53). El inicio de sesión y
        el usuario los crea un administrador fuera del repositorio.

   DECISIONES PENDIENTES MARCADAS EN EL TEXTO COMO «PENDIENTE»
     · Guest_Session se conserva sin cambios hasta decidir el invitado.
     · Unicidad de Friendship por pareja (BL-061): aplicada la opción de
       columnas calculadas; confirmar.
     · Permisos directos sobre [User] y Player: temporales (ver §8).

   USO: ejecutar con una cuenta administradora sobre un servidor sin la base.
   Para recrearla: DROP DATABASE TLMG_DB; y volver a ejecutar este script.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
GO

IF DB_ID(N'TLMG_DB') IS NULL
    CREATE DATABASE TLMG_DB;
GO

USE TLMG_DB;
GO

ALTER DATABASE TLMG_DB SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE TLMG_DB SET ARITHABORT ON;
GO

/* Protección: no se ejecuta sobre una base que ya tiene el esquema. */
IF OBJECT_ID(N'dbo.[User]', N'U') IS NOT NULL
BEGIN
    PRINT N'La base TLMG_DB ya contiene el esquema. No se ejecuta nada más.';
    SET NOEXEC ON;
END
GO

/* ============================================================================
   1. TABLAS
   ============================================================================ */

/* Cuenta registrada. El hash tiene el formato versionado v1$sal$hash (D-44). */
CREATE TABLE dbo.[User]
(
    id_user         INT IDENTITY(1,1) NOT NULL,
    email           VARCHAR(254)      NOT NULL,
    password_hash   VARCHAR(255)      NOT NULL,
    nickname        VARCHAR(12)       NOT NULL,
    state           VARCHAR(30)       NOT NULL
        CONSTRAINT DF_User_state DEFAULT ('USER_ACTIVE'),
    profile_image   VARCHAR(255)      NULL,
    last_time       DATETIME2(3)      NULL,
    CONSTRAINT PK_User PRIMARY KEY CLUSTERED (id_user),
    CONSTRAINT UQ_User_email UNIQUE (email),
    CONSTRAINT UQ_User_nickname UNIQUE (nickname),
    CONSTRAINT CK_User_state CHECK (state IN ('USER_ACTIVE', 'USER_SUSPENDED', 'USER_BANNED')),
    CONSTRAINT CK_User_email_length CHECK (LEN(email) >= 5),
    CONSTRAINT CK_User_nickname_length CHECK (LEN(nickname) BETWEEN 3 AND 12)
);
GO

/* Estadísticas permanentes del jugador registrado (alimentan el ranking). */
CREATE TABLE dbo.Player
(
    id_player           INT IDENTITY(1,1) NOT NULL,
    id_user             INT               NOT NULL,
    total_points        INT               NOT NULL CONSTRAINT DF_Player_points DEFAULT (0),
    total_wins          INT               NOT NULL CONSTRAINT DF_Player_wins DEFAULT (0),
    total_games_played  INT               NOT NULL CONSTRAINT DF_Player_games DEFAULT (0),
    CONSTRAINT PK_Player PRIMARY KEY CLUSTERED (id_player),
    CONSTRAINT UQ_Player_user UNIQUE (id_user),
    CONSTRAINT FK_Player_User FOREIGN KEY (id_user) REFERENCES dbo.[User] (id_user),
    CONSTRAINT CK_Player_points_non_negative CHECK (total_points >= 0),
    CONSTRAINT CK_Player_wins_non_negative CHECK (total_wins >= 0),
    CONSTRAINT CK_Player_games_non_negative CHECK (total_games_played >= 0),
    CONSTRAINT CK_Player_wins_within_games CHECK (total_wins <= total_games_played)
);
GO

/* Registro en curso mientras dura el 2FA (D-13). */
CREATE TABLE dbo.Pending_Registration
(
    id_pending      INT IDENTITY(1,1) NOT NULL,
    email           VARCHAR(254)      NOT NULL,
    nickname        VARCHAR(12)       NOT NULL,
    password_hash   VARCHAR(255)      NOT NULL,
    created_at      DATETIME2(3)      NOT NULL CONSTRAINT DF_PendingRegistration_created DEFAULT (SYSUTCDATETIME()),
    expires_at      DATETIME2(3)      NOT NULL,
    CONSTRAINT PK_PendingRegistration PRIMARY KEY CLUSTERED (id_pending),
    CONSTRAINT UQ_PendingRegistration_email UNIQUE (email),
    CONSTRAINT UQ_PendingRegistration_nickname UNIQUE (nickname),
    CONSTRAINT CK_PendingRegistration_expiry CHECK (expires_at > created_at)
);
GO

/* Códigos 2FA: se guardan hasheados y cuelgan de un usuario O de un pendiente. */
CREATE TABLE dbo.Verification_Code
(
    id_code          INT IDENTITY(1,1) NOT NULL,
    id_user          INT               NULL,
    id_pending       INT               NULL,
    code_hash        VARCHAR(255)      NOT NULL,
    type             VARCHAR(30)       NOT NULL,
    state            VARCHAR(30)       NOT NULL CONSTRAINT DF_VerificationCode_state DEFAULT ('CODE_GENERATED'),
    generation_date  DATETIME2(3)      NOT NULL CONSTRAINT DF_VerificationCode_generated DEFAULT (SYSUTCDATETIME()),
    expiration_date  DATETIME2(3)      NOT NULL,
    attempts_failed  INT               NOT NULL CONSTRAINT DF_VerificationCode_attempts DEFAULT (0),
    CONSTRAINT PK_VerificationCode PRIMARY KEY CLUSTERED (id_code),
    CONSTRAINT FK_VerificationCode_User FOREIGN KEY (id_user) REFERENCES dbo.[User] (id_user),
    CONSTRAINT FK_VerificationCode_Pending FOREIGN KEY (id_pending) REFERENCES dbo.Pending_Registration (id_pending),
    CONSTRAINT CK_VerificationCode_attempts CHECK (attempts_failed BETWEEN 0 AND 3),
    CONSTRAINT CK_VerificationCode_state CHECK (state IN ('CODE_GENERATED', 'CODE_USED', 'CODE_EXPIRED')),
    CONSTRAINT CK_VerificationCode_type CHECK (type IN ('CODE_REGISTRATION', 'CODE_LOGIN', 'CODE_PASSWORD_CHANGE', 'CODE_PASSWORD_RESET')),
    CONSTRAINT CK_VerificationCode_one_owner CHECK ((id_user IS NOT NULL AND id_pending IS NULL) OR (id_user IS NULL AND id_pending IS NOT NULL)),
    CONSTRAINT CK_VerificationCode_type_matches_owner CHECK
        ((type = 'CODE_REGISTRATION' AND id_pending IS NOT NULL) OR (type <> 'CODE_REGISTRATION' AND id_user IS NOT NULL))
);
GO

/* Solo cuentas registradas. pair_low y pair_high dan la pareja sin sentido. */
CREATE TABLE dbo.Friendship
(
    id_friendship    INT IDENTITY(1,1) NOT NULL,
    id_user_sender   INT               NOT NULL,
    id_user_receiver INT               NOT NULL,
    state            VARCHAR(30)       NOT NULL,
    request_date     DATETIME2(3)      NOT NULL CONSTRAINT DF_Friendship_request DEFAULT (SYSUTCDATETIME()),
    pair_low  AS (CASE WHEN id_user_sender < id_user_receiver THEN id_user_sender ELSE id_user_receiver END) PERSISTED,
    pair_high AS (CASE WHEN id_user_sender < id_user_receiver THEN id_user_receiver ELSE id_user_sender END) PERSISTED,
    CONSTRAINT PK_Friendship PRIMARY KEY CLUSTERED (id_friendship),
    CONSTRAINT FK_Friendship_Sender FOREIGN KEY (id_user_sender) REFERENCES dbo.[User] (id_user),
    CONSTRAINT FK_Friendship_Receiver FOREIGN KEY (id_user_receiver) REFERENCES dbo.[User] (id_user),
    CONSTRAINT CK_Friendship_not_self CHECK (id_user_sender <> id_user_receiver),
    CONSTRAINT CK_Friendship_state CHECK (state IN ('FRIEND_PENDING', 'FRIEND_ACCEPTED', 'FRIEND_REJECTED')),
    /* PENDIENTE (BL-061): una fila por pareja, sin importar quién la envió. */
    CONSTRAINT UQ_Friendship_pair UNIQUE (pair_low, pair_high)
);
GO

/* El bloqueo es bidireccional en el acceso a salas (Regla 12). */
CREATE TABLE dbo.Block_List
(
    id_block         INT IDENTITY(1,1) NOT NULL,
    id_user_blocker  INT               NOT NULL,
    id_user_blocked  INT               NOT NULL,
    block_date       DATETIME2(3)      NOT NULL CONSTRAINT DF_BlockList_date DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_BlockList PRIMARY KEY CLUSTERED (id_block),
    CONSTRAINT UQ_BlockList_pair UNIQUE (id_user_blocker, id_user_blocked),
    CONSTRAINT FK_BlockList_Blocker FOREIGN KEY (id_user_blocker) REFERENCES dbo.[User] (id_user),
    CONSTRAINT FK_BlockList_Blocked FOREIGN KEY (id_user_blocked) REFERENCES dbo.[User] (id_user),
    CONSTRAINT CK_BlockList_not_self CHECK (id_user_blocker <> id_user_blocked)
);
GO

/* Sanciones (D-24): 3 reportes = suspensión de 3 días; la 5.ª suspensión es permanente. */
CREATE TABLE dbo.Ban_Record
(
    id_ban             INT IDENTITY(1,1) NOT NULL,
    id_user            INT               NOT NULL,
    type               VARCHAR(30)       NOT NULL,
    reason             VARCHAR(40)       NOT NULL,
    suspension_number  INT               NOT NULL,
    ban_start_date     DATETIME2(3)      NOT NULL CONSTRAINT DF_BanRecord_start DEFAULT (SYSUTCDATETIME()),
    ban_ending_date    DATETIME2(3)      NULL,
    CONSTRAINT PK_BanRecord PRIMARY KEY CLUSTERED (id_ban),
    CONSTRAINT FK_BanRecord_User FOREIGN KEY (id_user) REFERENCES dbo.[User] (id_user),
    CONSTRAINT CK_BanRecord_type CHECK (type IN ('BAN_TEMPORARY', 'BAN_PERMANENT')),
    CONSTRAINT CK_BanRecord_reason CHECK (reason IN ('BAN_REASON_REPORTS_THRESHOLD', 'BAN_REASON_SUSPENSION_LIMIT')),
    CONSTRAINT CK_BanRecord_number CHECK (suspension_number >= 1),
    CONSTRAINT CK_BanRecord_dates CHECK
        ((type = 'BAN_PERMANENT' AND ban_ending_date IS NULL)
         OR (type = 'BAN_TEMPORARY' AND ban_ending_date > ban_start_date))
);
GO

/* PENDIENTE (decisión del invitado): se conserva sin cambios de estructura.
   El session_token persistido es un secreto en la base; se revisa con esa decisión. */
CREATE TABLE dbo.Guest_Session
(
    id_guest       INT IDENTITY(1,1) NOT NULL,
    guest_number   INT               NOT NULL,
    display_name   VARCHAR(20)       NOT NULL,
    session_token  VARCHAR(64)       NOT NULL,
    created_at     DATETIME2(3)      NOT NULL CONSTRAINT DF_GuestSession_created DEFAULT (SYSUTCDATETIME()),
    ended_at       DATETIME2(3)      NULL,
    CONSTRAINT PK_GuestSession PRIMARY KEY CLUSTERED (id_guest),
    CONSTRAINT UQ_GuestSession_number UNIQUE (guest_number),
    CONSTRAINT UQ_GuestSession_token UNIQUE (session_token)
);
GO

/* La fila nace al abrir la sala (D-16). La modalidad se fija al iniciar (Regla 5). */
CREATE TABLE dbo.Game
(
    id_game           INT IDENTITY(1,1) NOT NULL,
    state             VARCHAR(30)       NOT NULL CONSTRAINT DF_Game_state DEFAULT ('GAME_LOBBY'),
    mode              VARCHAR(30)       NULL,
    lobby_open_date   DATETIME2(3)      NOT NULL CONSTRAINT DF_Game_lobby_open DEFAULT (SYSUTCDATETIME()),
    game_start_date   DATETIME2(3)      NULL,
    game_ending_date  DATETIME2(3)      NULL,
    CONSTRAINT PK_Game PRIMARY KEY CLUSTERED (id_game),
    CONSTRAINT CK_Game_state CHECK (state IN ('GAME_LOBBY', 'GAME_IN_PROGRESS', 'GAME_FINISHED', 'GAME_ABANDONED')),
    CONSTRAINT CK_Game_mode CHECK (mode IS NULL OR mode IN ('MODE_FOUR_CARDS', 'MODE_SIX_CARDS', 'MODE_EIGHT_CARDS')),
    CONSTRAINT CK_Game_mode_required CHECK (mode IS NOT NULL OR state IN ('GAME_LOBBY', 'GAME_ABANDONED')),
    CONSTRAINT CK_Game_dates CHECK (game_ending_date IS NULL OR game_start_date IS NULL OR game_ending_date >= game_start_date)
);
GO

/* Un asiento: registrado O invitado. La fila se conserva al salir (joined_at / left_at):
   Report y Kicked_Out_Voting dependen de ella. left_at NULL = sigue en la sala;
   quien manda sobre la presencia es la memoria del servidor, no esta columna. */
CREATE TABLE dbo.Game_Player
(
    id_game_player  INT IDENTITY(1,1) NOT NULL,
    id_game         INT               NOT NULL,
    id_player       INT               NULL,
    id_guest        INT               NULL,
    points_earned   INT               NOT NULL CONSTRAINT DF_GamePlayer_points DEFAULT (0),
    final_position  INT               NULL,
    outcome         VARCHAR(30)       NULL,
    joined_at       DATETIME2(3)      NOT NULL CONSTRAINT DF_GamePlayer_joined DEFAULT (SYSUTCDATETIME()),
    left_at         DATETIME2(3)      NULL,
    CONSTRAINT PK_GamePlayer PRIMARY KEY CLUSTERED (id_game_player),
    CONSTRAINT FK_GamePlayer_Game FOREIGN KEY (id_game) REFERENCES dbo.Game (id_game),
    CONSTRAINT FK_GamePlayer_Player FOREIGN KEY (id_player) REFERENCES dbo.Player (id_player),
    CONSTRAINT FK_GamePlayer_Guest FOREIGN KEY (id_guest) REFERENCES dbo.Guest_Session (id_guest),
    CONSTRAINT CK_GamePlayer_one_occupant CHECK
        ((id_player IS NOT NULL AND id_guest IS NULL) OR (id_player IS NULL AND id_guest IS NOT NULL)),
    CONSTRAINT CK_GamePlayer_points_non_negative CHECK (points_earned >= 0),
    CONSTRAINT CK_GamePlayer_position CHECK (final_position IS NULL OR final_position >= 1),
    CONSTRAINT CK_GamePlayer_outcome CHECK
        (outcome IS NULL OR outcome IN ('OUTCOME_WINNER', 'OUTCOME_ELIMINATED', 'OUTCOME_ABANDONED', 'OUTCOME_KICKED')),
    CONSTRAINT CK_GamePlayer_left_after_joined CHECK (left_at IS NULL OR left_at >= joined_at)
);
GO

CREATE TABLE dbo.Report
(
    id_report                INT IDENTITY(1,1) NOT NULL,
    id_game_player_reporter  INT               NOT NULL,
    id_game_player_reported  INT               NOT NULL,
    reason                   VARCHAR(40)       NOT NULL,
    comment                  VARCHAR(300)      NULL,
    report_date              DATETIME2(3)      NOT NULL CONSTRAINT DF_Report_date DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Report PRIMARY KEY CLUSTERED (id_report),
    CONSTRAINT UQ_Report_pair UNIQUE (id_game_player_reporter, id_game_player_reported),
    CONSTRAINT FK_Report_Reporter FOREIGN KEY (id_game_player_reporter) REFERENCES dbo.Game_Player (id_game_player),
    CONSTRAINT FK_Report_Reported FOREIGN KEY (id_game_player_reported) REFERENCES dbo.Game_Player (id_game_player),
    CONSTRAINT CK_Report_not_self CHECK (id_game_player_reporter <> id_game_player_reported),
    CONSTRAINT CK_Report_reason CHECK (reason IN ('REASON_OFFENSIVE_LANG', 'REASON_HARASSMENT', 'REASON_CHEATING',
                                                  'REASON_SPAM', 'REASON_INAPPROPRIATE_NAME', 'REASON_OTHER'))
);
GO

CREATE TABLE dbo.Kicked_Out_Voting
(
    id_vote                  INT IDENTITY(1,1) NOT NULL,
    id_game_player_sender    INT               NOT NULL,
    id_game_player_receiver  INT               NOT NULL,
    vote_date                DATETIME2(3)      NOT NULL CONSTRAINT DF_Vote_date DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_KickedOutVoting PRIMARY KEY CLUSTERED (id_vote),
    CONSTRAINT UQ_Vote_pair UNIQUE (id_game_player_sender, id_game_player_receiver),
    CONSTRAINT FK_Vote_Sender FOREIGN KEY (id_game_player_sender) REFERENCES dbo.Game_Player (id_game_player),
    CONSTRAINT FK_Vote_Receiver FOREIGN KEY (id_game_player_receiver) REFERENCES dbo.Game_Player (id_game_player),
    CONSTRAINT CK_Vote_not_self CHECK (id_game_player_sender <> id_game_player_receiver)
);
GO

/* Chat: retención MÁXIMA de 24 h, solo para reportes. La vista del chat es en memoria.
   Solo cuentas registradas. En el lobby se guarda room_number (informativo, se
   reinicia con el servidor); id_game puede llenarse cuando D-16 se persista. */
CREATE TABLE dbo.Message
(
    id_message    INT IDENTITY(1,1) NOT NULL,
    id_user       INT               NOT NULL,
    scope         VARCHAR(10)       NOT NULL,
    id_game       INT               NULL,
    room_number   INT               NULL,
    text_message  NVARCHAR(160)     NOT NULL,
    sent_at       DATETIME2(3)      NOT NULL CONSTRAINT DF_Message_sent DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Message PRIMARY KEY CLUSTERED (id_message),
    CONSTRAINT FK_Message_User FOREIGN KEY (id_user) REFERENCES dbo.[User] (id_user),
    CONSTRAINT FK_Message_Game FOREIGN KEY (id_game) REFERENCES dbo.Game (id_game),
    CONSTRAINT CK_Message_scope CHECK (scope IN ('LOBBY', 'MATCH')),
    CONSTRAINT CK_Message_length CHECK (LEN(text_message) BETWEEN 1 AND 160),
    CONSTRAINT CK_Message_context CHECK (id_game IS NOT NULL OR room_number IS NOT NULL),
    CONSTRAINT CK_Message_match_needs_game CHECK (scope = 'LOBBY' OR id_game IS NOT NULL)
);
GO

/* ============================================================================
   2. ÍNDICES (claves foráneas, ranking y purgas)
   ============================================================================ */

CREATE UNIQUE NONCLUSTERED INDEX UQ_GamePlayer_game_player ON dbo.Game_Player (id_game, id_player) WHERE id_player IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UQ_GamePlayer_game_guest ON dbo.Game_Player (id_game, id_guest) WHERE id_guest IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_GamePlayer_game ON dbo.Game_Player (id_game);
CREATE NONCLUSTERED INDEX IX_GamePlayer_player ON dbo.Game_Player (id_player) WHERE id_player IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_Game_open ON dbo.Game (state, lobby_open_date) WHERE state IN ('GAME_LOBBY', 'GAME_IN_PROGRESS');

CREATE NONCLUSTERED INDEX IX_Player_points ON dbo.Player (total_points DESC) INCLUDE (id_user, total_wins, total_games_played);

CREATE NONCLUSTERED INDEX IX_Friendship_sender ON dbo.Friendship (id_user_sender, state);
CREATE NONCLUSTERED INDEX IX_Friendship_receiver ON dbo.Friendship (id_user_receiver, state);
CREATE NONCLUSTERED INDEX IX_BlockList_blocked ON dbo.Block_List (id_user_blocked);
CREATE NONCLUSTERED INDEX IX_BanRecord_user ON dbo.Ban_Record (id_user, type, ban_ending_date);

CREATE NONCLUSTERED INDEX IX_Report_reported ON dbo.Report (id_game_player_reported);
CREATE NONCLUSTERED INDEX IX_Vote_receiver ON dbo.Kicked_Out_Voting (id_game_player_receiver);

CREATE NONCLUSTERED INDEX IX_Message_sent_at ON dbo.Message (sent_at, id_message);
CREATE NONCLUSTERED INDEX IX_Message_user ON dbo.Message (id_user, sent_at);
CREATE NONCLUSTERED INDEX IX_Message_game ON dbo.Message (id_game) WHERE id_game IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_PendingRegistration_expires ON dbo.Pending_Registration (expires_at);
CREATE NONCLUSTERED INDEX IX_VerificationCode_pending ON dbo.Verification_Code (id_pending, state);
CREATE NONCLUSTERED INDEX IX_VerificationCode_user ON dbo.Verification_Code (id_user, state, expiration_date);
GO

/* ============================================================================
   3. VISTA DEL RANKING (BL-015, BL-091, BL-103)
   Posición compartida en caso de empate de puntos.
   ============================================================================ */

CREATE VIEW dbo.vw_Leaderboard
AS
SELECT
    RANK() OVER (ORDER BY p.total_points DESC) AS ranking_position,
    u.id_user,
    u.nickname,
    p.total_points,
    p.total_wins,
    p.total_games_played
FROM dbo.Player AS p
INNER JOIN dbo.[User] AS u ON u.id_user = p.id_user;
GO

/* ============================================================================
   4. PROCEDIMIENTOS
   Cada uno devuelve UNA fila con una columna status (más los datos que
   correspondan). Los errores esperados se resuelven como status; lo inesperado
   se relanza con THROW para que el servidor lo registre.
   El servidor toma la hora UTC de los eventos y la pasa como parámetro.
   ============================================================================ */

/* ---- 4.1 Alta de cuenta y 2FA (BL-006, BL-094) ---------------------------- */

/* status: Created | EmailTaken | NicknameTaken */
CREATE PROCEDURE dbo.CreatePendingRegistration
    @email          VARCHAR(254),
    @nickname       VARCHAR(12),
    @password_hash  VARCHAR(255),
    @expires_at_utc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now_utc DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @id_pending INT = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        /* Libera las reservas vencidas de este correo o nickname. */
        DELETE vc
        FROM dbo.Verification_Code AS vc
        INNER JOIN dbo.Pending_Registration AS pr ON pr.id_pending = vc.id_pending
        WHERE pr.expires_at < @now_utc AND (pr.email = @email OR pr.nickname = @nickname);

        DELETE FROM dbo.Pending_Registration
        WHERE expires_at < @now_utc AND (email = @email OR nickname = @nickname);

        IF EXISTS (SELECT 1 FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK) WHERE email = @email)
           OR EXISTS (SELECT 1 FROM dbo.Pending_Registration WITH (UPDLOCK, HOLDLOCK) WHERE email = @email)
        BEGIN
            SET @status = 'EmailTaken';
        END
        ELSE IF EXISTS (SELECT 1 FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK) WHERE nickname = @nickname)
           OR EXISTS (SELECT 1 FROM dbo.Pending_Registration WITH (UPDLOCK, HOLDLOCK) WHERE nickname = @nickname)
        BEGIN
            SET @status = 'NicknameTaken';
        END
        ELSE
        BEGIN
            INSERT INTO dbo.Pending_Registration (email, nickname, password_hash, created_at, expires_at)
            VALUES (@email, @nickname, @password_hash, @now_utc, @expires_at_utc);

            SET @id_pending = SCOPE_IDENTITY();
            SET @status = 'Created';
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;

        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            /* Otra solicitud ganó la carrera por el mismo dato. */
            IF EXISTS (SELECT 1 FROM dbo.[User] WHERE email = @email)
               OR EXISTS (SELECT 1 FROM dbo.Pending_Registration WHERE email = @email)
                SET @status = 'EmailTaken';
            ELSE
                SET @status = 'NicknameTaken';
        END
        ELSE
        BEGIN
            THROW;
        END
    END CATCH

    SELECT @status AS status, @id_pending AS id_pending;
END
GO

/* status: Created | InvalidOwner. Invalida los códigos vigentes del mismo dueño y tipo. */
CREATE PROCEDURE dbo.CreateVerificationCode
    @id_user        INT = NULL,
    @id_pending     INT = NULL,
    @type           VARCHAR(30),
    @code_hash      VARCHAR(255),
    @expiration_utc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @id_code INT = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.Verification_Code
        SET state = 'CODE_EXPIRED'
        WHERE state = 'CODE_GENERATED'
          AND type = @type
          AND ((@id_user IS NOT NULL AND id_user = @id_user)
               OR (@id_pending IS NOT NULL AND id_pending = @id_pending));

        INSERT INTO dbo.Verification_Code (id_user, id_pending, code_hash, type, generation_date, expiration_date)
        VALUES (@id_user, @id_pending, @code_hash, @type, SYSUTCDATETIME(), @expiration_utc);

        SET @id_code = SCOPE_IDENTITY();
        SET @status = 'Created';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;

        IF ERROR_NUMBER() = 547
            SET @status = 'InvalidOwner';
        ELSE
            THROW;
    END CATCH

    SELECT @status AS status, @id_code AS id_code;
END
GO

/* status: Valid | Invalid | TooManyAttempts | Expired | NoActiveCode.
   Compara code_hash por igualdad: el servidor debe calcularlo con el mismo
   algoritmo con que lo guardó (el código de 6 dígitos no lleva sal aleatoria;
   lo protegen el límite de 3 intentos y la vigencia corta). */
CREATE PROCEDURE dbo.ValidateVerificationCode
    @id_user    INT = NULL,
    @id_pending INT = NULL,
    @type       VARCHAR(30),
    @code_hash  VARCHAR(255),
    @now_utc    DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @id_code INT = NULL;
    DECLARE @stored_hash VARCHAR(255);
    DECLARE @attempts INT;
    DECLARE @expiration DATETIME2(3);

    BEGIN TRANSACTION;

    SELECT TOP (1)
        @id_code = id_code, @stored_hash = code_hash,
        @attempts = attempts_failed, @expiration = expiration_date
    FROM dbo.Verification_Code WITH (UPDLOCK, ROWLOCK)
    WHERE state = 'CODE_GENERATED'
      AND type = @type
      AND ((@id_user IS NOT NULL AND id_user = @id_user)
           OR (@id_pending IS NOT NULL AND id_pending = @id_pending))
    ORDER BY generation_date DESC, id_code DESC;

    IF @id_code IS NULL
    BEGIN
        SET @status = 'NoActiveCode';
    END
    ELSE IF @expiration < @now_utc
    BEGIN
        UPDATE dbo.Verification_Code SET state = 'CODE_EXPIRED' WHERE id_code = @id_code;
        SET @status = 'Expired';
    END
    ELSE IF @stored_hash = @code_hash
    BEGIN
        UPDATE dbo.Verification_Code SET state = 'CODE_USED' WHERE id_code = @id_code;
        SET @status = 'Valid';
    END
    ELSE
    BEGIN
        SET @attempts = @attempts + 1;

        IF @attempts >= 3
        BEGIN
            UPDATE dbo.Verification_Code
            SET attempts_failed = @attempts, state = 'CODE_EXPIRED'
            WHERE id_code = @id_code;
            SET @status = 'TooManyAttempts';
        END
        ELSE
        BEGIN
            UPDATE dbo.Verification_Code SET attempts_failed = @attempts WHERE id_code = @id_code;
            SET @status = 'Invalid';
        END
    END

    COMMIT TRANSACTION;

    SELECT @status AS status, @attempts AS attempts_failed;
END
GO

/* status: Created | PendingNotFound | PendingExpired | CodeNotVerified | EmailTaken | NicknameTaken.
   Crea la cuenta real tras superar el 2FA (D-13). */
CREATE PROCEDURE dbo.PromoteRegistration
    @id_pending INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @id_user INT = NULL;
    DECLARE @email VARCHAR(254), @nickname VARCHAR(12), @hash VARCHAR(255), @expires DATETIME2(3);
    DECLARE @now_utc DATETIME2(3) = SYSUTCDATETIME();

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @email = email, @nickname = nickname, @hash = password_hash, @expires = expires_at
        FROM dbo.Pending_Registration WITH (UPDLOCK)
        WHERE id_pending = @id_pending;

        IF @email IS NULL
        BEGIN
            SET @status = 'PendingNotFound';
        END
        ELSE IF @expires < @now_utc
        BEGIN
            SET @status = 'PendingExpired';
        END
        ELSE IF NOT EXISTS (SELECT 1 FROM dbo.Verification_Code
                            WHERE id_pending = @id_pending AND type = 'CODE_REGISTRATION' AND state = 'CODE_USED')
        BEGIN
            SET @status = 'CodeNotVerified';
        END
        ELSE
        BEGIN
            INSERT INTO dbo.[User] (email, password_hash, nickname, state, profile_image, last_time)
            VALUES (@email, @hash, @nickname, 'USER_ACTIVE', '/img/default.png', @now_utc);

            SET @id_user = SCOPE_IDENTITY();

            INSERT INTO dbo.Player (id_user) VALUES (@id_user);

            DELETE FROM dbo.Verification_Code WHERE id_pending = @id_pending;
            DELETE FROM dbo.Pending_Registration WHERE id_pending = @id_pending;

            SET @status = 'Created';
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;

        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            SET @id_user = NULL;
            IF EXISTS (SELECT 1 FROM dbo.[User] WHERE email = @email)
                SET @status = 'EmailTaken';
            ELSE
                SET @status = 'NicknameTaken';
        END
        ELSE
        BEGIN
            THROW;
        END
    END CATCH

    SELECT @status AS status, @id_user AS id_user;
END
GO

/* Borra los registros pendientes vencidos hace más de 10 minutos y sus códigos. */
CREATE PROCEDURE dbo.PurgeExpiredRegistrations
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @cutoff_utc DATETIME2(3) = DATEADD(MINUTE, -10, SYSUTCDATETIME());
    DECLARE @deleted_count INT;

    BEGIN TRANSACTION;

    DELETE vc
    FROM dbo.Verification_Code AS vc
    INNER JOIN dbo.Pending_Registration AS pr ON pr.id_pending = vc.id_pending
    WHERE pr.expires_at < @cutoff_utc;

    DELETE FROM dbo.Pending_Registration WHERE expires_at < @cutoff_utc;
    SET @deleted_count = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT @deleted_count AS deleted_count;
END
GO

/* ---- 4.2 Chat (retención máxima de 24 h) ---------------------------------- */

/* status: Saved | InvalidText | UserNotFound | InvalidContext.
   El servidor ya validó y recortó el texto; aquí se vuelve a verificar. sent_at es la hora
   del servidor de juego (la misma que viaja a los clientes). */
CREATE PROCEDURE dbo.InsertChatMessage
    @id_user      INT,
    @scope        VARCHAR(10),
    @id_game      INT = NULL,
    @room_number  INT = NULL,
    @text_message NVARCHAR(160),
    @sent_at_utc  DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @text NVARCHAR(160) = LTRIM(RTRIM(@text_message));
    DECLARE @id_message INT = NULL;

    IF @text IS NULL OR LEN(@text) NOT BETWEEN 1 AND 160
    BEGIN
        SET @status = 'InvalidText';
    END
    ELSE IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE id_user = @id_user)
    BEGIN
        SET @status = 'UserNotFound';
    END
    ELSE
    BEGIN
        BEGIN TRY
            INSERT INTO dbo.Message (id_user, scope, id_game, room_number, text_message, sent_at)
            VALUES (@id_user, @scope, @id_game, @room_number, @text, @sent_at_utc);

            SET @id_message = SCOPE_IDENTITY();
            SET @status = 'Saved';
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() = 547
                SET @status = 'InvalidContext';
            ELSE
                THROW;
        END CATCH
    END

    SELECT @status AS status, @id_message AS id_message;
END
GO

/* Borra por lotes lo anterior al corte (UTC) que calcula el servicio: ahora menos 24 h. */
CREATE PROCEDURE dbo.PurgeExpiredMessages
    @cutoff_utc DATETIME2(3),
    @batch_size INT = 500
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @deleted_total INT = 0;
    DECLARE @deleted_batch INT = 1;

    IF @batch_size IS NULL OR @batch_size < 1 OR @batch_size > 5000
        SET @batch_size = 500;

    WHILE @deleted_batch > 0
    BEGIN
        DELETE TOP (@batch_size) FROM dbo.Message WHERE sent_at < @cutoff_utc;
        SET @deleted_batch = @@ROWCOUNT;
        SET @deleted_total = @deleted_total + @deleted_batch;
    END

    SELECT @deleted_total AS deleted_count;
END
GO

/* Evidencia para reportes: solo mensajes dentro de la ventana (corte UTC del servicio). */
CREATE PROCEDURE dbo.GetChatEvidence
    @id_user    INT,
    @cutoff_utc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT m.id_message, m.id_user, u.nickname, m.scope, m.id_game, m.room_number, m.text_message, m.sent_at
    FROM dbo.Message AS m
    INNER JOIN dbo.[User] AS u ON u.id_user = m.id_user
    WHERE m.id_user = @id_user AND m.sent_at >= @cutoff_utc
    ORDER BY m.sent_at, m.id_message;
END
GO

/* ---- 4.3 Lobby y partida (D-16) ------------------------------------------- */

/* status: Created | PlayerNotFound. Abre la partida en GAME_LOBBY con el creador adentro. */
CREATE PROCEDURE dbo.CreateLobbyGame
    @id_user   INT,
    @event_utc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @id_game INT = NULL;
    DECLARE @id_player INT = (SELECT id_player FROM dbo.Player WHERE id_user = @id_user);

    IF @id_player IS NULL
    BEGIN
        SET @status = 'PlayerNotFound';
    END
    ELSE
    BEGIN
        BEGIN TRANSACTION;

        INSERT INTO dbo.Game (state, mode, lobby_open_date) VALUES ('GAME_LOBBY', NULL, @event_utc);
        SET @id_game = SCOPE_IDENTITY();

        INSERT INTO dbo.Game_Player (id_game, id_player, joined_at) VALUES (@id_game, @id_player, @event_utc);

        COMMIT TRANSACTION;
        SET @status = 'Created';
    END

    SELECT @status AS status, @id_game AS id_game;
END
GO

/* status: Entered | Reentered | AlreadyInside | StaleEvent | GameNotFound | GameNotOpen | PlayerNotFound.
   Idempotente: un reingreso reutiliza la fila y deja left_at en NULL. */
CREATE PROCEDURE dbo.RecordPlayerEntered
    @id_game   INT,
    @id_user   INT,
    @event_utc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @game_state VARCHAR(30);
    DECLARE @id_game_player INT;
    DECLARE @left_at DATETIME2(3);
    DECLARE @id_player INT = (SELECT id_player FROM dbo.Player WHERE id_user = @id_user);

    IF @id_player IS NULL
    BEGIN
        SET @status = 'PlayerNotFound';
    END
    ELSE
    BEGIN
        BEGIN TRANSACTION;

        SELECT @game_state = state FROM dbo.Game WITH (UPDLOCK) WHERE id_game = @id_game;

        IF @game_state IS NULL
        BEGIN
            SET @status = 'GameNotFound';
        END
        ELSE IF @game_state <> 'GAME_LOBBY'
        BEGIN
            SET @status = 'GameNotOpen';
        END
        ELSE
        BEGIN
            SELECT @id_game_player = id_game_player, @left_at = left_at
            FROM dbo.Game_Player WITH (UPDLOCK)
            WHERE id_game = @id_game AND id_player = @id_player;

            IF @id_game_player IS NULL
            BEGIN
                INSERT INTO dbo.Game_Player (id_game, id_player, joined_at) VALUES (@id_game, @id_player, @event_utc);
                SET @status = 'Entered';
            END
            ELSE IF @left_at IS NULL
            BEGIN
                SET @status = 'AlreadyInside';
            END
            ELSE IF @event_utc >= @left_at
            BEGIN
                UPDATE dbo.Game_Player SET joined_at = @event_utc, left_at = NULL WHERE id_game_player = @id_game_player;
                SET @status = 'Reentered';
            END
            ELSE
            BEGIN
                SET @status = 'StaleEvent';
            END
        END

        COMMIT TRANSACTION;
    END

    SELECT @status AS status;
END
GO

/* status: Left | AlreadyLeft | StaleEvent | NotFound | PlayerNotFound.
   Si el lobby se queda sin nadie, la partida pasa a GAME_ABANDONED. */
CREATE PROCEDURE dbo.RecordPlayerLeft
    @id_game   INT,
    @id_user   INT,
    @event_utc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @game_abandoned BIT = 0;
    DECLARE @game_state VARCHAR(30);
    DECLARE @id_game_player INT;
    DECLARE @joined_at DATETIME2(3);
    DECLARE @left_at DATETIME2(3);
    DECLARE @id_player INT = (SELECT id_player FROM dbo.Player WHERE id_user = @id_user);

    IF @id_player IS NULL
    BEGIN
        SET @status = 'PlayerNotFound';
    END
    ELSE
    BEGIN
        BEGIN TRANSACTION;

        SELECT @game_state = state FROM dbo.Game WITH (UPDLOCK) WHERE id_game = @id_game;

        SELECT @id_game_player = id_game_player, @joined_at = joined_at, @left_at = left_at
        FROM dbo.Game_Player WITH (UPDLOCK)
        WHERE id_game = @id_game AND id_player = @id_player;

        IF @id_game_player IS NULL
        BEGIN
            SET @status = 'NotFound';
        END
        ELSE IF @left_at IS NOT NULL
        BEGIN
            SET @status = 'AlreadyLeft';
        END
        ELSE IF @event_utc < @joined_at
        BEGIN
            SET @status = 'StaleEvent';
        END
        ELSE
        BEGIN
            UPDATE dbo.Game_Player SET left_at = @event_utc WHERE id_game_player = @id_game_player;
            SET @status = 'Left';

            IF @game_state = 'GAME_LOBBY'
               AND NOT EXISTS (SELECT 1 FROM dbo.Game_Player WHERE id_game = @id_game AND left_at IS NULL)
            BEGIN
                UPDATE dbo.Game SET state = 'GAME_ABANDONED', game_ending_date = @event_utc WHERE id_game = @id_game;
                SET @game_abandoned = 1;
            END
        END

        COMMIT TRANSACTION;
    END

    SELECT @status AS status, @game_abandoned AS game_abandoned;
END
GO

/* status: Started | GameNotInLobby | InvalidMode. La modalidad se fija al iniciar (Regla 5). */
CREATE PROCEDURE dbo.MarkGameStarted
    @id_game   INT,
    @mode      VARCHAR(30),
    @event_utc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @status VARCHAR(20) = NULL;

    BEGIN TRY
        UPDATE dbo.Game
        SET state = 'GAME_IN_PROGRESS', mode = @mode, game_start_date = @event_utc
        WHERE id_game = @id_game AND state = 'GAME_LOBBY';

        IF @@ROWCOUNT = 1
            SET @status = 'Started';
        ELSE
            SET @status = 'GameNotInLobby';
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() = 547
            SET @status = 'InvalidMode';
        ELSE
            THROW;
    END CATCH

    SELECT @status AS status;
END
GO

/* Al arrancar el servidor: la memoria se perdió, así que cierra las partidas que quedaron
   abiertas (lobby o en curso) y los asientos pendientes. Devuelve cuántas cerró. */
CREATE PROCEDURE dbo.AbandonStaleLobbies
    @event_utc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @abandoned_count INT;

    BEGIN TRANSACTION;

    UPDATE gp
    SET left_at = CASE WHEN @event_utc >= gp.joined_at THEN @event_utc ELSE gp.joined_at END
    FROM dbo.Game_Player AS gp
    INNER JOIN dbo.Game AS g ON g.id_game = gp.id_game
    WHERE g.state IN ('GAME_LOBBY', 'GAME_IN_PROGRESS') AND gp.left_at IS NULL;

    UPDATE dbo.Game
    SET state = 'GAME_ABANDONED',
        game_ending_date = CASE WHEN game_start_date IS NOT NULL AND game_start_date > @event_utc
                                THEN game_start_date ELSE @event_utc END
    WHERE state IN ('GAME_LOBBY', 'GAME_IN_PROGRESS');
    SET @abandoned_count = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT @abandoned_count AS abandoned_count;
END
GO

/* ---- 4.4 Moderación (D-24) ------------------------------------------------ */

/* status: Suspended | Banned | AlreadyBanned | UserNotFound.
   El servidor decide CUÁNDO sancionar (3 reportes); aquí se registra y se actualiza el estado.
   La suspensión dura 3 días; la 5.ª suspensión es baneo permanente. */
CREATE PROCEDURE dbo.ApplySanction
    @id_user   INT,
    @event_utc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @suspension_days INT = 3;
    DECLARE @permanent_at_number INT = 5;
    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @user_state VARCHAR(30);
    DECLARE @suspension_number INT;

    BEGIN TRANSACTION;

    SELECT @user_state = state FROM dbo.[User] WITH (UPDLOCK) WHERE id_user = @id_user;

    IF @user_state IS NULL
    BEGIN
        SET @status = 'UserNotFound';
    END
    ELSE IF @user_state = 'USER_BANNED'
    BEGIN
        SET @status = 'AlreadyBanned';
    END
    ELSE
    BEGIN
        SELECT @suspension_number = COUNT(*) + 1 FROM dbo.Ban_Record WHERE id_user = @id_user;

        IF @suspension_number >= @permanent_at_number
        BEGIN
            INSERT INTO dbo.Ban_Record (id_user, type, reason, suspension_number, ban_start_date, ban_ending_date)
            VALUES (@id_user, 'BAN_PERMANENT', 'BAN_REASON_SUSPENSION_LIMIT', @suspension_number, @event_utc, NULL);

            UPDATE dbo.[User] SET state = 'USER_BANNED' WHERE id_user = @id_user;
            SET @status = 'Banned';
        END
        ELSE
        BEGIN
            INSERT INTO dbo.Ban_Record (id_user, type, reason, suspension_number, ban_start_date, ban_ending_date)
            VALUES (@id_user, 'BAN_TEMPORARY', 'BAN_REASON_REPORTS_THRESHOLD', @suspension_number,
                    @event_utc, DATEADD(DAY, @suspension_days, @event_utc));

            UPDATE dbo.[User] SET state = 'USER_SUSPENDED' WHERE id_user = @id_user;
            SET @status = 'Suspended';
        END
    END

    COMMIT TRANSACTION;

    SELECT @status AS status, @suspension_number AS suspension_number;
END
GO

/* status: Lifted | StillSuspended | NotSuspended. Se invoca al iniciar sesión. */
CREATE PROCEDURE dbo.LiftExpiredSuspension
    @id_user INT,
    @now_utc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @status VARCHAR(20) = NULL;
    DECLARE @user_state VARCHAR(30);

    BEGIN TRANSACTION;

    SELECT @user_state = state FROM dbo.[User] WITH (UPDLOCK) WHERE id_user = @id_user;

    IF @user_state IS NULL OR @user_state <> 'USER_SUSPENDED'
    BEGIN
        SET @status = 'NotSuspended';
    END
    ELSE IF EXISTS (SELECT 1 FROM dbo.Ban_Record
                    WHERE id_user = @id_user AND type = 'BAN_TEMPORARY' AND ban_ending_date > @now_utc)
    BEGIN
        SET @status = 'StillSuspended';
    END
    ELSE
    BEGIN
        UPDATE dbo.[User] SET state = 'USER_ACTIVE' WHERE id_user = @id_user;
        SET @status = 'Lifted';
    END

    COMMIT TRANSACTION;

    SELECT @status AS status;
END
GO

/* ---- 4.5 Ranking (BL-015, BL-091, BL-103) --------------------------------- */

/* Los primeros N jugadores (máximo 100). */
CREATE PROCEDURE dbo.GetLeaderboard
    @top INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    IF @top IS NULL OR @top < 1 OR @top > 100
        SET @top = 100;

    SELECT TOP (@top) ranking_position, id_user, nickname, total_points, total_wins, total_games_played
    FROM dbo.vw_Leaderboard
    ORDER BY ranking_position, nickname;
END
GO

/* Puntos, victorias y puesto del jugador en sesión (la identidad sale del token, no del cliente). */
CREATE PROCEDURE dbo.GetPlayerStanding
    @id_user INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) + 1 FROM dbo.Player AS better WHERE better.total_points > p.total_points) AS ranking_position,
        p.total_points,
        p.total_wins,
        p.total_games_played
    FROM dbo.Player AS p
    WHERE p.id_user = @id_user;
END
GO

/* ============================================================================
   5. PERMISOS (mínimo privilegio, D-53, Estándar §5.2)
   Este script NO crea inicios de sesión ni contraseñas. Un administrador crea
   la cuenta de la aplicación fuera del repositorio y la agrega al rol:

       -- Ejemplo (NO guardar contraseñas en Git):
       -- CREATE LOGIN [tlmg_app] WITH PASSWORD = N'<definir fuera del repositorio>';
       -- CREATE USER  [tlmg_app] FOR LOGIN [tlmg_app];
       -- ALTER ROLE role_tlmg_app ADD MEMBER [tlmg_app];

   Los procedimientos y las tablas son del mismo propietario (dbo): el
   encadenamiento de propiedad permite que un procedimiento escriba en tablas
   sobre las que el rol no tiene permiso directo.
   ============================================================================ */

CREATE ROLE role_tlmg_app AUTHORIZATION dbo;
GO

/* Procedimientos: la vía normal de escritura. */
GRANT EXECUTE ON dbo.CreatePendingRegistration  TO role_tlmg_app;
GRANT EXECUTE ON dbo.CreateVerificationCode     TO role_tlmg_app;
GRANT EXECUTE ON dbo.ValidateVerificationCode   TO role_tlmg_app;
GRANT EXECUTE ON dbo.PromoteRegistration        TO role_tlmg_app;
GRANT EXECUTE ON dbo.PurgeExpiredRegistrations  TO role_tlmg_app;
GRANT EXECUTE ON dbo.InsertChatMessage          TO role_tlmg_app;
GRANT EXECUTE ON dbo.PurgeExpiredMessages       TO role_tlmg_app;
GRANT EXECUTE ON dbo.GetChatEvidence            TO role_tlmg_app;
GRANT EXECUTE ON dbo.CreateLobbyGame            TO role_tlmg_app;
GRANT EXECUTE ON dbo.RecordPlayerEntered        TO role_tlmg_app;
GRANT EXECUTE ON dbo.RecordPlayerLeft           TO role_tlmg_app;
GRANT EXECUTE ON dbo.MarkGameStarted            TO role_tlmg_app;
GRANT EXECUTE ON dbo.AbandonStaleLobbies        TO role_tlmg_app;
GRANT EXECUTE ON dbo.ApplySanction              TO role_tlmg_app;
GRANT EXECUTE ON dbo.LiftExpiredSuspension      TO role_tlmg_app;
GRANT EXECUTE ON dbo.GetLeaderboard             TO role_tlmg_app;
GRANT EXECUTE ON dbo.GetPlayerStanding          TO role_tlmg_app;

/* PENDIENTE (temporal): lecturas y escrituras directas que hoy hace EF en los
   repositorios de jugador y amistad. Se retiran cuando pasen por procedimientos. */
GRANT SELECT ON dbo.[User]         TO role_tlmg_app;
GRANT SELECT ON dbo.Player         TO role_tlmg_app;
GRANT SELECT ON dbo.Friendship     TO role_tlmg_app;
GRANT SELECT ON dbo.vw_Leaderboard TO role_tlmg_app;
GRANT INSERT ON dbo.[User]         TO role_tlmg_app;
GRANT INSERT ON dbo.Player         TO role_tlmg_app;
GRANT UPDATE (last_time) ON dbo.[User] TO role_tlmg_app;

/* Lo demás se niega de forma explícita: el borrado y los cambios de estructura
   no son de la aplicación, y los secretos solo se tocan por procedimientos. */
DENY DELETE ON SCHEMA::dbo TO role_tlmg_app;
DENY ALTER  ON SCHEMA::dbo TO role_tlmg_app;
DENY SELECT, INSERT, UPDATE ON dbo.Verification_Code    TO role_tlmg_app;
DENY SELECT, INSERT, UPDATE ON dbo.Pending_Registration TO role_tlmg_app;
DENY SELECT, INSERT, UPDATE ON dbo.Guest_Session        TO role_tlmg_app;
DENY SELECT, INSERT, UPDATE ON dbo.Ban_Record           TO role_tlmg_app;
DENY SELECT, INSERT, UPDATE ON dbo.Message              TO role_tlmg_app;
GO

PRINT N'TLMG_DB v4.0 creada.';
GO

SET NOEXEC OFF;
GO
