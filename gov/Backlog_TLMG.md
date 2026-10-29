# Backlog · The Last Meow (TLMG)

Control de pendientes, `FIXME`, `TODO` y actividades. Reglas fijas:

1. Toda entrada lleva **fecha y hora** de creación (`AAAA-MM-DD HH:MM`).
2. Toda entrada lleva una **justificación breve** del porqué.
3. Si depende de algo, se indica en **Depende de**.
4. Al terminar se marca: `Finalizado el AAAA-MM-DD · commit <hash>`. Sin commit no se cierra.
5. Todo `FIXME` o `TODO` del código tiene su entrada aquí.

Formato de entrada:

```
### BL-000 · Título corto
- Creado: AAAA-MM-DD HH:MM
- Por qué: justificación breve.
- Depende de: BL-000 / decisión / nada
- Estado: Pendiente | En curso | Finalizado el AAAA-MM-DD · commit <hash>
```

---

## Pendientes

### BL-001 · Hash de contraseña: SHA-256 + sal (decidido), implementar
- Por qué: hoy la contraseña se guarda en claro. Decisión del equipo (2026-10-20 22:32): SHA-256 con sal por contraseña, opción A, alineada con el SAD; se descartó PBKDF2 por familiaridad del equipo. Implementación prevista: `IPasswordHasher` y `PasswordHasher` en el servidor, formato versionado (`v1$sal$hash`), sal aleatoria, comparación de tiempo constante. Decisión registrada en la bitácora (D-44).
- Depende de: BL-016 (el login debe pasar por el servidor antes o junto con el hash)
- Estado: Finalizado el 2026-10-25 · commits 3b6c905, bdd0dfe y 445d216
- Nota (2026-10-25): cierre. El hash lo calcula el servidor (`IPasswordHasher` y `PasswordHasher`, formato `v1$sal$hash`) y el inicio de sesión pasa por el servidor. Las cuentas viejas con la contraseña en claro ya no pueden iniciar sesión.

### BL-002 · Renombrar el contrato de registro a `IUsersManager`
- Creado: 2026-10-20 10:30
- Por qué: `IAccountRegistrationService` no sigue D-41 ni §3.14. Incluye callback, archivo parcial, endpoint y regenerar el proxy.
- Depende de: acuerdo con Nicole sobre quién lo hace
- Depende de: BL-003 y BL-004 (ronda 1, integrada);
- Estado: Finalizado el 2026-10-25 · commits af8fbce y 563cd51
- Nota (2026-10-25): cierre. El renombre del contrato, el callback, el archivo parcial y el endpoint del servidor se hicieron en af8fbce, y la regeneración del proxy del cliente en 563cd51. La dirección del endpoint conserva el nombre `AccountRegistrationService`: el Estándar no tiene regla para ello; decisión del encargado.

### BL-003 · Renombrar `AccountRegistrationManager`
- Creado: 2026-10-20 10:30
- Por qué: `Manager` no aplica a lógica de negocio (§3.14); falta decidir el sufijo (D-42).
- Depende de: BL-002
- Estado: Finalizado el 2026-10-20 · commit 879b9e2 (renombre a PlayerAccountRegistrationService) y 3f7dea2 (carpeta Accounts), integrados en d317003

### BL-004 · Mover `Server/Services` a `Server/TLMG.Services` y pasar a PackageReference
- Creado: 2026-10-20 10:30
- Por qué: no sigue la estructura `Server/TLMG.X/`, usa `packages.config` y el ensamblado se llama `Services`.
- Depende de: BL-002
- Estado: En curso (parcial): proyecto movido y renombrado a TLMG.Services en el commit 30d5588, integrado en d317003. Falta migrar a PackageReference.

### BL-005 · Quitar el proxy `…Async` del cliente
- Creado: 2026-10-20 10:30
- Por qué: ADR-03 y la regla de no usar `Task`. Desmarcar "Allow generation of asynchronous operations" y regenerar la referencia.
- Depende de: nada
- Estado: Finalizado el 2026-10-25 · commit 563cd51

### BL-006 · Script de BD v3.2 con procedimientos del 2FA
- Creado: 2026-10-20 10:30
- Por qué: operaciones atómicas de códigos y registros pendientes (`CreatePendingRegistration`, `CreateVerificationCode`, `ValidateVerificationCode`) y resolver el trigger `INSTEAD OF` con EF6.
- Depende de: ADR-05 enmendado (hecho)
- Estado: Pendiente

### BL-007 · Implementar el 2FA por correo (pasos 3 a 7 del plan)
- Creado: 2026-10-20 10:30
- Por qué: CU-01. Se construye con la bandera `VerificationEnabled` apagada por ahora.
- Depende de: BL-006, BL-002, cuenta Gmail emisora
- Estado: Pendiente

### BL-008 · Crear la cuenta Gmail emisora y el archivo de secretos
- Creado: 2026-10-20 10:30
- Por qué: el envío SMTP necesita credenciales; irán en un archivo cifrado fuera de Git. La cuenta Gmail es solo el remitente: los destinatarios siguen la regla RC-E-03. Requiere verificación en dos pasos y contraseña de aplicación (no se pega en chats ni en el repo).
- Depende de: nada
- Estado: Pendiente

### BL-009 · Revisar con el encargado el uso de `lock` y `SemaphoreSlim`
- Creado: 2026-10-20 10:30
- Por qué: ADR-05 usa primitivas de `System.Threading`; la regla del curso es no usar threading.
- Depende de: antes de BL-007
- Estado: Pendiente

### BL-010 · Revisar ADR-07 (colas priorizadas del chat) contra la regla de no usar hilos
- Creado: 2026-10-20 10:30
- Por qué: usa un esquema de colas parecido al que se descartó en ADR-05.
- Depende de: BL-009
- Estado: Pendiente

### BL-011 · Alinear Drivers priorizados_V2 y Equipo3_Hito1_V2 con ADR-05 enmendado
- Creado: 2026-10-20 10:30
- Por qué: ambos mencionan la cola de despacho 2FA (ADR-05 "Propuesto" y Escenario 6).
- Depende de: nada
- Estado: Pendiente

### BL-012 · Actualizar los contratos y `Contratos_WCF_TLMG.md` por rol
- Creado: 2026-10-20 10:30
- Por qué: faltan los roles jugador registrado e invitado, y la enmienda de D-41 (callback en `IUsersManager`).
- Depende de: BL-002
- Estado: Pendiente

### BL-013 · Verificar el usuario de conexión a la base de datos
- Creado: 2026-10-20 10:30
- Por qué: el curso pide usuario de mínimo privilegio y no entrar como administrador; revisar la cadena de conexión.
- Depende de: nada
- Estado: Pendiente

### BL-014 · Diseño de la pantalla del Gatito Fugitivo
- Creado: 2026-10-20 10:30
- Por qué: solo diseño por ahora, en otro chat; sin implementación.
- Depende de: nada
- Estado: Pendiente

