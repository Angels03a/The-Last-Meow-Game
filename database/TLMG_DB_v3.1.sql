/* ============================================================================
   The Last Meow Game — TLMG_DB
   Script de creación v3.1 · Microsoft SQL Server
   ----------------------------------------------------------------------------
   v3.1 · Los procedimientos pierden el prefijo sp_, reservado en SQL Server
   para procedimientos del sistema: el motor los busca primero en master.
   Se nombran con verbo en PascalCase, coherente con la sección 3.7 del
   Estándar de Codificación (métodos = verbo o frase verbal).

   v3 sustituye a la v2. El cambio de fondo es la adopción de la OPCIÓN B para
   la cadena de registro (decisión D-13): los datos de un registro en curso
   viven en su propia tabla mientras dura el 2FA, y sólo al superarlo se crea
   la cuenta real.

   QUÉ CAMBIA RESPECTO A LA v2
     1. Nueva tabla Pending_Registration. Reserva correo y nickname mientras
        dura el 2FA y guarda el hash de la contraseña fuera de [User].
     2. Verification_Code vuelve a no tener columna 'email'. Ahora apunta O a
        un User O a un registro pendiente, con CHECK de exclusividad. Así CU-01
        no ramifica según quién lo invoque: siempre escribe una fila y el
        llamador decide cuál vínculo rellena.
     3. Los códigos se guardan HASHEADOS, no en claro.
     4. Se incluye la transacción de alta de cuenta y la sentencia de purga.

   SE MANTIENE DE LA v2
     · Guest_Session y asientos que admiten registrado o invitado (D-15).
     · Game y Game_Player desde la apertura del lobby (D-16).
     · Restricciones UNIQUE que los casos de uso exigían.
     · Tipos alineados con el Diccionario de Reglas de Validación.
     · Ban_Record sin total_bans, CHECK de estados, id_game_player.
   ============================================================================ */

IF DB_ID('TLMG_DB') IS NULL
    CREATE DATABASE TLMG_DB;
GO

USE TLMG_DB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---------- Limpieza en orden inverso de dependencias ---------- */
DROP TABLE IF EXISTS Kicked_Out_Voting;
DROP TABLE IF EXISTS Report;
DROP TABLE IF EXISTS Message;
DROP TABLE IF EXISTS Game_Player;
DROP TABLE IF EXISTS Game;
DROP TABLE IF EXISTS Guest_Session;
DROP TABLE IF EXISTS Friendship;
DROP TABLE IF EXISTS Block_List;
DROP TABLE IF EXISTS Player;
DROP TABLE IF EXISTS Ban_Record;
DROP TABLE IF EXISTS Verification_Code;
DROP TABLE IF EXISTS Pending_Registration;
DROP TABLE IF EXISTS [User];
GO


/* ============================================================================
   1. User
   ----------------------------------------------------------------------------
   Contiene EXCLUSIVAMENTE cuentas reales, activas y verificadas. Por decisión
   D-13 una fila aquí no existe hasta que el 2FA fue superado, así que no hay
   estado 'pendiente' ni filas a medias que haya que filtrar en cada consulta.
   ============================================================================ */
CREATE TABLE [User] (
    id_user         INT IDENTITY(1,1) PRIMARY KEY,
    email           VARCHAR(100)  NOT NULL UNIQUE,
    password_hash   VARCHAR(255)  NOT NULL,
    nickname        VARCHAR(20)   NOT NULL UNIQUE,
    state           VARCHAR(30)   NOT NULL,
    profile_image   VARCHAR(255)  NULL,
    last_time       DATETIME      NULL,
    CONSTRAINT CK_User_state CHECK (state IN (
        'USER_ACTIVE', 'USER_SUSPENDED', 'USER_BANNED'))
);
GO


