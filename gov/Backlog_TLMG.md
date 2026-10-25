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
- Estado: Decidido; pendiente de implementar

### BL-002 · Renombrar el contrato de registro a `IUsersManager`
- Creado: 2026-10-20 10:30
- Por qué: `IAccountRegistrationService` no sigue D-41 ni §3.14. Incluye callback, archivo parcial, endpoint y regenerar el proxy.
- Depende de: acuerdo con Nicole sobre quién lo hace
- Depende de: BL-003 y BL-004 (ronda 1, integrada);

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
- Estado: Pendiente

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
- Estado: Pendiente

### BL-017 · Centralizar el manejo de errores, `ErrorKey` y mensajes (cliente y servidor)
- Creado: 2026-10-20 12:30
- Por qué: hoy el servidor decide la `ErrorKey` de un fallo de guardado dentro del manager (`ResolveSaveFailureErrorKey`, `isDuplicated`, cerca de la línea 152) y la pantalla de registro resuelve las claves y el Dispatcher en `RegisterPlayerAccountResult` (cerca de la línea 46). Ambos acoplan reglas de persistencia, errores e internacionalización en una sola clase. Además, `UnexpectedErrorKey` (en `AccountRegistration.cs`) repite el valor de `InternalErrorKey`, que es `private const` en `PlayerAccountRegistrationManager`: unificar en una sola constante. Hacer primero un inventario de solo lectura (dependencias, llamadas y otros módulos con el mismo patrón) y luego decidir con alternativas.
- Depende de: inventario previo; decisión de Martín (alternativas por presentar)
- Estado: Pendiente

### BL-018 · Revisar el texto de `CreateAccount_DataModelError`
- Creado: 2026-10-20 12:30
- Por qué: se usa como clave genérica de error inesperado (servidor y cliente) pero su texto habla del "modelo de datos", que es un detalle interno que el jugador no debe ver.
- Depende de: BL-017
- Estado: Pendiente

### BL-019 · Carpeta para las interfaces de repositorios en `TLMG.DataAccess`
- Creado: 2026-10-20 12:30
- Por qué: separar las interfaces de las implementaciones. El Estándar §4.3 (estructura de carpetas y namespaces) no tiene contenido, así que hay que decidir la convención (carpeta y namespace) y extender el Estándar antes de mover archivos. El csproj clásico exige editar las entradas `Compile`.
- Depende de: decisión de estructura; completar Estándar §4.3
- Estado: Pendiente
- Nota (2026-10-25): posiblemente obsoleta por Estándar §4.3; confirmar con Martín.

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
- Depende de: decisión de Martín
- Estado: Pendiente

### BL-027 · Corregir los hallazgos de Estándar del código de registro integrado y retirar lo sin uso
- Creado: 2026-10-20 22:15
- Por qué: usings sin agrupar en `CreateAccountScreen` (§9.4); líneas de más de 120 columnas en `PlayerAccountRegistrationService` (§9.2) y espacios al final de línea en varios archivos; `OperationRegisterPlayerAccountResult` sin `<summary>` y con la llave en la misma línea (§12.2, §9.6); `FIXME` duplicado del hash de contraseña (ligar a BL-001); `<WCFMetadata Include="Connected Services\" />` sin uso en `TLMG.Server.csproj` y `TLMG.Contracts.csproj`; DTOs sin sufijo `Data` (D-41); proxy `TLMGService` y carpeta `Communication/` sin uso (solo se borran con autorización de Martín).
- Depende de: coordinación con Nicole
- Estado: Pendiente
- Nota (2026-10-25): posiblemente obsoleta en el punto de los DTO sin sufijo `Data` por D-56; confirmar con Martín.

### BL-028 · Añadir `Service` a la tabla del Estándar §3.14
- Creado: 2026-10-20 22:55
- Por qué: D-49 adopta el sufijo `Service` para la lógica de negocio del servidor y el Estándar §3.14 no lo tiene. Fila a agregar: `Service` · Lógica de negocio del servidor (componentes del SAD, por ejemplo C-06); nunca persistencia ni contratos WCF · `PlayerAccountRegistrationService`.
- Depende de: D-49
- Estado: Pendiente
- Nota (2026-10-25): cumplido en el Estándar v3; falta el hash de commit para cerrarlo.
- 

### BL-029 · Alinear gov/CLAUDE.md con el Estándar y reexportar docs/Estandar.md
- Creado: 2026-10-25 00:49
- Por qué: alinear gov/CLAUDE.md con el Estándar v3 (paso 1) y reexportar docs/Estandar.md cuando Martín termine de corregir el Estándar en Docs.
- Depende de: BL-026
- Estado: Pendiente

### BL-030 · Login por servidor y hash de contraseña (D-44); cierra BL-001 y BL-016 al terminar
- Creado: 2026-10-25 00:49
- Por qué: el cliente consulta la BD y compara la contraseña en claro (LoginScreen.xaml.cs:104-108); el registro la guarda en claro (PlayerAccountRegistrationService.cs:97-98, 147-151). Estándar §16.2 y D-44: SHA-256 con sal aleatoria por contraseña, solo en el servidor. Retira también la cadena de conexión y la referencia a DataAccess del cliente (§4.1 regla 2, §5.2).
- Depende de: BL-001, BL-002, BL-016
- Estado: Pendiente

### BL-031 · Script SQL: datos ficticios y encabezado versionado
- Creado: 2026-10-25 00:49
- Por qué: database/TLMG_DB_v3.1.sql:544-559 trae correos que parecen reales y contraseñas de prueba legibles; sustituirlos por datos ficticios. Estándar §5.3: el encabezado debe listar cada versión con su fecha.
- Depende de: nada
- Estado: Pendiente

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
- Depende de: autorización explícita de Martín; BL-005
- Estado: Pendiente

### BL-036 · Mover el callback y el cierre del canal a Communication/
- Creado: 2026-10-25 00:49
- Por qué: Estándar §4.1 y §11.1: la Vista no usa el proxy y el cierre del canal corresponde a Comunicación. Hoy CreateAccountScreen implementa el callback, crea el cliente y cierra el canal (CreateAccountScreen.xaml.cs:13-14, 109, 185-208). Incluye capturar TimeoutException (BL-020).
- Depende de: BL-020, BL-035; decisión de BL-042
- Estado: Pendiente

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

---

## Finalizados

### BL-025 · Reparar `origin/main` (marcadores de conflicto en 74e5f9a)
- Creado: 2026-10-20 22:15
- Por qué: `main` se subió con marcadores de conflicto en 4 archivos y no compila. `dev-LS_Angel` ya contiene el contenido válido de los commits de armasstella (reproducidos en 25aae1c y 53c0e8b; integrados en bb13661) y la fase 2 (8e260fc, 9e4365d). Plan: registrar `74e5f9a` en `dev-LS_Angel` con `merge -s ours` (el árbol no cambia) y avanzar `main` por fast-forward, sin forzar historia.
- Depende de: nada
- Estado: Finalizado el 2026-10-20 · commit 17a2738 (merge -s ours de main y avance de main por fast-forward, de 74e5f9a a 17a2738)