### BL-015 · Revisar el puntaje de la clasificación que no se muestra
- Creado: 2026-10-20 10:30
- Por qué: puede ser que `total_points` no tenga datos o un fallo previo; verificar.
- Depende de: nada
- Estado: Pendiente

### BL-016 · Migrar login y clasificación al servidor y quitar la referencia a TLMG.DataAccess del cliente
- Creado: 2026-10-20 11:40
- Por qué: hoy el cliente consulta la BD directamente (el login entra con el servidor apagado); viola SAD §4.3 y Zero-Trust.
- Depende de: BL-002
- Estado: Finalizado el 2026-10-25 · commits 3b6c905, bdd0dfe, 445d216, c0fc127, e09ed5b, ea27455, b628f1b
- Nota (2026-10-25): evidencia del acceso directo a la base desde el cliente (git grep sobre Client): LoginScreen.xaml.cs:104 y LeaderboardScreen.xaml.cs:76 abren new TLMG_DBEntities() desde la vista; Client/TLMG.Client/App.config:7 tiene su propia cadena de conexión TLMG_DBEntities; Client/TLMG.Client/TLMG.Client.csproj:242 y Client/TLMG.Client.slnx:3 referencian TLMG.DataAccess. Contradice Estándar §4.1 y Zero-Trust. Comprobación funcional: con el servidor apagado el inicio de sesión sigue funcionando; el registro ya pasa por WCF y con el servidor apagado muestra el error de red controlado. El alcance incluye retirar de la tabla de posiciones su consulta directa y usar un contrato del servidor para la clasificación (`ILeaderboardManager`, D-41).
- Nota (2026-10-25): el cliente conserva el `PackageReference` a EntityFramework (Client/TLMG.Client/TLMG.Client.csproj, alrededor de la línea 233); se retira junto con la referencia a TLMG.DataAccess, cuando ya no quede código del cliente que use TLMG_DBEntities. Quitarlo antes rompería la compilación de LoginScreen y LeaderboardScreen.
- Nota (2026-10-25): el login y el hash están hechos (3b6c905, bdd0dfe, 445d216). Las referencias a LoginScreen.xaml.cs:104 y a las líneas de consulta directa de esa pantalla ya no existen: LoginScreen usa el servidor. Queda el ranking por el servidor (`ILeaderboardManager`, LeaderboardScreen.xaml.cs:76) y, cuando LeaderboardScreen deje de usar TLMG_DBEntities, retirar TLMG_DBEntities de App.config, el `PackageReference` a EntityFramework, la referencia a TLMG.DataAccess y Client.slnx. No cerrada.
- Nota (2026-10-25): cierre. El login pasa por `IUsersManager` (3b6c905, bdd0dfe, 445d216) y el ranking por `ILeaderboardManager` (servidor c0fc127, proxy e09ed5b, pantalla ea27455). Con b628f1b el cliente quedó sin Entity Framework, sin referencia a TLMG.DataAccess y sin la cadena de conexión TLMG_DBEntities (git grep sobre Client: sin coincidencias de TLMG_DBEntities, System.Data.Entity, TLMG.DataAccess ni EntityFramework). Quedan fuera de esta entrada los valores fijos del jugador actual (BL-030) y la capa Comunicación (BL-036).

### BL-017 · Centralizar el manejo de errores, `ErrorKey` y mensajes (cliente y servidor)
- Creado: 2026-10-20 12:30
- Por qué: hoy el servidor decide la `ErrorKey` de un fallo de guardado dentro del manager (`ResolveSaveFailureErrorKey`, `isDuplicated`, cerca de la línea 152) y la pantalla de registro resuelve las claves y el Dispatcher en `RegisterPlayerAccountResult` (cerca de la línea 46). Ambos acoplan reglas de persistencia, errores e internacionalización en una sola clase. Además, `UnexpectedErrorKey` (en `AccountRegistration.cs`) repite el valor de `InternalErrorKey`, que es `private const` en `PlayerAccountRegistrationManager`: unificar en una sola constante. Hacer primero un inventario de solo lectura (dependencias, llamadas y otros módulos con el mismo patrón) y luego decidir con alternativas.
- Depende de: inventario previo; decisión del encargado (alternativas por presentar)
- Estado: Pendiente

### BL-018 · Revisar el texto de `CreateAccount_DataModelError`
- Creado: 2026-10-20 12:30
- Por qué: se usa como clave genérica de error inesperado (servidor y cliente) pero su texto habla del "modelo de datos", que es un detalle interno que el jugador no debe ver.
- Depende de: BL-017
- Estado: Pendiente
- Nota (2026-10-25): `Login_DataModelError` ya tiene texto neutro (445d216); `CreateAccount_DataModelError` sigue con texto técnico. Pendiente: actualizar a mano la fila de `Login_DataModelError` en TLMG_Diccionario_v3.xlsx.
- Nota (2026-10-25): `Leaderboard_DataModelErrorMessage` ya no se referencia en ningún `.cs` ni `.xaml` (git grep: solo aparece en Strings.es-MX.xaml:192 y Strings.en-US.xaml:192, sus definiciones). Es candidata a retirarse de ambos diccionarios y de TLMG_Diccionario_v3.xlsx.

### BL-019 · Carpeta para las interfaces de repositorios en `TLMG.DataAccess`
- Creado: 2026-10-20 12:30
- Por qué: separar las interfaces de las implementaciones. El Estándar §4.3 (estructura de carpetas y namespaces) no tiene contenido, así que hay que decidir la convención (carpeta y namespace) y extender el Estándar antes de mover archivos. El csproj clásico exige editar las entradas `Compile`.
- Depende de: decisión de estructura; completar Estándar §4.3
- Estado: Pendiente
- Nota (2026-10-25): posiblemente obsoleta por Estándar §4.3; confirmar con el encargado.