/* ============================================================================
   2. Pending_Registration  — TABLA NUEVA (Opción B de ID-35)
   ----------------------------------------------------------------------------
   Vive aquí un registro en curso, entre el momento en que el jugador envía el
   formulario y el momento en que supera el 2FA.

   Por qué existe:
     · UNIQUE sobre email y nickname actúa como RESERVA TEMPORAL. Sin ella, dos
       personas pueden llegar al 2FA con el mismo correo y la colisión estalla
       hasta el final: CU-02 EX-02 dejaría de ser un caso raro para volverse el
       camino habitual. Con la reserva, el segundo choca al empezar.
     · El hash de la contraseña de alguien que nunca completó el registro no se
       queda para siempre en [User]: muere con la purga.
     · [User] no necesita conocer estados intermedios.

   Esta tabla es efímera por diseño: toda fila debe desaparecer al promoverse a
   cuenta real o al expirar. Ver la sección de PURGA al final.
   ============================================================================ */
CREATE TABLE Pending_Registration (
    id_pending       INT IDENTITY(1,1) PRIMARY KEY,
    email            VARCHAR(100)  NOT NULL UNIQUE,
    nickname         VARCHAR(20)   NOT NULL UNIQUE,
    password_hash    VARCHAR(255)  NOT NULL,
    created_at       DATETIME      NOT NULL,
    expires_at       DATETIME      NOT NULL
);
GO

CREATE INDEX IX_PendingRegistration_expires ON Pending_Registration (expires_at);
GO

CREATE OR ALTER TRIGGER TR_PendingRegistration_CheckExistingUser
ON Pending_Registration
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    /* Impide reservar correo o nickname que ya pertenezcan a una cuenta real activa */
    IF EXISTS (
        SELECT 1 
        FROM inserted i
        JOIN [User] u ON u.email = i.email OR u.nickname = i.nickname
    )
    BEGIN
        THROW 50003, 'El correo o nickname ya se encuentra registrado por una cuenta activa.', 1;
    END

    /* Inserta en Pending_Registration respetando sus restricciones UNIQUE */
    INSERT INTO Pending_Registration (email, nickname, password_hash, created_at, expires_at)
    SELECT email, nickname, password_hash, created_at, expires_at
    FROM inserted;
END;
GO


/* ============================================================================
   3. Verification_Code
   ----------------------------------------------------------------------------
   CAMBIO RESPECTO A LA v2 · Se elimina la columna 'email' y se añade
   'id_pending'. Una fila apunta O a un usuario existente O a un registro
   pendiente, nunca a ambos ni a ninguno.

   Esto mantiene CU-01 como un flujo único: siempre inserta aquí, y el caso de
   uso que lo invoca decide qué vínculo rellenar.
     · CU-02 (registro)                 → id_pending
     · CU-03, CU-05, CU-06              → id_user

   CAMBIO · 'code' pasa a 'code_hash'. El código de 6 dígitos se hashea en la
   aplicación antes de persistirse. Con 6 dígitos y 3 intentos el riesgo real
   es bajo, pero guardarlo en claro significa que quien lea la base ve todos
   los códigos vivos del sistema.
   ============================================================================ */
CREATE TABLE Verification_Code (
    id_code          INT IDENTITY(1,1) PRIMARY KEY,
    id_user          INT           NULL,
    id_pending       INT           NULL,
    code_hash        VARCHAR(255)  NOT NULL,
    type             VARCHAR(30)   NOT NULL,
    state            VARCHAR(30)   NOT NULL,
    generation_date  DATETIME      NOT NULL,
    expiration_date  DATETIME      NOT NULL,
    attempts_failed  INT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_VerificationCode_User
        FOREIGN KEY (id_user)    REFERENCES [User](id_user),
    CONSTRAINT FK_VerificationCode_Pending
        FOREIGN KEY (id_pending) REFERENCES Pending_Registration(id_pending),
    /* Exactamente un titular del código. */
    CONSTRAINT CK_VerificationCode_one_owner CHECK (
        (id_user IS NOT NULL AND id_pending IS NULL) OR
        (id_user IS NULL AND id_pending IS NOT NULL)),
    /* Un código de registro sólo puede colgar de un pendiente, y viceversa. */
    CONSTRAINT CK_VerificationCode_type_matches_owner CHECK (
        (type = 'CODE_REGISTRATION' AND id_pending IS NOT NULL) OR
        (type <> 'CODE_REGISTRATION' AND id_user IS NOT NULL)),
    CONSTRAINT CK_VerificationCode_type CHECK (type IN (
        'CODE_REGISTRATION', 'CODE_LOGIN',
        'CODE_PASSWORD_CHANGE', 'CODE_PASSWORD_RESET')),
    CONSTRAINT CK_VerificationCode_state CHECK (state IN (
        'CODE_GENERATED', 'CODE_USED', 'CODE_EXPIRED')),
    CONSTRAINT CK_VerificationCode_attempts CHECK (attempts_failed BETWEEN 0 AND 3)
);
GO

