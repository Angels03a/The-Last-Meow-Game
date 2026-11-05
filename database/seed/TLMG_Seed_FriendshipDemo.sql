/* ============================================================================
   The Last Meow Game — TLMG_DB
   Datos de demostración · Amistad aceptada entre dos jugadores
   ----------------------------------------------------------------------------
   PROPÓSITO
     Deja una amistad ACEPTADA entre dos cuentas que ya existen, para poder
     revisar el lobby (invitar a un amigo desde la lista de amigos) mientras
     solicitar y aceptar amistades reales no esté implementado.

   NO ES PARTE DEL ESQUEMA (D-59)
     Es un script de demostración y de revisión. No crea ni cambia tablas, no
     cambia la versión de TLMG_DB_v3.1.sql (Estándar §5.3) y no se ejecuta en
     una instalación de producción. No lleva contraseñas ni datos personales:
     solo recibe dos nicknames de cuentas creadas desde la pantalla de registro.

   CON QUÉ CUENTA SE EJECUTA (D-53)
     Con una cuenta administradora de SQL Server, nunca con la cuenta de mínimo
     privilegio que usa la aplicación.

   CÓMO USARLO
     1. Crea dos cuentas desde la pantalla de registro del cliente.
     2. Reemplaza los dos valores de ejemplo de @nickname_a y @nickname_b.
     3. Ejecuta el script completo. Se puede ejecutar varias veces.

   QUÉ HACE
     · Resuelve los dos usuarios por nickname.
     · Si ya existe una fila entre la pareja, en cualquier sentido (Friendship
       permite A→B y B→A), la deja como FRIEND_ACCEPTED; si no existe,
       inserta una (A→B). Nunca crea una segunda fila para la misma pareja.
     · Todo ocurre en una transacción. Si los nicknames conservan el valor de
       ejemplo, no existen o son la misma cuenta, aborta con un mensaje
       claro y sin cambios.
     · Al terminar muestra las filas que hay entre la pareja.
   ============================================================================ */
USE TLMG_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

/* ---------- Reemplaza estos dos valores por los nicknames reales ---------- */
DECLARE @nickname_a VARCHAR(20) = 'NICKNAME_UNO';
DECLARE @nickname_b VARCHAR(20) = 'NICKNAME_DOS';
/* -------------------------------------------------------------------------- */

DECLARE @example_nickname_a VARCHAR(20) = 'NICKNAME_UNO';
DECLARE @example_nickname_b VARCHAR(20) = 'NICKNAME_DOS';
DECLARE @id_user_a INT;
DECLARE @id_user_b INT;

BEGIN TRY
    BEGIN TRANSACTION;

    IF @nickname_a = @example_nickname_a OR @nickname_b = @example_nickname_b
    BEGIN
        THROW 50101, 'Reemplaza @nickname_a y @nickname_b por dos nicknames reales. No se hizo ningun cambio.', 1;
    END

    SELECT @id_user_a = id_user FROM [User] WHERE nickname = @nickname_a;
    SELECT @id_user_b = id_user FROM [User] WHERE nickname = @nickname_b;

    IF @id_user_a IS NULL OR @id_user_b IS NULL
    BEGIN
        THROW 50102, 'Alguno de los dos nicknames no existe en User. No se hizo ningun cambio.', 1;
    END

    IF @id_user_a = @id_user_b
    BEGIN
        THROW 50103, 'Los dos nicknames son la misma cuenta. No se hizo ningun cambio.', 1;
    END

    IF EXISTS (SELECT 1 FROM Friendship WITH (UPDLOCK, HOLDLOCK)
               WHERE (id_user_sender = @id_user_a AND id_user_receiver = @id_user_b)
                  OR (id_user_sender = @id_user_b AND id_user_receiver = @id_user_a))
    BEGIN
        UPDATE Friendship
        SET state = 'FRIEND_ACCEPTED'
        WHERE (id_user_sender = @id_user_a AND id_user_receiver = @id_user_b)
           OR (id_user_sender = @id_user_b AND id_user_receiver = @id_user_a);
    END
    ELSE
    BEGIN
        INSERT INTO Friendship (id_user_sender, id_user_receiver, state, request_date)
        VALUES (@id_user_a, @id_user_b, 'FRIEND_ACCEPTED', GETUTCDATE());
    END

    COMMIT TRANSACTION;

    /* Comprobación: debe haber una sola fila aceptada entre la pareja. */
    SELECT id_friendship, id_user_sender, id_user_receiver, state, request_date
    FROM Friendship
    WHERE (id_user_sender = @id_user_a AND id_user_receiver = @id_user_b)
       OR (id_user_sender = @id_user_b AND id_user_receiver = @id_user_a);
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

/* Agregar otro amigo  osea Id 23 en mi caso puse jugadorTest3@gmail.com y los otro los tenía como id 21,22,23
USE TLMG_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

DECLARE @id_new_user INT = 23;
DECLARE @others TABLE (id_other INT);
INSERT INTO @others (id_other) VALUES (1), (2);

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [User] WHERE id_user = @id_new_user)
       OR EXISTS (SELECT 1 FROM @others o
                  WHERE NOT EXISTS (SELECT 1 FROM [User] u WHERE u.id_user = o.id_other))
    BEGIN
        THROW 50104, 'Alguno de los usuarios no existe en User. No se hizo ningun cambio.', 1;
    END

    UPDATE f
    SET f.state = 'FRIEND_ACCEPTED'
    FROM Friendship f WITH (UPDLOCK, HOLDLOCK)
    JOIN @others o
      ON (f.id_user_sender = @id_new_user AND f.id_user_receiver = o.id_other)
      OR (f.id_user_sender = o.id_other AND f.id_user_receiver = @id_new_user);

    INSERT INTO Friendship (id_user_sender, id_user_receiver, state, request_date)
    SELECT o.id_other, @id_new_user, 'FRIEND_ACCEPTED', GETUTCDATE()
    FROM @others o
    WHERE NOT EXISTS (
        SELECT 1 FROM Friendship f
        WHERE (f.id_user_sender = @id_new_user AND f.id_user_receiver = o.id_other)
           OR (f.id_user_sender = o.id_other AND f.id_user_receiver = @id_new_user));

    COMMIT TRANSACTION;

    SELECT id_friendship, id_user_sender, id_user_receiver, state, request_date
    FROM Friendship
    WHERE id_user_sender = @id_new_user OR id_user_receiver = @id_new_user;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO
\*