### BL-020 · Cierre del canal en `CreateAccountScreen.CloseRegistrationClient`: excepciones y bloqueo de la UI
- Creado: 2026-10-20 12:32
- Por qué: `Close()` bloquea el hilo de la interfaz hasta el `CloseTimeout` y el método no captura `TimeoutException` ni `CommunicationException` (Estándar §10.2: capturar el tipo más específico). Si el servidor cae con el canal abierto, el cierre puede lanzar una excepción sin controlar.
- Hallazgo (2026-10-20 16:20, análisis de solo lectura): el canal se abre de forma implícita en la primera llamada (`CreateAccountScreen.xaml.cs`, línea 111) y todo ocurre síncrono en el hilo de la interfaz. Ninguno de los dos `App.config` define tiempos límite, así que rigen los valores por defecto de WCF (1 min de apertura, envío y cierre). Con el servidor apagado la ventana se bloquea de unos segundos (conexión rechazada) hasta 1 minuto (puerto que no responde). El mensaje de error se oculta a los 4 s. No lo introdujo la fase 2. Observado en pruebas (2026-10-20 22:06): con el servidor encendido, el clic en "Crear Cuenta" deja la ventana aparentemente congelada unos 4 s antes de pasar a la verificación; con el servidor apagado, el error aparece en unos 5 a 6 s; el texto de error queda en el borde inferior de la ventana y es fácil no verlo.
- Depende de: decisión sobre timeouts del binding en `App.config` (mínimo y sin hilos); fase 2, cambio 3 (lo hace más visible)
- Estado: En curso (mitigación parcial: timeouts del binding en commit 95279e7, integrado en 9e4365d; cierre del canal en Unloaded en 84e35b8, integrado en 8e260fc). Falta capturar `TimeoutException` y `CommunicationException` en `CloseRegistrationClient` y decidir sobre el bloqueo de la interfaz.

### BL-021 · Probar el cliente contra un servidor en otra PC
- Creado: 2026-10-20 16:25
- Por qué: hasta hoy cliente y servidor corren en la misma PC (`localhost:8081`). El servidor real será otra máquina: hay que verificar dirección configurable en `App.config`, regla de firewall entrante del puerto, autenticación de Windows entre dos equipos (el `netTcpBinding` por defecto usa seguridad de transporte con credenciales de Windows; verificar el modo real del binding) y latencia de la negociación de seguridad.
- Hechos verificados (2026-10-20 16:40, lectura de código): las direcciones viven solo en configuración (cliente `net.tcp://localhost:8081/AccountRegistrationService`; base del servidor `net.tcp://localhost:8081/`); ningún `.cs` las escribe. Ninguno de los dos bindings define `mode` ni `clientCredentialType`: rigen los valores por defecto de `netTcpBinding` (Transport y Windows). Sin `<identity>` en el cliente tras la fase 2 se usa la identidad por defecto; falta comprobar si sirve entre dos PC.
- Depende de: decisión de despliegue del servidor
- Estado: Pendiente
- Nota (2026-10-25): revisar `sslProtocols="None"` en Client/TLMG.Client/App.config:14 junto con la seguridad de transporte entre dos PC. El Estándar no regula este punto.

### BL-022 · Política de caídas y reconexión (servidor y clientes)
- Creado: 2026-10-20 16:25
- Por qué: definir qué pasa si el servidor se reinicia, cae abruptamente o se apaga la PC, y si un cliente se cae con una sesión dúplex abierta (jugadores fantasma en lobby o partida, estado de la partida en memoria). Opciones a evaluar sin hilos propios: tiempos de inactividad de WCF y, en el cliente, `DispatcherTimer` para un latido (Estándar §11.4). Es una decisión de arquitectura: requiere ADR y alinear las reglas del juego sobre desconexión.
- Depende de: BL-021; revisión del SAD y de las reglas del juego
- Estado: Pendiente
- Nota (2026-10-25): posiblemente superado por D-01, D-54 y ADR-04; decidir.

### BL-023 · Revisión de endurecimiento del host WCF
- Creado: 2026-10-20 16:25
- Por qué: verificar que `includeExceptionDetailInFaults` esté en `false`, revisar `serviceThrottling`, `maxConnections` y el `openTimeout` del servidor para conexiones que no completan el saludo, y limitar el puerto por firewall. Las conexiones abiertas sin hablar y las ráfagas de registro no las cubre el timeout del cliente.
- Hechos verificados (2026-10-20 16:40): `includeExceptionDetailInFaults="false"` ya está escrito en el servidor. El servidor no define `serviceThrottling`, `maxConnections` ni `listenBacklog`: rigen los valores por defecto de .NET (revisar sus valores contra la documentación y el número de jugadores).
- Depende de: BL-021
- Estado: Pendiente

### BL-024 · Verificar si hay tiempo límite de espera del resultado por callback en el registro
- Creado: 2026-10-20 16:50
- Por qué: la operación de registro es `IsOneWay` y el resultado llega por callback; el `sendTimeout` solo acota el envío, no la espera del resultado. Si el servidor se cuelga después de recibir la solicitud, el botón podría quedar deshabilitado sin límite. Verificar en `CreateAccountScreen`; si se confirma, valorar un `DispatcherTimer` en la interfaz (Estándar §11.4).
- Depende de: BL-020
- Estado: Pendiente

### BL-026 · Llevar los documentos de referencia a `docs/` y ajustar las referencias de CLAUDE.md
- Creado: 2026-10-20 22:15
- Por qué: `CLAUDE.md` cita `docs/Estandar-Codificacion-CSharp.md`, `docs/Contratos_WCF_TLMG.md`, `docs/Bitacora-de-decisiones.md`, `docs/SAD.md` y `docs/Reglas-de-The-Last-Meow.md`, pero hoy viven como .docx en el Project y no en el repo. Decidir formato (convertir a .md o subir los .docx) y corregir los nombres.
- Depende de: decisión del encargado
- Estado: Pendiente

### BL-027 · Corregir los hallazgos de Estándar del código de registro integrado y retirar lo sin uso
- Creado: 2026-10-20 22:15
- Por qué: usings sin agrupar en `CreateAccountScreen` (§9.4); líneas de más de 120 columnas en `PlayerAccountRegistrationService` (§9.2) y espacios al final de línea en varios archivos; `OperationRegisterPlayerAccountResult` sin `<summary>` y con la llave en la misma línea (§12.2, §9.6); `FIXME` duplicado del hash de contraseña (ligar a BL-001); `<WCFMetadata Include="Connected Services\" />` sin uso en `TLMG.Server.csproj` y `TLMG.Contracts.csproj`; DTOs sin sufijo `Data` (D-41); proxy `TLMGService` y carpeta `Communication/` sin uso (solo se borran con autorización del encargado).
- Depende de: coordinación con Nicole
- Estado: Pendiente
- Nota (2026-10-25): posiblemente obsoleta en el punto de los DTO sin sufijo `Data` por D-56; confirmar con el encargado.

### BL-028 · Añadir `Service` a la tabla del Estándar §3.14
- Creado: 2026-10-20 22:55
- Por qué: D-49 adopta el sufijo `Service` para la lógica de negocio del servidor y el Estándar §3.14 no lo tiene. Fila a agregar: `Service` · Lógica de negocio del servidor (componentes del SAD, por ejemplo C-06); nunca persistencia ni contratos WCF · `PlayerAccountRegistrationService`.
- Depende de: D-49
- Estado: Pendiente
- Nota (2026-10-25): cumplido en el Estándar v3; falta el hash de commit para cerrarlo.
- 