CREATE INDEX IX_VerificationCode_pending ON Verification_Code (id_pending, state);
CREATE INDEX IX_VerificationCode_user    ON Verification_Code (id_user, state, expiration_date);
GO


/* ============================================================================
   4. Ban_Record
   ----------------------------------------------------------------------------
   Sin 'total_bans' (era un contador acumulado dentro de la propia tabla de
   historial). El criterio de baneo permanente se evalúa contando:
       SELECT COUNT(*) FROM Ban_Record
       WHERE id_user = @id AND type = 'BAN_TEMPORARY';
   ============================================================================ */
CREATE TABLE Ban_Record (
    id_ban           INT IDENTITY(1,1) PRIMARY KEY,
    id_user          INT          NOT NULL,
    type             VARCHAR(30)  NOT NULL,
    ban_start_date   DATETIME     NOT NULL,
    ban_ending_date  DATETIME     NULL,
    CONSTRAINT FK_BanRecord_User
        FOREIGN KEY (id_user) REFERENCES [User](id_user),
    CONSTRAINT CK_BanRecord_type CHECK (type IN (
        'BAN_TEMPORARY', 'BAN_PERMANENT'))
);
GO


/* ============================================================================
   5. Block_List
   ============================================================================ */
CREATE TABLE Block_List (
    id_block          INT IDENTITY(1,1) PRIMARY KEY,
    id_user_blocker   INT       NOT NULL,
    id_user_blocked   INT       NOT NULL,
    block_date        DATETIME  NOT NULL,
    CONSTRAINT FK_BlockList_Blocker
        FOREIGN KEY (id_user_blocker) REFERENCES [User](id_user),
    CONSTRAINT FK_BlockList_Blocked
        FOREIGN KEY (id_user_blocked) REFERENCES [User](id_user),
    CONSTRAINT UQ_BlockList_pair UNIQUE (id_user_blocker, id_user_blocked),
    CONSTRAINT CK_BlockList_not_self CHECK (id_user_blocker <> id_user_blocked)
);
GO


/* ============================================================================
   6. Friendship
   ----------------------------------------------------------------------------
   NOTA (D-17) · Sólo registra amistades entre jugadores REGISTRADOS. La lista
   de amigos de un invitado vive únicamente en el estado de sesión del servidor
   y no se escribe aquí: un invitado no persiste, así que su amistad tampoco.
   Un registrado le envía la solicitud por su nickname generado, el invitado la
   acepta en memoria, y con eso ya puede ser invitado a la sala (Restricción
   No. 14). Al cerrarse la sesión desaparece sin dejar rastro.
   ============================================================================ */
CREATE TABLE Friendship (
    id_friendship      INT IDENTITY(1,1) PRIMARY KEY,
    id_user_sender     INT          NOT NULL,
    id_user_receiver   INT          NOT NULL,
    state              VARCHAR(30)  NOT NULL,
    request_date       DATETIME     NOT NULL,
    CONSTRAINT FK_Friendship_Sender
        FOREIGN KEY (id_user_sender)   REFERENCES [User](id_user),
    CONSTRAINT FK_Friendship_Receiver
        FOREIGN KEY (id_user_receiver) REFERENCES [User](id_user),
    CONSTRAINT UQ_Friendship_pair UNIQUE (id_user_sender, id_user_receiver),
    CONSTRAINT CK_Friendship_not_self CHECK (id_user_sender <> id_user_receiver),
    CONSTRAINT CK_Friendship_state CHECK (state IN (
        'FRIEND_PENDING', 'FRIEND_ACCEPTED', 'FRIEND_REJECTED'))
);
GO


/* ============================================================================
   7. Player
   ----------------------------------------------------------------------------
   DESNORMALIZACIÓN DELIBERADA · total_points, total_wins y total_games_played
   son derivables de Game_Player. Se conservan por rendimiento del leaderboard.
   Debe declararse así en la §8 del documento, en lugar de afirmar 3FN pura.
   ============================================================================ */