### BL-029 · Alinear gov/CLAUDE.md con el Estándar y reexportar docs/Estandar.md
- Creado: 2026-10-25 00:49
- Por qué: alinear gov/CLAUDE.md con el Estándar v3 (paso 1) y reexportar docs/Estandar.md cuando el encargado termine de corregir el Estándar en Docs.
- Depende de: BL-026
- Estado: Finalizado el 2026-10-25 · commit bcc901f
- Nota (2026-10-25): cierre. La entrada se cumplió en tres commits: 1d2f401 (reexportación de docs/Estandar.md), 92ca7e4 y bcc901f (alineación de gov/CLAUDE.md con el Estándar).

### BL-030 · Login por servidor y hash de contraseña (D-44); cierra BL-001 y BL-016 al terminar
- Creado: 2026-10-25 00:49
- Por qué: el cliente consulta la BD y compara la contraseña en claro (LoginScreen.xaml.cs:104-108); el registro la guarda en claro (PlayerAccountRegistrationService.cs:97-98, 147-151). Estándar §16.2 y D-44: SHA-256 con sal aleatoria por contraseña, solo en el servidor. Retira también la cadena de conexión y la referencia a DataAccess del cliente (§4.1 regla 2, §5.2).
- Depende de: BL-001, BL-002, BL-016
- Estado: Pendiente
- Nota (2026-10-25): el alcance incluye retirar la cadena de conexión TLMG_DBEntities del cliente (Client/TLMG.Client/App.config:7) y la referencia a TLMG.DataAccess (TLMG.Client.csproj:242 y TLMG.Client.slnx:3). Comprobación funcional: con el servidor apagado el inicio de sesión sigue funcionando porque LoginScreen.xaml.cs:104 consulta la base directamente.
- Nota (2026-10-25): el cliente conserva el `PackageReference` a EntityFramework (Client/TLMG.Client/TLMG.Client.csproj, alrededor de la línea 233); se retira junto con la referencia a TLMG.DataAccess, cuando ya no quede código del cliente que use TLMG_DBEntities. Quitarlo antes rompería la compilación de LoginScreen y LeaderboardScreen.
- Nota (2026-10-25): el login y el hash están hechos (3b6c905, bdd0dfe, 445d216). Ya no existen las referencias a LoginScreen.xaml.cs:104-108 ni a PlayerAccountRegistrationService.cs:97-98 y 147-151: el servidor hashea con `IPasswordHasher` y LoginScreen usa `LogIn`.
- Nota (2026-10-25): el ranking ya viaja por `ILeaderboardManager` (c0fc127, e09ed5b, ea27455): máximo 100 jugadores, solo cuentas USER_ACTIVE, ordenados por puntos y luego por nickname; el cliente ya no usa EF ni TLMG.DataAccess (b628f1b). Queda pendiente: los valores fijos del jugador actual (`CurrentUserStandingRank` y `CurrentUserStandingPoints`, FIXME en LeaderboardScreen.xaml.cs:43, que cita esta entrada), que dependen del registro de sesiones (D-41). `ILeaderboardManager` se puede consultar sin sesión porque el ranking es público; se revisará cuando exista el registro de sesiones. No cerrada.

### BL-031 · Script SQL: datos ficticios y encabezado versionado
- Creado: 2026-10-25 00:49
- Por qué: database/TLMG_DB_v3.1.sql:544-559 trae correos que parecen reales y contraseñas de prueba legibles; sustituirlos por datos ficticios. Estándar §5.3: el encabezado debe listar cada versión con su fecha.
- Depende de: nada
- Estado: Pendiente
- Nota (2026-10-25): las cuentas de prueba con contraseña en claro ya no sirven para iniciar sesión; los datos de prueba deben crearse registrando cuentas desde la aplicación o con un mecanismo que genere el hash.

### BL-032 · .gitignore, ConnectionStrings.config y archivo cifrado de secretos
- Creado: 2026-10-25 00:49
- Por qué: Estándar §8.3 y §5.2 declaran estas reglas como pendientes. Server/TLMG.Server/App.config lleva la cadena inline; falta ConnectionStrings.config (configSource) y ConnectionStrings.config.example. Reglas de ignorado para ambos y para el archivo DPAPI de SMTP (D-43).
- Depende de: BL-008
- Estado: Pendiente
- Nota (2026-10-25): Cadena de conexión del servidor movida a ConnectionStrings.config y regla de .gitignore hechas en el commit a733631. Pendiente: nombre y patrón del archivo cifrado de SMTP (D-43, BL-008). No cerrada.

### BL-033 · Nivel de log por configuración y consola del servidor
- Creado: 2026-10-25 00:49
- Por qué: ambos log4net.config fijan DEBUG; Estándar §10.3 exige Trace y Debug fuera de las builds de entrega. El servidor define ConsoleAppender pero no lo referencia (Server/TLMG.Server/log4net.config:18-21), contra lo que dice §10.3.
- Depende de: decisión de mecanismo (transformación o nivel único)
- Estado: Pendiente

### BL-034 · Tiempos límite y endurecimiento del binding del servidor (D-46)
- Creado: 2026-10-25 00:49
- Por qué: el cliente fija 10/15/5 s; Server/TLMG.Server/App.config:25 no define ninguno (Estándar §11.1 lo declara pendiente). Complementa BL-021 y BL-023 (mex, serviceThrottling).
- Depende de: BL-021
- Estado: Pendiente

### BL-035 · Retirar el proxy TLMGService y los archivos obsoletos de Communication/
- Creado: 2026-10-25 00:49
- Por qué: apuntan a un contrato que ya no existe (OnRegisterResult, OperationResultData) y nadie los usa. La carpeta Communication/ se conserva: es la capa de Comunicación (Estándar §4.1, §4.3). Ya listado en BL-027; se separa para cerrarlo con su propio commit.
- Depende de: autorización explícita del encargado; BL-005
- Estado: Finalizado el 2026-10-25 · commit a8cdce2
- Nota (2026-10-25): el proxy TLMGService y los dos archivos de Communication/ se retiraron en a8cdce2; no quedan referencias en el código. Como Git no guarda carpetas vacías, Communication/ ya no aparece en el repo; BL-036 la vuelve a crear con el callback.

### BL-036 · Mover el callback y el cierre del canal a Communication/
- Creado: 2026-10-25 00:49
- Por qué: Estándar §4.1 y §11.1: la Vista no usa el proxy y el cierre del canal corresponde a Comunicación. Hoy CreateAccountScreen implementa el callback, crea el cliente y cierra el canal (CreateAccountScreen.xaml.cs:13-14, 109, 185-208). Incluye capturar TimeoutException (BL-020).
- Depende de: BL-020, BL-035; decisión de BL-042
- Estado: Pendiente
- Nota (2026-10-25): los dos TODO provisionales que citan esta entrada: callback `LogInResult` vacío en CreateAccountScreen.xaml.cs:58 y callback `RegisterPlayerAccountResult` vacío en LoginScreen.xaml.cs:57. Se quitan al mover los callbacks a la capa Comunicación.
- Nota (2026-10-25): LeaderboardScreen ya implementa `ILeaderboardManagerCallback` con su propio canal (ea27455); es la tercera pantalla con el patrón. TODO vigentes que citan esta entrada (git grep): CreateAccountScreen.xaml.cs:58, LoginScreen.xaml.cs:57 y LeaderboardScreen.xaml.cs:73, los tres con el texto "Mover ... a la capa Comunicación".