CREATE TABLE Player (
    id_player            INT IDENTITY(1,1) PRIMARY KEY,
    id_user              INT  NOT NULL UNIQUE,
    total_points         INT  NOT NULL DEFAULT 0,
    total_wins           INT  NOT NULL DEFAULT 0,
    total_games_played   INT  NOT NULL DEFAULT 0,
    CONSTRAINT FK_Player_User
        FOREIGN KEY (id_user) REFERENCES [User](id_user),
    CONSTRAINT CK_Player_points_non_negative CHECK (total_points >= 0)
);
GO


/* ============================================================================
   8. Guest_Session  (D-15)
   ----------------------------------------------------------------------------
   Identidad del invitado SÓLO durante su sesión: nombre generado y número.
   Sin correo, sin contraseña, sin perfil editable, sin acumulados.
   ============================================================================ */
CREATE TABLE Guest_Session (
    id_guest        INT IDENTITY(1,1) PRIMARY KEY,
    guest_number    INT          NOT NULL UNIQUE,
    display_name    VARCHAR(20)  NOT NULL,
    session_token   VARCHAR(64)  NOT NULL UNIQUE,
    created_at      DATETIME     NOT NULL,
    ended_at        DATETIME     NULL
);
GO


/* ============================================================================
   9. Game  (D-16)
   ----------------------------------------------------------------------------
   La fila nace al ABRIR EL LOBBY, no al iniciar la partida. Es lo que permite
   reportar y votar expulsión en el lobby (CU-09 PRE-01, Restricción No. 10)
   sin tocar las claves foráneas de Report ni Kicked_Out_Voting.
   ============================================================================ */
CREATE TABLE Game (
    id_game           INT IDENTITY(1,1) PRIMARY KEY,
    state             VARCHAR(30)  NOT NULL,
    mode              VARCHAR(30)  NOT NULL,
    lobby_open_date   DATETIME     NOT NULL,
    game_start_date   DATETIME     NULL,
    game_ending_date  DATETIME     NULL,
    CONSTRAINT CK_Game_state CHECK (state IN (
        'GAME_LOBBY', 'GAME_IN_PROGRESS', 'GAME_FINISHED', 'GAME_ABANDONED')),
    CONSTRAINT CK_Game_mode CHECK (mode IN (
        'MODE_FOUR_CARDS', 'MODE_SIX_CARDS', 'MODE_EIGHT_CARDS'))
);
GO


/* ============================================================================
   10. Game_Player
   ----------------------------------------------------------------------------
   El asiento lo ocupa un Player registrado O un invitado (D-15).
   points_earned y final_position son lo único que se persiste del desarrollo
   de la partida: rondas y retos viven en memoria (D-08).
   ============================================================================ */
CREATE TABLE Game_Player (
    id_game_player   INT IDENTITY(1,1) PRIMARY KEY,
    id_game          INT  NOT NULL,
    id_player        INT  NULL,
    id_guest         INT  NULL,
    points_earned    INT  NOT NULL DEFAULT 0,
    final_position   INT  NULL,
    outcome          VARCHAR(30) NULL,
    CONSTRAINT FK_GamePlayer_Game
        FOREIGN KEY (id_game)   REFERENCES Game(id_game),
    CONSTRAINT FK_GamePlayer_Player
        FOREIGN KEY (id_player) REFERENCES Player(id_player),
    CONSTRAINT FK_GamePlayer_Guest
        FOREIGN KEY (id_guest)  REFERENCES Guest_Session(id_guest),
    CONSTRAINT CK_GamePlayer_one_occupant CHECK (
        (id_player IS NOT NULL AND id_guest IS NULL) OR
        (id_player IS NULL AND id_guest IS NOT NULL)),
    CONSTRAINT CK_GamePlayer_points_non_negative CHECK (points_earned >= 0),
    CONSTRAINT CK_GamePlayer_outcome CHECK (outcome IS NULL OR outcome IN (
        'OUTCOME_WINNER', 'OUTCOME_ELIMINATED',
        'OUTCOME_ABANDONED', 'OUTCOME_KICKED'))
);
GO