### BL-037 · Sacar EF6 y SqlClient de TLMG.Services; DataAccess devuelve un resultado controlado
- Creado: 2026-10-25 00:49
- Por qué: Estándar §4.1 y §5.1: las excepciones de EF y SQL se capturan en DataAccess y las capas superiores no conocen esos tipos. PlayerAccountRegistrationService.cs:2-5, 102-121 y TLMG.Services.csproj:38-43 lo incumplen; PlayerRepository.CreatePlayer deja salir DbUpdateException. Conservar la detección de duplicados (ResolveSaveFailureErrorKey, :153) y moverla a DataAccess; no borrarla.
- Depende de: decisión del mecanismo (alternativas por presentar); BL-017
- Estado: Pendiente

### BL-038 · Citar el BL en los comentarios especiales y registrar los faltantes
- Creado: 2026-10-25 00:49
- Por qué: Estándar §12.3 exige el formato "// TOKEN: Texto. (BL-xxx)." y el registro en el backlog. Los 13 comentarios actuales no citan BL: LeaderboardScreen.xaml.cs:40, 80, 206, 207; LobbyScreen.xaml.cs:255, 263; ProfileScreen.xaml.cs:77; PlayerRepository.cs:16, 19; PlayerAccountRegistrationService.cs:97, 149, 153. Diez no tienen entrada. El :97 además no lleva punto final.
- Depende de: nada
- Estado: Pendiente

### BL-039 · Arrancar las pruebas unitarias (proyecto test/ y marco)
- Creado: 2026-10-25 00:49
- Por qué: 0 pruebas frente al mínimo de 300 (Estándar §15.1). Elegir marco (§15.1: decisión superior) y crear test/TLMG.Services.Tests con un stub de IPlayerRepository.
- Depende de: decisión del marco de pruebas
- Estado: Pendiente
- Nota (2026-10-25): el marco de pruebas sigue sin decidirse (Estándar §15.1 y §19.1). Se recomendó xUnit.net, sin decisión del equipo; consultar al profesor o la rúbrica. No se ha creado ningún proyecto de pruebas.

### BL-040 · Crear TLMG.Domain (motor de reglas puro)
- Creado: 2026-10-25 00:49
- Por qué: Estándar §4.2 y gov/CLAUDE.md §13; no existe. Base para llegar al mínimo de pruebas sin infraestructura.
- Depende de: Reglas de The Last Meow en docs/ (BL-026); BL-039
- Estado: Pendiente

### BL-041 · Migrar las pantallas a MVVM con CommunityToolkit.Mvvm (D-39, D-50)
- Creado: 2026-10-25 00:49
- Por qué: hoy no hay ViewModels, comandos ni DataContext y las 12 pantallas atan Click (Estándar §7.1, §6.2). Una pantalla por commit, empezando por CreateAccountScreen.
- Depende de: aprobación de D-50; BL-042; BL-036
- Estado: Pendiente

### BL-042 · Mecanismo de navegación y diálogos desde el ViewModel (§18.1)
- Creado: 2026-10-25 00:49
- Por qué: Estándar §18.1 deja sin decidir cómo solicita navegación un ViewModel sin referenciar la vista. Hay 27 usos de Application.Current.MainWindow en 11 pantallas y MessageBox en las vistas, que §4.1 prohíbe al ViewModel. La decisión puede tomarse fuera del equipo; mientras tanto no se actúa.
- Depende de: decisión (alternativas por presentar)
- Estado: Pendiente

### BL-043 · Limpieza de estilo del diagnóstico (no incluida en BL-027)
- Creado: 2026-10-25 00:49
- Por qué: variable con nombre de tipo (PlayerAccountRegistrationService.cs:81); constantes ...minimum/maximun (PlayerAccountRegistrationValidationHelper.cs:13-18); ternarios multilínea (6 sitios); 3 return de expresión (LeaderboardScreen.xaml.cs:187, 235; NewPasswordScreen.xaml.cs:113); manejadores Timer_Tick, CountdownTimer_Tick, ToastTimer_Tick y DigitBox_* (§7.2); <param> y <returns> de ValidationHelper (§12.2, §10.7); public const sin documentar; LoadLeaderboardData de 43 líneas; texto "{0} pts" en código; clave Common_ para CreateAccount_NicknameFormatError; llaves de OperationRegisterPlayerAccountResult.cs:3; referencia System.Web sin uso; Thread.CurrentThread en LanguageManager.cs:77-78 (§11.1); decidir si es variable local o constante en LobbyScreen.xaml.cs:255.
- Depende de: BL-027
- Estado: Pendiente
- Nota (2026-10-25): el uso de Thread.CurrentThread en LanguageManager se corrigió en 64d1ce1. La entrada sigue abierta por el resto de su alcance.
- Nota (2026-10-25): el commit 0f14a64 cambió a Error el nivel de log del fallo inesperado en ServiceImplementation.UsersManager.cs, documentó la constante EnglishCultureCode de LanguageManager y dejó su ternario en una sola línea. La entrada sigue abierta por el resto de su alcance.

### BL-044 · Reglas de validación solo en el cliente y desalineación con la BD
- Creado: 2026-10-25 00:49
- Por qué: dominios de correo, vocabulario ofensivo y apodos reservados solo se validan en el cliente (ValidationHelper.cs:17-27); Estándar §16.1 exige que el servidor valide todo. Correo hasta 254 en el validador contra VARCHAR(100); nickname 12 contra VARCHAR(20) en la BD.
- Depende de: decisión del equipo; Diccionario de Reglas de Validación en docs/
- Estado: Pendiente

### BL-045 · Imágenes: carpeta Images/ y valor de profile_image
- Creado: 2026-10-25 00:49
- Por qué: Estándar §4.4 pide Images/ y nombres PascalCase. Hoy hay img/default.jpg y la BD guarda "/img/default.png", que no existe y fuerza el HACK de LeaderboardScreen (:206-207). Decidir qué guarda la BD y cuándo entra la foto como byte[] (gov/CLAUDE.md §10, FIXME de PlayerRepository.cs:19).
- Depende de: decisión del equipo
- Estado: Pendiente

### BL-046 · Pantallas pendientes: GameplayScreen y ChangePasswordScreen
- Creado: 2026-10-25 14:23
- Por qué: LobbyScreen.xaml.cs:263 debe navegar a GameplayScreen al terminar la cuenta regresiva y ProfileScreen.xaml.cs:77 a ChangePasswordScreen; ninguna de las dos pantallas existe.
- Depende de: BL-040 y Reglas de The Last Meow (GameplayScreen); BL-042 (navegación)
- Estado: Pendiente

### BL-047 · Estados de usuario como enum
- Creado: 2026-10-25 14:23
- Por qué: el estado se maneja con cadenas ("USER_ACTIVE") en PlayerRepository.cs:17 y en la consulta de LeaderboardScreen.xaml.cs:81; usar un enum (PlayerRepository.cs:16) y filtrar por estado activo (LeaderboardScreen.xaml.cs:80).
- Depende de: nada
- Estado: Pendiente

### BL-048 · Los archivos generados del proxy contienen la identidad de Windows (UPN) del WSDL
- Creado: 2026-10-25 16:50
- Por qué: configuration.svcinfo, configuration91.svcinfo y service.wsdl de Connected Services/UsersManagerReference contienen la identidad de Windows (UPN) que el servidor publica en el WSDL. Es el mecanismo normal de Visual Studio y ya estaba en los proxies anteriores del historial; esos archivos no se editan a mano (Estándar §4.3).
- Depende de: BL-021 (seguridad entre dos PC, identidad explícita del endpoint)
- Estado: Pendiente

### BL-049 · Revisar el nombre del método del callback y el atributo [ServiceContract] en IUsersManagerCallback
- Creado: 2026-10-25 17:10
- Por qué: el método `RegisterPlayerAccountResult` termina en sustantivo (§3.7 pide verbo o frase verbal; el ejemplo de §3.10 usa la forma On…) y el callback lleva `[ServiceContract]` mientras el ejemplo de §3.10 no. Cambiarlo obliga a regenerar el proxy, así que se hace en la misma regeneración que BL-030 (login).
- Depende de: BL-030
- Estado: Pendiente

### BL-050 · Corregir el Estándar tras la revisión de los pasos 4a/4b y publicar docs/Estandar.md
- Creado: 2026-10-25 17:57
- Por qué: la revisión de los pasos 4a/4b contra el Estándar dejó correcciones de redacción y de criterio. Cambios: §10.2 y §10.3 (`catch (Exception)` solo en límites, registro con `_log.Error`, Fatal solo según §10.3); §10.9 y D-57 (criterio de accesibilidad relajado; D-32 no cambia); §15.2 (pruebas de integración del servidor en un único proyecto /test/TLMG.Server.Tests); §4.4 y §7.3 (se aceptan las dos formas de pack URI); §13.5 (ternario en una sola línea); §3.14, §17.1 y §20.2 (sufijo Request y D-56); se retiró del Estándar el estado actual y las menciones a BL. D-57 se registra en la Bitácora, que vive fuera del repositorio.
- Depende de: nada
- Estado: Finalizado el 2026-10-25 · commit 1d2f401

### BL-051 · Medir el rendimiento del login y del ranking
- Creado: 2026-10-25 19:32
- Por qué: medir primero el tiempo desde el clic hasta la respuesta. Hipótesis a descartar: costo de la primera consulta de EF6, carga de la entidad completa con seguimiento de cambios, y la unión del ranking con la imagen de todos.
- Depende de: BL-016 y BL-030
- Estado: Pendiente
- Nota (2026-10-25): observación: el login tarda unos 5 s con unas 20 cuentas; el tamaño de la tabla no lo explica. Hipótesis sin medir: arranque en frío de EF en la primera consulta. Pendiente: comparar el primer y el segundo inicio de sesión en la misma ejecución del servidor, y medir el ranking.

### BL-052 · Estados USER_SUSPENDED y USER_BANNED en el login
- Creado: 2026-10-25 19:32
- Por qué: CU-03 no define qué pasa ni el texto para estas cuentas; hoy reciben el error genérico de credenciales inválidas.
- Depende de: definir la regla y los textos
- Estado: Pendiente

### BL-053 · Login con 2FA tras 30 días de inactividad (CU-03 FA-02)
- Creado: 2026-10-25 19:32
- Por qué: CU-03 FA-02 pide un segundo factor cuando la cuenta lleva 30 días sin iniciar sesión. El servidor ya guarda last_time en UTC (3b6c905).
- Depende de: BL-007 y BL-008 (el 2FA está apagado)
- Estado: Pendiente

### BL-054 · Revisión del esquema de la tabla User
- Creado: 2026-10-25 19:32
- Por qué: evaluar un catálogo de estados o el perfil en una tabla aparte, en una etapa más avanzada y después de medir (BL-051). Impacto si se hace: versión mayor del script (§5.3), modelo de EF, la base de la compañera, diccionario y SAD.
- Depende de: BL-051
- Estado: Pendiente

### BL-055 · Nombre del helper de validación de correo
- Creado: 2026-10-25 19:32
- Por qué: `PlayerAccountRegistrationValidationHelper` lo usa también el login; evaluar un nombre o ubicación común. Prioridad baja.
- Depende de: nada
- Estado: Pendiente

### BL-056 · Registrar en la Bitácora D-57 y la enmienda a D-40
- Creado: 2026-10-25 19:32
- Por qué: la Bitácora vive fuera del repositorio. Falta registrar D-57 (criterio de accesibilidad) y la enmienda a D-40: la referencia de servicio de Visual Studio usa el mismo generador que svcutil.exe. Queda abierta hasta que el encargado lo haga.
- Depende de: el encargado
- Estado: Pendiente

### BL-057 · Revisar App.config del cliente tras actualizar la referencia de servicio
- Creado: 2026-10-25 20:30
- Por qué: al regenerar la referencia de servicio, Visual Studio crea el binding nuevo con valores por defecto y agrega la identidad de la máquina de quien actualiza. Revisar Client/TLMG.Client/App.config: los bindings deben conservar los tiempos de D-46 (10/15/5 s) y los endpoints no deben llevar `<identity>` con cuentas de Windows.
- Depende de: nada
- Estado: Pendiente

### BL-058 · Cerrar las sesiones abandonadas del SessionRegistry
- Creado: 2026-10-29 12:55
- Por qué: `SessionRegistry.EndSession` aún no tiene quién lo llame y el registro no vence sesiones. Si el cliente cierra la ventana o pierde la conexión, su sesión y su token quedan en memoria hasta reiniciar el servidor. El vencimiento debe medirse por hora límite en UTC al recibir el latido, sin temporizadores (Estándar §10.4, D-54).
- Depende de: BL-022; del paso que agregue cerrar sesión y latido
- Estado: Pendiente