CREATE UNIQUE INDEX UQ_GamePlayer_game_player
    ON Game_Player (id_game, id_player) WHERE id_player IS NOT NULL;
CREATE UNIQUE INDEX UQ_GamePlayer_game_guest
    ON Game_Player (id_game, id_guest)  WHERE id_guest IS NOT NULL;
GO


/* ============================================================================
   11. Message
   ----------------------------------------------------------------------------
   'scope' distingue chat de lobby y de partida (RF-44). text_message se ajusta
   al máximo de RC-M-03 (160) y al mínimo de RC-M-02 (2).
   El índice sobre message_date sostiene el purgado de RF-45; la política sigue
   abierta en Q-06.
   ============================================================================ */
CREATE TABLE Message (
    id_message       INT IDENTITY(1,1) PRIMARY KEY,
    id_game_player   INT           NOT NULL,
    scope            VARCHAR(10)   NOT NULL,
    text_message     VARCHAR(160)  NOT NULL,
    message_date     DATETIME      NOT NULL,
    CONSTRAINT FK_Message_GamePlayer
        FOREIGN KEY (id_game_player) REFERENCES Game_Player(id_game_player),
    CONSTRAINT CK_Message_scope CHECK (scope IN ('LOBBY', 'MATCH')),
    CONSTRAINT CK_Message_min_length CHECK (LEN(text_message) >= 2)
);
GO

CREATE INDEX IX_Message_date ON Message (message_date);
GO


/* ============================================================================
   12. Report   (D-09: un reporte SÍ cuenta hacia las 3 faltas)
   ============================================================================ */
CREATE TABLE Report (
    id_report                INT IDENTITY(1,1) PRIMARY KEY,
    id_game_player_reporter  INT           NOT NULL,
    id_game_player_reported  INT           NOT NULL,
    reason                   VARCHAR(40)   NOT NULL,
    comment                  VARCHAR(300)  NULL,
    report_date              DATETIME      NOT NULL,
    CONSTRAINT FK_Report_Reporter
        FOREIGN KEY (id_game_player_reporter) REFERENCES Game_Player(id_game_player),
    CONSTRAINT FK_Report_Reported
        FOREIGN KEY (id_game_player_reported) REFERENCES Game_Player(id_game_player),
    CONSTRAINT CK_Report_not_self
        CHECK (id_game_player_reporter <> id_game_player_reported),
    CONSTRAINT CK_Report_reason CHECK (reason IN (
        'REASON_OFFENSIVE_LANG', 'REASON_HARASSMENT', 'REASON_CHEATING',
        'REASON_SPAM', 'REASON_INAPPROPRIATE_NAME', 'REASON_OTHER')),
    CONSTRAINT UQ_Report_pair UNIQUE (id_game_player_reporter, id_game_player_reported)
);
GO


/* ============================================================================
   13. Kicked_Out_Voting
   ----------------------------------------------------------------------------
   D-09 · Un voto de expulsión NO alimenta el contador de reportes y no puede
   producir una suspensión por sí mismo. Por eso vive en su propia tabla y
   nunca escribe en Report (RF-55).
   ============================================================================ */
CREATE TABLE Kicked_Out_Voting (
    id_vote                 INT IDENTITY(1,1) PRIMARY KEY,
    id_game_player_sender   INT       NOT NULL,
    id_game_player_receiver INT       NOT NULL,
    vote_date               DATETIME  NOT NULL,
    CONSTRAINT FK_Vote_Sender
        FOREIGN KEY (id_game_player_sender)   REFERENCES Game_Player(id_game_player),
    CONSTRAINT FK_Vote_Receiver
        FOREIGN KEY (id_game_player_receiver) REFERENCES Game_Player(id_game_player),
    CONSTRAINT CK_Vote_not_self
        CHECK (id_game_player_sender <> id_game_player_receiver),
    CONSTRAINT UQ_Vote_pair UNIQUE (id_game_player_sender, id_game_player_receiver)
);
GO