### BL-059 · Registrar en la Bitácora las decisiones D-58 a D-61
- Creado: 2026-10-29 HH:MM
- Por qué: el Estándar (§3.14, §4.3, §5.3, §10.3, §11.1, §15.1, §16.2, §19.1, §20.2) y el SAD (ADR-08, C-02, C-06, §4.2, §4.3, §7.3) ya citan D-58 a D-61 y ADR-08, pero la Bitácora (fuera del repositorio, BL-056) aún no las contiene: D-58 (SessionRegistry con lock único, sessionToken, el último inicio de sesión gana), D-59 (scripts de demostración en database/seed/), D-60 (ServerConnectionManager) y D-61 (xUnit.net). Relacionada con BL-009 y BL-056.
- Depende de: decisión del encargado sobre la redacción
- Estado: Pendiente


### BL-060 · Retirar o reemplazar el script de demostración de amistades
- Creado: 2026-10-29 13:05
- Por qué: `database/seed/TLMG_Seed_FriendshipDemo.sql` (D-59) existe solo para revisar el lobby mientras las solicitudes reales no estén implementadas. No es parte del esquema y no debe llegar a una instalación real. Se elimina, o se sustituye por datos de prueba más completos, cuando exista BL-062.
- Depende de: BL-062
- Estado: Pendiente

### BL-061 · Definir la regla de unicidad por pareja en Friendship
- Creado: 2026-10-29 13:05
- Por qué: `UQ_Friendship_pair` es dirigido (sender, receiver) y no hay restricción de orden entre ids: la base permite A→B y B→A como dos filas. Por ahora se acepta y todas las consultas del repositorio consideran ambos sentidos. Falta decidir si se impone una fila por pareja (CHECK de orden entre ids, índice único sobre la pareja ordenada o regla en el servicio) y el cambio de esquema que eso implique (Estándar §5.3).
- Depende de: decisión del encargado
- Estado: Pendiente

### BL-062 · Implementar las solicitudes de amistad reales
- Creado: 2026-10-29 13:05
- Por qué: solicitar, aceptar, rechazar y cancelar amistades quedó fuera del paso 4; hoy solo se precargan con el script de demostración. `FriendshipRepository` solo consulta aceptadas. Incluye los estados FRIEND_PENDING y FRIEND_REJECTED, el contrato y la pantalla de amigos.
- Depende de: BL-061
- Estado: Pendiente

### BL-063 · Definir en el repositorio los permisos de la cuenta de mínimo privilegio
- Creado: 2026-10-29 13:05
- Por qué: `database/TLMG_DB_v3.1.sql` no tiene GRANT, DENY, CREATE USER ni CREATE LOGIN, y ningún documento del repositorio detalla qué permisos tiene la cuenta de la aplicación (D-53, Estándar §5.2). No se puede comprobar que pueda leer `Friendship` y `User` sin ejecutar la aplicación contra la base. Verificación local del 29-oct-2026: el servidor de desarrollo se conecta con Integrated Security=True (cuenta de Windows), por lo que no se pudo comprobar el mínimo privilegio con la cuenta de la aplicación.
- Depende de: BL-013
- Estado: Pendiente

### BL-064 · Emitir las autorizaciones de entrada al aceptar una invitación
- Creado: 2026-10-29 19:30
- Por qué: `ISessionRegistry.IssueRoomEntryAuthorization` existe y EnterLobby ya las exige y las consume, pero nadie las emite todavía (hay un UNDONE que cita esta entrada). Las emitirá el paso de invitaciones, al aceptar el invitado; y quien invita debe estar en la sala y tener al invitado como amigo aceptado (IFriendshipRepository).
- Depende de: paso de invitaciones
- Estado: Finalizado el 2026-10-29 · commit bac66e1

### BL-065 · Avisar a la sala cuando una sesión termina o es reemplazada
- Creado: 2026-10-29 19:30
- Por qué: `StartSession` (reemplazo) y `EndSession` retiran al jugador de su sala, de su callback y de sus autorizaciones, pero no avisan a los demás jugadores: ven la lista vieja hasta el siguiente cambio. Hay que devolver los avisos desde el registro y entregarlos fuera del candado, como ya se hace en EnterLobby y LeaveLobby.
- Depende de: nada
- Estado: Finalizado el 2026-10-29 · commit d6d6326

### BL-066 · Vencimiento de autorizaciones de entrada y de sesiones sin canal de lobby
- Creado: 2026-10-29 19:30
- Por qué: una autorización emitida y nunca usada, o una sesión que no vuelve a abrir su canal de lobby, ocupa memoria hasta que la sala se elimina o la sesión termina. Medir el plazo por hora límite en UTC al recibir el latido, sin temporizadores (Estándar §10.4, D-54).
- Depende de: BL-058
- Estado: Pendiente

### BL-067 · Agregar al cliente las claves de idioma del lobby
- Creado: 2026-10-29 19:30
- Por qué: el servidor ya devuelve Lobby_InvalidSessionError, Lobby_AlreadyInRoomError, Lobby_CannotJoinRoomError, Lobby_RoomFullError y Lobby_UnexpectedError. Faltan en Strings.es-MX.xaml y Strings.en-US.xaml, con paridad exacta.
- Depende de: nada
- Estado: Pendiente

### BL-068 · Registrar en el Estándar y la Bitácora el estado de salas en SessionRegistry
- Creado: 2026-10-29 19:30
- Por qué: la autorización de lock de D-58 ahora cubre también las salas, las pertenencias, los callbacks y las autorizaciones de entrada; el límite de 6 jugadores por sala vive en `SessionRegistry` (Reglas §1.1.1). Falta reflejarlo en §11.1 y en la Bitácora (fuera del repositorio). Complementa BL-059.
- Depende de: decisión del encargado
- Estado: Pendiente

### BL-069 · Rechazar, cancelar y vencer invitaciones a sala
- Creado: 2026-10-30 10:00
- Por qué: hoy una invitación solo se acepta; solo desaparece al aceptarla, al eliminarse su sala o al terminar la sesión de quien invita o de quien recibe. Faltan rechazarla (invitado), cancelarla (quien invita) y vencerla por hora límite en UTC al recibir el latido, sin temporizadores (Estándar §10.4, D-54). Una invitación repetida al mismo jugador para la misma sala no vuelve a avisar, así que si el primer aviso se perdió el invitado no la ve.
- Depende de: BL-058
- Estado: Pendiente
### BL-070 · Entregar los avisos de EndSession cuando exista cerrar sesión
- Creado: 2026-10-30 10:00
- Por qué: `ISessionRegistry.EndSession` ya devuelve la sala y la presencia que hay que avisar (BL-065), pero nadie lo llama: el cierre de sesión por el jugador no existe. Cuando exista, el servicio que lo llame debe entregar `SessionNotificationHelper.CreateSessionEndNotifications` fuera del lock, como ya hace el login que reemplaza una sesión.
- Depende de: BL-058
- Estado: Pendiente
### BL-071 · Actualizar la lista de amigos guardada en el canal cuando cambien las amistades
- Creado: 2026-10-30 10:00
- Por qué: el registro guarda los ids de amigos de cada canal al suscribirse, para avisar la presencia sin consultar la base de datos al terminar la sesión. Cuando existan solicitudes de amistad reales (BL-062), un amigo nuevo o eliminado no se refleja hasta que el jugador se vuelva a suscribir; hay que refrescar esa lista o avisar el cambio.
- Depende de: BL-062
- Estado: Pendiente
### BL-072 · Retirar la autorización de entrada que ya no sirve
- Creado: 2026-10-30 10:00
- Por qué: una autorización emitida al aceptar una invitación se conserva si el invitado nunca llama EnterLobby, o si la sala estaba llena. Solo se retira al entrar, al eliminarse la sala o al terminar la sesión del invitado. Definir su vencimiento junto con BL-066.
- Depende de: BL-066
- Estado: Pendiente
### BL-073 · Agregar al cliente las claves de idioma de amigos y los renombres de errores
- Creado: 2026-10-30 10:00
- Por qué: faltan en Strings.es-MX.xaml y Strings.en-US.xaml, con paridad exacta, las claves nuevas (Friends_NotInRoomError, Friends_CannotInviteError, Friends_InvitationNotValidError) y los renombres Common_InvalidSessionError, Common_UnexpectedError y Common_RoomFullError (antes Lobby_*). Complementa BL-067, cuya lista queda desactualizada.
- Depende de: nada
- Estado: Pendiente
### BL-074 · Registrar en la Bitácora el canal de amigos como presencia
- Creado: 2026-10-30 10:00
- Por qué: "en línea" significa tener un canal de amigos vivo, no tener sesión, y cerrar ese canal termina la sesión del jugador (ADR-08, D-58). Falta registrarlo en la Bitácora (fuera del repositorio) y reflejarlo en §6.2 del Estándar.
- Depende de: decisión del encargado
- Estado: Pendiente

### BL-075 · El cliente no conoce su propio nickname ni su id de usuario
- Creado: 2026-10-29 16:36
- Por qué: OperationLogInResult solo trae IsSuccess, ErrorKey y SessionToken (decisión del paso 3), y OperationSubscribeToFriendsResult solo trae los amigos. ServerConnectionManager guarda únicamente el token, así que MainMenuScreen y LobbyScreen no pueden mostrar el nombre propio ni distinguirse en la lista de la sala. Hay que decidir si el login lo devuelve o si se agrega una operación de perfil.
- Depende de: decisión del encargado
- Estado: Pendiente

### BL-076 · Los eventos del servidor se pierden si ninguna pantalla está suscrita
- Creado: 2026-10-29 16:36
- Por qué: ServerConnectionManager dispara FriendListReceived, RoomInvitationReceived y los demás eventos solo a quien esté suscrito en ese momento. La lista de amigos llega justo después del login y una invitación puede llegar con el menú principal abierto. Hay que decidir quién escucha siempre (por ejemplo MainWindow) y cómo se muestra una invitación fuera de FriendsScreen. Mitigación actual: RequestFriendList().
- Depende de: pieza de FriendsScreen
- Estado: Pendiente

### BL-077 · Mostrar en pantalla el fallo de la suscripción a los amigos
- Creado: 2026-10-29 16:36
- Por qué: si SubscribeToFriends responde con ErrorKey (por ejemplo, fallo de la base de datos), el jugador queda con sesión pero sin canal de amigos registrado en el servidor y nadie lo ve. El manager pasa el error tal cual y ninguna pantalla lo atiende todavía.
- Depende de: BL-076
- Estado: Pendiente

### BL-078 · Unificar el cierre de canales WCF en el cliente
- Creado: 2026-10-29 16:36
- Por qué: ServerConnectionManager.CloseClientSafely repite la lógica Faulted→Abort / Close con Abort de respaldo que ya tienen LoginScreen, CreateAccountScreen y LeaderboardScreen. Al migrar esas pantallas a la capa de comunicación (BL-036) debe quedar una sola implementación, en el ServerConnectionManager de instancia que vive en App.
- Depende de: BL-036
- Estado: Pendiente

### BL-079 · El cliente se bloquea hasta 10 s al abrir un canal sin servidor
- Creado: 2026-10-29 16:36
- Por qué: Open() es síncrono con el openTimeout de D-46. Si el servidor no responde, la interfaz se congela hasta 10 s en el login, en OpenLobbyChannel y en el envío. Sin Task ni hilos, la salida es una pantalla de espera con DispatcherTimer o abrir el canal antes de mostrarla. El Estándar no lo regula.
- Depende de: BL-022
- Estado: Pendiente

### BL-080 · Extraer IServerConnectionManager cuando exista el primer ViewModel o prueba que lo necesite
- Creado: 2026-10-29 16:36
- Por qué: ServerConnectionManager es una clase de instancia que vive en App ((App)Application.Current).ServerConnection), con API y eventos públicos y sin estado estático. Para probar un ViewModel o una pantalla (D-61, D-51, ADR-02) hay que sustituirlo por una versión simulada, lo que exige una interfaz. Se extrae con un cambio mecánico solo cuando haya un primer consumidor que la necesite (D-31); la acción de crear los clientes WCF seguirá dentro del manager o de una fábrica que se decida entonces.
- Depende de: BL-039; BL-041
- Estado: Pendiente

### BL-081 · Hallazgos de estilo del Estándar para la capa de comunicación
- Creado: 2026-10-29 16:36
- Por qué: §3.14 no define un sufijo para la implementación de los callbacks (ServerCallback, nombrada por analogía con las interfaces I…ManagerCallback). Además, el manejador de Closed/Faulted y el último catch de la apertura de canales registran y no relanzan, igual que los del servidor; §10.2 sigue pidiendo relanzar o devolver un resultado y no cubre manejadores de eventos de canal. (Se retiró de esta entrada lo que dependía de la forma estática: el acceso global ya está cubierto por el patrón existente ((MainWindow)Application.Current.MainWindow).)
- Depende de: decisión del encargado
- Estado: Pendiente

---

## Finalizados

### BL-025 · Reparar `origin/main` (marcadores de conflicto en 74e5f9a)
- Creado: 2026-10-20 22:15
- Por qué: `main` se subió con marcadores de conflicto en 4 archivos y no compila. `dev-LS_Angel` ya contiene el contenido válido de los commits de armasstella (reproducidos en 25aae1c y 53c0e8b; integrados en bb13661) y la fase 2 (8e260fc, 9e4365d). Plan: registrar `74e5f9a` en `dev-LS_Angel` con `merge -s ours` (el árbol no cambia) y avanzar `main` por fast-forward, sin forzar historia.
- Depende de: nada
- Estado: Finalizado el 2026-10-20 · commit 17a2738 (merge -s ours de main y avance de main por fast-forward, de 74e5f9a a 17a2738)