/* ============================================================================
   ALTA DE CUENTA — transacción atómica
   ----------------------------------------------------------------------------
   Esto es lo que ocurre en el paso final de CU-02, cuando CU-01 marcó el
   código como usado. Las tres operaciones van juntas o no va ninguna: es
   exactamente el escenario que CU-02 EX-02 describe.

   Nota sobre el aislamiento: SERIALIZABLE protege de que otra sesión inserte
   el mismo correo entre la comprobación y la escritura. Con la reserva de
   Pending_Registration esa ventana ya es muy estrecha, pero el UNIQUE de
   [User] es la última línea de defensa y hay que capturar su violación.
   ============================================================================ */
GO
CREATE OR ALTER PROCEDURE PromoteRegistration
    @id_pending INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @email VARCHAR(100), @nickname VARCHAR(20), @hash VARCHAR(255);

        SELECT @email = email, @nickname = nickname, @hash = password_hash
        FROM Pending_Registration
        WHERE id_pending = @id_pending;

        IF @email IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50001, 'El registro pendiente no existe o ya expiró.', 1;
        END

        /* Verifica que el 2FA quedó efectivamente consumido. */
        IF NOT EXISTS (SELECT 1 FROM Verification_Code
                       WHERE id_pending = @id_pending
                         AND type  = 'CODE_REGISTRATION'
                         AND state = 'CODE_USED')
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50002, 'No existe un codigo de verificacion consumido para este registro.', 1;
        END

        INSERT INTO [User] (email, password_hash, nickname, state, profile_image, last_time)
        VALUES (@email, @hash, @nickname, 'USER_ACTIVE', '/img/default.png', GETDATE());

        DECLARE @id_user INT = SCOPE_IDENTITY();

        INSERT INTO Player (id_user) VALUES (@id_user);

        /* Los códigos cuelgan del pendiente: se borran antes que él. */
        DELETE FROM Verification_Code   WHERE id_pending = @id_pending;
        DELETE FROM Pending_Registration WHERE id_pending = @id_pending;

        COMMIT TRANSACTION;
        SELECT @id_user AS id_user;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;   /* El error 2627 (violación de UNIQUE) sube como CU-02 EX-02. */
    END CATCH
END
GO


/* ============================================================================
   PURGA — registros pendientes caducados
   ----------------------------------------------------------------------------
   Pending_Registration es efímera por diseño. Si no se purga, las reservas de
   correo y nickname de registros abandonados bloquean esos valores para
   siempre, que es justo lo contrario de lo que queremos.

   Ejecutar como job periódico, o de forma perezosa antes de insertar un
   registro nuevo. El margen de 10 minutos sobre la expiración del código (5
   min) deja hueco para el último reenvío.
   ============================================================================ */
GO
CREATE OR ALTER PROCEDURE PurgeExpiredRegistrations
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    DELETE vc
    FROM Verification_Code vc
    JOIN Pending_Registration pr ON pr.id_pending = vc.id_pending
    WHERE pr.expires_at < DATEADD(minute, -10, GETDATE());

    DELETE FROM Pending_Registration
    WHERE expires_at < DATEADD(minute, -10, GETDATE());

    COMMIT TRANSACTION;
END
GO


/* ============================================================================
   DATOS DE PRUEBA
   ----------------------------------------------------------------------------
   PENDIENTE DE DECISIÓN (ID-47) · Los nicknames 'ls_angell' y 'Armas0612s'
   contienen guion bajo, que RC-N-02 rechaza hoy. Como 'ls_angell' aparece en
   cuatro escenarios de calidad del Hito1, la vía barata es ampliar RC-N-02 a
   "sólo letras, números y guion bajo". Estos datos asumen esa ampliación, que
   sigue marcada como [PROPUESTO] en el diccionario de validaciones.

   NOTA · Los valores de code_hash son ilustrativos. El hash se calcula en la
   aplicación; el motor nunca ve el código de 6 dígitos en claro.
   ============================================================================ */
USE TLMG_DB;
GO

/* --- Registro en curso: el jugador envió el formulario y espera su código --- */
INSERT INTO Pending_Registration (email, nickname, password_hash, created_at, expires_at)
VALUES ('nuevo.jugador@gmail.com', 'MichiNuevo', 'hash_password_Nuev0Michi!',
        GETDATE(), DATEADD(minute, 5, GETDATE()));
GO

INSERT INTO Verification_Code (id_pending, code_hash, type, state, generation_date, expiration_date)
VALUES (1, 'sha256:5e88489...ilustrativo', 'CODE_REGISTRATION', 'CODE_GENERATED',
        GETDATE(), DATEADD(minute, 5, GETDATE()));
GO

/* --- Cuentas ya verificadas --- */
INSERT INTO [User] (email, password_hash, nickname, state, profile_image, last_time)
VALUES
('armas.mns0612@gmail.com',    'hash_password_100LQ0612b!', 'Armas0612s', 'USER_ACTIVE',    '/img/nicolearmas.png', GETDATE()),
('juarfernanda1830@gmail.com', 'hash_password_nose',        'luxjuar',    'USER_ACTIVE',    '/img/luzjuarez.png',   GETDATE()),
('abdiesitouri@gmail.com',     'hash_password_Uri4dbi._',   'abdiesito',  'USER_BANNED',    '/img/default.png',     GETDATE()),
('ls_ange11@gmail.com',        'hash_password_ls_ange11!',  'ls_angell',  'USER_SUSPENDED', '/img/cat.png',         GETDATE());
GO

INSERT INTO Friendship (id_user_sender, id_user_receiver, state, request_date)
VALUES (1, 2, 'FRIEND_ACCEPTED', GETDATE());

INSERT INTO Block_List (id_user_blocker, id_user_blocked, block_date)
VALUES (1, 3, GETDATE());

INSERT INTO Player (id_user, total_points, total_wins, total_games_played)
VALUES (1, 1500, 1, 4), (2, 1000, 0, 4), (3, 0, 0, 4), (4, 0, 0, 4);
GO

/* Un invitado en sesión: nombre generado, sin correo ni perfil. */
INSERT INTO Guest_Session (guest_number, display_name, session_token, created_at)
VALUES (12, 'Jugador 12', 'tok_9f3a1c7e5b204d88a1e6c0f4b7d239ae', GETDATE());
GO

/* La sala se abre: Game nace en GAME_LOBBY, sin fecha de inicio. */
INSERT INTO Game (state, mode, lobby_open_date, game_start_date, game_ending_date)
VALUES ('GAME_LOBBY', 'MODE_EIGHT_CARDS', GETDATE(), NULL, NULL);
GO

/* Tres registrados y un invitado ocupan asiento ya en el lobby. */
INSERT INTO Game_Player (id_game, id_player, id_guest, points_earned, final_position, outcome)
VALUES
(1, 1,    NULL, 0, NULL, NULL),
(1, 2,    NULL, 0, NULL, NULL),
(1, 3,    NULL, 0, NULL, NULL),
(1, NULL, 1,    0, NULL, NULL);
GO

/* Chat de lobby: posible porque Game_Player ya existe antes de iniciar. */
INSERT INTO Message (id_game_player, scope, text_message, message_date)
VALUES
(1, 'LOBBY', N'¡Buena suerte a todos!',   DATEADD(minute, -5, GETDATE())),
(2, 'LOBBY', N'Gracias, igualmente',      DATEADD(minute, -4, GETDATE())),
(3, 'LOBBY', N'Mensaje ofensivo o spam',  DATEADD(minute, -3, GETDATE()));
GO

/* Reporte y voto emitidos en el LOBBY — imposibles de representar en la v1. */
INSERT INTO Report (id_game_player_reporter, id_game_player_reported, reason, comment, report_date)
VALUES (1, 3, 'REASON_OFFENSIVE_LANG', N'El jugador insultó en el chat', GETDATE());

INSERT INTO Kicked_Out_Voting (id_game_player_sender, id_game_player_receiver, vote_date)
VALUES (2, 3, GETDATE());
GO


/* ============================================================================
   COMPROBACIONES
   ============================================================================ */

/* 1. El registro pendiente NO aparece como cuenta. [User] sigue limpio. */
SELECT 'Cuentas reales' AS tabla, COUNT(*) AS filas FROM [User]
UNION ALL
SELECT 'Registros en curso', COUNT(*) FROM Pending_Registration;
GO

/* 2. Promoción del registro pendiente a cuenta real. */
UPDATE Verification_Code SET state = 'CODE_USED'
WHERE id_pending = 1 AND type = 'CODE_REGISTRATION';

EXEC PromoteRegistration @id_pending = 1;
GO

/* 3. Tras promover: la cuenta existe y el pendiente desapareció. */
SELECT nickname, email, state FROM [User] WHERE nickname = 'MichiNuevo';
SELECT COUNT(*) AS pendientes_restantes FROM Pending_Registration;
GO

/* 4. La reserva funciona: este INSERT debe fallar porque el correo ya existe en [User]. */
BEGIN TRY
    INSERT INTO Pending_Registration (email, nickname, password_hash, created_at, expires_at)
    VALUES ('armas.mns0612@gmail.com', 'OtroNombre', 'hash', GETDATE(), DATEADD(minute,5,GETDATE()));
    SELECT 'ERROR: la reserva no impidio el duplicado' AS resultado;
END TRY
BEGIN CATCH
    SELECT 'OK: la reserva impidio el correo duplicado contra [User]' AS resultado, ERROR_NUMBER() AS numero, ERROR_MESSAGE() AS mensaje;
END CATCH
GO

/* 4.b La reserva funciona: este INSERT debe fallar por UNIQUE entre registros pendientes simultáneos. */
BEGIN TRY
    INSERT INTO Pending_Registration (email, nickname, password_hash, created_at, expires_at)
    VALUES ('temporal.reserva@gmail.com', 'MichiTemp1', 'hash', GETDATE(), DATEADD(minute,5,GETDATE()));

    INSERT INTO Pending_Registration (email, nickname, password_hash, created_at, expires_at)
    VALUES ('temporal.reserva@gmail.com', 'MichiTemp2', 'hash', GETDATE(), DATEADD(minute,5,GETDATE()));

    SELECT 'ERROR: se permitieron dos registros simultaneos con el mismo correo' AS resultado;
END TRY
BEGIN CATCH
    SELECT 'OK: la reserva impidio correo duplicado entre pendientes' AS resultado, ERROR_NUMBER() AS numero;
END CATCH
DELETE FROM Pending_Registration WHERE email = 'temporal.reserva@gmail.com';
GO

/* 5. Resultado de la partida. El invitado gana, pero con 0 puntos (D-14). */
UPDATE Game SET state = 'GAME_FINISHED',
                game_start_date  = DATEADD(minute,-2,GETDATE()),
                game_ending_date = GETDATE()
WHERE id_game = 1;

UPDATE Game_Player SET final_position = 1, points_earned = 0, outcome = 'OUTCOME_WINNER'
WHERE id_game_player = 4;   -- invitado: ganó, no acumula
UPDATE Game_Player SET final_position = 2, points_earned = 4, outcome = 'OUTCOME_ELIMINATED'
WHERE id_game_player = 1;   -- 4 jugadores: base 3; 2.o = (3-1) + bono 2 (8 cartas) = 4
UPDATE Game_Player SET final_position = 3, points_earned = 3, outcome = 'OUTCOME_ELIMINATED'
WHERE id_game_player = 2;   -- 3.er = (3-2) + bono 2 = 3
UPDATE Game_Player SET final_position = 4, points_earned = 0, outcome = 'OUTCOME_ABANDONED'
WHERE id_game_player = 3;   -- abandonó: 0 puntos (D-14)
GO

SELECT gp.id_game_player,
       COALESCE(u.nickname, g.display_name) AS jugador,
       CASE WHEN gp.id_guest IS NULL THEN 'Registrado' ELSE 'Invitado' END AS tipo,
       gp.final_position, gp.points_earned, gp.outcome
FROM Game_Player gp
LEFT JOIN Player p ON p.id_player = gp.id_player
LEFT JOIN [User]  u ON u.id_user   = p.id_user
LEFT JOIN Guest_Session g ON g.id_guest = gp.id_guest
WHERE gp.id_game = 1
ORDER BY gp.final_position;
GO

/* 6. Leaderboard: los invitados quedan fuera por construcción (RF-07). */
SELECT u.nickname, p.total_points, p.total_wins, p.total_games_played
FROM Player p
JOIN [User] u ON u.id_user = p.id_user
WHERE u.state = 'USER_ACTIVE'
ORDER BY p.total_points DESC;
GO
