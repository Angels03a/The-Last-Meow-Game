# Backlog · The Last Meow (TLMG)

Control de pendientes, `FIXME`, `TODO` y actividades. Reglas fijas:

1. Toda entrada lleva **fecha y hora** de creación (`AAAA-MM-DD HH:MM`).
2. Toda entrada lleva una **justificación breve** del porqué.
3. Si depende de algo, se indica en **Depende de**.
4. Al terminar se marca: `Finalizado el AAAA-MM-DD · commit <hash>`. Sin commit no se cierra. 
5. Para Descartada. - Estado: Pendiente | En curso | Finalizado el AAAA-MM-DD · commit <hash> | Descartada el AAAA-MM-DD · motivo 
6. Todo `FIXME` o `TODO` del código tiene su entrada aquí.

Nota de reconciliación (2026-11-20): V2 distingue prerrequisitos de tareas relacionadas; las dependencias históricas de V1 se conservan como registro y no constituyen por sí solas un orden de ejecución vigente.

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

### BL-005 · Quitar el proxy `…Async` del cliente
- Creado: 2026-10-20 10:30
- Por qué: ADR-03 y la regla de no usar `Task`. Desmarcar "Allow generation of asynchronous operations" y regenerar la referencia.
- Depende de: nada
- Estado: Requiere decisión
- Nota histórica: finalizado el 2026-10-25 · commit 563cd51.
- Nota (2026-11-20): R2B1, HEAD 08297935. El cierre histórico se acredita en 563cd51, pero la configuración actual Reference.svcmap desactiva GenerateAsynchronousMethods y activa GenerateTaskBasedAsynchronousMethod; Reference.cs expone operaciones Task<T> asíncronas, por ejemplo RegisterPlayerAccountAsync. No se identificó uso en código de aplicación. Contradicción actual con la restricción original de ADR-03 y Task; decidir si se autoriza esa generación o se regenera conforme a la regla, sin editar a mano archivos generados. El estado operativo se reabre por esta obligación concreta, no por cambios de nombres.


### BL-185 · Persistir resultados y estadísticas de partidas
- Creado: 2026-11-20 15:07
- Por qué: El cálculo y el ranking requieren guardar los resultados finales y estadísticas acumuladas de jugadores registrados; el ranking lee total_points, pero no se identificó una escritura de resultados finales.
- Depende de: BL-091; BL-103; reglas V3 de puntaje.
- Responsable: Compañera.
- Estado: Pendiente
- Nota (2026-11-20): revisión inicial sobre 0829793; requiere confirmar alcance detallado durante la tarea.

### BL-186 · Mostrar el beneficio de vida extra en Gameplay
- Creado: 2026-11-20 15:07
- Por qué: MatchPlayer.HasExtraLife existe en el dominio; falta acreditar un indicador visible del beneficio y su consumo en la partida.
- Depende de: BL-119; contrato y vista de partida.
- Responsable: Compañera.
- Estado: Pendiente
- Nota (2026-11-20): revisión inicial sobre 0829793; requiere confirmar alcance detallado durante la tarea.

### BL-188 · Preparar cuaderno integral de estudio técnico
- Creado: 2026-11-20 15:07
- Por qué: El equipo necesita un material de estudio para explicar y defender la implementación y sus decisiones con evidencia.
- Depende de: BL-026; BL-114; código vigente.
- Responsable: Luis.
- Estado: Pendiente
- Nota (2026-11-20): revisión inicial sobre 0829793; requiere confirmar alcance detallado durante la tarea.

### BL-189 · Revisar comentarios restantes en inglés
- Creado: 2026-11-20 15:07
- Por qué: La limpieza de comentarios de jugabilidad no acredita los comentarios restantes fuera de ese alcance.
- Depende de: Estándar §12; inventario de comentarios.
- Responsable: Compañera.
- Estado: Pendiente
- Nota (2026-11-20): revisión inicial sobre 0829793; requiere confirmar alcance detallado durante la tarea.

### BL-190 · Revalidar SonarQube en el commit definitivo
- Creado: 2026-11-20 15:07
- Por qué: El análisis anterior corresponde a una revisión anterior; hace falta evidencia del commit final y conservar incidencias visibles.
- Depende de: Commit definitivo; entorno SonarQube.
- Responsable: Compañera.
- Estado: Pendiente
- Nota (2026-11-20): revisión inicial sobre 0829793; requiere confirmar alcance detallado durante la tarea.

### BL-161 · Expulsión por votación en sala y partida
- Creado: 2026-11-20 14:41
- Por qué: las pantallas citan BL-161 pero faltaba su entrada; Dominio del juego V3 §2 exige el voto de todos los jugadores excepto el infractor.
- Depende de: BL-165 (moderación), integración de sala y partida.
- Estado: Pendiente
- Nota (2026-11-20): revisión sobre 0829793. GameplayScreen conserva un TODO para iniciar la votación y LobbyScreen referencia esta entrada. Falta implementar y validar el flujo y sus consecuencias en sala y partida conforme a las reglas V3.

### BL-004 · Mover `Server/Services` a `Server/TLMG.Services` y pasar a PackageReference
- Creado: 2026-10-20 10:30
- Por qué: no sigue la estructura `Server/TLMG.X/`, usa `packages.config` y el ensamblado se llama `Services`.
- Depende de: BL-002
- Estado: Postergado
- Nota (2026-10-29 22:53): TLMG.DataAccess, TLMG.Server y TLMG.Client ya usan PackageReference. Solo TLMG.Services conserva packages.config (EntityFramework 6.5.2 y log4net 3.5.0, con targetFramework="net472" aunque el proyecto es v4.8) y HintPath en TLMG.Services.csproj:39, 42 y 45. Si BL-037 saca EF de TLMG.Services, solo quedaría log4net por migrar.
- Nota (2026-11-20): revisión sobre 08297935 y configuración local cuando se indica. El proyecto fue movido y renombrado; TLMG.Services.csproj conserva packages.config, HintPath y targets EF6. No se encontró una regla explícita que obligue PackageReference ni evidencia actual de fallo de compilación atribuible a ese formato; los imports exigen restauración de paquetes. Se posterga la migración de mantenimiento hasta que exista necesidad concreta; las referencias EF se tratan en BL-037.

### BL-007 · Implementar el 2FA por correo (pasos 3 a 7 del plan)
- Creado: 2026-10-20 10:30
- Por qué: CU-01. Se construye con la bandera `VerificationEnabled` apagada por ahora.
- Depende de: BL-006, BL-002, cuenta Gmail emisora
- Estado: Parcial
- Nota (2026-11-11 23:55): relación. La recuperación de contraseña sigue simulada: ForgotPasswordViewModel.cs:137 y NewPasswordViewModel.cs:176 llevan TODO que citan esta entrada, y VerificationViewModel.cs:20 compara con el código fijo «123456». Falta una operación de servidor que envíe el código, lo valide y guarde la nueva contraseña.
- Nota (2026-11-15 00:32): avance en el rango 78e1fa1..8aa6844. El 2FA del registro ya funciona de extremo a extremo: SMTP y plantillas HTML (1b40829), generador de código, estrategia por idioma, EmailUtility y VerificationCodeStore (0e19d8e), VerificationCodeRepository con EF6 y procedimientos (b5abe2e, 0429cb1), operaciones RequestAccountVerificationCode, CompletePlayerAccountRegistration y ResendAccountVerificationCode (ead5dd3; IUsersManager.cs:28, 40 y 52, PlayerAccountRegistrationService.cs:55, 81 y 124) y la pantalla de verificación conectada al servidor (54fe884; VerificationViewModel.cs:247). La bandera VerificationEnabled no existe en el código (git grep sin resultados): el flujo de registro siempre usa 2FA. Sigue Pendiente porque falta: (a) la recuperación de contraseña: IForgotPasswordManager (ad8b5f4) es solo un contrato; ServiceImplementation no lo implementa, Server/TLMG.Server/App.config no tiene su endpoint, no está en el proxy, los TODO de ForgotPasswordViewModel.cs:137 y NewPasswordViewModel.cs:176 siguen y VerificationViewModel.cs:236 solo atiende VerificationFlow.AccountRegistration; (b) el login con 2FA (BL-053); (c) RegisterPlayerAccount sigue creando la cuenta sin código (BL-177); (d) nadie llama a dbo.PurgeExpiredRegistrations (database/TLMG_DB_v4.1.sql:644); solo se borra el pendiente vencido al consultarlo (VerificationCodeStore.FindValidPendingRegistration); (e) el envío real del código de registro por correo ya lo confirmó el encargado el 2026-11-15; sigue sin confirmarse el flujo completo con el cliente (registro, código, cuenta creada e inicio de sesión). Pendiente de ratificar o implementar: esta entrada (Por qué) y la nota de BL-053 describen el 2FA con la bandera VerificationEnabled apagada, y el Estándar lo repite («función apagada por ahora», docs/Estandar.md:1518, D-42) y fija las credenciales SMTP en un archivo ignorado por Git y cifrado con DPAPI (docs/Estandar.md:1431, D-43). El código hace lo contrario: el 2FA del registro siempre está encendido y EmailConfig.xml va en texto plano. Hay que ratificar lo implementado o ajustar el código, y la Bitácora debe reflejar la decisión (BL-114). La parte del archivo de secretos está en BL-171.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.
- Nota (2026-11-20): R2B1, HEAD 08297935. VerificationCodeRepository invoca dbo.SavePendingRegistration, ausente de los scripts SQL versionados consultados; v4.1 define dbo.CreatePendingRegistration. Verificar compatibilidad, implementación y flujo real de registro junto con BL-153; no se comprobó la base desplegada ni se ejecutó SQL.

### BL-009 · Revisar con el encargado el uso de `lock` y `SemaphoreSlim`
- Creado: 2026-10-20 10:30
- Por qué: ADR-05 usa primitivas de `System.Threading`; la regla del curso es no usar threading.
- Depende de: antes de BL-007
- Estado: Requiere decisión
- Nota (2026-10-29 22:53): la parte del lock quedó resuelta por D-58: el Estándar §11.1 autoriza un único lock para SessionRegistry y exige autorización del encargado para cualquier otro uso de lock, SemaphoreSlim o Interlocked. En 53263f1 los 12 lock del código son todos _sessionsLock y SemaphoreSlim tiene 0 usos. Sigue abierta la parte del SemaphoreSlim del 2FA, porque D-42 y ADR-05 todavía lo prescriben y BL-007 depende de ello.
- Nota (2026-11-15 00:32): verificado sobre 8aa6844. SemaphoreSlim tiene 0 usos y los 19 lock del servidor son todos _sessionsLock en SessionRegistry (git grep). El 2FA se implementó sin primitivas de sincronización: el estado vive en la base de datos (VerificationCodeStore, VerificationCodeRepository), por lo que la parte del SemaphoreSlim del 2FA ya no aplica en el código. Sigue Pendiente porque D-42 y ADR-05 todavía lo prescriben y esos documentos no están en el repositorio (BL-026); corregirlos o ratificarlos con el encargado.
- Nota (2026-11-20): revisión estática sobre 08297935. La búsqueda estática encontró 29 sentencias lock, todas en SessionRegistry y usando _sessionsLock; no encontró SemaphoreSlim ni Interlocked en código de servidor inspeccionado. Estándar §11.1 autoriza el lock D-58; 2FA utiliza VerificationCodeStore y repositorio de BD. El Estándar aún cita D-42/ADR-05 para Token Bucket; no se dispone del ADR-05 ni Bitácora originales para comprobar o ratificar su texto. La dependencia técnica histórica de BL-007 con SemaphoreSlim no se observa; falta ratificación documental.

### BL-010 · Revisar ADR-07 (colas priorizadas del chat) contra la regla de no usar hilos
- Creado: 2026-10-20 10:30
- Por qué: usa un esquema de colas parecido al que se descartó en ADR-05.
- Depende de: BL-009
- Estado: Requiere validación
- Nota (2026-11-20): revisión estática sobre 08297935. El Estándar lista D-55 como corrección de ADR-07 para canal dúplex; ADR-07 original no está disponible localmente. ChatService valida con ChatValidator, SessionRegistry aplica límite de frecuencia y las acciones entregan o guardan mensajes; no se identificaron colas prioritarias ni hilos propios en los archivos inspeccionados. Una cola conceptual no implica hilo. Consultar ADR-07 original y ratificar si está actualizado; no se demuestra necesidad de nuevas colas.

### BL-011 · Alinear Drivers priorizados_V2 y Equipo3_Hito1_V2 con ADR-05 enmendado
- Creado: 2026-10-20 10:30
- Por qué: ambos mencionan la cola de despacho 2FA (ADR-05 "Propuesto" y Escenario 6).
- Depende de: nada
- Estado: Requiere validación
- Nota (2026-11-20): revisión estática sobre 08297935. No se encontraron Drivers priorizados_V2 ni Equipo3_Hito1_V2 en el repositorio inspeccionado; la afirmación histórica sobre sus secciones no pudo contrastarse. Solicitar ambos originales y ADR-05, distinguir escenarios de calidad de decisiones de implementación y ajustar lo que siga desactualizado. Coordinar con BL-009 y BL-026.

### BL-012 · Actualizar los contratos y `Contratos_WCF_TLMG.md` por rol
- Creado: 2026-10-20 10:30
- Por qué: faltan los roles jugador registrado e invitado, y la enmienda de D-41 (callback en `IUsersManager`).
- Depende de: BL-002
- Estado: Requiere validación
- Nota (2026-11-15 00:32): los contratos cambiaron en 78e1fa1..8aa6844 y el documento de contratos sigue sin estar en docs/ (BL-026), así que no se puede actualizar. Para cuando exista: IFriendsManager ahora hereda IFriendshipManager e IRoomInvitationManager (2ab3567; IFriendsManager.cs:10) y su callback hereda IFriendshipManagerCallback e IRoomInvitationManagerCallback (IFriendsManagerCallback.cs:10); IUsersManager suma RequestAccountVerificationCode, CompletePlayerAccountRegistration y ResendAccountVerificationCode (ead5dd3); IForgotPasswordManager existe sin implementar (ad8b5f4); y OperationResult se usa directamente como resultado de varias operaciones (2ab3567; ver nota en BL-109). Sin cambio de estado.
- Nota (2026-11-20): revisión estática sobre 08297935. Existen IUsersManager con registro y verificación, IFriendsManager y callbacks heredados, ILobbyManager, IMatchManager e IPasswordManager para contraseña y recuperación. IForgotPasswordManager antiguo permanece declarado pero no se identificó implementación o endpoint; no equivale al flujo vigente de IPasswordManager. No se acredita sesión real de invitados (BL-111). Falta recuperar Contratos_WCF_TLMG original y cotejar roles, callbacks, operaciones y alcance real; coordinar BL-026.

### BL-013 · Verificar el usuario de conexión a la base de datos
- Creado: 2026-10-20 10:30
- Por qué: el curso pide usuario de mínimo privilegio y no entrar como administrador; revisar la cadena de conexión.
- Depende de: nada
- Estado: Requiere validación
- Nota (2026-11-11 12:32): el script v4.0 ya define role_tlmg_app con sus permisos (c16351b, BL-063). Falta comprobar en ejecución, con la cuenta de la aplicación y no con Integrated Security, que el servidor puede leer Friendship y User y ejecutar los procedimientos.
- Nota (2026-11-20): revisión sobre 08297935 y configuración local cuando se indica. SQL v4.1 declara role_tlmg_app, GRANT y DENY; CREATE USER y ALTER ROLE son ejemplos comentados. No se consultó el archivo de credenciales ni la base instalada, por lo que existencia del usuario, membresía, identidad efectiva y permisos no están acreditados. Comprobar en la base de desarrollo y entrega, con consultas de identidad y permisos de solo lectura y una conexión controlada usando la cuenta de aplicación; no registrar contraseñas ni cadenas.

### BL-015 · Revisar el puntaje de la clasificación que no se muestra
- Creado: 2026-10-20 10:30
- Por qué: puede ser que `total_points` no tenga datos o un fallo previo; verificar.
- Depende de: nada
- Estado: Requiere validación
- Nota (2026-11-20): revisión estática sobre 08297935. PlayerRepository.FindTopRankedPlayers lee total_points, LeaderboardService lo convierte a LeaderboardPlayer.TotalPoints y LeaderboardViewModel lo asigna a Score que XAML enlaza a ScoreLabel. Hay pruebas de mapeo del servicio, no ejecutadas aquí. Falta validar visualmente con una base que tenga puntos reales y confirmar el dato en instalación; puesto personal queda en BL-091 y persistencia de resultados en BL-185.

### BL-017 · Centralizar el manejo de errores, `ErrorKey` y mensajes (cliente y servidor)
- Creado: 2026-10-20 12:30
- Por qué: hoy el servidor decide la `ErrorKey` de un fallo de guardado dentro del manager (`ResolveSaveFailureErrorKey`, `isDuplicated`, cerca de la línea 152) y la pantalla de registro resuelve las claves y el Dispatcher en `RegisterPlayerAccountResult` (cerca de la línea 46). Ambos acoplan reglas de persistencia, errores e internacionalización en una sola clase. Además, `UnexpectedErrorKey` (en `AccountRegistration.cs`) repite el valor de `InternalErrorKey`, que es `private const` en `PlayerAccountRegistrationManager`: unificar en una sola constante. Hacer primero un inventario de solo lectura (dependencias, llamadas y otros módulos con el mismo patrón) y luego decidir con alternativas.
- Depende de: inventario previo; decisión del encargado (alternativas por presentar)
- Estado: Parcial
- Nota (2026-10-29 22:53): AccountRegistration.cs ya no existe. El servidor tiene CommonErrorKeyHelper (Common_UnexpectedError, usado por lobby y amigos) y el cliente ErrorMessageHelper (33dc497), que traduce la ErrorKey y cae en Common_UnexpectedError. Siguen tres claves de error inesperado: CreateAccount_DataModelError (ServiceImplementation.UsersManager.cs:14, PlayerAccountRegistrationService.cs:24-25, con dos constantes del mismo valor), Login_DataModelError (ServiceImplementation.UsersManager.cs:15, PlayerLogInService.cs:21) y Common_UnexpectedError. ResolveSaveFailureErrorKey está en PlayerAccountRegistrationService.cs:156.
- Nota (2026-11-02 16:06): hoy hay cinco claves para un fallo inesperado o de datos: Common_UnexpectedError, CreateAccount_DataModelError, Login_DataModelError, Leaderboard_ConnectionErrorMessage (el servidor la usa para un fallo de datos, LeaderboardService.cs:14) y, en el cliente, Common_ConnectionLostError. Se propone una sola clave del servidor (Common_UnexpectedError) y reutilizar ErrorMessageHelper en Login, CreateAccount y Leaderboard.
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sin avance de fondo. Las ErrorKey del servidor están en TLMG.Domain.Constants.ErrorKeys, que el rango amplió con Verification (0429cb1; ErrorKeys.cs:44-48) y Friends, y ambos diccionarios conservan paridad (391 claves cada uno, verificado). Sigue dispersa la parte del cliente: 36 constantes *ErrorKey con valores escritos en 13 archivos (por ejemplo VerificationViewModel.cs:23-26, GameplayViewModel.cs:21 y LobbyViewModel.cs:28, estas dos con la misma Lobby_ChatEmptyError). CreateAccount_DataModelError sigue como clave genérica y ahora también la usa el flujo del 2FA (cinco usos en PlayerAccountRegistrationService.cs:153, 198, 205, 238 y 257, y VerificationViewModel.cs:26). El inventario de diálogos y mensajes duplicados está en BL-174.
- Nota (2026-11-20): revisión sobre 08297935. ErrorKeys centraliza claves de servidor por área; cuentas, login, amigos y clasificación conservan errores específicos y OperationResult. En el cliente persisten literales repetidos, por ejemplo Common_UnexpectedError y Lobby_ChatEmptyError. Una clave de conexión y una validación específica no son automáticamente equivalentes: inventariar y decidir duplicados reales sin borrar información útil. La semántica de null y resultados se coordina con BL-187; texto técnico y carrera de registro siguen en BL-018 y BL-094.

### BL-018 · Revisar el texto de `CreateAccount_DataModelError`
- Creado: 2026-10-20 12:30
- Por qué: se usa como clave genérica de error inesperado (servidor y cliente) pero su texto habla del "modelo de datos", que es un detalle interno que el jugador no debe ver.
- Depende de: BL-017
- Estado: Pendiente
- Nota (2026-10-25): `Login_DataModelError` ya tiene texto neutro (445d216); `CreateAccount_DataModelError` sigue con texto técnico. Pendiente: actualizar a mano la fila de `Login_DataModelError` en TLMG_Diccionario_v3.xlsx.
- Nota (2026-10-25): `Leaderboard_DataModelErrorMessage` ya no se referencia en ningún `.cs` ni `.xaml` (git grep: solo aparece en Strings.es-MX.xaml:199 y Strings.en-US.xaml:199, sus definiciones). Es candidata a retirarse de ambos diccionarios y de TLMG_Diccionario_v3.xlsx.
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sin cambio. CreateAccount_DataModelError conserva el texto técnico «el modelo de datos no se encuentra disponible» (Strings.es-MX.xaml:194) y el rango lo usa en más lugares: el envío y la validación del código devuelven esa clave (PlayerAccountRegistrationService.cs:153, 198, 205, 238 y 257) y VerificationViewModel.cs:26 la toma como error inesperado. Leaderboard_DataModelErrorMessage sigue definida y sin uso (Strings.es-MX.xaml:199).
- Nota (2026-11-20): revisión sobre 08297935. CreateAccount_DataModelError muestra «el modelo de datos no se encuentra disponible» en español y equivalente en inglés; CreateAccountViewModel, VerificationViewModel y ServerConnectionManager.Accounts lo usan y el servidor expone ErrorKeys.CreateAccount.DataModel. Sustituir el texto por mensaje comprensible en ambos idiomas y cotejar el Excel; no requiere esperar una refactorización general. Leaderboard_DataModelErrorMessage es candidata histórica, no se acredita ausencia de uso dinámico.

### BL-021 · Probar el cliente contra un servidor en otra PC
- Creado: 2026-10-20 16:25
- Por qué: hasta hoy cliente y servidor corren en la misma PC (`localhost:8081`). El servidor real será otra máquina: hay que verificar dirección configurable en `App.config`, regla de firewall entrante del puerto, autenticación de Windows entre dos equipos (el `netTcpBinding` por defecto usa seguridad de transporte con credenciales de Windows; verificar el modo real del binding) y latencia de la negociación de seguridad.
- Hechos verificados (2026-10-20 16:40, lectura de código): las direcciones viven solo en configuración (cliente `net.tcp://localhost:8081/AccountRegistrationService`; base del servidor `net.tcp://localhost:8081/`); ningún `.cs` las escribe. Ninguno de los dos bindings define `mode` ni `clientCredentialType`: rigen los valores por defecto de `netTcpBinding` (Transport y Windows). Sin `<identity>` en el cliente tras la fase 2 se usa la identidad por defecto; falta comprobar si sirve entre dos PC.
- Depende de: decisión de despliegue del servidor
- Estado: Requiere validación
- Nota (2026-10-25): revisar `sslProtocols="None"` en App.config:12, 18, 24 y 30 (los cuatro bindings) junto con la seguridad de transporte entre dos PC. El Estándar no regula este punto.
- Nota (2026-11-20): revisión sobre 0829793. El desarrollador informa que cliente y servidor funcionaron en dos computadoras y se jugó una partida. No se documentaron por separado el modo de autenticación, la negociación ni su latencia o sslProtocols; confirmar los puntos originales que correspondan al despliegue.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Requiere validación conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-023 · Revisión de endurecimiento del host WCF
- Creado: 2026-10-20 16:25
- Por qué: verificar que `includeExceptionDetailInFaults` esté en `false`, revisar `serviceThrottling`, `maxConnections` y el `openTimeout` del servidor para conexiones que no completan el saludo, y limitar el puerto por firewall. Las conexiones abiertas sin hablar y las ráfagas de registro no las cubre el timeout del cliente.
- Hechos verificados (2026-10-20 16:40): `includeExceptionDetailInFaults="false"` ya está escrito en el servidor. El servidor no define `serviceThrottling`, `maxConnections` ni `listenBacklog`: rigen los valores por defecto de .NET (revisar sus valores contra la documentación y el número de jugadores).
- Depende de: BL-021
- Estado: Requiere validación
- Nota (2026-11-15 00:32): prueba de estrés del 2026-11-14 en la PC de Martín (dictado por el encargado, no verificable desde el repositorio): servidor y 6 clientes en 2 salas de 3 y 4 invitados; picos de aproximadamente 99 % de RAM y CPU; no se logró tumbar el servidor. Limitaciones: todo corrió en la misma máquina, no se revisó el log del servidor y no se probó entre dos PC (BL-021). No cierra la entrada: Server/TLMG.Server/App.config sigue sin serviceThrottling, maxConnections ni listenBacklog (git grep).
- Nota (2026-11-20): revisión sobre 08297935 y configuración local cuando se indica. El App.config local fija includeExceptionDetailInFaults=false; no declara serviceThrottling, maxConnections ni listenBacklog. El desarrollador comunicó una partida real entre dos PC, que no acredita resistencia ni firewall. Revisar valores predeterminados, límites, timeout de apertura y firewall conforme al despliegue sin fijar números arbitrarios. El archivo local modificado declara security mode=None en el binding del host; HEAD no contiene esa línea: revisar el transporte efectivo y la protección del entorno antes de entrega, sin asumir cifrado.

### BL-026 · Llevar los documentos de referencia a `docs/` y ajustar las referencias de CLAUDE.md
- Creado: 2026-10-20 22:15
- Por qué: `CLAUDE.md` cita `docs/Estandar-Codificacion-CSharp.md`, `docs/Contratos_WCF_TLMG.md`, `docs/Bitacora-de-decisiones.md`, `docs/SAD.md` y `docs/Reglas-de-The-Last-Meow.md`, pero hoy viven como .docx en el Project y no en el repo. Decidir formato (convertir a .md o subir los .docx) y corregir los nombres.
- Depende de: decisión del encargado
- Estado: Parcial
- Nota (2026-10-29 22:53): el Estándar ya vive en docs/Estandar.md (55d7070) y gov/CLAUDE.md §14 ya no cita rutas inventadas (92ca7e4, bcc901f). Faltan la Bitácora, el SAD, las Reglas de The Last Meow y los Contratos WCF.
- Nota (2026-11-20): revisión estática sobre 08297935. Están versionados docs/Estandar.md, docs/Dominio_del_juego_v3.md y docs/Reglas_de_The_Last_Meow_V3.docx; 7110f29 incorporó reglas y dominio. No se encontraron SAD, Bitácora ni documento descriptivo de contratos WCF en el repositorio; sí existen contratos en código. CLAUDE.md §14 aún dice que las Reglas no están en el repo. Recuperar originales, decidir formato y ubicación con el encargado y corregir referencias; DOCX no implica conversión obligatoria.

### BL-031 · Script SQL: datos ficticios y encabezado versionado
- Creado: 2026-10-25 00:49
- Por qué: database/TLMG_DB_v3.1.sql:544-559 trae correos que parecen reales y contraseñas de prueba legibles; sustituirlos por datos ficticios. Estándar §5.3: el encabezado debe listar cada versión con su fecha.
- Depende de: nada
- Estado: Parcial
- Nota (2026-10-25): las cuentas de prueba con contraseña en claro ya no sirven para iniciar sesión; los datos de prueba deben crearse registrando cuentas desde la aplicación o con un mecanismo que genere el hash.
- Nota (2026-11-11 12:32): avance: el script v4.0 ya no trae datos de demostración (database/TLMG_DB_v4.0.sql:15) y su encabezado lista las versiones (v4.0.sql:5-11). Falta: las versiones v1 a v3.1 dicen "(fecha por completar)" (v4.0.sql:6-10); completar sus fechas (Estándar §5.3).
- Nota (2026-11-20): Revisión sobre 08297935: database/TLMG_DB_v4.1.sql conserva fechas por completar de v1, v2, v3 y v3.1. El historial local muestra el traslado de v3.1 en 80d0054 y la transición a v4 en c16351b/bee19d0, pero esas fechas de commit no acreditan la fecha real de cada versión. La v4 ya no incluye datos de demostración según la nota previa. Falta recuperar fechas verificables y completar el encabezado; si no existe fuente, dejar constancia de la incertidumbre.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-033 · Nivel de log por configuración y consola del servidor
- Creado: 2026-10-25 00:49
- Por qué: ambos log4net.config fijan DEBUG; Estándar §10.3 exige Trace y Debug fuera de las builds de entrega. El servidor define ConsoleAppender pero no lo referencia (definido en Server/TLMG.Server/log4net.config:2; root en :18-21 solo referencia FileAppender), contra lo que dice §10.3.
- Depende de: decisión de mecanismo (transformación o nivel único)
- Estado: Parcial
- Nota (2026-11-03 10:31): avance. ea295c1 agregó el ConsoleAppender al root de Server/TLMG.Server/log4net.config: la consola del servidor ya muestra los mensajes de log. Sigue pendiente el nivel de log por compilación (Trace y Debug fuera de las builds de entrega) en ambos log4net.config.
- Nota (2026-11-11 08:14): el esquema de dos logs (BL-147) debe definir también el nivel por entorno.
- Nota (2026-11-20): revisión estática sobre 08297935. ConsoleAppender está referenciado en root del servidor desde ea295c1. Ambos log4net.config rastreados fijan DEBUG y no se identificó transformación por configuración en csproj revisados; Estándar §10.3 excluye Debug y Trace de entrega. Verificar configuración efectiva Release y definir mecanismo por entorno. La separación de auditoría corresponde a BL-147.

### BL-034 · Tiempos límite y endurecimiento del binding del servidor (D-46)
- Creado: 2026-10-25 00:49
- Por qué: el cliente fija 10/15/5 s; Server/TLMG.Server/App.config:21 no define ninguno (Estándar §11.1 lo declara pendiente). Complementa BL-021 y BL-023 (mex, serviceThrottling).
- Depende de: BL-021
- Estado: Requiere decisión
- Nota (2026-11-15 00:32): prueba de estrés del 2026-11-14 en la PC de Martín (ver la nota de BL-023): servidor y 6 clientes en 2 salas de 3 y 4 invitados, picos de aproximadamente 99 % de RAM y CPU, sin tumbar el servidor; misma máquina, sin revisar el log del servidor y sin probar entre dos PC (BL-021). No cierra la entrada: Server/TLMG.Server/App.config sigue sin tiempos límite en el binding (git grep de Timeout sin resultados).
- Nota (2026-11-20): revisión sobre 08297935 y configuración local cuando se indica. Los bindings del cliente local declaran apertura 10 s, envío 15 s y cierre 5 s; el binding del servidor no declara timeouts. Estándar §11.1 atribuye esos tres valores al cliente, no exige copiar números al servidor. Falta decidir y verificar tiempos adecuados para servidor y host según sus canales y despliegue, coordinado con BL-023 y BL-021; sslProtocols=None no se interpreta como ausencia de cifrado.

### BL-037 · Sacar EF6 y SqlClient de TLMG.Services; DataAccess devuelve un resultado controlado
- Creado: 2026-10-25 00:49
- Por qué: Estándar §4.1 y §5.1: las excepciones de EF y SQL se capturan en DataAccess y las capas superiores no conocen esos tipos. PlayerAccountRegistrationService.cs:2-5, 104-128 y TLMG.Services.csproj:38-43 lo incumplen; PlayerRepository.CreatePlayer deja salir DbUpdateException. Conservar la detección de duplicados (ResolveSaveFailureErrorKey, :156) y moverla a DataAccess; no borrarla.
- Depende de: decisión del mecanismo (alternativas por presentar); BL-017
- Estado: Parcial
- Nota (2026-11-02 16:06): avance en 0358fe9. PlayerAccountRegistrationService ya no conoce EF ni SQL y PlayerRepository captura sus excepciones y devuelve PlayerExistenceLookupStatus o bool. Falta: la detección de duplicados se borró en lugar de moverse (BL-094), y TLMG.Services.csproj:3 y 38-43 y packages.config siguen referenciando EntityFramework.
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; avance de patrón, sin cierre. Los repositorios nuevos del rango capturan SqlException y EntityException dentro de TLMG.DataAccess (VerificationCodeRepository y ChatMessageRepository; 0429cb1, 65c9d8a) y TLMG.Services no tiene using de EF ni de System.Data (git grep). Siguen sin cambio TLMG.Services.csproj:3, 38-42 y 159-162 y packages.config:3 con EntityFramework 6.5.2, y la detección de duplicados al guardar (BL-094).
- Nota (2026-11-20): revisión sobre 08297935 y configuración local cuando se indica. No se encontraron referencias a tipos EF o SqlClient en los .cs de Services mediante búsqueda; DataAccess captura excepciones en repositorios y devuelve resultados controlados, aunque la revisión no demuestra todas las rutas. TLMG.Services.csproj conserva referencias e imports EF y targets obligatorios: comprobar dependencias de compilación y ejecución antes de retirarlos o justificar la frontera. La distinción de duplicados al guardar permanece solo en BL-094; el formato PackageReference en BL-004.

### BL-039 · Arrancar las pruebas unitarias (proyecto test/ y marco)
- Creado: 2026-10-25 00:49
- Por qué: 0 pruebas frente al mínimo de 300 (Estándar §15.1). Elegir marco (§15.1: decisión superior) y crear test/TLMG.Services.Tests con un stub de IPlayerRepository.
- Depende de: decisión del marco de pruebas
- Estado: Parcial
- Nota (2026-10-25): el marco de pruebas sigue sin decidirse (Estándar §15.1 y §19.1). Se recomendó xUnit.net, sin decisión del equipo; consultar al profesor o la rúbrica. No se ha creado ningún proyecto de pruebas.
- Nota (2026-10-29 22:53): el marco ya está decidido: xUnit.net (D-61, Estándar §15.1). Sigue sin existir ningún proyecto de pruebas.
- Nota (2026-11-07 04:28): incluir las pruebas del catálogo de rutas del Gatito Fugitivo: cada ruta suma la ventana, el escape no es negativo, cada túnel sale por otra gatera y cada escondite sale por la misma caja. Depende también de BL-116.

- Nota (2026-11-11 14:08): se integran los proyectos de pruebas en la solución del servidor y se agregan el SDK y el adaptador de Visual Studio. MSBuild compiló la solución sin errores y VSTest descubrió y ejecutó 178 pruebas correctas. Pendiente de confirmar mediante commit; la meta de 300 pruebas sigue abierta.
- Nota (2026-11-15 00:32): dato del encargado (2026-11-15): 248 pruebas en verde, 104 en TLMG.Domain.Tests y 144 en TLMG.Services.Tests; el cliente sigue con 0. Conteo estático sobre 8aa6844 (no se pudo ejecutar en Linux): 2 [Fact] + 102 [InlineData] en Domain y 68 [Fact] + 76 [InlineData] en Services, que da 104 y 144 y coincide. test/ no tiene TLMG.Client.Tests (BL-164). Faltan 52 pruebas para el mínimo de 300 (Estándar §15.1). El rango actualizó stubs y pruebas a los contratos nuevos (c78a54f). No cierra la entrada.
- Nota (2026-11-18): La infraestructura xUnit y los proyectos Domain y Services existen; en la integración se ejecutaron 483 y 384 pruebas, respectivamente. No se reejecutaron en esta revisión. Quedan las pruebas del catálogo del Gatito Fugitivo incorporadas al alcance de esta entrada (BL-116) y las del cliente (BL-164).
- Nota (2026-11-18): d12839b agrega pruebas de continuidad, duración y tolerancia de la ruta; faltan pruebas explícitas de extremos distintos en túneles y del mismo escondite. BL-164 conserva las pruebas del cliente.
- Nota (2026-11-20): revisión sobre 0829793. Existen proyectos xUnit Domain y Services y ejecuciones históricas superiores al mínimo. FugitiveKittenRouteTests cubre rutas; falta verificar expresamente las propiedades de extremos de túneles y escondites añadidas al alcance. Las pruebas WPF tienen BL-164.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-041 · Migrar las pantallas a MVVM con CommunityToolkit.Mvvm (D-39, D-50)
- Creado: 2026-10-25 00:49
- Por qué: hoy no hay ViewModels, comandos ni DataContext y las 13 pantallas atan Click (Estándar §7.1, §6.2). Una pantalla por commit, empezando por CreateAccountScreen.
- Depende de: aprobación de D-50; BL-042; BL-036
- Estado: Parcial
- Nota (2026-11-07 04:28): FugitiveKittenScreen nace sin ViewModel; su estado visible vive en el code-behind. Al migrarla, crear FugitiveKittenViewModel con CatchKittenCommand, ejecutable solo mientras el tramo es visible, y un modelo de presentación propio.
- Nota (2026-11-10 12:00): avance de infraestructura. Fase 4b (8c4e9be): ViewModelBase hereda de ObservableObject y los comandos son ICommand creados con el RelayCommand del toolkit, sin generadores (§7.1, D-50); el paquete se instaló en 6eb2fec. Fase 5 (cefa0a3): LoginViewModel y CreateAccountViewModel dependen de IAccountsConnection (D-60, D-70). El resto de las pantallas sigue pendiente.
- Nota (2026-11-11 23:55): avance (b6dec93). En origin/dev-LS_Angel hay 12 pantallas migradas. Once reciben el ViewModel por constructor desde ScreenFactory (B3): Login y CreateAccount (384ef1a), MainMenu (c6a081a), ForgotPassword (7d882b1), Friends (93fa788), Lobby (0fc59c2), Options (f4b2ff6), Profile (e74e10b), NewPassword (da8518b), HowToPlay (09d5647) y Leaderboard (b6dec93). La duodécima, Verification, está en la fábrica (db51858) pero su ViewModel se crea dentro de la vista (VerificationScreen.xaml.cs:61). Gameplay tiene GameplayViewModel creado dentro de la vista (GameplayScreen.xaml.cs:32) y se migra en mvvm-resto. En la rama mvvm-resto, sin integrar: Gameplay, GameOver, RoundResult y Moderation (04a7d13) y MatchClosed (2d4bafb); con ellas serían 17. Fuera de la migración por decisión: FugitiveKitten, Loading y ChangePassword (BL-169) y MainWindow (BL-142).
- Nota (2026-11-15 00:32): revisado sobre 8aa6844. La fábrica de Verification avanzó: ScreenFactory.BuildVerificationEntry crea el VerificationViewModel y se lo pasa a la vista para los flujos AccountRegistration y PasswordRecovery (ScreenFactory.cs:242-270), con lo que Verification cumple B3, lo que la nota del 2026-11-11 23:55 dejaba pendiente (VerificationScreen.xaml.cs:61). GameplayViewModel también se crea en la fábrica (ScreenFactory.cs:101). No cambia el estado.
- Nota (2026-11-18): Avance local FK-01 sin commit: FugitiveKitten se compone con ViewModel y ScreenEntry; el estado de presentación y el comando pasan al ViewModel y las animaciones permanecen en la vista. La migración general no se cierra.
- Nota (2026-11-18): FugitiveKitten se migró a MVVM en 8c6d1d0 y se conectó al canal de partida en d12839b; la migración general sigue pendiente.
- Nota (2026-11-20): revisión sobre 0829793. La fábrica y ViewModels cubren numerosas pantallas y FugitiveKitten se migró en 8c6d1d0. Falta una comprobación completa de las pantallas del alcance original y decidir la excepción de Loading (BL-169).
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-043 · Limpieza de estilo del diagnóstico (no incluida en BL-027)
- Creado: 2026-10-25 00:49
- Por qué: variable con nombre de tipo (PlayerAccountRegistrationService.cs:81); constantes ...minimum/maximun (PlayerAccountRegistrationValidationHelper.cs:13-18); ternarios multilínea (6 sitios); 3 return de expresión (LeaderboardScreen.xaml.cs:187, 235; NewPasswordScreen.xaml.cs:113); manejadores Timer_Tick, CountdownTimer_Tick, ToastTimer_Tick y DigitBox_* (§7.2); <param> y <returns> de ValidationHelper (§12.2, §10.7); public const sin documentar; LoadLeaderboardData de 43 líneas; texto "{0} pts" en código; clave Common_ para CreateAccount_NicknameFormatError; llaves de OperationRegisterPlayerAccountResult.cs:3; referencia System.Web sin uso; Thread.CurrentThread en LanguageManager.cs:77-78 (§11.1); decidir si es variable local o constante en LobbyScreen.xaml.cs:255.
- Depende de: BL-027
- Estado: Parcial
- Nota (2026-10-25): el uso de Thread.CurrentThread en LanguageManager se corrigió en 64d1ce1. La entrada sigue abierta por el resto de su alcance.
- Nota (2026-10-25): el commit 0f14a64 cambió a Error el nivel de log del fallo inesperado en ServiceImplementation.UsersManager.cs, documentó la constante EnglishCultureCode de LanguageManager y dejó su ternario en una sola línea. La entrada sigue abierta por el resto de su alcance.
- Nota (2026-10-29 22:53): estado en 53263f1. Pendiente: variable con nombre de tipo (PlayerAccountRegistrationService.cs:83); constantes ...minimum/maximun (PlayerAccountRegistrationValidationHelper.cs:13-18); 4 ternarios multilínea (LocalizableScreen.cs:47-49, LoginScreen.xaml.cs:70-71, NewPasswordScreen.xaml.cs:99-100, VerificationScreen.xaml.cs:198-199); return de expresión (LeaderboardScreen.xaml.cs:301, NewPasswordScreen.xaml.cs:113); manejadores Timer_Tick (VerificationScreen.xaml.cs:126), CountdownTimer_Tick (LobbyScreen.xaml.cs:191), ToastTimer_Tick (:171) y DigitBox_* (VerificationScreen.xaml.cs:68, 85); public const sin documentar (ValidationHelper.cs:12-16); "{0} pts" como respaldo en LeaderboardScreen.xaml.cs:299; llaves de OperationRegisterPlayerAccountResult.cs:3; referencia System.Web en TLMG.Services.csproj:52; FIXME de LobbyScreen.xaml.cs:195 (antes :255). Ya no aplica: LoadLeaderboardData (se reemplazó en ea27455).
- Nota (2026-11-02 20:33): avance. ca112d2 documentó las public const de ValidationHelper y 391433c renombró LoadingScreen_Loaded a OnScreenLoaded; la variable con nombre de tipo ya no existe desde 0358fe9. Sigue pendiente (git grep sobre 635c66f): constantes ...minimum/maximun (PlayerAccountRegistrationValidationHelper.cs:13-18); 4 ternarios multilínea (LocalizableScreen.cs:47-49, LoginScreen.xaml.cs:70-71, NewPasswordScreen.xaml.cs:99-100, VerificationScreen.xaml.cs:198-199); return de expresión (LeaderboardScreen.xaml.cs:301, NewPasswordScreen.xaml.cs:113); manejadores Timer_Tick (VerificationScreen.xaml.cs:126), DigitBox_* (:68, 85), ToastTimer_Tick y CountdownTimer_Tick (LobbyScreen.xaml.cs:171, 191); <param> y <returns> de los tres métodos públicos de ValidationHelper (:36, 62, 97); "{0} pts" como respaldo (LeaderboardScreen.xaml.cs:299); llaves de OperationRegisterPlayerAccountResult.cs:3; referencia System.Web (TLMG.Services.csproj:52); FIXME de LobbyScreen.xaml.cs:195.
- Nota (2026-11-07 05:08): GlobalStyles.xaml tiene 77 líneas de más de 120 columnas (§9.2), todas anteriores a la pantalla del Gatito Fugitivo (medidas en caracteres sobre 5e4a8f9): 60, 61, 177, 181, 340, 341, 369, 378, 702, 804, 808, 812, 953, 957, 961, 965, 1027, 1032, 1037, 1066, 1131, 1207, 1215, 1249, 1250, 1285, 1290, 1316, 1322, 1336, 1341, 1391, 1393, 1433, 1434, 1578, 1724, 1728, 1778, 1783, 1786, 1810, 1815, 1821, 1837, 1844, 1846, 1869, 1879, 2077, 2085, 2093, 2101, 2149, 2157, 2230, 2235, 2240, 2257, 2261, 2265, 2435, 2453, 2471, 2489, 2527, 2538, 2542, 2571, 2583, 2817, 2828, 2834, 2845, 2918, 2922 y 2986. Además, FugitiveKittenScreen nace sin ViewModel y con su estado visible en el code-behind; su migración queda en BL-041.
- Nota (2026-11-11 23:55): avance (b6dec93). En la migración de LeaderboardScreen quedaron corregidos el return de expresión (LeaderboardViewModel.CreateEntry usa variable local y un solo return) y el respaldo «{0} pts» (ahora «{0}» y el texto sale del diccionario, LeaderboardViewModel.cs:29). El return de expresión de NewPasswordScreen también desapareció (da8518b; NewPasswordScreen.xaml.cs:112 devuelve una variable local). La entrada sigue abierta por el resto de su alcance.
- Nota (2026-11-20): revisión estática sobre 08297935. Las notas previas acreditan corrección de Thread.CurrentThread, nombres y migraciones. Búsqueda actual no encontró ese uso ni el respaldo literal histórico; VerificationScreen conserva DigitBox_TextChanged y DigitBox_PreviewKeyDown, patrón específico de manejador permitido por §7.2. Services.csproj aún referencia System.Web sin using localizado, pendiente confirmar necesidad; GlobalStyles tiene 80 líneas mayores de 120 caracteres. Constantes históricas de validador fueron reemplazadas por AccountFieldValidator; no se completó inspección exhaustiva de cada ternario, XML, literal y comentario histórico: conservar revisión residual y no inferir cierre. Otros hallazgos concretos mantienen sus BL.
- Nota (2026-11-20): auditoría individual sobre 08297935; disposiciones H01–H18 (no son nuevos BL):
  - H01 Corregido: 0358fe9 renombra la variable con nombre de tipo; la implementación actual usa nombres descriptivos.
  - H02 Corregido: el helper histórico fue reemplazado; ValidationRules usa MinimumLength y MaximumLength correctamente.
  - H03 Corregido en los cuatro casos identificados: LocalizableScreen no contiene ternarios, Login usa asignaciones de una línea y NewPassword/Verification usan coalescencia en una línea; los otros dos originales no están individualizados en la historia y no se identifican en esas rutas.
  - H04 Corregido: LeaderboardViewModel.CreateEntry y NewPasswordScreen.FindMessage devuelven variables; las notas históricas citan b6dec93 y da8518b.
  - H05 Corregido: OnCountdownTimerTick, OnToastTimerTick y OnScreenLoaded reemplazan nombres históricos; DigitBox_TextChanged y DigitBox_PreviewKeyDown cumplen patrón control_evento §7.2.
  - H06 Vigente obligatorio: ValidationHelper.CalculatePasswordStrength, GetEmailValidationErrorKey y GetNicknameValidationErrorKey tienen summary pero carecen de param y returns exigidos por §12.2.
  - H07 Corregido: las cuatro constantes públicas de ValidationHelper tienen summary; ca112d2 documentó las históricas.
  - H08 Corregido: LoadLeaderboardData ya no existe; RequestLeaderboardData y UpdateLeaderboard conservan consulta y presentación.
  - H09 Corregido: Leaderboard_PointsFormat vive en ambos diccionarios y FormatPoints lo usa; no queda el respaldo literal histórico en código.
  - H10 Absorbido por BL-017/BL-187: CreateAccount_NicknameFormatError usa prefijo de área en servidor y cliente; decisiones generales de claves y contratos siguen en esas entradas.
  - H11 Corregido: OperationRegisterPlayerAccountResult fue reemplazado por OperationResult usado en IUsersManager; la corrección histórica de llaves se registra en BL-027/71eef62.
  - H12 No verificable: System.Web sigue referenciado en TLMG.Services.csproj; no se localizaron usos directos comunes, pero la lectura estática no acredita innecesariedad de dependencias transitivas o de ejecución. Verificar antes de retirar.
  - H13 Corregido: LanguageManager usa CultureInfo.CurrentCulture y CurrentUICulture; no usa Thread.CurrentThread; 64d1ce1 es ancestro.
  - H14 Corregido: el FIXME señalaba CountdownSecondsLimitForStart=0; el flujo actual usa NoSecondsRemaining en LobbyViewModel y el FIXME desapareció.
  - H15 Absorbido: el FIXME histórico de Lobby ya no existe; TODO de chat y votación se siguen en BL-178 y BL-161, sin repetir auditoría de esos comentarios.
  - H16 Vigente obligatorio: GlobalStyles.xaml tiene 3592 líneas y 80 de más de 120 caracteres, contadas por longitud de caracteres excluyendo terminadores; ejemplos 60, 61 y 177. Es formato, sin defecto funcional demostrado; puede reformatearse preservando XAML y validando recursos.
  - H17 Absorbido por BL-041: ScreenFactory inyecta FugitiveKittenViewModel; la vista conserva interacción visual WPF y no se exige retirar todo code-behind.
  - H18 Corregido para el avance histórico adicional: la nota 0f14a64 registra nivel Error y documentación de EnglishCultureCode; las decisiones vigentes de logs continúan en BL-033/BL-147. No se detectó otra categoría histórica fuera de H01–H17 y este avance.
  - Límite de H03: faltan los lugares exactos de dos ternarios originales; la clasificación de esos dos es No verificable hasta obtener el diagnóstico original. Las cuatro ubicaciones posteriores sí se inspeccionaron. No se ejecutaron compilaciones ni pruebas.

### BL-044 · Reglas de validación solo en el cliente y desalineación con la BD
- Creado: 2026-10-25 00:49
- Por qué: dominios de correo, vocabulario ofensivo y apodos reservados solo se validan en el cliente (ValidationHelper.cs:17-27); Estándar §16.1 exige que el servidor valide todo. Correo hasta 254 en el validador contra VARCHAR(100); nickname 12 contra VARCHAR(20) en la BD.
- Depende de: decisión del equipo; Diccionario de Reglas de Validación en docs/
- Estado: Parcial
- Nota (2026-11-20): revisión sobre 08297935. AccountFieldValidator y PlayerAccountRegistrationValidator validan formato y longitudes también en el servidor; el servicio los invoca para registro. SQL v4.1 declara correo VARCHAR(254) y nickname VARCHAR(12), coherentes con máximos del dominio; no se comprobó la base instalada. No se identificaron filtros de dominios restringidos, apodos reservados o vocabulario ofensivo en esos validadores. El diccionario de reglas original no está disponible aquí: ratificar qué reglas son obligatorias y cubrir en servidor las que correspondan; vocabulario ofensivo continúa en BL-107. No se ejecutó una prueba de bypass.

### BL-045 · Imágenes: carpeta Images/ y valor de profile_image
- Creado: 2026-10-25 00:49
- Por qué: Estándar §4.4 pide Images/ y nombres PascalCase. Hoy hay img/default.jpg y la BD guarda "/img/default.png", que no existe y fuerza el HACK de LeaderboardScreen (LeaderboardScreen.xaml.cs:272-273). Decidir qué guarda la BD y cuándo entra la foto como byte[] (gov/CLAUDE.md §10, FIXME de PlayerRepository.cs:26).
- Depende de: decisión del equipo
- Estado: Parcial
- Nota (2026-11-11 23:55): avance (b6dec93). El HACK ya tiene comentario en el código: LeaderboardViewModel.cs:327 («Tratar como ruta de la aplicación el profile_image que guarda la BD, que no existe. (BL-045).») sobre ResolveAvatarImagePath. Sigue pendiente la decisión sobre qué guarda la BD y la carpeta Images/.
- Nota (2026-11-20): revisión estática sobre 08297935. PlayerProfileRepository guarda profile_image como clave y ProfileViewModel usa AvatarCatalog con fallback; SQL v4.1 conserva ruta heredada /img/default.png, mientras img/default.jpg existe. LeaderboardViewModel construye AvatarEmoji pero XAML enlaza AvatarImagePath, propiedad ausente en PlayerRankEntry; ResolveAvatarImagePath no tiene llamador identificado. Existe incompatibilidad de binding por inspección, por validar visualmente. Resolver representación de avatar y ruta heredada; carpeta Images y byte[] requieren decisión separada, sin migración amplia supuesta.

### BL-047 · Estados de usuario como enum
- Creado: 2026-10-25 14:23
- Por qué: el estado se maneja con cadenas ("USER_ACTIVE") en PlayerRepository.cs:23-24 y 29-30 y en PlayerLogInService.cs:17; usar un enum.
- Depende de: nada
- Estado: Postergado
- Nota (2026-11-20): revisión sobre 08297935. ValidationRules.ActiveUserState se usa para filtrar y crear usuarios en PlayerRepository y para autorizar el inicio de sesión en PlayerLogInService; SQL v4.1 conserva cadenas USER_ACTIVE, USER_SUSPENDED y USER_BANNED mediante CHECK. Un enum requeriría conversión entre dominio y almacenamiento. La sola constante no demuestra defecto funcional ni de seguridad; §3.11 regula nombres de enums, sin exigir uno para este estado. Se posterga la mejora de claridad hasta que exista necesidad concreta; las decisiones sobre cuentas suspendidas y bloqueadas se mantienen en sus requisitos propios.

### BL-048 · Los archivos generados del proxy contienen la identidad de Windows (UPN) del WSDL
- Creado: 2026-10-25 16:50
- Por qué: configuration.svcinfo, configuration91.svcinfo y service.wsdl de Connected Services/UsersManagerReference contienen la identidad de Windows (UPN) que el servidor publica en el WSDL. Es el mecanismo normal de Visual Studio y ya estaba en los proxies anteriores del historial; esos archivos no se editan a mano (Estándar §4.3).
- Depende de: BL-021 (seguridad entre dos PC, identidad explícita del endpoint)
- Estado: Requiere validación
- Nota (2026-11-17 11:49): la regeneración del proxy del 2026-11-17 (bf24d95) volvió a escribir en configuration.svcinfo, configuration91.svcinfo y service.wsdl la identidad de Windows (UPN) del equipo desde el que se regeneró. Se mantiene el criterio de la entrada: son archivos generados y no se editan a mano. No se anota aquí el usuario ni el equipo. Sin cambio de estado.
- Nota (2026-11-20): revisión sobre 08297935. En UsersManagerReference no se encontraron localmente archivos .wsdl ni .svcinfo y no aparecen rastreados; .gitignore excluye los WSDL y los dos archivos de configuración indicados. Eso reduce exposición futura de esas rutas, pero el historial documenta metadatos anteriores y una regeneración puede recrearlos. Verificar política de despliegue, identidad y exposición histórica sin publicar identificadores ni editar generados; coordinar BL-021 y BL-023.

### BL-049 · Revisar el nombre del método del callback y el atributo [ServiceContract] en IUsersManagerCallback
- Creado: 2026-10-25 17:10
- Por qué: el método `RegisterPlayerAccountResult` termina en sustantivo (§3.7 pide verbo o frase verbal; el ejemplo de §3.10 usa la forma On…) y el callback lleva `[ServiceContract]` mientras el ejemplo de §3.10 no. Cambiarlo obliga a regenerar el proxy, así que se hace en la misma regeneración que BL-030 (login).
- Depende de: decisión del encargado (extender Estándar §3.10 o renombrar)
- Estado: Requiere decisión
- Nota (2026-10-29 22:53): ya no depende de BL-030: el proxy se regeneró (445d216, 3fd8662) sin este cambio. Hoy los cuatro callbacks llevan [ServiceContract] (I*ManagerCallback.cs) y las respuestas usan el patrón …Result (CreateRoomResult, LogInResult…); la forma On… se usa solo para avisos (OnLobbyRoomChanged, OnFriendPresenceChanged). Se propone resolverlo extendiendo el Estándar §3.10 para aceptar …Result en respuestas por callback, en lugar de renombrar.
- Nota (2026-11-02 16:06): la parte de nombres queda cumplida por D-62 (Estándar §3.7 y §3.10, 53226c4). Sigue abierto el atributo [ServiceContract] en los cuatro I*ManagerCallback.cs, que el ejemplo de §3.10 no lleva.
- Nota (2026-11-20): revisión sobre 08297935. IUsersManagerCallback ya no existe en los contratos actuales. D-62 y §3.10 aceptan el sufijo Result para respuestas y On para avisos; 53226c4 es ancestro de HEAD. IFriendsManagerCallback conserva [ServiceContract], ausente en el ejemplo del Estándar §3.10. Queda consultar al responsable y aclarar o ratificar el ejemplo; no se demuestra un defecto funcional ni se propone cambiar atributos o proxies.

### BL-051 · Medir el rendimiento del login y del ranking
- Creado: 2026-10-25 19:32
- Por qué: medir primero el tiempo desde el clic hasta la respuesta. Hipótesis a descartar: costo de la primera consulta de EF6, carga de la entidad completa con seguimiento de cambios, y la unión del ranking con la imagen de todos.
- Depende de: BL-016 y BL-030
- Estado: Requiere decisión
- Nota (2026-10-25): observación: el login tarda unos 5 s con unas 20 cuentas; el tamaño de la tabla no lo explica. Hipótesis sin medir: arranque en frío de EF en la primera consulta. Pendiente: comparar el primer y el segundo inicio de sesión en la misma ejecución del servidor, y medir el ranking.
- Nota (2026-11-10 11:20): medido a mano. Primer inicio de sesión tras arrancar el servidor: ~5 s; segundo, sin reiniciar: ~1 s. Se confirma el arranque en frío de EF6 como causa principal. La tabla User no es el cuello de botella.
- Nota (2026-11-11 08:00): medido tras el calentamiento (BL-125): primer inicio de sesión ~3 s (antes ~5 s); con el servidor ya caliente y un cliente nuevo, ~1.8 s; ranking 1.42 s. El calentamiento recortó ~2 s. Queda ~1.2 s sobre el login de cliente nuevo, probablemente de la ruta de login que User.Any() no cubre (lectura completa o primera escritura), y ~0.8 s de arranque frío del cliente (primer canal WCF, seguridad de transporte, JIT). Pendiente: decidir si basta para la demostración o se amplía el calentamiento.
- Nota (2026-11-20): revisión sobre 0829793. Las mediciones históricas del 11 de noviembre son primer login ~3 s, login con servidor preparado ~1.8 s y ranking ~1.42 s; resta decidir si son aceptables para la demostración.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Requiere validación conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.
- Nota (2026-11-20): reconciliación sobre 08297935. Las mediciones históricas existen; queda decidir su aceptación, por lo que la disposición correcta es Requiere decisión.

### BL-052 · Estados USER_SUSPENDED y USER_BANNED en el login
- Creado: 2026-10-25 19:32
- Por qué: CU-03 no define qué pasa ni el texto para estas cuentas; hoy reciben el error genérico de credenciales inválidas.
- Depende de: definir la regla y los textos
- Estado: Requiere decisión
- Nota (2026-11-20): revisión sobre 08297935. PlayerRepository entrega estado en PlayerCredentials; PlayerLogInService verifica la contraseña y después exige USER_ACTIVE. Un estado no activo devuelve Login_InvalidCredentialsError, igual que credenciales incorrectas; un fallo de datos usa otra clave. SQL v4.1 contempla suspendido y bloqueado. No se encontró política definitiva que obligue revelar la distinción: decidir con el equipo si y cuándo comunicarla tras verificar identidad, coordinado con BL-165.

### BL-053 · Login con 2FA tras 30 días de inactividad (CU-03 FA-02)
- Creado: 2026-10-25 19:32
- Por qué: CU-03 FA-02 pide un segundo factor cuando la cuenta lleva 30 días sin iniciar sesión. El servidor ya guarda last_time en UTC (3b6c905).
- Depende de: BL-007 y BL-008 (el 2FA está apagado)
- Estado: Pendiente
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sin cambio. El 2FA del registro ya existe (BL-007), pero PlayerLogInService no pide código tras 30 días de inactividad (git grep de VerificationCode en PlayerLogInService.cs sin resultados).
- Nota (2026-11-20): revisión sobre 08297935. PlayerLogInService verifica contraseña y estado, actualiza last_time con UTC y crea la sesión sin flujo de código por 30 días. La verificación de registro y la recuperación son procesos distintos; las pruebas de login inspeccionadas cubren estados y actualización, no acreditan este 2FA. Al actualizar last_time antes de una comparación futura puede perderse la referencia previa si no se conserva o consulta primero. Implementar y validar el requisito CU-03 FA-02 con BL-007; no se ejecutaron pruebas.

### BL-054 · Revisión del esquema de la tabla User
- Creado: 2026-10-25 19:32
- Por qué: evaluar un catálogo de estados o el perfil en una tabla aparte, en una etapa más avanzada y después de medir (BL-051). Impacto si se hace: versión mayor del script (§5.3), modelo de EF, la base de la compañera, diccionario y SAD.
- Depende de: BL-051
- Estado: Postergado
- Nota (2026-11-20): revisión sobre 08297935. SQL v4.1 mantiene state y profile_image en User, con Player separado; no se encontró evidencia de defecto funcional que exija catálogo de estados o más tablas. Es exploración arquitectónica ligada a BL-051, no requisito demostrado para esta entrega. Retomar ante mediciones o requisitos que justifiquen el impacto en SQL, EF y documentación.

### BL-058 · Cerrar las sesiones abandonadas del SessionRegistry
- Creado: 2026-10-29 12:55
- Por qué: `SessionRegistry.EndSession` aún no tiene quién lo llame y el registro no vence sesiones. Si el cliente cierra la ventana o pierde la conexión, su sesión y su token quedan en memoria hasta reiniciar el servidor. El vencimiento debe medirse por hora límite en UTC al recibir el latido, sin temporizadores (Estándar §10.4, D-54).
- Depende de: ADR-04 y D-54; del paso que agregue el latido
- Estado: Requiere validación
- Nota (2026-10-29 22:53): avance. La sesión termina cuando el canal de amigos se cierra o falla (d6d6326; ServiceImplementation.FriendsManager.cs:121, FriendsService.EndSessionByFriendsChannel), así que cerrar la ventana o perder la conexión ya libera sesión, sala y presencia. Siguen sin vencerse una sesión cuyo canal de amigos nunca se abrió (login exitoso con SubscribeToFriends fallido o cliente caído antes de abrirlo) y una conexión TCP medio abierta, que solo detecta el receiveTimeout de WCF [S: el servidor no lo define, rige el valor por defecto]. Falta el latido de ADR-04.
- Nota (2026-11-10 12:04): el caso «login exitoso con SubscribeToFriends fallido» ya listado arriba tiene ahora un punto concreto en el cliente: ServerConnectionManager.LogIn (cefa0a3) llama a StartRegisteredSession y, si el canal de amigos falla justo después de un login correcto, muestra Login_NetworkError y descarta el token, pero el servidor ya emitió la sesión y puede dejarla viva hasta que se venza. Se registra aquí por ser equivalente y no como entrada nueva. Depende de la Fase 8.
- Nota (2026-11-17 11:49): reconexión del canal de partida (I2a, dff94a9). Sin reconexión hasta I4: si el canal de partida falla, ServerConnectionManager.Match.cs lo limpia sin desconectar la sesión, y el jugador sigue en la sesión y en la sala pero sin tablero jugable (Gameplay muestra Gameplay_MatchUnavailableLabel). No hay reintentos ni latido. En el servidor, perder el canal de partida solo quita su callback (SessionRegistry.RemoveMatchChannel) y no saca al jugador de la sala. La auditoría AUD-002 (ventana de reconexión) no tiene entrada en este archivo; la ventana de gracia sigue pendiente aquí (ADR-04). Sin cambio de estado.

- Pendiente de 1C: reconciliar el cliente si el servidor revoca el token en LogOut pero la respuesta se pierde antes de llegar.
- Nota (2026-11-20): revisión sobre 0829793. Existe barrido por silencio en SessionRegistry.Heartbeat.cs y limpieza de canales; verificar el vencimiento y limpieza de sesiones sin canal, incluida la interacción con consultas de identidad. El desarrollador informa pruebas de conexión, reconexión y abandono; no acreditan todos los casos de sesión huérfana.
- Nota (2026-11-20): revisión sobre 0829793. El desarrollador confirma conexión, reconexión, abandono y continuidad del juego con clientes reales; no acreditan el vencimiento de tokens huérfanos sin canales ni la limpieza de registros.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Requiere validación conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.
- Nota (2026-11-20): reconciliación sobre 08297935. TryGetUserId llama a TryFindSessionByToken, que actualiza LastActivityUtc cuando encuentra el token; el barrido usa ese valor. Esto justifica verificar la caducidad previa a acceso de identidad, sin declarar aquí una prueba de extremo a extremo.

### BL-060 · Retirar o reemplazar el script de demostración de amistades
- Creado: 2026-10-29 13:05
- Por qué: `database/seed/TLMG_Seed_FriendshipDemo.sql` (D-59) existe solo para revisar el lobby mientras las solicitudes reales no estén implementadas. No es parte del esquema y no debe llegar a una instalación real. Se elimina, o se sustituye por datos de prueba más completos, cuando exista BL-062.
- Depende de: BL-062
- Estado: Requiere decisión
- Nota (2026-11-11 23:55): BL-062 quedó finalizada (6696c4f, 27cb845, 93fa788) y database/seed/TLMG_Seed_FriendshipDemo.sql sigue en el repositorio. Falta decidir si se elimina o se sustituye por datos de prueba más completos.
- Nota (2026-11-20): revisión sobre 08297935. El script de seed declara uso de demostración no productivo y transacción; puede insertar o actualizar una amistad existente como aceptada si se suministran cuentas. La búsqueda local no encontró invocaciones literales desde archivos no SQL; eso no prueba el contenido del paquete de entrega. Las solicitudes reales existen; decidir si conservarlo solo para desarrollo y verificar su exclusión de instalación y entrega. No se ejecutó.

### BL-066 · Vencimiento de autorizaciones de entrada y de sesiones sin canal de lobby
- Creado: 2026-10-29 15:07
- Por qué: una autorización emitida y nunca usada, o una sesión que no vuelve a abrir su canal de lobby, ocupa memoria hasta que la sala se elimina o la sesión termina. Medir el plazo por hora límite en UTC al recibir el latido, sin temporizadores (Estándar §10.4, D-54).
- Depende de: BL-058
- Estado: Parcial
- Nota (2026-11-20): revisión estática sobre 08297935. Heartbeat evalúa enlaces silenciosos y sesiones sin enlace tras 30 s, excluyendo participación activa recuperable. Las autorizaciones usan HashSet de ids sin plazo UTC; se retiran al consumir, cerrar sala o terminar sesión. Falta vencimiento diferido de autorizaciones y comprobar limpieza de sesiones sin canal; BL-058 conserva defectos de sesión y BL-072 el caso concreto de autorización. Hay pruebas de heartbeat, no se ejecutaron.

### BL-069 · Rechazar, cancelar y vencer invitaciones a sala
- Creado: 2026-10-29 15:42
- Por qué: hoy una invitación solo se acepta; solo desaparece al aceptarla, al eliminarse su sala o al terminar la sesión de quien invita o de quien recibe. Faltan rechazarla (invitado), cancelarla (quien invita) y vencerla por hora límite en UTC al recibir el latido, sin temporizadores (Estándar §10.4, D-54). Una invitación repetida al mismo jugador para la misma sala no vuelve a avisar, así que si el primer aviso se perdió el invitado no la ve.
- Depende de: BL-058
- Estado: Parcial
- Nota (2026-11-11 23:55): hallazgo en el cliente (93fa788). FriendsViewModel.ExecuteInviteFriend (:230) no bloquea el segundo clic mientras llega el resultado (CanExecuteInviteFriend, :240, solo mira CanBeInvited), así que un doble clic envía dos InviteFriendToRoom y, como el servidor no vuelve a avisar de una invitación repetida, el segundo no se nota.
- Nota (2026-11-20): revisión estática sobre 08297935. IRoomInvitationManager ofrece invitar y aceptar; RoomInvitationRecord no guarda vencimiento. La sala o sesión eliminada limpia invitaciones. No se identificaron operaciones de rechazo o cancelación ni plazo UTC. CreateOrFindInvitation reutiliza una invitación repetida y FriendsService solo notifica cuando es nueva; FriendsViewModel no bloquea la invitación mientras espera. Revisar notificaciones y ciclo completo; no son solicitudes de amistad.

### BL-071 · Actualizar la lista de amigos guardada en el canal cuando cambien las amistades
- Creado: 2026-10-29 15:42
- Por qué: el registro guarda los ids de amigos de cada canal al suscribirse, para avisar la presencia sin consultar la base de datos al terminar la sesión. Cuando existan solicitudes de amistad reales (BL-062), un amigo nuevo o eliminado no se refleja hasta que el jugador se vuelva a suscribir; hay que refrescar esa lista o avisar el cambio.
- Depende de: BL-062
- Estado: Parcial
- Nota (2026-11-20): revisión sobre 0829793. FriendsService usa AddFriendshipToOnlineChannels al aceptar amistades. La eliminación y actualización correspondiente siguen dependiendo de BL-156.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-072 · Retirar la autorización de entrada que ya no sirve
- Creado: 2026-10-29 15:42
- Por qué: una autorización emitida al aceptar una invitación se conserva si el invitado nunca llama EnterLobby, o si la sala estaba llena. Solo se retira al entrar, al eliminarse la sala o al terminar la sesión del invitado. Definir su vencimiento junto con BL-066.
- Depende de: BL-066
- Estado: Pendiente
- Nota (2026-11-20): revisión estática sobre 08297935. La autorización se guarda como HashSet sin fecha; EnterRoom solo la consume tras entrada exitosa. Si no se llama o se rechaza por sala llena, permanece hasta limpieza de sala o sesión; podría permitir entrar después si siguen cumpliéndose las demás condiciones. No hay vencimiento UTC identificado. BL-066 lleva el mecanismo de plazo; conservar aquí el caso de uso y sus pruebas de autorización.

### BL-077 · Mostrar en pantalla el fallo de la suscripción a los amigos
- Creado: 2026-10-29 16:36
- Por qué: si SubscribeToFriends responde con ErrorKey (por ejemplo, fallo de la base de datos), el jugador queda con sesión pero sin canal de amigos registrado en el servidor y nadie lo ve. El manager pasa el error tal cual y ninguna pantalla lo atiende todavía.
- Depende de: BL-076
- Estado: Parcial
- Nota (2026-10-29 22:53): avance (33dc497). Si FriendsScreen está abierta, muestra el fallo traducido; si FriendList es null al abrirla, pide la lista otra vez. Fuera de esa pantalla, el fallo sigue sin mostrarse.
- Nota (2026-11-20): revisión estática sobre 08297935. FriendsViewModel se suscribe en Activate, desuscribe en Deactivate y muestra ErrorKey fallida si recibe el evento; solicita lista si es null. El manager emite el evento sin guardar el error para otras pantallas. Recovery intenta reabrir canal de amigos cuando el cliente es null, con límite; eso no demuestra que una respuesta fallida con canal abierto reintente ni que haya aviso global. Falta verificar degradación y recuperación, incluyendo posibles avisos perdidos; coordinar con BL-058.

### BL-079 · El cliente se bloquea hasta 10 s al abrir un canal sin servidor
- Creado: 2026-10-29 16:36
- Por qué: Open() es síncrono con el openTimeout de D-46. Si el servidor no responde, la interfaz se congela hasta 10 s en el login, en OpenLobbyChannel y en el envío. Sin Task ni hilos, la salida es una pantalla de espera con DispatcherTimer o abrir el canal antes de mostrarla. El Estándar no lo regula.
- Depende de: decisión del encargado (pantalla de espera o abrir el canal antes de mostrarla); relacionada con BL-020
- Estado: Requiere validación
- Nota (2026-11-20): revisión sobre 0829793. El desarrollador informa que con el servidor apagado se mostró un error, se desconectó y se regresó al login. No se midieron la duración ni la capacidad de interacción durante Open().
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Requiere validación conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.
- Nota (2026-11-20): R2A: incluir la respuesta de interfaz durante el cierre síncrono de canales (alcance residual de BL-020), además de apertura y envío; las capturas de excepciones se registraron como implementadas en BL-078.

### BL-080 · Extraer IServerConnectionManager cuando exista el primer ViewModel o prueba que lo necesite
- Creado: 2026-10-29 16:36
- Por qué: ServerConnectionManager es una clase de instancia que vive en App ((App)Application.Current).ServerConnection), con API y eventos públicos y sin estado estático. Para probar un ViewModel o una pantalla (D-61, D-51, ADR-02) hay que sustituirlo por una versión simulada, lo que exige una interfaz. Se extrae con un cambio mecánico solo cuando haya un primer consumidor que la necesite (D-31); la acción de crear los clientes WCF seguirá dentro del manager o de una fábrica que se decida entonces.
- Depende de: BL-039; BL-041
- Estado: Requiere decisión
- Nota (2026-11-10 12:06): la Fase 5 (cefa0a3) aplicó D-70 y creó IAccountsConnection, una interfaz por área con sufijo Connection que implementa ServerConnectionManager (D-60); Friends, Lobby y Chat se extraerán cuando una pantalla los necesite (D-31). Una sola IServerConnectionManager, como pide el título, probablemente ya no aplica tal cual: queda por confirmar con el encargado si esta entrada se descarta o se limita a las áreas pendientes.
- Nota (2026-11-20): revisión sobre 08297935. D-60 y D-70 (§3.14 del Estándar) mantienen ServerConnectionManager e interfaces por área. IAccountsConnection se incorporó en cefa0a3, ancestro de HEAD; IFriendsConnection, ILobbyConnection, IMatchConnection y otras se inyectan en ViewModels. No existe IChatConnection: chat se expone en ILobbyConnection, consumida por LobbyViewModel y GameplayViewModel. La interfaz monolítica original carece de necesidad demostrada; queda decidir si esa agrupación satisface D-70 y las pruebas antes de descartar o acotar el requisito. No se ha probado sustitución en ejecución.

### BL-081 · Hallazgos de estilo del Estándar para la capa de comunicación
- Creado: 2026-10-29 16:36
- Por qué: §3.14 no define un sufijo para la implementación de los callbacks (ServerCallback, nombrada por analogía con las interfaces I…ManagerCallback). Además, el manejador de Closed/Faulted y el último catch de la apertura de canales registran y no relanzan, igual que los del servidor; §10.2 sigue pidiendo relanzar o devolver un resultado y no cubre manejadores de eventos de canal. (Se retiró de esta entrada lo que dependía de la forma estática: el acceso global ya está cubierto por el patrón existente ((MainWindow)Application.Current.MainWindow).)
- Depende de: decisión del encargado
- Estado: Requiere decisión
- Nota (2026-11-20): revisión estática sobre 08297935. El Estándar §3.14 define sufijos de interfaces callback, no el de su implementación ServerCallback. ServerConnectionManager suscribe Closed y Faulted a OnServerChannelEnded; apertura y cierre capturan errores de comunicación y timeout. §10.2 y gov/CLAUDE.md no describen del mismo modo los límites de captura; falta aclarar tratamiento de manejadores sin exigir relanzamiento indiscriminado. La red del servidor sigue en BL-093; documentar decisión con el encargado.

### BL-082 · Reactivar los controles ocultos de LobbyScreen
- Creado: 2026-10-29 21:38
- Por qué: la Pieza 2 ocultó (Collapsed) el chat, el panel de jugadores listos, el combo de cartas por jugador, el bloque del perfil propio, y dejó sin uso el modal de expulsión, el toast, la cuenta regresiva y el popup de opciones; no tienen servidor detrás. StartCountdown y BrdPlayer_MouseRightButtonDown quedan sin quien los llame. Los controles ocultos conservan textos de prueba (NYANPROWLER, WhiskerKnight, 142, #38, 1/3, chat simulado, Lobby_KickModalDescription con NyanProwler). La simulación de "listo" sobre las tarjetas de prueba se retiró; se recupera de 376a6fe.
- Depende de: contratos de listo, chat y expulsión; BL-075 (perfil propio); BL-046 (GameplayScreen)
- Estado: Parcial
- Nota (2026-11-03 10:31): el servidor ya entrega el nickname y el id del jugador en OperationLogInResult (UserId y Nickname, solo si el inicio de sesión fue aceptado; parte del servidor de G6). Con eso se puede reactivar el nombre del bloque del perfil propio; las victorias y el rango siguen sin dato del servidor (BL-103).
- Nota (2026-11-03 12:28): avance de G6: el servidor entrega UserId y Nickname en OperationLogInResult (7f23bda) y el proxy del cliente ya los trae (bcf1ca7). Falta el commit del cliente; las victorias y el rango del bloque del perfil siguen sin dato del servidor (BL-103).
- Nota (2026-11-11 23:55): avance (0fc59c2). El combo de cartas por jugador se muestra con sala y se conecta a ChangeCardsPerPlayer; la cuenta regresiva pasó al LobbyViewModel (IsCountdownVisible). En LobbyScreen.xaml el chat no está en Collapsed. Siguen ocultos: bloque del perfil propio (:159, :176), panel de listos (:365), modal de expulsión (:21) y toast (:63); la votación de expulsión es BL-161.
- Nota (2026-11-20): revisión estática sobre 08297935. LobbyViewModel conecta chat, cambios de modalidad, jugadores y cuenta regresiva; XAML muestra lista Players con IsReady, aunque el panel separado de listos sigue Collapsed. El perfil propio y estadísticas siguen ocultos; la votación y su modal o toast no acreditan moderación real. Precisar y validar la presentación funcional restante; estadísticas corresponden a BL-103 y votación/moderación a BL-161/BL-165, sin activar simulaciones.

### BL-083 · El cliente no puede consultar al servidor su sala actual
- Creado: 2026-10-29 21:38
- Por qué: ILobbyManager no tiene una operación para pedir la sala del jugador; la foto vive solo en ServerConnectionManager.CurrentRoom. Si se perdiera con la sesión viva, CreateRoom respondería Lobby_AlreadyInRoomError sin sala y el jugador quedaría atorado hasta salir o cerrar sesión.
- Depende de: decisión del encargado
- Estado: Parcial
- Nota (2026-11-20): revisión estática sobre 08297935. GetRecoveryContext valida token y sesión registrada, evalúa plazos y devuelve RoomId y MatchId solo para partida recuperable de un jugador que no abandonó; token inválido falla. No devuelve sala en espera o cuenta regresiva sin MatchId. El cliente mantiene CurrentRoom y recupera contexto de partida, pero no queda demostrada reconstrucción de sala previa si pierde estado local. Verificar el caso sin contradecir V3: al perder conexión en sala el jugador sale; no inventar reincorporación.

### BL-084 · El máximo de jugadores por sala está duplicado en el cliente
- Creado: 2026-10-29 21:38
- Por qué: LobbyScreen usa la constante 6 (Reglas §1.1.1) solo para mostrar "n/6"; el valor que manda es el de SessionRegistry. LobbyRoom no trae la capacidad. Decidir si se agrega al DTO o se acepta la duplicación.
- Depende de: decisión del encargado
- Estado: Requiere decisión
- Nota (2026-11-20): revisión estática sobre 08297935. LobbyViewModel.MaximumPlayersPerRoom y ValidationRules.MaximumPlayersPerMatchCount valen 6, conforme a dominio V3 §1; el servidor comprueba el límite y LobbyRoom no publica capacidad. La duplicación solo puede desalinear la presentación si cambia la regla, no demuestra defecto actual. Decidir si aceptar la constante fija o propagar capacidad; un cambio de DTO no se justifica automáticamente.

### BL-086 · Mostrar si un amigo ya está en una sala
- Creado: 2026-10-29 21:52
- Por qué: Friend solo trae IsOnline; el cliente habilita Invitar con cualquier amigo en línea y el servidor rechaza con Friends_CannotInviteError si ya está en una sala. Falta decidir si la presencia incluye "en sala" o "en partida" (Friends_StatusInGame existe sin uso).
- Depende de: decisión del encargado
- Estado: Postergado
- Nota (2026-11-20): revisión estática sobre 08297935. Friend expone IsOnline; FriendsViewModel permite invitar a amigos en línea cuando hay sala. El servidor rechaza amigos ya en sala con Friends_CannotInviteError, cuyo texto explica la situación en ambos idiomas. Friends_StatusInGame existe; no se encontró requisito V3 explícito de mostrar estado ocupado. Retomar si el equipo prioriza esta mejora de experiencia; no se propone ampliar contratos ahora.

### BL-087 · Aviso de invitación propio en lugar de MessageBox
- Creado: 2026-10-29 21:52
- Por qué: la invitación usa MessageBox Sí/No: los botones los traduce Windows y no dicen Aceptar/Rechazar, es modal, y dos invitaciones seguidas apilan avisos. Rechazar no avisa a quien invitó (BL-069).
- Depende de: BL-042; BL-069
- Estado: Parcial
- Nota (2026-11-20): revisión estática sobre 08297935. MainWindow muestra MessageBox YesNo: el texto del aviso se traduce mediante recursos, los botones dependen de Windows. Yes llama AcceptRoomInvitation y el resultado exitoso inicia EnterLobby; No solo cierra el diálogo, sin avisar al servidor ni al emisor. La modalidad puede acumular avisos; no se midió en ejecución. La presentación y localización siguen aquí; rechazo, cancelación y avisos funcionales siguen en BL-069.

### BL-088 · Marca visual del creador de la sala en LobbyScreen
- Creado: 2026-10-29 22:40
- Por qué: se pidió destacar al jugador que creó la sala (por ejemplo con un contorno de color), solo como marca visual y sin ninguna capacidad: la Regla 1.1.2 y el RF-12 dicen que ningún jugador inicia ni lidera la partida. LobbyRoom y LobbyPlayer no traen quién creó la sala; hace falta un dato en el contrato (por ejemplo IsCreator) o decidir una inferencia. Cambia contrato, servidor y proxy.
- Depende de: decisión del encargado; revisión del 30-oct
- Estado: Postergado
- Nota (2026-11-20): revisión estática sobre 08297935. LobbyScreen.xaml contiene Lobby_RoomCreator dentro de un Grid Collapsed, sin nombre enlazado; LobbyRoom y LobbyPlayer no exponen identidad explícita del creador. No hay evidencia de marca visible autoritativa. V3 distingue creación y orden, sin otorgar privilegios; la marca original es visual. Retomar si el encargado confirma prioridad y fuente autoritativa, sin atribuir liderazgo.

### BL-089 · Invitados en LobbyScreen y FriendsScreen: aviso repetido y botón Invitar Amigos visible
- Creado: 2026-10-29 22:53
- Por qué: LobbyScreen muestra Lobby_GuestCannotCreateRoomError en un MessageBox en cada Loaded (LobbyScreen.xaml.cs:28 y ShowOrCreateRoom), así que vuelve a salir al regresar de FriendsScreen; FriendsScreen reescribe su mensaje de invitado en cada carga (FriendsScreen.xaml.cs:139). El botón "Invitar Amigos" de LobbyScreen sigue visible y habilitado para el invitado, que no tiene amigos. Decidir si el invitado entra siquiera a LobbyScreen y FriendsScreen desde el menú (MainMenuScreen.xaml.cs:32, 37) o si se ocultan esos botones.
- Depende de: decisión del encargado (rol invitado en el lobby, gov/CLAUDE.md §4)
- Estado: Parcial
- Nota (2026-11-03 15:41): cuando el invitado pase por el servidor, su número lo debe asignar el servidor y no el cliente, porque dos clientes podrían elegir el mismo. El formato lo fijan hoy las Reglas §3.0.1 ("Jugador N"); si se prefiere otro, hay que cambiar las Reglas en el mismo paso.
- Nota (2026-11-11 08:08): prueba del 2026-11-11: el invitado entra al menú sin pasar por el servidor, por eso no aparece en el log del servidor. La BD v4.0 conserva dbo.Guest_Session (database/TLMG_DB_v4.0.sql:210, «sin cambios hasta decidir el invitado», :32) y el modelo EF la mapea, pero ningún servicio la usa (D-15).
- Nota (2026-11-11 23:55): avance. FriendsScreen (93fa788): el invitado ve un solo mensaje fijo, sin caja de nickname, botón Añadir ni pestaña Pendientes (FriendsViewModel.IsGuest). Siguen abiertos: LobbyViewModel.cs:374 muestra Lobby_GuestCannotCreateRoomError en cada Activate (0fc59c2), el botón Invitar Amigos de LobbyScreen.xaml:199 sigue visible para el invitado y MainMenuViewModel.cs:30 conserva el TODO del nickname vacío.
- Nota (2026-11-20): revisión estática sobre 08297935. FriendsScreen oculta controles mediante IsRegisteredPlayer y muestra aviso de invitado. LobbyViewModel.Activate llama ShowOrCreateRoom, que muestra error si necesita sala y no hay sesión registrada; el botón Invitar Amigos permanece visible sin condición de invitado. No se acredita repetición mediante prueba actual. Ajustar y validar presentación tras definir sesión invitada real en BL-111; esa funcionalidad no se implementa aquí.

### BL-091 · Puesto y puntos del jugador en sesión en LeaderboardScreen
- Creado: 2026-10-29 22:53
- Por qué: LeaderboardScreen.xaml.cs:43-45 muestra un puesto (#38) y unos puntos (1840) fijos. El ranking ya viene del servidor, pero el cliente no sabe quién es (BL-075) y ILeaderboardManager no devuelve la posición de quien consulta. Sustituye el pendiente que quedaba en BL-030; su FIXME debe citar esta entrada.
- Depende de: BL-075; decisión de contrato (ILeaderboardManager con sessionToken o dato en el login)
- Estado: Parcial
- Nota (2026-11-03 10:31): el servidor ya entrega el nickname y el id del jugador en OperationLogInResult (UserId y Nickname, solo si el inicio de sesión fue aceptado; parte del servidor de G6). Como LeaderboardPlayer no trae id, el cliente puede ubicarse en el ranking por su nickname (único en User); si el jugador no está entre los primeros 100, el puesto y los puntos propios siguen ocultos hasta BL-103.
- Nota (2026-11-03 12:28): avance de G6: el servidor entrega UserId y Nickname en OperationLogInResult (7f23bda) y el proxy del cliente ya los trae (bcf1ca7). Falta el commit del cliente que resalta la fila propia por nickname; el puesto y los puntos propios siguen ocultos hasta BL-103.
- Nota (2026-11-03 15:41): sigue abierta: el resaltado de la fila propia por nickname (parche 3 de G6) aún no se aplica. El puesto y los puntos propios dejan esta entrada y pasan a BL-103.
- Nota (2026-11-11 23:55): estado (b6dec93). El FIXME se conserva en LeaderboardViewModel.cs:45 con el puesto y los puntos fijos. El resaltado de la fila propia por nickname sigue sin existir (PlayerRankEntry no tiene una propiedad de jugador local).
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-092 · El historial de navegación crece sin límite
- Creado: 2026-10-29 22:53
- Por qué: ShowScreen guarda la pantalla anterior por defecto (MainWindow.xaml.cs, saveInHistory = true), y los botones de regreso de Options, Profile, HowToPlay y Leaderboard crean un MainMenuScreen nuevo (OptionsScreen.xaml.cs:94, ProfileScreen.xaml.cs:36, HowToPlayScreen.xaml.cs:161, LeaderboardScreen.xaml.cs:326) en lugar de usar GoBack. Cerrar Sesión (MainMenuScreen.xaml.cs:62) también apila el menú y el login apila LoginScreen (LoginScreen.xaml.cs:86, 233). Cada ida y vuelta deja pantallas vivas en la pila. Solo FriendsScreen usa GoBack. Decidir una regla de navegación (GoBack o ShowScreen sin historial) junto con BL-042.
- Depende de: BL-042
- Estado: Requiere validación
- Nota (2026-11-11 08:06): la Fase 6.3 (6d344e7) unificó la pila en el NavigationManager y la 6.5 (57eafa3) saca el login del historial. El puente legacy sigue empujando duplicados al crear un MainMenuScreen nuevo en cada regreso.
- Nota (2026-11-11 23:55): avance (b6dec93). Friends (93fa788), Options (f4b2ff6), Profile (e74e10b), HowToPlay (09d5647) y Leaderboard (b6dec93) regresan con INavigationManager.GoBack (git grep: FriendsViewModel.cs:227, OptionsViewModel.cs:136, ProfileViewModel.cs:148, HowToPlayViewModel.cs:86 y :93, LeaderboardViewModel.cs:372). Lobby sale con ClearHistory y NavigateToWithoutHistory al menú (0fc59c2) y Cerrar sesión limpia el historial (c6a081a). Los casos que describe el Por qué ya no apilan en dev-LS_Angel; GameOver, Gameplay, MatchClosed y RoundResult usan NavigateToWithoutHistory (BL-141). Queda decidir si se cierra junto con BL-141.
- Nota (2026-11-18): Los regresos identificados usan GoBack o navegación sin historial, y la salida de GameOver limpia el historial (992eb8d). Continúa el puente ShowExistingScreen y su uso legacy; queda por verificar que los flujos restantes no acumulen pantallas (BL-141).
- Nota (2026-11-20): revisión estática sobre 08297935. Los retornos de Friends, Options, Profile, HowToPlay y Leaderboard usan GoBack; login/logout, Lobby, Gameplay, FugitiveKitten y GameOver emplean navegación sin historial o limpieza según flujo. RoundResult y MatchClosed usan NavigateToWithoutHistory; no limpian explícitamente historial en esas rutas. ShowExistingScreen permanece como puente. No se identificó reproducción de acumulación en los recorridos originales, pero falta validar secuencias reales restantes y el estado de la pila; no se midió memoria. No retirar el puente por sí solo (BL-141), ni confundir destinos con BL-143.

### BL-093 · Red de seguridad de excepciones en los puntos de entrada y en los cierres de canal del servidor
- Creado: 2026-11-02 16:06
- Por qué: 0358fe9 quitó los catch (Exception) de las 10 operaciones de contrato (ServiceImplementation.*Manager) y de RemovePlayerOfLobbyChannel y EndSessionOfFriendsChannel. §10.2 los permite en puntos de entrada y límites, y D-45 pide registrar lo inesperado y responder con un mensaje genérico. Hoy un error inesperado no llega a log4net (no hay IErrorHandler ni trazas WCF; includeExceptionDetailInFaults=false), falla el canal y el jugador ve "Se perdió la conexión"; en un manejador de cierre de canal puede terminar el proceso del servidor. Opción recomendada: un método privado que envuelva cada operación y cada limpieza de canal con un único catch que registra con _log.Error y responde con Common_UnexpectedError.
- Depende de: decisión del encargado (un catch por operación o un método común)
- Estado: Parcial
- Nota (2026-11-02 17:23): diferida por decisión del encargado; pendiente conocido. Severidad alta: es una deuda latente; un error inesperado no deja rastro en el log y en un cierre de canal puede terminar el proceso del servidor.
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sigue Pendiente. Hecho: los catch (OutOfMemoryException) que lanzaban FaultException<SystemFault> con Common_OutOfMemoryError pasaron de 17 a 5: 0 en Leaderboard, Lobby, Chat y Friends, y quedan solo los de ServiceImplementation.UsersManager.cs. El cambio vino en bbf1a49, 0e568ef y 4f122cc. Sigue sin catch (Exception) general en los puntos de entrada y sin IErrorHandler (git grep: solo Program.cs:75 y PlayerRepository.cs:115). Decisión del encargado (2026-11-15): el manejo actual de excepciones se mantiene sin cambios por ahora; la forma de resolverlo se definirá después. Dependencia con BL-098 sin cambio.
- Nota (2026-11-15 00:32): hallazgo de estilo, independiente de lo anterior (Estándar §9): el cuerpo de las operaciones quedó con un nivel de sangría de más tras quitar el try, por ejemplo en ServiceImplementation.LeaderboardManager.cs:11-18; revisar los demás ServiceImplementation.*Manager.cs.
- Nota (2026-11-20): revisión estática sobre 08297935. SafeNotify captura excepciones de comunicación, timeout, objeto liberado y memoria durante callbacks; repositorios capturan varias excepciones de datos. Las operaciones dúplex como SubscribeToFriends llaman al servicio antes de SafeNotify sin captura general; un fallo anterior puede impedir el callback. Los manejadores Closed/Faulted delegan limpieza que tampoco tiene red general visible. No se reprodujo caída ni se acreditó diagnóstico global; includeExceptionDetailInFaults=false evita detalle en faults. El Estándar §10.2 y gov/CLAUDE.md describen alcances distintos para catch general: decidir tratamiento de límites, registrar errores y probar sin capturas indiscriminadas.
- Nota (2026-11-20): R2B1, HEAD 08297935. Los FaultReason de los faults OutOfMemory de UsersManager siguen con texto fijo en español aunque SystemErrorMessage es una clave y el cliente tipado usa Detail.SystemErrorMessage. Verificar exposición por otras rutas y decidir tratamiento en los límites WCF; no se ejecutó prueba.

### BL-094 · El registro ya no distingue el correo o nickname duplicado al guardar
- Creado: 2026-11-02 16:06
- Por qué: 0358fe9 cambió PlayerRepository.CreatePlayer a bool (PlayerRepository.cs:86) y borró ResolveSaveFailureErrorKey e IsDuplicateKeyError, que BL-037 pedía mover a DataAccess y no borrar. Si dos registros con el mismo correo o nickname coinciden, el jugador recibe CreateAccount_DataModelError en lugar de EmailTaken o NicknameTaken. Seguir el patrón de PlayerExistenceLookupStatus: un estado de creación (Created, EmailTaken, NicknameTaken, DataFailure) decidido en DataAccess.
- Depende de: nada
- Estado: Pendiente
- Nota (2026-11-02 17:23): diferida por decisión del encargado; pendiente conocido.
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sin cambio. PlayerRepository.CreatePlayer sigue devolviendo bool (PlayerRepository.cs:82) y el rango agregó un segundo llamador, la finalización del registro tras verificar el código (PlayerAccountRegistrationService.cs:227-238), que ante una carrera devuelve CreateAccount_DataModelError en lugar de EmailTaken o NicknameTaken. La comprobación previa de duplicados no cubre dos finalizaciones simultáneas.
- Nota (2026-11-20): revisión sobre 08297935. IPlayerRepository.CreatePlayer devuelve bool; PlayerRepository registra DbUpdateException y otros errores, sin distinguir duplicados. FinalizePlayer comprueba duplicados antes de guardar y traduce false a ErrorKeys.CreateAccount.DataModel; la ruta directa hace lo mismo. SQL v4.1 declara UQ_User_email y UQ_User_nickname: con esas restricciones aplicadas se protege unicidad, pero una carrera posterior a la consulta termina en error genérico. Lectura estática, sin prueba concurrente ni verificación del despliegue. Diferenciar duplicados de fallos de datos sin perder logs.

### BL-098 · Dos mecanismos de avisos en el servidor
- Creado: 2026-11-02 16:06
- Por qué: el lobby usa LobbyOutcome y LobbyNotification con NotifyLobbyMembers, y amigos y sesiones usan SessionOutcome con Action y SafeNotifyAll; LobbyService.CreateNotifications y SessionNotificationHelper.CreateRoomNotifications repiten la conversión de la foto de sala. Unificar en SessionOutcome y unir los manejadores Closed y Faulted de cada canal en uno solo.
- Depende de: BL-093 (mismos archivos)
- Estado: Postergado
- Nota (2026-11-02 17:23): diferida por decisión del encargado; pendiente conocido.
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sin cambio. Siguen los dos mecanismos: LobbyOperationResultWithNotification y LobbyChangeNotification en el lobby (carpeta Lobby/ y ServiceImplementation.LobbyManager.cs) y ResultWithNotification<T> con Action en amigos, chat y cuentas (ChatService.cs, FriendsService.cs y ServiceImplementation.UsersManager.cs). Los manejadores Closed y Faulted siguen separados (ServiceImplementation.LobbyManager.cs:109-110 y ServiceImplementation.FriendsManager.cs:100-101).
- Nota (2026-11-18): ffc1d3d unificó la proyección de avisos de sala en SessionNotificationHelper y ResultWithNotification. Los callbacks Closed y Faulted del servidor siguen usando manejadores separados que delegan en un método común; queda pendiente decidir o completar la unificación prevista.
- Nota (2026-11-20): revisión estática sobre 08297935. ffc1d3d, ancestro de HEAD, unificó proyección de sala con SessionNotificationHelper y ResultWithNotification; lobby, amigos y partida usan notificaciones y SafeNotify. Closed y Faulted son manejadores separados que delegan limpieza común; esa separación no demuestra duplicación defectuosa. Retomar solo si se identifica divergencia funcional; la red de excepciones continúa en BL-093.

### BL-101 · Corregir los defectos del Estándar detectados en las revisiones del lobby
- Creado: 2026-11-02 16:06
- Por qué: §10.2 (línea 2391) "Ejemplo correcto " perdió su descripción en 53226c4; §11.1 tiene un "Ejemplo incorrecto" idéntico al correcto, "Ejemplo Ejemplo incorrecto" y espacios sueltos en las reglas (2) y (3); §5.3 tiene sangría de 3 espacios y "v<mayor>" frente al archivo v3.1; §4.3 (1251, 1274) tiene errores de puntuación; §6.2 marca SessionRegistry y Observador como "por implementar"; la tabla de §10.3 no coincide con el código; §10.2 no dice que todo punto de entrada y todo manejador de canal deba tener red, lo que permitió 0358fe9. Complementa BL-090.
- Depende de: el encargado (se edita en Docs y se reexporta, BL-050)
- Estado: Pendiente
- Nota (2026-11-20): Revisión sobre 08297935: §10.2 conserva un encabezado «Ejemplo correcto» sin descripción (líneas 2399 y 2426); §11.1 conserva los ejemplos correcto e incorrecto con el mismo código (3162–3194), «Ejemplo Ejemplo incorrecto» (3196) y espacios en las reglas (2) y (3). §5.3 conserva sangría y el nombre v<mayor> pese a versiones menores; §4.3 conserva defectos editoriales (por ejemplo LMG.Services en 1330), cuya puntuación histórica debe cotejarse en la fuente editable. §6.2 todavía marca SessionRegistry y Observador por implementar (1527 y 1530). §10.3 presenta una tabla de módulos que requiere cotejo completo con el código actual; §10.2 regula capturas generales pero no explicita una red obligatoria para cada punto de entrada y cierre de canal. No se acredita la corrección completa de ningún hallazgo histórico; corregir o ratificar cada punto en el documento fuente y reexportar.
- Nota (2026-11-20): R2A: incluir el ejemplo de LanguageManager public frente a internal (BL-090) y cotejar §11.1 y §6.2 por estado de salas y presencia (BL-068/BL-074); no presumir que la regla histórica de cierre del canal sigue vigente.

### BL-103 · Operación de perfil con puntos, rango y racha del jugador en sesión
- Creado: 2026-11-03 10:31
- Por qué: la opción 1 de G6 solo agrega UserId y Nickname a OperationLogInResult; no trae puntos, rango ni racha. Hace falta una operación con sessionToken que devuelva el perfil del jugador en sesión (la identidad sale del token, nunca de un id enviado por el cliente). Mientras no exista, BL-091 y BL-100 siguen mostrando esos valores ocultos.
- Depende de: G6 (parte del cliente: regenerar el proxy y guardar la identidad en ServerConnectionManager); decisión del encargado sobre el contrato
- Estado: Parcial
- Nota (2026-11-11 23:55): avance. ProfileViewModel migrado a B3 (e74e10b) conserva los datos de muestra: DaysSincePasswordChange = 45 (ProfileViewModel.cs:16), «WhiskerKnight» (:23) y, en ProfileScreen.xaml, 142, 1,840, #38 y 87% (:91-112). Su <summary> cita esta entrada; el FIXME de LeaderboardViewModel cita BL-091, que remite aquí. Sin cambios de fondo hasta que exista la operación de perfil.
- Nota (2026-11-17 11:49): avance (dff94a9). Gameplay dibuja el mismo avatar para todos los jugadores porque ni la vista de partida (MatchPlayerView) ni el inicio de sesión traen imagen. Los avatares reales dependen de esta operación de perfil y de un campo de imagen en el contrato de partida. Sin cambio de estado.
- Nota (2026-11-18): GetPlayerProfile y UpdatePlayerProfile ya existen (e53df9a), pero OperationPlayerProfileResult solo expone nickname y avatar; aún faltan puntos, rango y racha autoritativos.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-104 · Campo _hasVotedToKick de LobbyScreen asignado y nunca leído
- Creado: 2026-11-03 15:41
- Por qué: LobbyScreen.xaml.cs:35 declara _hasVotedToKick, que se asigna en :50 y :160 pero nunca se lee; el compilador lo avisa con CS0414. Es parte de la votación de expulsión oculta (BL-082): quitarlo o usarlo cuando esa función se reactive.
- Depende de: nada
- Estado: Postergado
- Nota (2026-11-20): revisión sobre 08297935. LobbyScreen.xaml.cs:25 contiene únicamente la declaración de _hasVotedToKick: la búsqueda en el cliente no encontró lecturas ni escrituras. BtnConfirmKick_Click cambia controles visuales, pero no utiliza ese campo; no se acredita una votación real. Es limpieza menor sin impacto funcional demostrado. Retomar al implementar o revisar la expulsión por votación de BL-161 y decidir si eliminarlo o utilizar estado autoritativo.

### BL-106 · Conservar el chat 24 horas (RF-51)
- Creado: 2026-11-03 23:53
- Por qué: hoy el chat es solo en memoria y sin historial; falta conservar los mensajes durante 24 horas.
- Depende de: persistencia del chat
- Estado: Pendiente
- Nota (2026-11-15 00:32): avance en el rango 78e1fa1..8aa6844. Se guarda cada mensaje aceptado como evidencia después de entregarlo: interfaz y tipos del dominio (d64acfa), ChatMessageRepository con dbo.InsertChatMessage (65c9d8a; database/TLMG_DB_v4.1.sql:674) y ChatService.SaveChatMessage (55f7571; ChatService.cs:151-163). D-67 fija el chat como evidencia de 24 h como máximo, no como historial visible. Sigue Pendiente: nadie llama a dbo.PurgeExpiredMessages ni a dbo.GetChatEvidence (git grep sin resultados en C#), así que la retención de 24 h no se cumple; y ChatService.cs:16-17 guarda siempre el ámbito Lobby con un TODO sin BL (BL-178). El estado Depende de «persistencia del chat» ya se cumple.

### BL-107 · Filtrar palabras ofensivas en el chat y el nickname (RC-M-01, RC-N-01)
- Creado: 2026-11-04 16:40
- Por qué: el servidor valida longitud y caracteres del chat, pero no filtra palabras ofensivas; el diccionario de validación lo exige para chat y nickname, en español e inglés.
- Depende de: definir la fuente de la lista de palabras y dónde se valida (servidor)
- Estado: Pendiente
- Nota (2026-11-20): revisión estática sobre 08297935. ChatValidator y AccountFieldValidator comprueban caracteres y longitud; ChatService valida en servidor. No se identificó filtro de expresiones ofensivas en esos validadores. V1 atribuye la obligación bilingüe a RC-M-01 y RC-N-01, pero el diccionario original y lista o criterio aprobado no están disponibles en docs. Consultarlos y validar en servidor chat y nickname sin inventar listas; coordinar BL-044.

### BL-108 · Dividir ServerConnectionManager por canal (amigos, lobby y chat)
- Creado: 2026-11-06 12:15
- Por qué: la clase concentra amigos, lobby, Listo, cuenta regresiva y chat en tres archivos parciales. Cada canal debe tener su propio administrador de conexión. Cambia D-60 (un solo ServerConnectionManager en App), que debe registrarse en la Bitácora.
- Depende de: BL-078, BL-080; decisión de D-60
- Estado: Postergado
- Nota (2026-11-06 13:30): antes de dividir, definir dónde viven el token, la sala actual, la lista de amigos y el cierre de sesión (ADR-08, BL-078).
- Nota (2026-11-10 12:08): la Fase 5 (cefa0a3) agregó ServerConnectionManager.Accounts.cs como un cuarto archivo parcial de la misma clase (A4, D-60). Cuentas usa UsersManagerClient por solicitud y no un canal de sesión, así que no cambia esta división por canal; las interfaces por área (D-70) la facilitan.
- Nota (2026-11-20): revisión estática sobre 08297935. ServerConnectionManager conserva sesión, token, sala y canales; parciales e interfaces por área exponen dependencias acotadas a ViewModels según D-60/D-70. Separarlo físicamente obligaría a coordinar estado compartido; no se identificó defecto funcional que lo exija. Retomar si una prueba o requisito demuestra necesidad; BL-080 conserva decisión sobre chat y BL-175 inventario de cohesión.

### BL-110 · Depurar los diccionarios y alinear el Excel
- Creado: 2026-11-06 12:15
- Por qué: hay claves repetidas por prefijo (Gameplay_, RoundResult_, GameOver_, MatchClosed_, Moderation_) y claves sin uso (Lobby_MockChatPlayer, Lobby_ChatSystemPrefix, Lobby_SystemJoinedMessage, Lobby_SystemReadyMessage). El Excel no tiene las 13 claves nuevas del Listo y del chat.
- Depende de: nada
- Estado: Parcial
- Nota (2026-11-11 08:12): falta Common_OutOfMemoryError en el Excel TLMG_Diccionario_v3.xlsx.
- Nota (2026-11-17 11:49): 14 claves nuevas para el Excel TLMG_Diccionario_v3.xlsx (dff94a9): las nueve Match_* (Match_NotInMatchError, Match_NotSubscribedError, Match_NotYourTurnError, Match_BetInProgressError, Match_HandEmptyError, Match_CardNotInHandError, Match_PunishmentPendingError, Match_FinishedError y Match_CardNotValidError) y Gameplay_PhasePlacingCardsLabel, Gameplay_PhaseChallengeReadyLabel, Gameplay_PhasePunishmentPendingLabel, Gameplay_PhaseMatchFinishedLabel y Gameplay_MatchUnavailableLabel. Verificado sobre dff94a9: las 14 existen en Strings.es-MX.xaml y Strings.en-US.xaml, que tienen 405 claves cada uno, sin repetidas y con paridad exacta. Gameplay_PhaseBiddingLabel y Common_InvalidSessionError ya existían y se reutilizan. Sin cambio de estado.
- Nota (2026-11-20): Revisión sobre 08297935: Strings.es-MX.xaml y Strings.en-US.xaml tienen 489 claves cada uno, todas únicas y con conjuntos idénticos. Lobby_MockChatPlayer, Lobby_ChatSystemPrefix, Lobby_SystemJoinedMessage y Lobby_SystemReadyMessage permanecen en ambos diccionarios; una búsqueda en Client fuera de Strings.*.xaml no encontró referencias literales. No demuestra ausencia de usos dinámicos. El Excel TLMG_Diccionario_v3.xlsx no se encontró en el repositorio, por lo que su alineación no se verificó; consultar el original y cotejarlo, y decidir qué claves redundantes o sin uso retirar.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-111 · Sesión de servidor para invitados (crear sala y jugar sin chat)
- Creado: 2026-11-06 12:15
- Por qué: el servidor rechaza la creación de sala de un invitado y no le da sesión. Las Reglas permiten invitados sin chat. El chat ya los excluye en el cliente; falta que el servidor lo valide.
- Depende de: BL-089; ADR-08
- Estado: Pendiente
- Nota (2026-11-11 08:10): prueba del 2026-11-11: el invitado entra al menú sin pasar por el servidor, por eso no aparece en el log del servidor. La BD v4.0 conserva dbo.Guest_Session (database/TLMG_DB_v4.0.sql:210, «sin cambios hasta decidir el invitado», :32) y el modelo EF la mapea, pero ningún servicio la usa (D-15).

### BL-112 · Lista de caracteres del chat duplicada en cliente y servidor
- Creado: 2026-11-06 12:15
- Por qué: ChatInputHelper (cliente) y ChatValidationHelper (servidor) llevan la misma lista, que no puede compartirse entre soluciones. Si una cambia, la otra debe cambiar. Cubrir ambas con pruebas.
- Depende de: BL-039
- Estado: Postergado
- Nota (2026-11-15 00:32): revisado sobre 8aa6844. El lado servidor ya no es ChatValidationHelper sino TLMG.Domain.Validators.ChatValidator, con la lista en ValidationRules.cs:43 y pruebas en TLMG.Domain.Tests/ChatValidatorTests.cs (el cambio es anterior al rango, b098001). El cliente conserva su copia en ChatInputHelper.cs:10 sin pruebas (no existe TLMG.Client.Tests, BL-164) y el rango no cambió ninguna de las dos. Sigue Pendiente.
- Nota (2026-11-20): revisión estática sobre 08297935. ChatInputHelper.AllowedCharacters y ValidationRules.ChatAllowedCharacters coinciden exactamente en HEAD y ambos limitan a 160. ChatService usa ChatValidator en servidor; cliente filtra anticipadamente. Hay pruebas de ChatValidator en Domain, no se identificó proyecto de pruebas de cliente. Retomar consistencia al desarrollar BL-164 o cambiar la regla; no hace falta una biblioteca compartida por sí sola.

### BL-114 · Actualizar la Bitácora con D-58 a D-66
- Creado: 2026-11-06 13:30
- Por qué: el Estándar cita D-58 a D-63 y el SAD cita ADR-08; la Bitácora no los contiene. A registrar además: navegación por interfaz (INavigationManager e IDialogManager), Domain amplio, OperationResult y la enmienda a D-60. Numeración tentativa D-64 a D-66; confirmar contra la Bitácora.
- Depende de: nada
- Estado: Requiere validación
- Nota (2026-11-17 11:49): a registrar también en la Bitácora, y reflejar en el SAD, lo de I2a en el cliente (dff94a9): el canal Match (ServerConnectionManager.Match e IMatchConnection), el descarte de vistas por versión (MatchViewVersionHelper: solo se muestra una vista con versión mayor a la ya mostrada) y que la caída del canal de partida solo limpia ese canal, sin desconectar la sesión ni sacar al jugador de la sala. Sin cambio de estado.
- Nota (2026-11-20): revisión estática sobre 08297935. El Estándar enumera D-58 y D-64 a D-66 y describe D-60/D-70; código contiene navegación y diálogos por interfaz, Domain, OperationResult, canal Match y control de versiones. Bitácora y SAD originales no están en el repositorio: no se puede verificar qué registran ni numeración disponible. Cotejar D-58 a D-66, D-60/D-70, canal y recuperación; documentar enmiendas aprobadas y distinguir propuestas de BL-081, BL-108, BL-142 y BL-169 sin asignar números nuevos.
- Nota (2026-11-20): R2A: cotejar expresamente D-57 y enmienda a D-40 (BL-056), D-58 a D-61 y ADR-08 (BL-059), estado de salas y autorizaciones (BL-068), y semántica vigente de presencia del canal de amigos (BL-074); consultar Bitácora original antes de afirmar registro.
- Nota (2026-11-20): R2B1, HEAD 08297935. Cotejar con el encargado la desviación de BL-109/D-66: varias firmas y callbacks usan OperationResult directamente pese a la regla histórica de tipos derivados; decidir y registrar la enmienda y alinear Estándar §17.1 si procede, sin presumir ratificación.

### BL-115 · Unificar dónde viven los validadores del servidor
- Creado: 2026-11-06 13:30
- Por qué: Estándar §4.3 los ubica en TLMG.Domain.Validators y §16.1 en TLMG.Services.Helpers (carpeta que §4.3 no lista). Decidir y corregir el que sobre. Recomendado: Domain.Validators.
- Depende de: BL-040
- Estado: Parcial
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sin cambio en el rango. El código ya cumple la recomendación: los validadores viven en TLMG.Domain.Validators desde b098001 (anterior al rango) y Server/TLMG.Services/Helpers ya no tiene validadores. Falta corregir el Estándar, cuyo §16.1 (docs/Estandar.md:4295) sigue ubicando PlayerAccountRegistrationValidationHelper en TLMG.Services.Helpers.
- Nota (2026-11-20): revisión sobre 0829793. Los validadores están en TLMG.Domain.Validators; resta corregir las referencias contradictorias del Estándar. Es una tarea documental.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-118 · Navegación al iniciar la partida según la modalidad
- Creado: 2026-11-07 04:28
- Por qué: con 8 cartas se debe abrir FugitiveKittenScreen y con 4 y 6, LoadingScreen; tras el resultado, pasar a la partida a los 2.8 s. Hoy MainWindow.NavigateToAuthorizedMatch abre GameplayScreen directamente.
- Depende de: BL-046, BL-117
- Estado: Parcial
- Nota (2026-11-18): FK-01 no modifica la navegación de modalidades ni agrega transición automática; F8 sigue como acceso explícito de previsualización.
- Nota (2026-11-18): FK-02 navega por modalidad y presenta el resultado antes de Gameplay; pendiente prueba real.
- Nota (2026-11-18): d12839b conserva Gameplay directo para cuatro y seis cartas; LoadingScreen sigue sin demostrarse y la entrada permanece pendiente.
- Nota (2026-11-20): revisión sobre 0829793. El desarrollador informa que varios clientes ven el minijuego en modalidad de ocho cartas. La navegación directa a Gameplay en cuatro y seis cartas y la pantalla de carga siguen por verificar y corregir según el alcance.
- Nota (2026-11-20): revisión sobre 0829793. El desarrollador confirma ocho cartas con minijuego y cuatro/seis sin minijuego. El dominio V3 aún indica LoadingScreen para cuatro/seis; confirmar y cubrir esa transición.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-119 · Reproducción de la ruta de 15 s del gatito en la pantalla
- Creado: 2026-11-07 04:28
- Por qué: la pantalla solo muestra los 6 estados fijos. Falta recorrer los tramos con DispatcherTimer (movimiento, cambio de pose cada 100 ms, volteo según la dirección, brillo 300 ms antes de cada salida, contador en vivo y cierre a los 15 s). La ruta de muestra local se retira cuando llegue la del servidor.
- Depende de: aprobación del encargado; BL-117 para la ruta real
- Estado: Requiere validación
- Nota (2026-11-18): FK-01 conserva las seis escenas y tiempos fijos de previsualización; no implementa la ruta dinámica de 15 s.
- Nota (2026-11-18): FK-02 reproduce los segmentos recibidos; quedan pendientes revisión visual y sincronización real.
- Nota (2026-11-20): revisión sobre 0829793. La reproducción de segmentos existe; faltan comprobaciones visuales y de sincronización en clientes reales.
- Nota (2026-11-20): revisión sobre 0829793. El desarrollador informa captura correcta en una prueba multicliente, con resultado esperado y continuación. La presentación, los tiempos y la sincronización no quedan acreditados por esa prueba.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Requiere validación conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-120 · Sprites finales y colores del gatito
- Creado: 2026-11-07 04:28
- Por qué: el gatito se dibuja con formas de XAML; los colores blanco (#F4F1EC/#BDB6AA/#FFFFFF) y gris (#8E8EA3/#5F5F75/#D2D2DE) son provisionales y no vienen del Figma. Reemplazarlos por los sprites finales en img/ (Estándar §4.4).
- Depende de: entrega de arte
- Estado: Postergado
- Nota (2026-11-20): revisión estática sobre 08297935. GlobalStyles define formas y pinceles XAML con colores provisionales y TODO de sprite; no se encontraron sprites finales ni aprobación del diseño en el repositorio. La captura real del gatito fue informada por el desarrollador, pero no acredita fidelidad visual. Retomar si se entregan recursos finales o se confirma exigencia de evaluación.

### BL-121 · Fuentes Cinzel y Outfit del prototipo
- Creado: 2026-11-07 04:28
- Por qué: hoy se usa Georgia Bold en lugar de Cinzel (sin peso Black) y la fuente por defecto en lugar de Outfit; WPF no tiene espaciado entre letras en TextBlock. Agregar las fuentes es una dependencia nueva (licencia y archivos) y se decide aparte.
- Depende de: decisión del encargado (§19.1)
- Estado: Postergado
- Nota (2026-11-20): revisión estática sobre 08297935. GlobalStyles usa Georgia y fuentes predeterminadas; conserva TODO para Cinzel y Outfit. No se localizaron archivos de fuentes ni requisito explícito en los documentos inspeccionados que exija esas familias. Retomar si se aprueba fidelidad al prototipo y se dispone de archivos y licencias.

### BL-133 · Probar a mano los botones y el CanExecute de los 12 ViewModels, y el login y el registro
- Creado: 2026-11-10 11:44
- Por qué: la Fase 4b cambió al RelayCommand del toolkit, que no reevalúa CanExecute por sí solo (el propio sí lo hacía con CommandManager), y la Fase 5 movió el login y el registro a IAccountsConnection. Ninguna de las dos se compiló ni se probó. Probar que cada botón se habilita o deshabilita cuando cambia el estado que lee su CanExecute. Login y registro: caso correcto, contraseña incorrecta, servidor apagado, correo repetido y cambio de idioma en caliente.
- Depende de: Fase 6
- Estado: Requiere validación
- Nota (2026-11-11 08:02): probados a mano el inicio de sesión, el registro completo, el cambio de idioma y el servidor apagado. Faltan los CanExecute de los demás ViewModels.
- Nota (2026-11-11 23:55): avance, según lo dictado por Martín. El 2026-11-11 probó a mano en dev-LS_Angel, con resultado correcto: Friends, Lobby (incluido el inicio de partida y el chat), MainMenu, ForgotPassword, Options, Profile, NewPassword, HowToPlay y Leaderboard, con 3 clientes en Friends y Lobby. No se registró si se cubrió el CanExecute de cada ViewModel. Pendiente: confirmar los CanExecute de cada ViewModel y probar a mano los ViewModels que no se mencionaron (Verification y los de la rama mvvm-resto).
- Nota (2026-11-17 11:49): avance de I2a (dff94a9). El encargado probó a mano en Gameplay, con tres jugadores registrados, la colocación de cartas por turnos y los controles de apuesta deshabilitados. No se registró si se cubrió el CanExecute del botón de cada carta de la mano. Las demás pruebas manuales de la partida se llevan en BL-182. Sigue pendiente lo de esta entrada para los demás ViewModels.
- Nota (2026-11-20): revisión sobre 0829793. Las pruebas históricas cubren varios flujos y pantallas, pero no documentan cada CanExecute; el alcance no se limita a invitados. BL-111 lleva invitados.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Requiere validación conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-134 · Unificar ErrorMessageHelper.FindErrorMessage con LanguageManager.FindText
- Creado: 2026-11-10 11:46
- Por qué: los dos buscan la clave en el diccionario del idioma activo y caen en Common_UnexpectedError si falta; ErrorMessageHelper exige un FrameworkElement y LanguageManager.FindText (Fase 5, cefa0a3) usa Application.Current. Hay que dejar una sola implementación (D-31). Relacionada con BL-017.
- Depende de: Fase 6
- Estado: Requiere validación
- Nota (2026-11-11 23:55): relación (b6dec93). LanguageManager.FindText depende de Application.Current (LanguageManager.cs:86-92) y lo llaman 8 ViewModels (Login, CreateAccount, ForgotPassword, Friends, Leaderboard, Lobby, NewPassword y Profile), lo que impide probarlos sin una aplicación WPF. Resolver esto es requisito de BL-164.
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sin cambio. ErrorMessageHelper.FindErrorMessage sigue con un solo llamador (MainWindow.xaml.cs:161) y LanguageManager.FindText con Application.Current (LanguageManager.cs:86) lo usan los ViewModels y DialogManager. Inventario ampliado en BL-174.
- Nota (2026-11-20): revisión sobre 08297935. ErrorMessageHelper usa FrameworkElement.TryFindResource y MainWindow; LanguageManager.FindText usa Application.Current y ViewModels, con fallback similar pero sin aplicación devuelve cadena vacía. Existe duplicación, aunque los ámbitos de búsqueda no son idénticos. Su unificación por sí sola es simplificación; BL-164 requiere determinar cómo probar ViewModels con recursos WPF y qué dependencia realmente impide sustituirlos. Evaluar esa necesidad antes de modificar arquitectura; coordinar null con BL-187.

### BL-141 · Retirar el puente de MainWindow y migrar los 7 BackRequested al manager
- Creado: 2026-11-11 08:40
- Por qué: MainWindow.ShowScreen, GoBack y ClearHistory siguen públicos como puente hacia NavigationManager.ShowExistingScreen, porque unas 15 pantallas legacy y el acceso F8 a FugitiveKittenScreen dependen de ellos. Al migrar cada pantalla a B3, su ViewModel debe llamar a INavigationManager; los 7 BackRequested (Verification, NewPassword, ForgotPassword, Friends, Leaderboard, Profile y Options) y los demás eventos de navegación pasan al manager. Al terminar se retiran el puente y ShowExistingScreen.
- Depende de: BL-041
- Estado: Postergado
- Nota (2026-11-11 23:55): avance (b6dec93). Solo queda 1 de los 7 BackRequested (VerificationViewModel.cs:68); Friends, Profile, Options, NewPassword, ForgotPassword y Leaderboard ya usan INavigationManager. Application.Current.MainWindow sigue en 5 archivos (24 usos en la nota de BL-042): FugitiveKittenScreen.xaml.cs:452, GameOverScreen.xaml.cs:37, GameplayScreen.xaml.cs:145, ModerationScreen.xaml.cs:131 y RoundResult.xaml.cs:50. Puentes transitorios ((App)Application.Current).Navigation en 4 archivos: GameOverScreen.xaml.cs:32, GameplayScreen.xaml.cs:78, MatchClosedScreen.xaml.cs:25 y RoundResult.xaml.cs:45. MainWindow.ShowScreen lo usan GameOver, Gameplay, Moderation, RoundResult y MainWindow (:47, :186). La rama mvvm-resto (04a7d13, 2d4bafb) migra Gameplay, GameOver, RoundResult, Moderation y MatchClosed; falta integrarla.
- Nota (2026-11-20): revisión estática sobre 08297935. Los regresos migrados usan INavigationManager. MainWindow.ShowScreen todavía compone LoadingScreen y delega en ShowExistingScreen; GoBack y ClearHistory siguen como puentes. VerificationViewModel conserva BackRequested y VerificationScreen lo conecta y desconecta. No se identificó una fuga o fallo atribuible al puente por lectura estática. Postergar su retirada hasta que Loading o un consumidor real requiera migración; BL-092 valida pila y BL-143 destinos.

### BL-142 · Mover los MessageBox de MainWindow a IDialogManager y los 6 eventos del servidor a MainWindowViewModel
- Creado: 2026-11-11 08:42
- Por qué: MainWindow todavía muestra con MessageBox la conexión perdida, la invitación a sala y los errores del servidor, y atiende en su code-behind 6 eventos de ServerConnection (§7.1). La invitación necesita un Confirm con argumentos de formato (el nickname), que IDialogManager no tiene; hay que decidir cómo ampliarlo.
- Depende de: nada
- Estado: Requiere decisión
- Nota (2026-11-20): revisión estática sobre 08297935. MainWindow atiende conexión, invitación, resultados y navegación de partida; muestra MessageBox para pérdida, invitación y error. MainWindowViewModel implementa INavigationHost y expone CurrentScreen; DialogManager e IDialogManager existen. La invitación requiere mensaje formateado. Decidir separación pequeña de responsabilidades sin trasladar todo a una clase monolítica; presentación de invitación sigue en BL-087 y aviso de solicitudes en BL-163.

### BL-143 · Destinos sin pantalla en la fábrica de pantallas
- Creado: 2026-11-11 08:44
- Por qué: ScreenFactory no tiene caso para Loading, Leaderboard ni Moderation, y Verification solo atiende el registro de cuenta. ForgotPasswordScreen arma su propia VerificationScreen y necesitará un caso para VerificationFlow.PasswordRecovery. Revisar también el "Atrás" de Verification (hoy crea un formulario de registro nuevo aunque la cuenta ya se guardó) cuando exista el 2FA real (BL-007).
- Depende de: BL-041; BL-007
- Estado: Requiere validación
- Nota (2026-11-11 23:55): avance (b6dec93). VerificationFlow.PasswordRecovery tiene caso en ScreenFactory (7d882b1) y Leaderboard tiene el suyo (BuildLeaderboardEntry, b6dec93); FugitiveKitten sigue registrada en el mapa heredado (c6a081a). Siguen sin caso en la fábrica Loading y Moderation (git grep sobre ScreenFactory.cs). Pendiente también revisar el «Atrás» de Verification con el 2FA real (BL-007).
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sin cambio de fondo. Loading y Moderation siguen sin caso en ScreenFactory. El «Atrás» de Verification pasó al flujo del 2FA real: ReturnToCreateAccount (ScreenFactory.cs) se ejecuta con la cuenta ya guardada como registro pendiente, no como cuenta; comprobar a mano qué ve el jugador al volver.
- Nota (2026-11-18): Moderation ya tiene caso en ScreenFactory (2e26417), al igual que Leaderboard y PasswordRecovery. Loading aún se crea desde MainWindow fuera de la fábrica; queda pendiente revisar el regreso de Verification con el flujo real.
- Nota (2026-11-20): revisión sobre 08297935. ScreenFactory contempla Leaderboard, Moderation, Gameplay y ambos flujos de Verification; ReturnToCreateAccount navega a CreateAccount y ReturnToForgotPassword a ForgotPassword. MainWindow construye LoadingScreen directamente: es una excepción de composición, no un fallo funcional demostrado; su decisión corresponde a BL-169. Falta comprobar manualmente qué ve el jugador al regresar desde verificación de registro con un registro pendiente, sin suponer el resultado. Las transiciones de modalidad se llevan en BL-118.

### BL-144 · NavigationManager sin <summary> y falta la clave Common_InfoTitle
- Creado: 2026-11-11 08:46
- Por qué: los miembros públicos de NavigationManager no tienen <summary> ni <inheritdoc /> (Estándar §12). Además IDialogManager.ShowMessage usa MainWindow_Title como título porque no existe una clave informativa; Common_InfoTitle se agrega en ambos diccionarios cuando se pueda editar Strings.*.xaml.
- Depende de: nada
- Estado: Parcial
- Nota (2026-11-20): revisión sobre 08297935. NavigationManager documenta clase, constructor, ScreenFactory y ShowExistingScreen; las dos sobrecargas de NavigateTo, las dos de NavigateToWithoutHistory, GoBack y ClearHistory carecen de summary o inheritdoc. DialogManager.ShowMessage usa MainWindow_Title y Common_InfoTitle no aparece en el cliente. Falta documentar los seis métodos y resolver el título informativo en ambos diccionarios.

### BL-145 · Patrón constructor/Unloaded en las pantallas legacy y recarga de la pantalla al cambiar el idioma
- Creado: 2026-11-11 08:48
- Por qué: las pantallas legacy crean su ViewModel y suscriben sus eventos en el constructor, y LanguageManager.RefreshActiveWindows hace Window.Content = null y lo restaura, lo que descarga y recarga la pantalla visible. BL-140 evita los botones muertos, pero la recarga sigue ocurriendo en cada cambio de idioma. Se elimina al migrar las pantallas a B3 (el ViewModel llega por constructor) y al quitar la recarga.
- Depende de: BL-041
- Estado: Requiere validación
- Nota (2026-11-11 23:55): avance (b6dec93). Login, CreateAccount, MainMenu, ForgotPassword, Friends, Lobby, Options, Profile, NewPassword, HowToPlay y Leaderboard reciben el ViewModel por constructor desde ScreenFactory y ya no lo crean en la vista (git grep de «new …ViewModel()» sobre Views solo encuentra VerificationScreen.xaml.cs:61 y GameplayScreen.xaml.cs:32). La recarga al cambiar de idioma persiste: LanguageManager.cs:147-149 pone Content = null y lo restaura.
- Nota (2026-11-20): revisión estática sobre 08297935. LanguageManager.RefreshActiveWindows guarda Content, lo pone a null y restaura la misma instancia tras cambiar diccionario; provoca descarga y recarga visual potencial. Las vistas migradas reciben ViewModel y usan recursos DynamicResource; varios pares Loaded/Unloaded administran suscripciones. No se demostró fuga, pérdida de estado ni que recarga sea necesaria. Verificar cambio de idioma y ciclo de vida antes de decidir una alternativa simple.

### BL-146 · Mover GlobalStyles.xaml a App.xaml
- Creado: 2026-11-11 08:50
- Por qué: GlobalStyles.xaml (3 476 líneas) se mezcla en los recursos de cada pantalla, así que se vuelve a analizar al abrir cada una. Mezclarlo una sola vez en App.xaml evita ese costo. Prioridad baja.
- Depende de: nada
- Estado: Postergado
- Nota (2026-11-20): revisión estática sobre 08297935. GlobalStyles.xaml se mezcla desde múltiples vistas XAML; App.xaml no lo referencia. Moverlo puede cambiar alcance y precedencia de recursos. No hay medición de rendimiento que justifique priorizarlo. Retomar si medición o mantenimiento acredita necesidad y entonces verificar resolución de recursos.

### BL-147 · Dos logs en el servidor: auditoría y operativo
- Creado: 2026-11-11 08:52
- Por qué: decisión del encargado (2026-11-11, opción A): un logger nombrado de auditoría y el log operativo, sin librerías nuevas. El servidor es autoritativo: su log es la prueba. Auditoría: solo ids, nunca correos, contraseñas ni tokens; registra las acciones validadas (inicio de sesión, sala, invitación). Operativo: arranque, apagado, errores y «servidor sobrecargado» (registrar OutOfMemoryException y los límites de conexión; sin hilos ni temporizadores). Idea futura: un archivo marcador para detectar un cierre no limpio.
- Depende de: BL-093; BL-033
- Estado: Pendiente
- Nota (2026-11-20): revisión estática sobre 08297935. log4net.config configura un root DEBUG con appender de archivo y consola: dos destinos no son dos registros con responsabilidades diferenciadas. Hay logs operativos y de acciones, pero no se acreditó logger nombrado de auditoría ni cobertura completa de acciones validadas. Definir e implementar separación decidida, verificando eventos y privacidad; niveles corresponden a BL-033, límites a BL-093 y correo a BL-179.

### BL-148 · Modo degradado del servidor con la base de datos apagada
- Creado: 2026-11-11 08:54
- Por qué: con la BD apagada el servidor sigue arrancando y los invitados entran. Decidir si ese modo degradado es intencional (arranca sin BD) o si el servidor debe negarse a arrancar.
- Depende de: decisión del encargado
- Estado: Requiere decisión
- Nota (2026-11-20): revisión estática sobre 08297935. Program.WarmUpEntityFramework captura Exception y continúa hasta ServiceHost.Open; esto no garantiza que el host abra ni que operaciones con SQL funcionen. Repositorios devuelven errores controlados en diversas rutas; no se probó SQL apagado y los invitados dependen de BL-111. Alternativas: impedir arranque sin BD (estado claro, menor disponibilidad) o arrancar degradado con señalización y operaciones restringidas (mayor disponibilidad, más validación). Se recomienda impedir presentar como operativo lo dependiente de BD hasta decidir el comportamiento y probarlo; el encargado debe elegir.

### BL-150 · Log del cliente: solo seguimiento de las acciones del jugador
- Creado: 2026-11-11 08:58
- Por qué: el log del cliente (log4net) sirve solo para seguir las acciones del jugador; nunca es evidencia contra el servidor, cuyo log es la prueba (BL-147). Se inicia en App.
- Depende de: BL-147
- Estado: Requiere validación
- Nota (2026-11-20): revisión estática sobre 08297935. App configura log4net y captura fallos de UI; el cliente registra diagnóstico de comunicación además de acciones. No se identificó en el código revisado atribución de autoridad a ese log; la autoridad de servidor corresponde a BL-147. Una búsqueda estática superficial de mensajes no demuestra ausencia de secretos en excepciones o datos en tiempo de ejecución. Revisar eventos y tratamiento de excepciones sin exponer datos, y documentar su papel frente al log de servidor.

### BL-153 · Aplicar la v4.1 del script de base de datos y registrarla en la Bitácora
- Creado: 2026-11-11 19:30
- Por qué: database/TLMG_DB_v4.1.sql agrega GRANT INSERT y GRANT UPDATE (state, id_user_sender, id_user_receiver, request_date) sobre Friendship para role_tlmg_app (Estándar §5.3) y se puede volver a ejecutar. Hay que ejecutarlo con una cuenta administradora en cada base (desarrollo y entrega), comprobar que SET NOEXEC OFF antes de la sección 5 reaplica los permisos sobre una base v4.0, verificar con la cuenta de la aplicación que inserta y actualiza solo esas columnas (ver la nota de BL-013) y registrar la versión y la decisión en la Bitácora (BL-114).
- Depende de: Paquete 1
- Estado: Requiere validación
- Nota (2026-11-20): revisión sobre 0829793. El equipo informa operaciones de amistad funcionales con persistencia en la base usada para las pruebas; esto no acredita versión y permisos en todas las bases ni actualización de la Bitácora.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Requiere validación conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.
- Nota (2026-11-20): R2B1, HEAD 08297935. Verificar si las bases requeridas disponen de dbo.SavePendingRegistration y sus permisos y compatibilidad: VerificationCodeRepository lo invoca, pero los scripts database no lo definen; v4.1 define dbo.CreatePendingRegistration. Coordinar con BL-007 sin asumir que el procedimiento existe o falta en una base no inspeccionada.

### BL-155 · Amistades: enumeración de nicknames y falta de límite de solicitudes
- Creado: 2026-11-11 21:22
- Por qué: Friends_UserNotFoundError revela si un nickname existe, así que cualquier jugador registrado puede recorrer nicknames, y no hay límite de solicitudes: tras un rechazo se puede reenviar sin espera y sin tope por jugador ni por tiempo. Decidir una respuesta que no distinga entre inexistente y rechazada, un tope de solicitudes pendientes y un enfriamiento (este requeriría una columna de fecha de respuesta en Friendship, versión menor del script, Estándar §5.3). Decisión del encargado: queda fuera de BL-152.
- Depende de: BL-152; decisión del encargado
- Estado: Requiere decisión
- Nota (2026-11-20): revisión estática sobre 08297935. FriendsService devuelve Friends_UserNotFoundError para nickname inexistente y rechaza solicitud pendiente repetida; tras rechazo admite reactivación. SQL v4.1 impone unicidad por pareja y el servicio valida sesión, pero no se identificó límite global o temporal en la ruta revisada. Existe potencial de enumeración y abuso, no explotación demostrada. Decidir respuesta y límites proporcionados al juego; BL-152 conserva lo ya implementado.

### BL-156 · Amistades: cancelar solicitud, quitar amigo y bloquear
- Creado: 2026-11-11 21:22
- Por qué: BL-152 solo cubre enviar, aceptar y rechazar. Faltan cancelar una solicitud enviada (la tabla no tiene estado de cancelada, sería un DELETE con GRANT DELETE sobre Friendship), quitar un amigo y bloquear o desbloquear (Block_List, BL-085), con sus operaciones de contrato, avisos y pantallas. La fila rechazada se reactiva con UPDATE; un DELETE cambiaría esa regla. Reportar jugadores no está en este alcance.
- Depende de: BL-152; BL-085; BL-154
- Estado: Pendiente

### BL-162 · Aviso al remitente cuando rechazan su solicitud de amistad
- Creado: 2026-11-11 23:55
- Por qué: DeclineFriendRequest solo responde a quien rechaza (FriendsService.cs:295-296 crea el resultado sin avisos) y el remitente no se entera: su solicitud sigue como «enviada» en el cliente hasta que vuelve a suscribirse a los amigos. Hace falta un callback nuevo en IFriendsManagerCallback (cambio de contrato) y su manejo en FriendsViewModel. Decidirlo junto con BL-155, porque avisar el rechazo también revela información.
- Depende de: BL-152; BL-155 (decisión del encargado)
- Estado: Requiere decisión
- Nota (2026-11-20): revisión estática sobre 08297935. DeclineFriendRequest cambia el estado mediante AnswerFriendRequest y devuelve resultado al receptor sin lista de notificaciones; el remitente puede conservar vista pendiente hasta nueva suscripción, que consulta filas pendientes. El callback existente informa resultado a quien rechaza y no se identificó aviso de rechazo al remitente. Decidir necesidad y efecto de privacidad con BL-155, evaluando reutilización de mecanismos sin presumir requisito de callback nuevo.

### BL-163 · Aviso de solicitud recibida fuera de la pantalla de Amigos
- Creado: 2026-11-11 23:55
- Por qué: ServerConnectionManager guarda la solicitud recibida (PendingFriendRequests, 93fa788), pero solo FriendsViewModel muestra el aviso (Friends_NewRequestFormat). Con otra pantalla abierta, el jugador no se entera hasta abrir Amigos. Decidir dónde avisar (insignia en el menú, diálogo por IDialogManager o aviso propio como BL-087), sin MessageBox en las vistas.
- Depende de: BL-087; BL-142
- Estado: Requiere decisión
- Nota (2026-11-20): revisión estática sobre 08297935. ServerConnectionManager agrega solicitudes a su lista cuando está inicializada y emite evento; FriendsViewModel se suscribe solo activo y reconstruye lista al activar. No se identificó insignia global en MainMenu ni aviso en Lobby o Gameplay. Mientras el canal funcione, una solicitud puede quedar guardada pero sin aviso visible; si la lista es null no se agrega ahí y requiere verificar recuperación. Decidir necesidad de aviso inmediato y reutilizar presentación existente; coordinar BL-087 y BL-142.

### BL-164 · Proyecto TLMG.Client.Tests (xUnit, D-61)
- Creado: 2026-11-11 23:55
- Por qué: no existe proyecto de pruebas del cliente: test/ solo tiene TLMG.Services.Tests y TLMG.Domain.Tests. Estándar §15.2 pide un proyecto .Tests con el nombre exacto del de producción (test/TLMG.Client.Tests) y xUnit.net (D-61). Cubrir cada ViewModel con flujo normal, 3 alternos y 1 de excepción; ya dependen de interfaces por área (IAccountsConnection, IFriendsConnection, ILobbyConnection, ILeaderboardConnection) y de INavigationManager e IDialogManager. Obstáculos: LanguageManager.FindText usa Application.Current (BL-134) y los DispatcherTimer. Decidido: se crea después de la demo.
- Depende de: BL-039; BL-080; BL-134
- Estado: Postergado
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sin cambio. test/ solo tiene TLMG.Domain.Tests y TLMG.Services.Tests.
- Nota (2026-11-20): revisión estática sobre 08297935. Solo existen proyectos Domain.Tests y Services.Tests. §15.2 y D-61 exigen estructura y xUnit; el histórico decidió crear Client.Tests después de la demo y no se halló decisión posterior en el repositorio. Interfaces por área facilitan sustitución; Application.Current y DispatcherTimer necesitan estrategia de prueba, BL-134. Retomar tras la demo con un ViewModel representativo, flujo normal, alternos y excepción según el Estándar; no se ejecutaron pruebas.

### BL-165 · Reportes y bloqueos reales en Moderation
- Creado: 2026-11-11 23:55
- Por qué: el flujo de moderación es de demostración: en la rama mvvm-resto, ModerationViewModel.cs:44 lleva un FIXME sin entrada («todavía no envía reportes ni bloqueos al servidor», 04a7d13) que debe citar esta entrada. Faltan los contratos de reporte y de bloqueo (Block_List) y la navegación desde la sala y la partida. Los FIXME de mvvm-resto sin entrada (GameOverViewModel.cs:26, RoundResultViewModel.cs:26 y ModerationViewModel.cs:44) deben citar su BL antes del merge (Estándar §12.3): Moderation, BL-165; GameOver y RoundResult, BL-166. ForgotPasswordScreen.xaml.cs:64 de esa rama lleva un TODO sin BL; en dev-LS_Angel ese archivo ya no lo tiene (7d882b1, el TODO equivalente está en ForgotPasswordViewModel.cs:137 y cita BL-007), así que al integrar hay que revisar si sigue existiendo y, si no, conservar la versión de dev-LS_Angel.
- Depende de: BL-156; decisión de contrato (reportar quedó fuera de BL-152)
- Estado: Pendiente

### BL-166 · Datos de demostración en resultado de ronda, fin de partida, inactividad y partida cerrada
- Creado: 2026-11-11 23:55
- Por qué: en la rama mvvm-resto, RoundResultViewModel.cs:26-33 y GameOverViewModel.cs:26-28 llevan un FIXME sin entrada y valores fijos («WhiskerKnight», «NyanProwler»), GameOverScreen.xaml:63 repite un nombre fijo (y :97 en dev-LS_Angel) y MatchClosedViewModel.cs:11 fija 10 minutos de inactividad (04a7d13, 2d4bafb). En dev-LS_Angel, GameplayViewModel.cs:42-44 expone los comandos de demostración DemoRevealCards, DemoGameOver y DemoInactivity. Los datos deben salir del servidor cuando exista el contrato de partida, y los comandos de demostración no deben llegar a la entrega. Los FIXME de mvvm-resto sin entrada (GameOverViewModel.cs:26, RoundResultViewModel.cs:26 y ModerationViewModel.cs:44) deben citar su BL antes del merge (Estándar §12.3): Moderation, BL-165; GameOver y RoundResult, BL-166. ForgotPasswordScreen.xaml.cs:64 de esa rama lleva un TODO sin BL; en dev-LS_Angel ese archivo ya no lo tiene (7d882b1, el TODO equivalente está en ForgotPasswordViewModel.cs:137 y cita BL-007), así que al integrar hay que revisar si sigue existiendo y, si no, conservar la versión de dev-LS_Angel.
- Depende de: BL-116; BL-117; BL-046
- Estado: Parcial
- Nota (2026-11-17 11:49): avance (dff94a9). El panel de demostración de GameplayScreen (Revelar Cartas, Fin de Partida e Inactividad) queda oculto cuando la sala tiene una partida autorizada (GameplayViewModel.IsDemoPanelVisible; GameplayScreen.xaml:374). Los comandos DemoRevealCardsCommand, DemoGameOverCommand y DemoInactivityCommand siguen en GameplayViewModel.cs:69-71 y las pantallas de demostración no cambiaron, así que la entrada sigue abierta. Para I2b, que no tiene entrada propia: Gameplay muestra una «Apuesta actual» fija en 0 (CurrentBidAmount), con los controles de apuesta, Retar y Rendirse deshabilitados, y el marcador muestra vidas (mano más pila sobre las cartas por jugador) porque la vista de partida aún no trae los retos ganados. Las cuatro cartas apiladas del centro de la mesa son un adorno fijo que no representa las cartas en mesa; se juzga sin impacto funcional, pero conviene sustituirlas por el dato real cuando llegue I2b. Sin cambio de estado.
- Nota (2026-11-18): GameOver recibe ganador y clasificación desde MatchView (293482a), y su salida confirmada se ajustó en 992eb8d. RoundResultViewModel conserva datos de demostración y MatchClosedViewModel fija 10 minutos; faltan los resultados y causas autoritativos restantes.
- Nota (2026-11-20): revisión estática sobre 08297935. Gameplay recibe MatchView y navega a GameOver con resultado real; GameOverViewModel obtiene ganador y clasificación. MatchViewVisibilityTests inspeccionados cubren proyección, no se ejecutaron. RoundResultViewModel conserva nombres y valores demo; MatchClosedViewModel fija 10 minutos. Los comandos demo siguen públicos y su panel se oculta si CurrentRoom tiene MatchId, pero esa visibilidad no restringe por sí sola la ejecución directa del comando. Verificar rutas accesibles y sustituir datos o causas demo cuando corresponda, sin afirmar que afectan partida real sin prueba; no duplicar BL-116/117.

### BL-167 · Retirar Lobby_ErrorTitle y Lobby_GameplayUnavailableError (claves sin uso)
- Creado: 2026-11-11 23:55
- Por qué: tras migrar LobbyScreen a MVVM (0fc59c2), los diálogos usan Common_ErrorTitle y Lobby_ErrorTitle ya no se usa fuera de los diccionarios (git grep). Lobby_GameplayUnavailableError todavía la usa MainWindow.xaml.cs:180 en dev-LS_Angel; en mvvm-resto ya no tiene uso, así que solo se puede retirar tras integrar esa rama. Leaderboard_ErrorTitle quedó igual tras b6dec93. Quitarlas de es-MX y en-US con paridad exacta de claves.
- Depende de: integración de mvvm-resto; BL-110
- Estado: Postergado
- Nota (2026-11-20): revisión sobre 08297935. Lobby_ErrorTitle, Lobby_GameplayUnavailableError y Leaderboard_ErrorTitle aparecen literalmente en ambos diccionarios; la búsqueda de C# y XAML no encontró referencias literales fuera de ellos. LanguageManager.FindText y ErrorMessageHelper.FindErrorMessage aceptan claves en tiempo de ejecución: la búsqueda literal no descarta usos indirectos o claves recibidas. No se demuestra necesidad funcional inmediata de retirarlas. Retomar con BL-110: rastrear productores dinámicos y referencias indirectas antes de eliminar y conservar paridad entre idiomas.

### BL-168 · Log «jugador 0» en la validación del nickname antes de tener id
- Creado: 2026-11-11 23:55
- Por qué: FriendsService.cs:445 y :450 llaman a LogRejectedOperation con ValidationRules.NoUserId (0) cuando el nickname no es válido o la sesión no existe todavía, y el mensaje (FriendsService.cs:851-853) queda «Se rechazó solicitar amistad del jugador 0: …». El 0 confunde al leer el log. Resolver la sesión antes de validar el nickname o registrar sin id; el mensaje sigue en español y sin datos sensibles (gov/CLAUDE.md §6).
- Depende de: BL-152
- Estado: Postergado
- Nota (2026-11-20): revisión sobre 08297935. FriendsService.LogRejectedOperation usa el formato «jugador {1}»; ValidateSendFriendRequest registra NoUserId=0 para nickname inválido y sesión inválida, y también se usa en otros rechazos sin identidad. Es una imprecisión de observabilidad, sin impacto funcional ni riesgo de seguridad demostrado por este hallazgo. Retomar al mejorar logs de amistades sin registrar datos sensibles.

### BL-169 · Decisión D-xx para pantallas atómicas sin ViewModel (Loading, ChangePassword)
- Creado: 2026-11-11 23:55
- Por qué: la migración a B3 (BL-041) deja fuera LoadingScreen (la crea MainWindow.xaml.cs:47) y ChangePasswordScreen (Page de plantilla de 28 líneas, sin uso; ProfileViewModel.cs:190 la cita en un TODO de BL-046) porque no tienen estado ni comandos propios. Decidir si se les crea un ViewModel o si se excluyen del patrón con una decisión registrada en la Bitácora (BL-114).
- Depende de: decisión del encargado; BL-114
- Estado: Requiere decisión
- Nota (2026-11-18): ChangePassword ya dispone de ChangePasswordViewModel y de navegación desde perfil (fd9d37a); su actualización sigue sin implementar. Loading se crea en MainWindow sin ViewModel; queda pendiente la decisión sobre esa pantalla y su registro documental.
- Nota (2026-11-20): revisión estática sobre 08297935. ChangePasswordScreen recibe ChangePasswordViewModel y ScreenFactory la construye. LoadingScreen anima una barra y emite LoadingCompleted; MainWindow la crea directamente. El Estándar §7.1 dice cada pantalla tiene ViewModel y permite animaciones en vista. Recomendar al encargado una excepción documentada para Loading si acepta enmendar o aclarar la norma, sin ViewModel vacío; registrar decisión en BL-114 y coordinar con BL-143. No se presume aprobación.

### BL-170 · Declaración de IA del SAD §1.4
- Creado: 2026-11-11 23:55
- Por qué: gov/CLAUDE.md §1 dice que el uso de IA se declara en el SAD (§1.4) y no en el historial de commits. Falta redactarla: qué se pidió a la herramienta en cada paso, qué se aceptó o rechazó y cómo se verificó (compilación en Visual Studio, pruebas manuales y revisión del diff).
- Depende de: BL-026
- Estado: Requiere validación
- Nota (2026-11-20): revisión estática sobre 08297935. gov/CLAUDE.md exige declaración en SAD §1.4. docs/Estandar.md §21 contiene una declaración para ese documento, pero no sustituye ni acredita la del SAD, ausente localmente. Recuperar SAD original y evidencia de actividades, propuestas, revisión humana, pruebas realmente ejecutadas y responsabilidad del equipo; no inventar resultados ni decisiones.

### BL-172 · LoginScreen.xaml.cs ganó 44 líneas de code-behind con el selector de idioma (por revisar)
- Creado: 2026-11-15 00:32
- Por qué: 3569016 agregó a Client/TLMG.Client/Views/LoginScreen.xaml.cs (63 líneas) los manejadores OnScreenLoaded y OnScreenUnloaded, la suscripción a PropertyChanged del ViewModel y ShowActiveLanguageButton, que cambia el Style de btnLanguageEnglish y btnLanguageSpanish según LoginViewModel.IsEnglishActive. Revisar contra B3 y el Estándar §7.1 (la vista solo lleva InitializeComponent, el DataContext y lógica puramente visual; docs/Estandar.md:1528) y buscar duplicidad: ¿otras pantallas (Options, por ejemplo) ya cambian el idioma con otra mecánica? Candidata de duplicidad. Solo inventario: no se propone ni se hace ningún cambio de código hasta decidirlo.
- Depende de: nada
- Estado: Postergado
- Nota (2026-11-20): revisión sobre 08297935. LoginScreen suscribe y desuscribe PropertyChanged y activa o desactiva el ViewModel; ShowActiveLanguageButton cambia estilos visuales, coherente con §7.1. OptionsScreen repite realmente constantes, suscripción y método visual semejante. No se acredita defecto funcional; la deduplicación visual no es obligatoria ahora. Retomar si una modificación común o mantenimiento justifica eliminar la duplicación.

### BL-173 · GameplayViewModel cambió en dos ramas (Codex y Nicole): revisar estados y formas duplicadas (por revisar)
- Creado: 2026-11-15 00:32
- Por qué: GameplayViewModel.cs y sus modelos los tocaron dos líneas de trabajo: Luis Ángel (rama de Codex: 04a7d13, 2e26417) y Nicole (0cd0d9f, b68a4f6, 650ff5f). Verificar que no queden dos estados o dos formas de lo mismo. Hallazgos del inventario sobre 8aa6844: (1) el envío y la recepción del chat están repetidos en GameplayViewModel.cs:205-240 y LobbyViewModel.cs:277-300 y 388-398, con diferencias (Lobby valida con ChatInputHelper y avisa del texto vacío; Gameplay no); (2) Lobby_ChatEmptyError está como constante en ambos (GameplayViewModel.cs:21, LobbyViewModel.cs:28); (3) 4, 6 y 8 cartas por jugador están definidos en MatchModeSettings.cs:11-14, LobbyViewModel.cs:20-22 y CardsPerPlayerValidator del dominio; (4) la ronda actual y las cartas boca abajo siguen fijas (GameplayViewModel.cs:15 y 19, ligado a BL-166). Solo inventario: no se propone ni se hace ningún cambio de código.
- Depende de: BL-166
- Estado: Postergado
- Nota (2026-11-20): revisión estática sobre 08297935. Gameplay y Lobby usan ChatInputHelper y ChatEntryHelper; ambos validan entrada, con comportamiento de presentación distinto y literales repetidos. Gameplay obtiene ronda desde el tablero construido de MatchView y modalidad desde sala con fallback de cuatro; 4/6/8 coinciden con dominio. No se demostró fallo por estado duplicado ni se probaron transiciones visuales. Retomar ante divergencia concreta; datos de demostración de resultados permanecen en BL-166. No se propone clase base nueva.

### BL-175 · Clases grandes por revisar tras el PR #4 (inventario previo)
- Creado: 2026-11-15 00:32
- Por qué: Tamaño sobre 8aa6844 (antes en 78e1fa1): PlayerAccountRegistrationService.cs 376 líneas (159; +245 en el diff: sumó el 2FA con el registro directo, el pendiente, el reenvío y la finalización); FriendsService.cs 965 (889; el diff cambió ~680 líneas); ServerConnectionManager.cs 603 (745), repartido ya en seis archivos parciales; entre todos suman 1 262 líneas (Accounts, Chat, FriendRequests, Leaderboard, Lobby y Readiness; ver BL-108); GameplayViewModel.cs 280 (203). Revisar si alguna concentra más de una responsabilidad (Estándar §3.14 y gov/CLAUDE.md §5: clases que hacen de todo). Solo inventario: no se propone ni se hace ningún cambio de código.
- Depende de: BL-108
- Estado: Postergado
- Nota (2026-11-20): revisión estática sobre 08297935. Sobre HEAD: PlayerAccountRegistrationService 372 líneas, FriendsService 912, ServerConnectionManager.cs 787 más parciales y GameplayViewModel 577. Registro delega repositorios y hash; FriendsService concentra reglas y notificaciones; manager mantiene estado y canales tras interfaces; Gameplay combina presentación de partida y chat con helpers. Existe costo de lectura y acoplamiento potencial, pero tamaño por sí solo no demuestra defecto ni justifica división; BL-108, BL-173 y BL-183 conservan casos concretos. Retomar ante cambio que demuestre responsabilidad o problema de prueba.

### BL-176 · Advertencias y mensajes de compilación: IDE0059, IDE0270 y xUnit2013
- Creado: 2026-11-15 00:32
- Por qué: El encargado reportó en Visual Studio 1 advertencia y 5 mensajes, sin poder reproducirlos en Linux. IDE0059 (asignación innecesaria) e IDE0270 (simplificar con coalescencia) aparecen como mensajes; xUnit2013 está en test/TLMG.Services.Tests/FriendsServiceAcceptRequestTests.cs:56 (Assert.Equal(1, actual.Notifications.Count) debe ser Assert.Single). No se pudo confirmar qué diagnóstico es la advertencia y cuáles los mensajes, ni ubicar IDE0059 e IDE0270: tomar el número de línea de la lista de errores de Visual Studio; las primeras sospechas son las inicializaciones en null de PlayerAccountRegistrationService.cs (:86, 127, 168, 177, 217, 267, 335 y 351-352). Otros Assert.Equal con tamaño de colección mayor que 1 no avisan (por ejemplo LobbyServiceCardsPerPlayerTests.cs:43). Revisar la lista completa al corregir.
- Depende de: nada
- Estado: Requiere validación
- Nota (2026-11-20): revisión sobre 08297935. FriendsServiceAcceptRequestTests.cs:65 usa Assert.Single(actual.Notifications), por lo que la expresión concreta antes señalada ya no está presente. No se ejecutaron analizadores y la búsqueda de reportes locales no acredita el estado global de xUnit2013, IDE0059 o IDE0270. Consultar la lista actual de Visual Studio, distinguir advertencias de sugerencias y registrar ubicaciones antes de decidir cambios.

### BL-177 · RegisterPlayerAccount crea cuentas sin código y evita el 2FA (hallazgo adicional)
- Creado: 2026-11-15 00:32
- Por qué: IUsersManager.RegisterPlayerAccount (IUsersManager.cs:18) sigue publicado y PlayerAccountRegistrationService.RegisterPlayerAccount (:36) guarda la cuenta de inmediato con CreateAccount (:290), sin pasar por el código del correo. El cliente ya no la usa en el registro (CreateAccountViewModel.cs:206 llama a RequestAccountVerificationCode), pero el servidor no confía en el cliente (gov/CLAUDE.md §7): cualquier cliente WCF puede registrar una cuenta con un correo ajeno sin verificarlo. Opciones por presentar al encargado: retirar la operación del contrato (cambia el contrato y exige regenerar el proxy, BL-113) o hacer que falle cuando el 2FA esté activo. Severidad alta.
- Depende de: BL-007; decisión del encargado
- Estado: Pendiente

### BL-178 · TODO del chat sin identificador BL y con formato ajeno al Estándar (hallazgo adicional)
- Creado: 2026-11-15 00:32
- Por qué: Server/TLMG.Services/Chat/ChatService.cs:16 tiene «TODO: Persist the game (R-12), so that messages are saved in the appropriate scope.» sin (BL-xxx), en inglés y sin el formato de §12.3; el ámbito se guarda siempre como Lobby (ChatService.cs:17). Además Views/GameplayScreen.xaml.cs:84 usa «TODO (BL-161)» fuera del formato y cita BL-161, que sigue reservada sin entrada. Corregir ambos comentarios cuando se defina el ámbito de la partida (BL-106) y dar de alta BL-161 con su alcance.
- Depende de: BL-106; BL-161
- Estado: Parcial
- Nota (2026-11-20): revisión estática sobre 08297935. ChatService conserva TODO sin identificador BL y fija PersistedChatScope=Lobby al persistir; no basta corregir el comentario para resolver el ámbito funcional, que permanece en BL-106. GameplayScreen conserva TODO con referencia BL-161, que ya existe y está activo; el formato actual incluye token, texto y referencia, aunque la traducción y consistencia con §12.3 requieren revisión. Corregir trazabilidad del TODO de chat y coordinar comentarios con BL-189; no crear BL-161.

### BL-179 · EmailUtility registra el correo del destinatario y no valida su configuración (hallazgo adicional)
- Creado: 2026-11-15 00:32
- Por qué: EmailUtility.cs:101 y 105-106 escriben el correo del destinatario en el log. gov/CLAUDE.md §6 lo prohíbe de forma expresa (no registrar correos); el Estándar §10.3 (docs/Estandar.md:2571) prohíbe los «datos personales» pero no nombra el correo, así que se cita gov/CLAUDE.md §6. Los mensajes de log de ese archivo están en inglés y §6 pide español. Si EmailConfig.xml falta o está incompleto, el constructor solo registra el error y deja los campos en null (EmailUtility.cs:52-70), y SendEmail usa _senderEmail al crear la dirección (:83) sin comprobarlo; los catch de SendEmail solo cubren SmtpFailedRecipientException y SmtpException (:99-107). No se propone cambiar código hasta que el encargado decida.
- Depende de: BL-171
- Estado: Pendiente
- Nota (2026-11-20): revisión estática sobre 08297935. EmailUtility.SendEmail registra el destinatario explícitamente en ramas SmtpFailedRecipientException y SmtpException; además pasa la excepción completa, que podría contener datos personales y requiere revisión. Los logs revisados de despachadores usan ids y resultado, sin acreditar todos los mensajes indirectos. Configuración ausente o inválida se registra y puede dejar campos sin inicializar. Retirar datos personales tanto del mensaje como del detalle registrado manteniendo diagnóstico útil, validar fallos de configuración y verificar con pruebas; no se inspeccionaron logs reales. Independiente de BL-171 y de la separación de BL-147.

### BL-180 · Avatares fantasma en los lugares vacíos de la mesa de Gameplay
- Creado: 2026-11-17 11:49
- Por qué: en la prueba manual de I2a con tres jugadores aparecen, arriba de la mesa, avatares sin nombre con punto naranja y marcador de turno en los lugares donde no hay rival: los lugares sin rival no se ocultan. Cada lugar es un ContentControl con la plantilla OpponentTemplate (GameplayScreen.xaml:184-205) y la causa exacta en WPF está por confirmar. Defecto visual: no afecta datos ni turnos. Se corrige en un parche visual aparte, previo a I2b si no hay complicaciones.
- Depende de: nada
- Estado: Requiere validación
- Nota (2026-11-18): afc804b agrega ocultamiento de lugares sin rival; falta confirmar visualmente el escenario informado con clientes reales antes de cerrar.
- Nota (2026-11-20): revisión estática sobre 08297935. afc804b es ancestro de HEAD. OpponentSlotContentStyle colapsa ContentControl cuando Content es null; MatchBoardHelper asigna oponentes a posiciones a partir de jugadores. La lectura no acredita renderizado visual ni cobertura de clientes reales. En partida de tres comprobar que solo se ven los dos rivales y quedan vacíos los demás lugares; coordinar con BL-181.

### BL-181 · La mesa y la barra inferior de Gameplay cambian de altura al cambiar la pila de cartas
- Creado: 2026-11-17 11:49
- Por qué: en la prueba manual de I2a, al cambiar la pila de cartas la mesa y la barra inferior de GameplayScreen cambian de altura y el tablero se desplaza. La causa está por confirmar (la fila inferior es Auto, GameplayScreen.xaml:91-95, y la pila del jugador local se dibuja en ella). Defecto visual: no afecta datos ni turnos. Se corrige en el mismo parche visual de BL-180.
- Depende de: nada
- Estado: Requiere validación
- Nota (2026-11-18): afc804b reserva el espacio de las pilas; falta confirmar visualmente el escenario informado con clientes reales antes de cerrar.
- Nota (2026-11-20): revisión estática sobre 08297935. afc804b es ancestro de HEAD y añadió reserva para pilas; el XAML actual usa Viewbox con MaxHeight para pilas y conserva filas Auto, cuyo comportamiento final depende del contenido. No se encontraron mediciones o capturas posteriores en esta revisión. En la misma partida de BL-180 variar pilas y comprobar estabilidad de mesa y barra sin ocultar cartas.

### BL-182 · Pruebas manuales pendientes de I2a (canal de partida del cliente)
- Creado: 2026-11-17 11:49
- Por qué: I2a (servidor 52060b4, proxy bf24d95 y c8d5677, cliente dff94a9, sobre la partida en memoria de bb46e5b) quedó funcionalmente completa para su alcance. Verificado a mano por el encargado: tres jugadores registrados, modalidad de cuatro cartas, tres suscripciones aceptadas, mano propia privada y cantidades públicas de los rivales, turnos sincronizados y dos vueltas completas, colocación autoritativa, controles de apuesta deshabilitados y sin errores nuevos en el log de la partida probada. Sin verificar: (1) mesa con cinco y con seis jugadores; (2) caída aislada del canal de partida sin tirar la sesión (no se puede provocar solo con la interfaz); (3) cambio de idioma con la partida abierta; (4) los mensajes traducidos de las ErrorKey Match_* en la interfaz (el servidor las cubre con pruebas, la interfaz no); (5) las regresiones de AUD-001 (F1b, L1 y L2; la auditoría no tiene entrada propia en este archivo); (6) PENDIENTE DE REPRODUCCIÓN, no es un defecto confirmado: por la lectura del código, una suscripción rechazada podría mostrar la leyenda de conexión perdida (Gameplay_MatchUnavailableLabel) además del error, porque GameplayViewModel.OnMatchAvailabilityChanged marca la partida como no disponible siempre que IsMatchSubscribed es falso.
- Depende de: nada
- Estado: Parcial
- Nota (2026-11-20): revisión sobre 0829793. El desarrollador comunica pruebas funcionales de conexión, reconexión y abandono. No se ha probado una partida con seis jugadores; las demás comprobaciones de esta entrada continúan pendientes.
- Nota (2026-11-20): revisión sobre 0829793. El desarrollador confirma que no terminó la batería. Se conservan como pruebas previas las de tres jugadores, reconexión y abandono; faltan cinco/seis jugadores y demás casos enumerados.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

### BL-183 · Validación de identidad duplicada entre el chat y la partida en SessionRegistry
- Creado: 2026-11-17 11:49
- Por qué: ValidateMatchIdentityInLock (SessionRegistry.MatchPlay.cs:189) repite el criterio de ValidateChatIdentityInLock (SessionRegistry.Chat.cs:110): token vigente, cuenta registrada y canal que no pertenezca a otro jugador, cada una con su propio mapa de canales. gov/CLAUDE.md §3 manda no duplicar lógica y reportar la duplicidad en el backlog; se dejó así en I2a para no modificar el chat. Unificarla en un solo helper parametrizado por el mapa de canales, cubierto por las pruebas existentes de TLMG.Services.Tests. BL-112 no la cubre: trata la lista de caracteres del chat.
- Depende de: nada
- Estado: Postergado
- Nota (2026-11-20): revisión estática sobre 08297935. ValidateChatIdentityInLock y ValidateMatchIdentityInLock requieren sesión/canal, token reconocido, id registrado y canal no asignado a otro usuario; usan mapas distintos y luego comprueban pertenencia específica por separado. La duplicación puede divergir al evolucionar, pero no se identificó diferencia de autorización actual. Retomar ante cambio de reglas o divergencia, preservando pruebas de identidad, canal ajeno y pertenencia de ambos; no se ejecutaron.

### BL-184 · Validación manual multicliente de FugitiveKitten (FK-02)
- Creado: 2026-11-18 23:48
- Por qué: d12839b integra el minijuego, pero falta probar con tres clientes reales el atrape simultáneo, el escape, el resultado y su recuperación, la suscripción tardía, la latencia visual, el cambio de idioma y la transición a Gameplay; también comprobar las modalidades de cuatro y seis cartas. La evidencia WCF y WPF aislada no es E2E.
- Depende de: nada para la prueba básica; BL-058 para validar estabilidad prolongada y BL-118 y BL-119 para sus requisitos específicos.
- Estado: Parcial
- Nota (2026-11-20): revisión sobre 0829793. El desarrollador comunica que varios clientes ven Fugitive Kitten al iniciar con ocho cartas. Esto no acredita captura simultánea, escape ni sincronización visual.
- Nota (2026-11-20): revisión sobre 0829793. El desarrollador informa una partida real de tres clientes: apareció el minijuego, capturó al gatito, la partida continuó y terminó con ganador. No se acreditan captura casi simultánea, escape, recuperación, suscripción tardía, latencia, idioma ni errores excepcionales.
- Nota (2026-11-20): reconciliación V1/V2 sobre 08297935: se precisa el estado como Parcial conforme a las evidencias y faltantes ya registrados; no se realizó una prueba nueva.

## Finalizados

### BL-102 · Nombres genéricos en el cliente
- Creado: 2026-11-02 16:06
- Por qué: Handle* (ServerConnectionManager.cs:554 y 569; CreateAccountScreen.xaml.cs:139, 154 y 228; LoginScreen.xaml.cs:146 y 175; LeaderboardScreen.xaml.cs:120 y 134) y Process* (LobbyScreen.xaml.cs:234; VerificationScreen.xaml.cs:225) no dicen qué hacen (§3.7, gov/CLAUDE.md §9). HasFriendsChannel describe el mecanismo y no el significado ("es jugador registrado"). CommonErrorKeyHelper solo contiene constantes. Propuestas en el informe del 2026-11-02.
- Depende de: nada
- Estado: Finalizado el 2026-11-20 · commit 95fe633
- Nota (2026-11-02 17:23): en esta pasada se renombran los Handle* de las pantallas y de ServerConnectionManager, los Process* de LobbyScreen y VerificationScreen y HasFriendsChannel, que pasa a HasRegisteredSession. CommonErrorKeyHelper no se renombra: depende de la decisión sobre las claves de error (BL-017).
- Nota (2026-11-02 20:33): avance. d6b9ecf (ServerConnectionManager: DisconnectAndNotifyConnectionLost y DisconnectIfCurrentChannelEnded), 09a74d1 (HasRegisteredSession), bc85a6d (FinishRegistrationAttempt, FinishLogInAttempt, ShowLeaderboardResult, ShowConnectionError, ReturnToLogInAfterVerification) y bf490af (AppendOutgoingChatMessage, RecordFailedVerificationAttempt). Queda CommonErrorKeyHelper, que depende de la decisión de BL-017.
- Nota (2026-11-18): CommonErrorKeyHelper ya no aparece en el código actual y los renombres históricos están integrados. LeaderboardViewModel todavía contiene ProcessLeaderboardData; queda por revisar si cumple el criterio de nombres de la entrada y su relación con BL-017.
- Nota (2026-11-20): revisión sobre 08297935. Los renombres históricos constan en d6b9ecf, 09a74d1, bc85a6d y bf490af; CommonErrorKeyHelper ya no aparece y ErrorKeys agrupa constantes. 95fe633, ancestro de HEAD, renombra ProcessLeaderboardData a UpdateLeaderboard. HandleVerificationResult restante describe su contexto y no es el nombre genérico histórico. Las decisiones de claves siguen en BL-017.


### BL-149 · Datos fijos en ProfileViewModel
- Creado: 2026-11-11 08:56
- Por qué: ProfileViewModel conserva datos de prueba: «45 días» (DaysSincePasswordChange) y el nickname «WhiskerKnight». Deben salir del servidor; se ligan a BL-103.
- Depende de: BL-103
- Estado: Finalizado el 2026-11-20 · commit b97b4e7
- Nota (2026-11-11 23:55): estado (e74e10b). La migración de ProfileScreen a MVVM no quitó estos datos: DaysSincePasswordChange = 45 sigue en ProfileViewModel.cs:16 y «WhiskerKnight» en :23.
- Nota (2026-11-18): El nickname y avatar se cargan desde el servidor mediante perfil (ffc74cc, integrado en a335d85). DaysSincePasswordChange sigue fijo en 45; falta un dato autoritativo para eliminarlo (BL-103).
- Nota (2026-11-20): revisión sobre 08297935. ProfileViewModel obtiene el nickname de GetPlayerProfile y calcula los días con PasswordChangedAtUtc; PlayerProfileService y PlayerProfileRepository lo llevan desde user.password_changed_at al DTO. b97b4e7 contiene el cálculo en cliente y es ancestro de HEAD. No se encontraron los valores fijos originales en ProfileViewModel ni ProfileScreen. Puntos, rango y racha permanecen en BL-103; no se ejecutó prueba gráfica.


### BL-152 · Servidor: solicitudes de amistad (enviar, aceptar y rechazar)
- Creado: 2026-11-11 19:30
- Por qué: el Paquete 1 congeló el contrato (SendFriendRequest, AcceptFriendRequest, DeclineFriendRequest, sus Result, FriendRequest, OnFriendRequestReceived, OnFriendAdded y PendingFriendRequests en OperationSubscribeToFriendsResult) y dejó cascarones en FriendsService que responden Common_UnexpectedError. Falta: IFriendshipRepository (búsqueda por nickname, fila de la pareja, pendientes, alta y cambio de estado condicionado, con EF y sin concatenar SQL), las reglas de pareja en FriendsService (solicitud cruzada se acepta sola; reactivar tras rechazo sin espera; ErrorKey de ErrorKeys.Friends), los avisos fuera del lock, el refresco de la lista de amigos del canal en SessionRegistry (parte de altas de BL-071) y el rechazo del invitado con Common_InvalidSessionError. Fuera de alcance: cancelar, bloquear, reportar y eliminar amigos. Incluye pruebas con un stub de IFriendshipRepository.
- Depende de: Paquete 1; BL-153
- Estado: Finalizado el 2026-11-20 · commit 27cb845
- Nota (2026-11-11 21:22): avance en la rama paquete2-amigos, sin confirmar: SendFriendRequest, AcceptFriendRequest y DeclineFriendRequest quedaron implementados en FriendsService con IFriendshipRepository (FriendshipRepository con EF; los cambios de estado son UPDATE parametrizados con las filas afectadas revisadas), FindFriendsCallbackOfUser y AddFriendshipToOnlineChannels en SessionRegistry (esta última cubre la parte de altas de BL-071) y PendingFriendRequests en la suscripción, con pruebas en TLMG.Services.Tests. Falta compilar, correr las pruebas, aplicar la v4.1 (BL-153) y probarlas con el cliente (BL-154). Sigue pendiente hasta tener el commit.
- Nota (2026-11-11 23:55): commit encontrado: 27cb845 (FriendsService, FriendshipRepository y SessionRegistry), con pruebas FriendsService*Tests y SessionRegistryFriendsTests. Sigue pendiente porque no consta que se hayan corrido las pruebas ni aplicado la v4.1 (BL-153); el cliente ya la usa (93fa788).
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sigue Pendiente. El rango partió el contrato de amistades (IFriendshipManager, 2ab3567) y reescribió FriendsService (0e568ef, bbf1a49); las pruebas de FriendsService se adaptaron en c78a54f y entran en las 144 en verde que reporta el encargado. Falta aplicar y verificar la v4.1 de la base (BL-153), de la que depende.
- Nota (2026-11-18): Las 384 pruebas de Services se ejecutaron correctamente durante la integración anterior. Eso no acredita la aplicación de la v4.1 en las bases de desarrollo y entrega (BL-153), que sigue pendiente de verificación.
- Nota (2026-11-20): revisión sobre 0829793. FriendsService y FriendshipRepository implementan solicitudes; la aplicación y permisos efectivos de SQL v4.1 no están acreditados en esta revisión (BL-153).
- Nota (2026-11-20): revisión sobre 0829793. FriendsService implementa enviar, aceptar y rechazar, con repositorio y pruebas FriendsService; el equipo confirma los tres flujos con persistencia en la base utilizada. La aplicación y permisos en todas las bases siguen en BL-153; cancelación, eliminación y bloqueo en BL-156.


### BL-046 · Pantallas pendientes: GameplayScreen y ChangePasswordScreen
- Creado: 2026-10-25 14:23
- Por qué: LobbyScreen.xaml.cs:203 debe navegar a GameplayScreen al terminar la cuenta regresiva y ProfileScreen.xaml.cs:77 a ChangePasswordScreen; ninguna de las dos pantallas existe.
- Depende de: BL-040 y Reglas de The Last Meow (GameplayScreen); BL-042 (navegación)
- Estado: Finalizado el 2026-11-20 · commit b97b4e7
- Nota (2026-11-11 23:55): avance. GameplayScreen ya existe (Views/GameplayScreen.xaml.cs) y MainWindow la abre por reflexión en NavigateToAuthorizedMatch (MainWindow.xaml.cs:171-186, comentario BL-046). ChangePasswordScreen existe solo como Page de plantilla, sin lógica ni uso (Views/ChangePasswordScreen.xaml.cs, 28 líneas); ProfileViewModel.cs:190 conserva el TODO que cita esta entrada. Ver BL-169.
- Nota (2026-11-17 11:49): avance (dff94a9). GameplayScreen ya muestra datos reales de la partida: GameplayViewModel recibe la vista privada por IMatchConnection y la mano, la pila, el turno, la ronda, la fase y los contadores salen del servidor; los datos ficticios de jugadores y mano se retiraron de la pantalla. La apuesta actual permanece fija en 0 y sus controles (−, +, Retar y Rendirse) quedan deshabilitados hasta I2b (ver BL-166). Siguen pendientes ChangePasswordScreen (ver BL-169) y las apuestas, el revelado y el castigo (I2b). Sin cambio de estado.
- Nota (2026-11-18): Gameplay y ChangePassword ya tienen destino en ScreenFactory y navegación desde sus flujos (dff94a9, fd9d37a y a335d85). La actualización de contraseña sigue siendo un TODO sin servicio; no se cierra por existir únicamente la pantalla.
- Nota (2026-11-20): revisión sobre 0829793. Gameplay y ChangePassword tienen pantalla y navegación; ChangePasswordViewModel invoca al servidor desde b97b4e7. El desarrollador informa validación manual de la compañera, sin detalle adicional de pasos. Otros requisitos de Gameplay y validaciones siguen en sus BL propios.


### BL-171 · URGENTE: EmailConfig.xml está versionado; ignorarlo y dejar un .example
- Creado: 2026-11-15 00:32
- Por qué: Server/TLMG.Services/EmailConfig.xml está en Git (agregado en 1b40829, copiado a la salida en TLMG.Services.csproj:138) con el campo AppPassword con un texto de ejemplo. Es urgente porque ya hay cuentas válidas: la cuenta Gmail emisora existe y los códigos ya llegan por correo (BL-008, cerrada el 2026-11-15), así que el archivo recibirá la contraseña de aplicación real en la copia local del encargado y quedaría lista para un commit. El .gitignore no lo cubre (solo ignora ConnectionStrings.config, .gitignore:432) y gov/CLAUDE.md §5 y §7 prohíben secretos en Git. Pasos: (1) mientras no se ignore, quien tenga la clave real en su copia local usa git update-index --skip-worktree sobre ese archivo para no subirla por accidente; (2) antes de sacarlo de Git (git rm --cached), quien tenga su clave real debe respaldarla fuera del repositorio, porque el pull borra la copia local; (3) dejar EmailConfig.xml.example versionado e ignorar EmailConfig.xml; (4) ajustar la referencia de TLMG.Services.csproj:138 para que el build no falle si el archivo no existe; (5) pendiente de D-43: cifrado con DPAPI (BL-007). No se escribe ninguna credencial en esta entrada. Severidad urgente.
- Depende de: BL-032
- Estado: Finalizado el 2026-11-20 · commit 5df5e4f
- Nota (2026-11-18): avance sin commit: se agrega una plantilla SMTP con marcadores; el archivo local sigue ignorado y su copia en compilación es condicional. Pendiente cifrado DPAPI (D-43, BL-007).
- Nota (2026-11-18): La eliminación del XML rastreado y su exclusión llegaron desde main (eb09866); a335d85 condicionó su copia en compilación y 5df5e4f agregó EmailConfig.xml.example con marcadores. El archivo real no está rastreado. Sigue pendiente el cifrado DPAPI exigido por D-43 (BL-007); la nota anterior «sin commit» quedó desactualizada.
- Nota (2026-11-20): revisión sobre 0829793. El XML real no está rastreado; eb09866 lo ignora, a335d85 retiró el archivo rastreado y condicionó su inclusión, y 5df5e4f agregó la plantilla. El cifrado DPAPI continúa en BL-007/D-43.


### BL-070 · Entregar los avisos de EndSession cuando exista cerrar sesión
- Creado: 2026-10-29 15:42
- Por qué: `ISessionRegistry.EndSession` ya devuelve la sala y la presencia que hay que avisar (BL-065), pero nadie lo llama: el cierre de sesión por el jugador no existe. Cuando exista, el servicio que lo llame debe entregar `SessionNotificationHelper.CreateSessionEndNotifications` fuera del lock, como ya hace el login que reemplaza una sesión.
- Depende de: BL-058
- Estado: Finalizado el 2026-11-20 · commit 317ff25
- Nota (2026-10-29 22:53): el cierre de sesión ya existe sin EndSession(token): MainMenuScreen.BtnLogOut_Click llama a Disconnect, que cierra el canal de amigos, y el servidor termina la sesión con sus avisos al detectar el cierre (d6d6326; ServiceImplementation.FriendsManager.cs:121). EndSession(token) (SessionRegistry.cs:73) sigue sin quién lo llame. Decidir entre retirarlo o agregar una operación LogOut explícita; con la primera opción la entrada se cierra.
- Nota (2026-11-20): revisión sobre 0829793. IUsersManager.LogOut llama TryLogOut y entrega CreateSessionEndNotifications después de la revocación. La reconciliación por respuesta perdida pertenece a BL-058.


### BL-040 · Crear TLMG.Domain (motor de reglas puro)
- Creado: 2026-10-25 00:49
- Por qué: Estándar §4.2 y gov/CLAUDE.md §13; no existe. Base para llegar al mínimo de pruebas sin infraestructura.
- Depende de: Reglas de The Last Meow en docs/ (BL-026); BL-039
- Estado: Finalizado el 2026-11-18 · commit d12839b
- Nota (2026-11-06 13:30): decisión del encargado: Domain amplio (entidades, Validators, Repositories, Engine; Estándar §4.3), sin "Managers" (§3.14). Etapa 1: constantes, entidades y validadores. Etapa 2: interfaces de repositorio (DataAccess referencia a Domain; revisar cómo Services obtiene los repositorios, D-51). Etapa 3: motor de reglas.
- Nota (2026-11-07 04:28): el dominio alojará las constantes de tiempo del Gatito Fugitivo (arranque, medio carril, oculto y tolerancia de 150 ms) y el catálogo de rutas, con el escape calculado como la ventana menos los demás tramos (etapa 1 de la decisión del 2026-11-06).
- Nota (2026-11-15 00:32): revisado sobre 8aa6844. Avance de la etapa 2: el rango agregó a TLMG.Domain.Repositories las interfaces y tipos del chat (IChatMessageRepository, ChatMessageRecord, ChatMessageSaveStatus y ChatMessageScope; d64acfa) y de los códigos de verificación (IVerificationCodeRepository, PendingRegistrationRecord, VerificationCodeRecord y VerificationCodeValidationResult; 0429cb1). Sigue sin Engine (no existe Server/TLMG.Domain/Engine/): la etapa 3 y el motor de reglas están pendientes. Un detalle por revisar: VerificationCodeValidationResult.Status es una cadena que PlayerAccountRegistrationService.cs:221-250 compara con «Valid», «Invalid», «TooManyAttempts», «Expired» y «NoActiveCode», sin enum ni constantes.
- Nota (2026-11-18): Engine ya existe con modalidades, apuestas, retos, castigos, salidas y clasificación (603b6fc, 3af8ea2, d35fcf7, 4189e02, 3b928cc y 257b5b1). Sigue pendiente el catálogo y las constantes del Gatito Fugitivo añadidos al alcance de esta entrada; no se concluye que todas las reglas estén implementadas.
- Nota (2026-11-18): Avance local FK-02 sin commit: catálogo inicial de ruta y tolerancia en Domain; falta integrarlo con Services, contratos y cliente. No se cierra la entrada.
- Nota (2026-11-18): El motor Domain y el catálogo aprobado, con sus duraciones y tolerancia, están implementados; el catálogo se integró en d12839b.

### BL-116 · Contrato IMatchManager del Gatito Fugitivo y sus DTO
- Creado: 2026-11-07 04:28
- Por qué: no existe ningún contrato de partida. Faltan la operación de un solo sentido TryCatchFugitiveKitten(sessionToken), los callbacks OnFugitiveKittenStarted y OnFugitiveKittenResolved, los DTO sin sufijo Data (D-56): FugitiveKittenRoute, KittenSegment y el resultado (nombre por decidir), su archivo ServiceImplementation.MatchManager.cs, el endpoint en el host y la regeneración del proxy.
- Depende de: BL-040; BL-109 (los resultados heredan de OperationResult; regenerar el proxy una sola vez); BL-113
- Estado: Finalizado el 2026-11-18 · commit d12839b
- Nota (2026-11-17 11:49): avance de I2a. Ya existe el contrato IMatchManager con SubscribeToMatch(sessionToken, matchId) y PlaceCard(sessionToken, cardKind), su callback IMatchManagerCallback (SubscribeToMatchResult, PlaceCardResult y OnMatchViewChanged), los DTO MatchView y MatchPlayerView, los enums MatchCardKind y MatchRoundPhase, ServiceImplementation.MatchManager.cs y el endpoint MatchManager del host. Evidencia: bb46e5b (partida en memoria), 52060b4 (contrato y servidor), bf24d95 y c8d5677 (proxy, con ReferenceAllAssemblies restaurado) y dff94a9 (cliente). El «Por qué» («no existe ningún contrato de partida») quedó desactualizado para ese alcance. Siguen pendientes la operación TryCatchFugitiveKitten con sus callbacks y DTO, y las operaciones posteriores a I2a (apuesta, revelado, castigo y salida). I2a quedó funcionalmente completa para su alcance; la evidencia manual y lo no verificado están en BL-182. Sin cambio de estado.
- Nota (2026-11-18): d12839b implementa el DTO incluido en MatchView, las operaciones y el proxy regenerado. La alternativa aprobada usa OnMatchViewChanged en lugar de callbacks de inicio y resolución separados.

### BL-117 · Envío del intento de atrape y recepción del resultado del Gatito Fugitivo
- Creado: 2026-11-07 04:28
- Por qué: el clic sobre el gatito no hace nada. Falta enviar el intento solo cuando el gatito está visible, recibir la ruta, el color y el resultado por callback con Dispatcher.BeginInvoke, y mostrar "lo atrapaste" o "lo atrapó otro" comparando el ganador que decide el servidor (QAS-04) con el jugador local, con el nickname real en lugar del de muestra.
- Depende de: BL-116, BL-041; BL-108 (canal por separado en ServerConnectionManager)
- Estado: Finalizado el 2026-11-18 · commit d12839b
- Nota (2026-11-18): FK-01 solo expone CatchKittenCommand deshabilitado y un estado de presentación; no envía intentos ni recibe ganador o color del servidor. El nombre de muestra se limita a la previsualización BL-122.
- Nota (2026-11-18): FK-02 en curso: arbitraje y barrera internos en Services; falta publicar estado y operaciones mediante WCF y conectarlos al cliente.
- Nota (2026-11-18): avance local etapa 3: DTO y operaciones del servidor; proxy y conexión del cliente pendientes de regeneración e integración.
- Nota (2026-11-18): avance local etapa 3B: proxy y metadatos generados con svcutil; canal y callbacks conectados. Falta integración visual y prueba WCF de partida.
- Nota (2026-11-18): FK-02 integra el ViewModel y el canal de partida; pendiente validar WCF real con clientes.
- Nota (2026-11-18): ajuste temporal local: la vista incluye hora de servidor y el cliente proyecta su antigüedad; pendiente validar latencia real.
- Nota (2026-11-18): d12839b conecta el intento y el resultado autoritativo, incluyendo nickname real y recuperación de la vista. La prueba multicliente queda en BL-184.

### BL-122 · Retirar los accesos temporales de FugitiveKittenScreen
- Creado: 2026-11-07 04:28
- Por qué: la tecla F8 en MainMenuScreen, las teclas 1 a 6 (estados), C (colores) y Esc (GoBack) dentro de la pantalla, los estados congelados y el nickname de muestra existen solo para revisar la interfaz y no deben llegar a la entrega.
- Depende de: BL-118, BL-119
- Estado: Finalizado el 2026-11-18 · commit d12839b
- Nota (2026-11-11 23:55): avance. El acceso F8 salió de MainMenuScreen: ahora es un KeyBinding en MainMenuScreen.xaml hacia MainMenuViewModel.NavigateToFugitiveKittenCommand (MainMenuViewModel.cs:100-101 conserva los TODO de BL-122 y BL-118) y ScreenId.FugitiveKitten está en la fábrica (c6a081a). Siguen pendientes las teclas internas de la pantalla; FugitiveKittenScreen.xaml.cs:452 aún usa MainWindow.
- Nota (2026-11-18): FK-01 conserva F8 y las teclas 1–6, C y Esc para previsualización explícita; fuera de ella las teclas de demostración no cambian el estado. Siguen pendientes su retirada y la integración real.
- Nota (2026-11-18): FK-02 retiró F8 y las teclas de previsualización; falta confirmar la ruta real en prueba manual.
- Nota (2026-11-18): d12839b retira F8, las teclas 1–6, C y Esc y el nickname de muestra; la revisión visual queda en BL-184.

### BL-131 · SetDigit de VerificationViewModel notifica el cambio con un nombre de propiedad equivocado
- Creado: 2026-11-10 11:40
- Por qué: SetDigit llama a SetProperty sin el nombre de la propiedad y [CallerMemberName] lo completa con «SetDigit»; el evento PropertyChanged no sale como Digit1 a Digit6 y la vista no se entera de los cambios hechos desde el ViewModel (por ejemplo, ClearDigits). Ya ocurría con la base anterior; no lo introdujo la Fase 4b. Hay que pasar el nombre de la propiedad a SetDigit y de ahí a SetProperty.
- Depende de: Fase 7
- Estado: Finalizado el 2026-11-18 · commit adc0991
- Nota (2026-11-18): corrección local sin commit: cada dígito pasa su nombre a SetProperty. Cliente Debug compilado; comprobación ejecutable aislada aprobó PropertyChanged de los seis dígitos, valores repetidos, ClearDigits y CanExecute. Pendiente hasta el commit.
- Nota (2026-11-18): adc0991 implementa la corrección de los seis nombres de propiedad; la nota previa documenta la comprobación aislada.


### BL-055 · Nombre del helper de validación de correo
- Creado: 2026-10-25 19:32
- Por qué: `PlayerAccountRegistrationValidationHelper` lo usa también el login; evaluar un nombre o ubicación común. Prioridad baja.
- Depende de: nada
- Estado: Finalizado el 2026-11-15 · commit a9de731
- Nota (2026-11-18): AccountFieldValidator.IsEmailValid es compartido por PlayerLoginValidator y PlayerAccountRegistrationValidator; la ubicación y el nombre comunes quedaron implementados.


### BL-095 · El cliente no tiene red para excepciones no controladas
- Creado: 2026-11-02 16:06
- Por qué: App.xaml.cs no maneja DispatcherUnhandledException, y CloseRegistrationClient, CloseLogInClient y CloseLeaderboardClient no capturan TimeoutException (CreateAccountScreen.xaml.cs:209, LoginScreen.xaml.cs:197, LeaderboardScreen.xaml.cs:187). Un error en la interfaz cierra la aplicación sin log ni mensaje. Agregar un manejador en App que registre con Error, muestre Common_UnexpectedError y regrese al inicio de sesión, como OnServerConnectionLost.
- Depende de: BL-017 (clave única de error inesperado)
- Estado: Finalizado el 2026-11-12 · commits ccad58d y 2e26417
- Nota (2026-11-02 17:23): diferida por decisión del encargado; pendiente conocido.
- Nota (2026-11-11 23:55): avance (b6dec93). Leaderboard ya captura TimeoutException al abrir el canal (ServerConnectionManager.Leaderboard.cs:37) y al cerrarlo (CloseClientSafely); con Login y CreateAccount (384ef1a) queda resuelta la parte de CloseRegistrationClient, CloseLogInClient y CloseLeaderboardClient. El manejador de DispatcherUnhandledException en App.xaml.cs está hecho en ccad58d (registra con Error, navega al login y muestra Common_UnexpectedError), pero en la rama mvvm-resto y no integrado en dev-LS_Angel; sigue en curso hasta el merge.
- Nota (2026-11-18): App registra DispatcherUnhandledException, informa Common_UnexpectedError y navega al login; los cierres usan CloseClientSafely con TimeoutException. ccad58d implementa el manejador y 2e26417 integra el conjunto en esta rama.


### BL-001 · Hash de contraseña: SHA-256 + sal (decidido), implementar
- Creado: 2026-10-20 10:30
- Por qué: hoy la contraseña se guarda en claro. Decisión del equipo (2026-10-20 22:32): SHA-256 con sal por contraseña, opción A, alineada con el SAD; se descartó PBKDF2 por familiaridad del equipo. Implementación prevista: `IPasswordHasher` y `PasswordHasher` en el servidor, formato versionado (`v1$sal$hash`), sal aleatoria, comparación de tiempo constante. Decisión registrada en la bitácora (D-44).
- Depende de: BL-016 (el login debe pasar por el servidor antes o junto con el hash)
- Estado: Finalizado el 2026-10-25 · commits 3b6c905, bdd0dfe y 445d216
- Nota (2026-10-25): cierre. El hash lo calcula el servidor (`IPasswordHasher` y `PasswordHasher`, formato `v1$sal$hash`) y el inicio de sesión pasa por el servidor. Las cuentas viejas con la contraseña en claro ya no pueden iniciar sesión.

### BL-002 · Renombrar el contrato de registro a `IUsersManager`
- Creado: 2026-10-20 10:30
- Por qué: `IAccountRegistrationService` no sigue D-41 ni §3.14. Incluye callback, archivo parcial, endpoint y regenerar el proxy.
- Depende de: acuerdo con Nicole sobre quién lo hace; BL-003 y BL-004 (ronda 1, integrada)
- Estado: Finalizado el 2026-10-25 · commits af8fbce y 563cd51
- Nota (2026-10-25): cierre. El renombre del contrato, el callback, el archivo parcial y el endpoint del servidor se hicieron en af8fbce, y la regeneración del proxy del cliente en 563cd51. La dirección del endpoint conserva el nombre `AccountRegistrationService`: el Estándar no tiene regla para ello; decisión del encargado.

### BL-003 · Renombrar `AccountRegistrationManager`
- Creado: 2026-10-20 10:30
- Por qué: `Manager` no aplica a lógica de negocio (§3.14); falta decidir el sufijo (D-42).
- Depende de: BL-002
- Estado: Finalizado el 2026-10-20 · commit 879b9e2 (renombre a PlayerAccountRegistrationService) y 3f7dea2 (carpeta Accounts), integrados en d317003

### BL-016 · Migrar login y clasificación al servidor y quitar la referencia a TLMG.DataAccess del cliente
- Creado: 2026-10-20 11:40
- Por qué: hoy el cliente consulta la BD directamente (el login entra con el servidor apagado); viola SAD §4.3 y Zero-Trust.
- Depende de: BL-002
- Estado: Finalizado el 2026-10-25 · commits 3b6c905, bdd0dfe, 445d216, c0fc127, e09ed5b, ea27455, b628f1b
- Nota (2026-10-25): evidencia del acceso directo a la base desde el cliente (git grep sobre Client): LoginScreen.xaml.cs:104 y LeaderboardScreen.xaml.cs:76 abren new TLMG_DBEntities() desde la vista; Client/TLMG.Client/App.config:7 tiene su propia cadena de conexión TLMG_DBEntities; Client/TLMG.Client/TLMG.Client.csproj:242 y Client/TLMG.Client.slnx:3 referencian TLMG.DataAccess. Contradice Estándar §4.1 y Zero-Trust. Comprobación funcional: con el servidor apagado el inicio de sesión sigue funcionando; el registro ya pasa por WCF y con el servidor apagado muestra el error de red controlado. El alcance incluye retirar de la tabla de posiciones su consulta directa y usar un contrato del servidor para la clasificación (`ILeaderboardManager`, D-41).
- Nota (2026-10-25): el cliente conserva el `PackageReference` a EntityFramework (Client/TLMG.Client/TLMG.Client.csproj, alrededor de la línea 233); se retira junto con la referencia a TLMG.DataAccess, cuando ya no quede código del cliente que use TLMG_DBEntities. Quitarlo antes rompería la compilación de LoginScreen y LeaderboardScreen.
- Nota (2026-10-25): el login y el hash están hechos (3b6c905, bdd0dfe, 445d216). Las referencias a LoginScreen.xaml.cs:104 y a las líneas de consulta directa de esa pantalla ya no existen: LoginScreen usa el servidor. Queda el ranking por el servidor (`ILeaderboardManager`, LeaderboardScreen.xaml.cs:76) y, cuando LeaderboardScreen deje de usar TLMG_DBEntities, retirar TLMG_DBEntities de App.config, el `PackageReference` a EntityFramework, la referencia a TLMG.DataAccess y Client.slnx. No cerrada.
- Nota (2026-10-25): cierre. El login pasa por `IUsersManager` (3b6c905, bdd0dfe, 445d216) y el ranking por `ILeaderboardManager` (servidor c0fc127, proxy e09ed5b, pantalla ea27455). Con b628f1b el cliente quedó sin Entity Framework, sin referencia a TLMG.DataAccess y sin la cadena de conexión TLMG_DBEntities (git grep sobre Client: sin coincidencias de TLMG_DBEntities, System.Data.Entity, TLMG.DataAccess ni EntityFramework). Quedan fuera de esta entrada los valores fijos del jugador actual (BL-030) y la capa Comunicación (BL-036).

### BL-019 · Carpeta para las interfaces de repositorios en `TLMG.DataAccess`
- Creado: 2026-10-20 12:30
- Por qué: separar las interfaces de las implementaciones. El Estándar §4.3 (estructura de carpetas y namespaces) no tiene contenido, así que hay que decidir la convención (carpeta y namespace) y extender el Estándar antes de mover archivos. El csproj clásico exige editar las entradas `Compile`.
- Depende de: decisión de estructura; completar Estándar §4.3
- Estado: Finalizado el 2026-10-25 · commit 55d7070
- Nota (2026-10-25): posiblemente obsoleta por Estándar §4.3; confirmar con el encargado.
- Nota (2026-10-29 22:53): se cierra como decisión adoptada: el Estándar §4.3 fija para TLMG.DataAccess las carpetas DatabaseModel y Repositories. Las interfaces (IPlayerRepository, IFriendshipRepository) viven en Repositories junto a sus implementaciones; no hay que mover nada ni queda implementación pendiente.

### BL-022 · Política de caídas y reconexión (servidor y clientes)
- Nota (2026-11-18 12:55): evidencia reportada por el encargado: en prueba con tres clientes, Chat cerró aproximadamente diez minutos después de su última operación; después cerró Friends y se invalidaron sesiones. P1 de estabilidad WCF; revisar trazas por correlación y corregir el ciclo de vida en un incremento independiente.
- Creado: 2026-10-20 16:25
- Por qué: definir qué pasa si el servidor se reinicia, cae abruptamente o se apaga la PC, y si un cliente se cae con una sesión dúplex abierta (jugadores fantasma en lobby o partida, estado de la partida en memoria). Opciones a evaluar sin hilos propios: tiempos de inactividad de WCF y, en el cliente, `DispatcherTimer` para un latido (Estándar §11.4). Es una decisión de arquitectura: requiere ADR y alinear las reglas del juego sobre desconexión.
- Depende de: BL-021; revisión del SAD y de las reglas del juego
- Estado: Finalizado el 2026-10-25 · commit 55d7070
- Nota (2026-10-25): posiblemente superado por D-01, D-54 y ADR-04; decidir.
- Nota (2026-10-29 22:53): se cierra como decisión adoptada. El Estándar ya adopta ADR-04 (latidos, ventana de gracia y snapshot), D-01 y D-54 (plazos por hora límite en UTC, sin temporizadores en el servidor). La implementación sigue en BL-058 y BL-066.

### BL-025 · Reparar `origin/main` (marcadores de conflicto en 74e5f9a)
- Creado: 2026-10-20 22:15
- Por qué: `main` se subió con marcadores de conflicto en 4 archivos y no compila. `dev-LS_Angel` ya contiene el contenido válido de los commits de armasstella (reproducidos en 25aae1c y 53c0e8b; integrados en bb13661) y la fase 2 (8e260fc, 9e4365d). Plan: registrar `74e5f9a` en `dev-LS_Angel` con `merge -s ours` (el árbol no cambia) y avanzar `main` por fast-forward, sin forzar historia.
- Depende de: nada
- Estado: Finalizado el 2026-10-20 · commit 17a2738 (merge -s ours de main y avance de main por fast-forward, de 74e5f9a a 17a2738)

### BL-028 · Añadir `Service` a la tabla del Estándar §3.14
- Creado: 2026-10-20 22:55
- Por qué: D-49 adopta el sufijo `Service` para la lógica de negocio del servidor y el Estándar §3.14 no lo tiene. Fila a agregar: `Service` · Lógica de negocio del servidor (componentes del SAD, por ejemplo C-06); nunca persistencia ni contratos WCF · `PlayerAccountRegistrationService`.
- Depende de: D-49
- Estado: Finalizado el 2026-10-25 · commit 55d7070
- Nota (2026-10-25): cumplido en el Estándar v3; falta el hash de commit para cerrarlo.
- Nota (2026-10-29 22:53): cierre. La fila `Service` está en docs/Estandar.md:1089 desde la primera versión en Markdown (55d7070). El límite "nunca persistencia ni contratos WCF" lo cubre la fila de la capa TLMG.Services (docs/Estandar.md:1164).

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
- Estado: Finalizado el 2026-10-25 · commits 3b6c905, bdd0dfe, 445d216, c0fc127, e09ed5b, ea27455 y b628f1b
- Nota (2026-10-25): el alcance incluye retirar la cadena de conexión TLMG_DBEntities del cliente (Client/TLMG.Client/App.config:7) y la referencia a TLMG.DataAccess (TLMG.Client.csproj:242 y TLMG.Client.slnx:3). Comprobación funcional: con el servidor apagado el inicio de sesión sigue funcionando porque LoginScreen.xaml.cs:104 consulta la base directamente.
- Nota (2026-10-25): el cliente conserva el `PackageReference` a EntityFramework (Client/TLMG.Client/TLMG.Client.csproj, alrededor de la línea 233); se retira junto con la referencia a TLMG.DataAccess, cuando ya no quede código del cliente que use TLMG_DBEntities. Quitarlo antes rompería la compilación de LoginScreen y LeaderboardScreen.
- Nota (2026-10-25): el login y el hash están hechos (3b6c905, bdd0dfe, 445d216). Ya no existen las referencias a LoginScreen.xaml.cs:104-108 ni a PlayerAccountRegistrationService.cs:97-98 y 147-151: el servidor hashea con `IPasswordHasher` y LoginScreen usa `LogIn`.
- Nota (2026-10-25): el ranking ya viaja por `ILeaderboardManager` (c0fc127, e09ed5b, ea27455): máximo 100 jugadores, solo cuentas USER_ACTIVE, ordenados por puntos y luego por nickname; el cliente ya no usa EF ni TLMG.DataAccess (b628f1b). Queda pendiente: los valores fijos del jugador actual (`CurrentUserStandingRank` y `CurrentUserStandingPoints`, FIXME en LeaderboardScreen.xaml.cs:43, que cita esta entrada), que dependen del registro de sesiones (D-41). `ILeaderboardManager` se puede consultar sin sesión porque el ranking es público; se revisará cuando exista el registro de sesiones. No cerrada.
- Nota (2026-10-29 22:53): cierre. El login y el hash (3b6c905, bdd0dfe, 445d216) y el ranking (c0fc127, e09ed5b, ea27455, b628f1b) pasan por el servidor. Los valores fijos del jugador actual (FIXME de LeaderboardScreen.xaml.cs:43) pasan a BL-091.

### BL-035 · Retirar el proxy TLMGService y los archivos obsoletos de Communication/
- Creado: 2026-10-25 00:49
- Por qué: apuntan a un contrato que ya no existe (OnRegisterResult, OperationResultData) y nadie los usa. La carpeta Communication/ se conserva: es la capa de Comunicación (Estándar §4.1, §4.3). Ya listado en BL-027; se separa para cerrarlo con su propio commit.
- Depende de: autorización explícita del encargado; BL-005
- Estado: Finalizado el 2026-10-25 · commit a8cdce2
- Nota (2026-10-25): el proxy TLMGService y los dos archivos de Communication/ se retiraron en a8cdce2; no quedan referencias en el código. Como Git no guarda carpetas vacías, Communication/ ya no aparece en el repo; BL-036 la vuelve a crear con el callback.

### BL-038 · Citar el BL en los comentarios especiales y registrar los faltantes
- Creado: 2026-10-25 00:49
- Por qué: Estándar §12.3 exige el formato "// TOKEN: Texto. (BL-xxx)." y el registro en el backlog. Los 13 comentarios actuales no citan BL: LeaderboardScreen.xaml.cs:40, 80, 206, 207; LobbyScreen.xaml.cs:255, 263; ProfileScreen.xaml.cs:77; PlayerRepository.cs:16, 19; PlayerAccountRegistrationService.cs:97, 149, 153. Diez no tienen entrada. El :97 además no lleva punto final.
- Depende de: nada
- Estado: Finalizado el 2026-10-25 · commit 7ee6f16
- Nota (2026-10-29 22:53): cierre. Los 13 comentarios especiales del código citan un BL existente y abierto, con el formato de §12.3 (git grep sobre 53263f1). Los que no tenían entrada quedaron registrados en BL-043 a BL-047.

### BL-050 · Corregir el Estándar tras la revisión de los pasos 4a/4b y publicar docs/Estandar.md
- Creado: 2026-10-25 17:57
- Por qué: la revisión de los pasos 4a/4b contra el Estándar dejó correcciones de redacción y de criterio. Cambios: §10.2 y §10.3 (`catch (Exception)` solo en límites, registro con `_log.Error`, Fatal solo según §10.3); §10.9 y D-57 (criterio de accesibilidad relajado; D-32 no cambia); §15.2 (pruebas de integración del servidor en un único proyecto /test/TLMG.Server.Tests); §4.4 y §7.3 (se aceptan las dos formas de pack URI); §13.5 (ternario en una sola línea); §3.14, §17.1 y §20.2 (sufijo Request y D-56); se retiró del Estándar el estado actual y las menciones a BL. D-57 se registra en la Bitácora, que vive fuera del repositorio.
- Depende de: nada
- Estado: Finalizado el 2026-10-25 · commit 1d2f401

### BL-057 · Revisar App.config del cliente tras actualizar la referencia de servicio
- Creado: 2026-10-25 20:30
- Por qué: al regenerar la referencia de servicio, Visual Studio crea el binding nuevo con valores por defecto y agrega la identidad de la máquina de quien actualiza. Revisar Client/TLMG.Client/App.config: los bindings deben conservar los tiempos de D-46 (10/15/5 s) y los endpoints no deben llevar `<identity>` con cuentas de Windows.
- Depende de: nada
- Estado: Finalizado el 2026-10-29 · commit 3fd8662
- Nota (2026-10-29 22:53): verificado sobre 53263f1. Los cuatro bindings de Client/TLMG.Client/App.config conservan 5/10/15 s (D-46) y ningún endpoint lleva <identity>. La revisión debe repetirse en cada regeneración del proxy; se propone llevarla como paso fijo de la regeneración (Estándar, D-40) en lugar de una entrada abierta.
- Nota (2026-11-15 00:32): verificado sobre 8aa6844, tras la regeneración del proxy de Nicole (62330d0). Client/TLMG.Client/App.config conserva openTimeout 10 s, sendTimeout 15 s y closeTimeout 5 s en los cinco bindings (App.config:9, 14, 19, 24 y 29) y ningún endpoint lleva <identity>; el único cambio del archivo fueron dos líneas en blanco (git diff 78e1fa1..8aa6844). Sin cambio de estado: la entrada ya está finalizada.
- Nota (2026-11-17 11:49): procedimiento real de la regeneración del proxy con Visual Studio el 2026-11-17. «Actualizar referencia de servicio» falló con NullReferenceException; el rodeo fue quitar la referencia y volver a agregarla con el mismo nombre. Visual Studio reescribió Client/TLMG.Client/App.config (se perdieron los tiempos de 10, 15 y 5 s y se agregó <identity> en los endpoints) y cambió ReferenceAllAssemblies a true en Reference.svcmap. Se descartó el App.config generado, se restauró el .svcmap (c8d5677) y el binding y el endpoint de IMatchManager se agregaron a mano en dff94a9, con los mismos tiempos y sin <identity>. Se agrega al paso fijo de la regeneración: revisar el git diff de App.config y de Reference.svcmap después de cada regeneración. Sin cambio de estado: la entrada ya está finalizada.

### BL-064 · Emitir las autorizaciones de entrada al aceptar una invitación
- Creado: 2026-10-29 15:07
- Por qué: `ISessionRegistry.IssueRoomEntryAuthorization` existe y EnterLobby ya las exige y las consume, pero nadie las emite todavía (hay un UNDONE que cita esta entrada). Las emitirá el paso de invitaciones, al aceptar el invitado; y quien invita debe estar en la sala y tener al invitado como amigo aceptado (IFriendshipRepository).
- Depende de: paso de invitaciones
- Estado: Finalizado el 2026-10-29 · commit bac66e1

### BL-065 · Avisar a la sala cuando una sesión termina o es reemplazada
- Creado: 2026-10-29 15:07
- Por qué: `StartSession` (reemplazo) y `EndSession` retiran al jugador de su sala, de su callback y de sus autorizaciones, pero no avisan a los demás jugadores: ven la lista vieja hasta el siguiente cambio. Hay que devolver los avisos desde el registro y entregarlos fuera del candado, como ya se hace en EnterLobby y LeaveLobby.
- Depende de: nada
- Estado: Finalizado el 2026-10-29 · commit d6d6326

### BL-067 · Agregar al cliente las claves de idioma del lobby
- Creado: 2026-10-29 15:07
- Por qué: el servidor ya devuelve Lobby_InvalidSessionError, Lobby_AlreadyInRoomError, Lobby_CannotJoinRoomError, Lobby_RoomFullError y Lobby_UnexpectedError. Faltan en Strings.es-MX.xaml y Strings.en-US.xaml, con paridad exacta.
- Depende de: nada
- Estado: Finalizado el 2026-10-29 · commits 1db83da, 33dc497 y e685982

### BL-073 · Agregar al cliente las claves de idioma de amigos y los renombres de errores
- Creado: 2026-10-29 15:42
- Por qué: faltan en Strings.es-MX.xaml y Strings.en-US.xaml, con paridad exacta, las claves nuevas (Friends_NotInRoomError, Friends_CannotInviteError, Friends_InvitationNotValidError) y los renombres Common_InvalidSessionError, Common_UnexpectedError y Common_RoomFullError (antes Lobby_*). Complementa BL-067, cuya lista queda desactualizada.
- Depende de: nada
- Estado: Finalizado el 2026-10-29 · commits 1db83da, 33dc497 y e685982

### BL-075 · El cliente no conoce su propio nickname ni su id de usuario
- Creado: 2026-10-29 16:36
- Por qué: OperationLogInResult solo trae IsSuccess, ErrorKey y SessionToken (decisión del paso 3), y OperationSubscribeToFriendsResult solo trae los amigos. ServerConnectionManager guarda únicamente el token, así que MainMenuScreen y LobbyScreen no pueden mostrar el nombre propio ni distinguirse en la lista de la sala. Hay que decidir si el login lo devuelve o si se agrega una operación de perfil.
- Depende de: decisión del encargado
- Estado: Finalizado el 2026-11-03 · commits 7f23bda, bcf1ca7, b241a87 y eb765d6
- Nota (2026-11-03 10:31): el servidor ya entrega el nickname y el id del jugador en OperationLogInResult (UserId y Nickname, solo si el inicio de sesión fue aceptado; parte del servidor de G6). Falta que el encargado regenere el proxy y que el cliente guarde esos datos (ServerConnectionManager) y los muestre.
- Nota (2026-11-03 12:28): avance de G6: el servidor entrega UserId y Nickname en OperationLogInResult (7f23bda) y el proxy del cliente ya los trae (bcf1ca7). Falta el commit del cliente: ServerConnectionManager guarda la identidad (PlayerUserId, PlayerNickname) y LobbyScreen marca al jugador propio.
- Nota (2026-11-03 15:41): cierre. Verificado en eb765d6: el servidor entrega UserId y Nickname en OperationLogInResult (7f23bda); el proxy los trae (bcf1ca7); ServerConnectionManager los guarda solo con un inicio de sesión aceptado y los limpia en Disconnect (PlayerUserId, PlayerNickname; b241a87); MainMenuScreen muestra el nickname propio (b241a87) y LobbyScreen marca la tarjeta propia comparando UserId (eb765d6). El ranking sigue en BL-091.

### BL-076 · Los eventos del servidor se pierden si ninguna pantalla está suscrita
- Creado: 2026-10-29 16:36
- Por qué: ServerConnectionManager dispara FriendListReceived, RoomInvitationReceived y los demás eventos solo a quien esté suscrito en ese momento. La lista de amigos llega justo después del login y una invitación puede llegar con el menú principal abierto. Hay que decidir quién escucha siempre (por ejemplo MainWindow) y cómo se muestra una invitación fuera de FriendsScreen. Mitigación actual: RequestFriendList().
- Depende de: pieza de FriendsScreen
- Estado: Finalizado el 2026-10-29 · commits 1db83da, 33dc497 y e685982
- Nota (2026-10-29 22:53): cierre. ServerConnectionManager conserva la sala (CurrentRoom) y la lista de amigos (FriendList) aunque no haya pantalla suscrita, y MainWindow escucha siempre las invitaciones y el resultado de aceptar y entrar. Lo que queda del fallo de suscripción sigue en BL-077.

### BL-096 · Restaurar los comentarios del porqué retirados en 0358fe9
- Creado: 2026-11-02 16:06
- Por qué: 0358fe9 borró once comentarios que explicaban decisiones no evidentes: el orden de los campos estáticos entre partials (ServiceImplementation.*Manager), por qué los canales del servidor no se desuscriben, por qué el manejador de canal no relanza, la cita de las Reglas §1.1.1 junto a MaximumPlayersPerRoom (SessionRegistry.Lobbies.cs:11), "el último canal gana", "una invitación repetida no crea otra" y la equivalencia de pareja por CK_Friendship_not_self (FriendshipRepository). Los textos están en 53263f1.
- Depende de: nada (ajustar después de BL-093 los que hablen del catch)
- Estado: Finalizado el 2026-11-02 · commit 1dcd92d
- Nota (2026-11-02 20:17): se restauraron 12 líneas de comentario. No se restauran los dos comentarios sobre por qué el manejador de canal no relanza (el catch ya no existe) ni los dos FIXME retirados (uno duplicado y otro sobre métodos que ya no existen).
- Nota (2026-11-02 20:33): cierre. Los nueve comentarios del porqué están en el código (git grep sobre 635c66f): ServiceImplementation.FriendsManager, .LobbyManager y .LeaderboardManager, SessionRegistry.Friends y .Lobbies (con la cita de las Reglas §1.1.1), FriendshipRepository y PlayerRepository.

### BL-097 · Hallazgos de estilo del Estándar en el lobby y en 0358fe9
- Creado: 2026-11-02 16:06
- Por qué: orden de miembros (§9.5) en MainWindow.xaml.cs:103 y FriendsScreen.xaml.cs:111; más de 3 operadores (§10.5) en ServerConnectionManager.cs:365, 368 y 407; return de expresión (§13.2) en LobbyService.cs:206 y SessionRegistry.Lobbies.cs:357; var con métodos (§9.7) en FriendshipRepository.cs:30, 35 y 40; espacios finales (§9.3) en ServiceImplementation.cs:74 (nuevo), LobbyScreen.xaml.cs:18, 20 y 22, HowToPlayScreen.xaml.cs:109 y 111 y OptionsScreen.xaml.cs:12; constructor sin <summary> en MainMenuScreen.xaml.cs:15 (§12.2); LoadingScreen_Loaded en lugar de OnScreenLoaded (§7.2). Ninguno cambia el comportamiento.
- Depende de: nada
- Estado: Finalizado el 2026-11-02 · commits 535509d, 923f821, aa216fa, 53c6db6, 085948b, ca112d2, 391433c y 635c66f
- Nota (2026-11-02 17:23): en esta pasada se corrigen todos los puntos listados: orden de miembros y uso de la propiedad ServerConnection en MainWindow (§9.5), IsSameRoom en ServerConnectionManager (§10.5), return con variable local (§13.2), tipos explícitos en FriendshipRepository (§9.7), espacios finales (§9.3), <summary> del constructor de MainMenuScreen (§12.2) y OnScreenLoaded en LoadingScreen (§7.2). También se agregan los <summary> de ValidationHelper, OperationRegisterPlayerAccountResult y PlayerAccountRegistration (§12.2), se agrupan los usings de CreateAccountScreen (§9.4) y FriendsService usa una constante propia de usuario ausente en lugar de la del lobby.
- Nota (2026-11-02 20:33): cierre. Verificado sobre 635c66f: ServerConnection va después del constructor en MainWindow y FriendsScreen y MainWindow ya no usa ((App)Application.Current).ServerConnection; ninguna expresión booleana de ServerConnectionManager pasa de 3 operadores; no queda return x.AsReadOnly(); FriendshipRepository solo conserva el var de la consulta en sintaxis LINQ; los cuatro archivos ya no tienen espacios finales; el constructor de MainMenuScreen tiene <summary>; no queda LoadingScreen_Loaded; FriendsService ya no usa LobbyMembershipChange.

### BL-099 · Código sin uso y visibilidad de más en el lobby
- Creado: 2026-11-02 16:06
- Por qué: ISessionRegistry.IssueRoomEntryAuthorization (ISessionRegistry.cs:85, SessionRegistry.Lobbies.cs:108) no tiene usos; ServerConnectionManager.LeaveLobbyResultReceived (:64) no tiene suscriptores; OpenLobbyChannel (:205) es público y solo lo usa su clase (§10.9).
- Depende de: nada
- Estado: Finalizado el 2026-11-02 · commit 131f701
- Nota (2026-11-02 20:33): cierre. git grep sobre 635c66f: no quedan IssueRoomEntryAuthorization (solo su versión InLock, que usa la aceptación de invitaciones) ni el evento LeaveLobbyResultReceived, y OpenLobbyChannel es privado. NotifyLeaveLobbyResultReceived se conserva a propósito: registra en el log si el servidor rechaza la salida.

### BL-100 · MainMenuScreen muestra datos de prueba visibles
- Creado: 2026-11-02 16:06
- Por qué: MainMenuScreen.xaml:44, 62, 69 y 76 muestran "interStella", 142, "1,840" y "#38", y MainMenuScreen.xaml.cs:10-11 fija una racha de 3 y 120 puntos. El jugador ve datos que no son suyos.
- Depende de: BL-075
- Estado: Finalizado el 2026-11-03 · commit b241a87
- Nota (2026-11-03 10:31): el servidor ya entrega el nickname y el id del jugador en OperationLogInResult (UserId y Nickname, solo si el inicio de sesión fue aceptado; parte del servidor de G6). Con eso se puede mostrar el nickname; los puntos, el rango y la racha siguen ocultos hasta BL-103.
- Nota (2026-11-03 12:28): avance de G6: el servidor entrega UserId y Nickname en OperationLogInResult (7f23bda) y el proxy del cliente ya los trae (bcf1ca7). Falta el commit del cliente que muestra el nickname real; los puntos, el rango y la racha quedan ocultos hasta BL-103.
- Nota (2026-11-03 15:41): cierre. Verificado en eb765d6: el menú muestra el nickname real (lblPlayerNickname) y ya no enseña datos ajenos: el panel de victorias, puntos y puesto y las tarjetas de última victoria, racha y siguiente rango quedan ocultos (Collapsed; los textos de prueba siguen en el XAML oculto), y se quitaron las constantes de racha y siguiente rango. Mostrar esos valores reales queda en BL-103.

### BL-014 · Diseño de la pantalla del Gatito Fugitivo// LUIS ANGEL
- Creado: 2026-10-20 10:30
- Por qué: solo diseño por ahora, en otro chat; sin implementación.
- Depende de: nada
- Estado: Finalizado el 2026-11-06 · commit 898d5b5
- Nota (2026-11-06 23:25): se cerró el diseño y la interfaz de FugitiveKittenScreen. La funcionalidad (contrato, ViewModel, ruta, navegación) sigue en BL-041 y BL-116 a BL-122.

### BL-123 · TLMG.DataAccess no compilaba: vw_Leaderboard fuera del csproj
- Creado: 2026-11-10 10:50
- Por qué: TLMGModel.Context.cs declara DbSet<vw_Leaderboard> y la clase vw_Leaderboard.cs (generada con el modelo de la BD v4.0) no estaba incluida en el .csproj de estilo clásico; el servidor no compilaba.
- Depende de: nada
- Estado: Finalizado el 2026-11-10 · commit 4392af6

### BL-124 · App.config del cliente perdió los tiempos de D-46 y recuperó <identity> tras regenerar el proxy
- Creado: 2026-11-10 11:00
- Por qué: al regenerar la referencia de servicio (BL-109, BL-113) los 5 bindings quedaron con los valores por defecto de WCF y los 5 endpoints volvieron a llevar <identity> con una cuenta de Windows. Es la regresión que BL-057 y BL-113 ya preveían. Con el login síncrono, sin sendTimeout la interfaz puede quedar congelada hasta un minuto.
- Depende de: nada
- Estado: Finalizado el 2026-11-10 · commit 35e3c13
- Nota (2026-11-10 11:15): verificado a mano. Con el servidor apagado el error aparece en ~2 s; con el servidor encendido el primer login tarda ~4 s (dato para BL-051).

### BL-126 · Fase 3: higiene del csproj del cliente
- Creado: 2026-11-10 11:30
- Por qué: tras el merge 3787004 el csproj de estilo clásico conservaba las entradas obsoletas de FugitiveKitten, no registraba los archivos de Navigation ni StatusMessage y apuntaba a FrindsViewModel; ModerationModel.cs era copia exacta de ModerationMode.cs. Se quitaron, se registraron, se borró la copia y se renombró a FriendsViewModel.
- Depende de: nada
- Estado: Finalizado el 2026-11-10 · commit f1081a9

### BL-127 · Fase 3b: WaitCursorBehavior y PasswordBindingBehavior
- Creado: 2026-11-10 11:32
- Por qué: LoginScreen.xaml y CreateAccountScreen.xaml usan el espacio de nombres TLMG.Client.Behaviors, que nunca existió en el repositorio. Se crearon el cursor de espera ligado a IsBusy y la propiedad adjunta que enlaza la contraseña del PasswordBox.
- Depende de: nada
- Estado: Finalizado el 2026-11-10 · commit a597cef
- Nota (2026-11-10 12:10): 384ef1a hacía referencia a Behaviors, que nunca existió en el repositorio; la Fase 3b lo resolvió.

### BL-128 · Instalar CommunityToolkit.Mvvm 8.4.2 en el cliente (D-50)
- Creado: 2026-11-10 11:34
- Por qué: es el paquete que el Estándar §7.1 fija para ObservableObject y RelayCommand, sin sus generadores de código.
- Depende de: D-50
- Estado: Finalizado el 2026-11-10 · commit 6eb2fec

### BL-129 · Fase 4b: ViewModelBase sobre ObservableObject y comandos del toolkit
- Creado: 2026-11-10 11:36
- Por qué: Estándar §7.1 y D-50. ViewModelBase hereda de ObservableObject, se retiró el RelayCommand propio y los comandos se exponen como ICommand creados con el RelayCommand del toolkit; donde el CanExecute lee estado, el ViewModel llama a NotifyCanExecuteChanged.
- Depende de: BL-128
- Estado: Finalizado el 2026-11-10 · commit 8c4e9be

### BL-130 · Fase 5: cuentas dentro de ServerConnectionManager con IAccountsConnection
- Creado: 2026-11-10 11:38
- Por qué: LoginViewModel, CreateAccountViewModel y StatusMessage dependían de ILoginService, IAccountRegistrationService, ITimer, ITimerFactory e ILocalizationService, que nunca existieron en el repositorio. Ahora dependen de IAccountsConnection (D-70), que implementa ServerConnectionManager en el parcial Accounts (D-60); la ErrorKey viaja al cliente, que la traduce con LanguageManager.FindText, y los temporizadores son DispatcherTimer.
- Depende de: BL-129; D-60, D-70
- Estado: Finalizado el 2026-11-10 · commit cefa0a3
- Nota (2026-11-10 12:12): 384ef1a hacía referencia a Services, que nunca existió en el repositorio; la Fase 5 lo resolvió para el login, el registro y StatusMessage. ScreenFactory, IDialogService y los demás tipos de Services quedan para la Fase 6.

### BL-125 · Calentar Entity Framework al iniciar el servidor
- Creado: 2026-11-10 11:22
- Por qué: la primera consulta tras arrancar el servidor tarda ~5 s (BL-051) y el primer jugador en entrar lo paga. Hacer una consulta mínima de lectura al arrancar, en el host, antes de abrir los canales, mueve ese costo al arranque. Sin hilos ni temporizadores (Estándar §11.1).
- Depende de: BL-051
- Estado: Finalizado el 2026-11-11 · commit c46c098
- Nota (2026-11-11 08:16): calentamiento activo; el arranque del servidor tarda ~4.7 s y el primer inicio de sesión baja de ~5 s a ~3 s (medido, BL-051).

### BL-135 · UsersManager del servidor manda texto en español en SystemErrorMessage
- Creado: 2026-11-10 11:48
- Por qué: ServiceImplementation.UsersManager.cs arma el SystemFault de RegisterPlayerAccount y LogIn con «El servidor se ha quedado sin memoria.», mientras que Lobby y Friends mandan ErrorKeys.Common.OutOfMemory. Debe mandar una clave que el cliente traduzca (gov/CLAUDE.md §6). El cliente hoy solo registra ese texto en el log y muestra un mensaje genérico.
- Depende de: nada
- Estado: Finalizado el 2026-11-11 · commit 6b11ce4
- Nota (2026-11-20): R2B1, HEAD 08297935. SystemErrorMessage usa ErrorKeys.Common.OutOfMemory y el cliente tipado traduce Detail.SystemErrorMessage; los FaultReason de UsersManager aún contienen una frase fija en español. Comprobar si esa razón llega a mostrarse en otras rutas y decidir su tratamiento dentro de BL-093; no se probó interoperabilidad.

### BL-136 · Agregar Common_OutOfMemoryError a los diccionarios es-MX y en-US
- Creado: 2026-11-10 11:50
- Por qué: ErrorKeys.Common.OutOfMemory vale Common_OutOfMemoryError y esa clave no existe en Strings.es-MX.xaml ni en Strings.en-US.xaml. Lobby y Friends pueden mandarla; hoy el cliente mostraría el mensaje de Common_UnexpectedError. Las claves deben quedar con paridad exacta en ambos diccionarios.
- Depende de: nada
- Estado: Finalizado el 2026-11-11 · commit 6b11ce4
- Nota (2026-11-11 08:18): la fila del Excel queda en BL-110.

### BL-027 · Corregir los hallazgos de Estándar del código de registro integrado y retirar lo sin uso
- Creado: 2026-10-20 22:15
- Por qué: usings sin agrupar en `CreateAccountScreen` (§9.4); líneas de más de 120 columnas en `PlayerAccountRegistrationService` (§9.2) y espacios al final de línea en varios archivos; `OperationRegisterPlayerAccountResult` sin `<summary>` y con la llave en la misma línea (§12.2, §9.6); `FIXME` duplicado del hash de contraseña (ligar a BL-001); `<WCFMetadata Include="Connected Services\" />` sin uso en `TLMG.Server.csproj` y `TLMG.Contracts.csproj`; DTOs sin sufijo `Data` (D-41); proxy `TLMGService` y carpeta `Communication/` sin uso (solo se borran con autorización del encargado).
- Depende de: coordinación con Nicole
- Estado: Finalizado el 2026-11-11 · commit 71eef62
- Nota (2026-10-25): posiblemente obsoleta en el punto de los DTO sin sufijo `Data` por D-56; confirmar con el encargado.
- Nota (2026-10-29 22:53): estado en 53263f1. Hecho: el proxy TLMGService se retiró (a8cdce2, BL-035) y el FIXME del hash desapareció con BL-001. Obsoleto: los DTO sin sufijo Data (D-56, Estándar §17.1). Pendiente: usings sin agrupar en CreateAccountScreen.xaml.cs:1-5; 4 líneas de más de 120 columnas en PlayerAccountRegistrationService.cs (:47, 76, 82, 155); espacios al final de línea en 7 archivos .cs (PlayerAccountRegistrationService.cs 6, ServiceImplementation.cs 5, entre otros); OperationRegisterPlayerAccountResult.cs con la llave del namespace en la línea 3 y propiedades sin <summary>; <WCFMetadata Include="Connected Services\" /> en TLMG.Contracts.csproj:69 y TLMG.Server.csproj:81.
- Nota (2026-11-02 16:06): 0358fe9 eliminó las líneas de más de 120 columnas de PlayerAccountRegistrationService. Siguen los usings de CreateAccountScreen.xaml.cs:1-5, OperationRegisterPlayerAccountResult.cs y WCFMetadata.
- Nota (2026-11-02 20:33): avance. 085948b quitó los espacios finales de ServiceImplementation.cs, LobbyScreen, HowToPlayScreen y OptionsScreen; 993fb59 agrupó los usings de CreateAccountScreen; ca112d2 documentó las propiedades de OperationRegisterPlayerAccountResult y PlayerAccountRegistration. Quedan la llave del namespace en la misma línea (OperationRegisterPlayerAccountResult.cs:3) y <WCFMetadata Include="Connected Services\" /> en TLMG.Contracts.csproj:69 y TLMG.Server.csproj:81.
- Nota (2026-11-11 08:22): cierre. Lo que quedaba según la nota del 2026-11-02 20:33: la llave del namespace en OperationRegisterPlayerAccountResult.cs:3 y <WCFMetadata Include="Connected Services\" /> en TLMG.Contracts.csproj:69 y TLMG.Server.csproj:81.

### BL-132 · ProfileViewModel: línea de 124 columnas y miembros públicos sin <summary>
- Creado: 2026-11-10 11:42
- Por qué: una línea de ProfileViewModel.cs mide 124 columnas (Estándar §9.2, máximo 120) y varios miembros públicos, por ejemplo IsEditingAvatar, no tienen <summary> (§12).
- Depende de: nada
- Estado: Finalizado el 2026-11-11 · commit db35e91
- Nota (2026-11-11 08:20): verificado en 3f53ab8: git grep de líneas de 121 o más columnas en ProfileViewModel.cs no devuelve nada.

### BL-137 · Actualizar el resumen de ServerConnectionManager
- Creado: 2026-11-10 11:52
- Por qué: el <summary> de la clase dice que es la única pieza que conoce los canales de amigos y de lobby, pero también atiende el chat (ServerConnectionManager.Chat.cs) y las cuentas (ServerConnectionManager.Accounts.cs, Fase 5), que usa UsersManagerClient.
- Depende de: nada
- Estado: Finalizado el 2026-11-11 · commit 3f53ab8

### BL-138 · Fase 6.1 a 6.3 y 6.6: navegación, diálogos y composición de la aplicación
- Creado: 2026-11-11 08:30
- Por qué: los ViewModels de login y registro dependían de TLMG.Client.Services, que nunca existió en el repositorio (384ef1a). Se crearon IDialogManager y DialogManager (solo ShowError y Confirm, con claves de recurso); ScreenFactory quedó con Login, CreateAccount y un diccionario de pantallas legacy, y registra un aviso en el log si un destino no tiene pantalla; INavigationService, NavigationService y ShellViewModel pasaron a INavigationManager, NavigationManager y MainWindowViewModel (D-64); App.OnStartup compone la navegación y MainWindow muestra la pantalla con un ContentControl; LoginScreen y CreateAccountScreen reciben su ViewModel por constructor (B3). También se agregó StatusMessageTextStyle a GlobalStyles.xaml, que 384ef1a usó en LoginScreen sin definirlo.
- Depende de: BL-130
- Estado: Finalizado el 2026-11-11 · commits 5e3b857, b2f08ec y 6d344e7

### BL-139 · Fase 6.4 y 6.5: Verification en la fábrica y login sin historial
- Creado: 2026-11-11 08:32
- Por qué: CreateAccountViewModel navegaba a Verification y la fábrica no tenía caso, así que no pasaba nada tras registrarse; además LoginViewModel dejaba el login en el historial. La 6.4 agrega el caso Verification solo para VerificationFlow.AccountRegistration (con otro flujo registra un aviso y no cambia de pantalla) y ShowMessage a IDialogManager para el mensaje de cuenta creada; la 6.5 hace que el login y jugar como invitado vayan al menú con NavigateToWithoutHistory.
- Depende de: BL-138
- Estado: Finalizado el 2026-11-11 · commit db51858 y 57eafa3

### BL-140 · Fase 6.7: las pantallas legacy ya no desuscriben los eventos de su propio ViewModel en Unloaded
- Creado: 2026-11-11 08:34
- Por qué: MainMenuScreen, OptionsScreen, ProfileScreen, FriendsScreen, LeaderboardScreen y ForgotPasswordScreen quitaban en Unloaded los eventos de su ViewModel y no los volvían a poner en Loaded. Una instancia que GoBack repone del historial, o que LanguageManager.RefreshActiveWindows descarga y recarga al cambiar el idioma, quedaba con los botones muertos. El ViewModel nace y muere con su pantalla, así que esa desuscripción no protegía nada; se conservó la de ServerConnection, los temporizadores y el cierre del cliente del ranking.
- Depende de: BL-138
- Estado: Finalizado el 2026-11-11 · commit 82c1041

### BL-006 · Script de BD v3.2 con procedimientos del 2FA
- Creado: 2026-10-20 10:30
- Por qué: operaciones atómicas de códigos y registros pendientes (`CreatePendingRegistration`, `CreateVerificationCode`, `ValidateVerificationCode`) y resolver el trigger `INSTEAD OF` con EF6.
- Depende de: ADR-05 enmendado (hecho)
- Estado: Finalizado el 2026-11-11 · commit c16351b
- Nota (2026-11-11 12:32): cierre: los procedimientos CreatePendingRegistration, CreateVerificationCode y ValidateVerificationCode están en la v4.0 del script (database/TLMG_DB_v4.0.sql:382, 452 y 501), no en una v3.2. El trigger INSTEAD OF se eliminó (v4.0.sql:15), no se manejó.
- Nota (2026-11-20): R2B1, HEAD 08297935. El script v4.0 del commit c16351b contiene los tres procedimientos y elimina el trigger; la v4.1 conserva esos nombres. VerificationCodeRepository invoca dbo.SavePendingRegistration, que no figura en los scripts database, mientras que sí llama CreateVerificationCode y ValidateVerificationCode. Verificar contrato, implementación y despliegue real sin presumir el resultado: BL-007 y BL-153 conservan esta obligación. No se ejecutó SQL.

### BL-042 · Mecanismo de navegación y diálogos desde el ViewModel (§18.1)
- Creado: 2026-10-25 00:49
- Por qué: Estándar §18.1 deja sin decidir cómo solicita navegación un ViewModel sin referenciar la vista. Hay 27 usos de Application.Current.MainWindow en 11 pantallas y MessageBox en las vistas, que §4.1 prohíbe al ViewModel. La decisión puede tomarse fuera del equipo; mientras tanto no se actúa.
- Depende de: decisión (alternativas por presentar)
- Estado: Finalizado el 2026-11-11 · commits 5e3b857 y b2f08ec
- Nota (2026-11-06 13:30): decisión del encargado: servicio por interfaz, ViewModel lo recibe por constructor. Nombres INavigationManager e IDialogManager (§3.14 reserva Service para la lógica del servidor); los diálogos reciben ErrorKey, no texto. Cierra §18.1.
- Nota (2026-11-10 12:02): la Fase 5 (cefa0a3) no tocó INavigationService ni IDialogService: LoginViewModel y CreateAccountViewModel los reciben por constructor con sus nombres actuales. Su renombre a INavigationManager e IDialogManager, decidido el 2026-11-06, queda para la Fase 6.
- Nota (2026-11-11 08:04): la Fase 6 (5e3b857, b2f08ec, 6d344e7 y db51858) creó INavigationManager e IDialogManager para Login, CreateAccount y Verification. Las pantallas legacy siguen por el puente de MainWindow (BL-141).
- Nota (2026-11-11 12:32): cierre: IDialogManager y DialogManager nacieron en 5e3b857 e INavigationManager es el renombre de b2f08ec (nació como INavigationService en 384ef1a). Quedan 24 usos de Application.Current.MainWindow (git grep), que cubren BL-141, BL-142 y BL-143.

### BL-061 · Definir la regla de unicidad por pareja en Friendship
- Creado: 2026-10-29 13:05
- Por qué: `UQ_Friendship_pair` es dirigido (sender, receiver) y no hay restricción de orden entre ids: la base permite A→B y B→A como dos filas. Por ahora se acepta y todas las consultas del repositorio consideran ambos sentidos. Falta decidir si se impone una fila por pareja (CHECK de orden entre ids, índice único sobre la pareja ordenada o regla en el servicio) y el cambio de esquema que eso implique (Estándar §5.3).
- Depende de: decisión del encargado
- Estado: Finalizado el 2026-11-11 · commit c16351b

### BL-063 · Definir en el repositorio los permisos de la cuenta de mínimo privilegio
- Creado: 2026-10-29 13:05
- Por qué: `database/TLMG_DB_v3.1.sql` no tiene GRANT, DENY, CREATE USER ni CREATE LOGIN, y ningún documento del repositorio detalla qué permisos tiene la cuenta de la aplicación (D-53, Estándar §5.2). No se puede comprobar que pueda leer `Friendship` y `User` sin ejecutar la aplicación contra la base. Verificación local del 29-oct-2026: el servidor de desarrollo se conecta con Integrated Security=True (cuenta de Windows), por lo que no se pudo comprobar el mínimo privilegio con la cuenta de la aplicación.
- Depende de: BL-013
- Estado: Finalizado el 2026-11-11 · commit c16351b

### BL-109 · Clase base OperationResult para IsSuccess y ErrorKey
- Creado: 2026-11-06 12:15
- Por qué: cada operación de ServiceImplementation verifica nulos y arma Common_UnexpectedError por su cuenta, y cada …Result repite IsSuccess y ErrorKey. Decisión del encargado (2026-11-06): clase base OperationResult en TLMG.Contracts.DataContracts, de la que heredan los …Result (Estándar §17.1). Las firmas de operaciones y callbacks se declaran con el tipo derivado, nunca con el base. Exige regenerar el proxy una sola vez y repetir el paso fijo de BL-057 (apertura 10 s, envío 15 s, cierre 5 s; sin <identity>). El cliente reutiliza ErrorMessageHelper.
- Depende de: BL-017 (inventario previo de los …Result)
- Estado: Finalizado el 2026-11-11 · commits d056740 y f82c786
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; la entrada sigue finalizada, con una desviación por decidir. La entrada dice que las firmas se declaran con el tipo derivado y nunca con el base, pero 2ab3567 hizo que operaciones y callbacks devuelvan OperationResult directamente (por ejemplo IUsersManager.cs:18 y IRoomInvitationManagerCallback.cs:14) para no tener una clase vacía por operación (OperationResult.cs:5-7). Es una decisión de contrato: confirmar con el encargado si reemplaza la regla del 2026-11-06 y, de ser así, registrarla en el Estándar §17.1 y la Bitácora (D-66).

### BL-113 · Regenerar el proxy cuando cambie un contrato
- Creado: 2026-11-06 12:15
- Se hace dentro de BL-109, que es el cambio de contrato; incluye corregir el nombre desfasado OperationRequestMatchStartRequest.
- Depende de: BL-109
- Estado: Finalizado el 2026-11-11 · commits 7b8ae86 y f82c786
- Nota (2026-11-15 00:32): revisado sobre 8aa6844. El rango cambió contratos otra vez (2ab3567) y el proxy se regeneró en 62330d0 (Connected Services/UsersManagerReference, un solo proxy con todos los contratos). Se repitió el paso fijo de BL-057: los bindings siguen con 10/15/5 s y sin <identity>. Sin cambio de estado: la entrada ya está finalizada.

### BL-036 · Mover el callback y el cierre del canal a Communication/
- Creado: 2026-10-25 00:49
- Por qué: Estándar §4.1 y §11.1: la Vista no usa el proxy y el cierre del canal corresponde a Comunicación. Hoy CreateAccountScreen implementa el callback, crea el cliente y cierra el canal (CreateAccountScreen.xaml.cs:13-14, 109, 185-208). Incluye capturar TimeoutException (BL-020).
- Depende de: BL-020, BL-035; decisión de BL-042
- Estado: Finalizado el 2026-11-11 · commits cefa0a3, 384ef1a y b6dec93
- Nota (2026-10-25): los dos TODO provisionales que citan esta entrada: callback `LogInResult` vacío en CreateAccountScreen.xaml.cs:58 y callback `RegisterPlayerAccountResult` vacío en LoginScreen.xaml.cs:57. Se quitan al mover los callbacks a la capa Comunicación.
- Nota (2026-10-25): LeaderboardScreen ya implementa `ILeaderboardManagerCallback` con su propio canal (ea27455); es la tercera pantalla con el patrón. TODO vigentes que citan esta entrada (git grep): CreateAccountScreen.xaml.cs:58, LoginScreen.xaml.cs:57 y LeaderboardScreen.xaml.cs:73, los tres con el texto "Mover ... a la capa Comunicación".
- Nota (2026-10-29 22:53): Communication/ ya existe (928bcaf) y concentra los canales de lobby y amigos (1db83da, 33dc497, e685982). Login, CreateAccount y Leaderboard conservan su propio canal y callback. Los TODO siguen en CreateAccountScreen.xaml.cs:58, LoginScreen.xaml.cs:57 y LeaderboardScreen.xaml.cs:73. Las líneas del "Por qué" cambiaron: el callback está en CreateAccountScreen.xaml.cs:14-16, la creación del cliente en :118 y el cierre en :194-217. BL-035 ya está cerrada.
- Nota (2026-11-11 23:55): cierre: Login y CreateAccount pasaron por ServerConnectionManager.Accounts.cs (cefa0a3) y sus CloseRegistrationClient y CloseLogInClient se retiraron (384ef1a); LeaderboardScreen dejó su callback y su canal propios y ahora usa LeaderboardCallback y ServerConnectionManager.Leaderboard.cs (b6dec93). git grep de ManagerCallback, InstanceContext, ManagerClient y UsersManagerReference sobre Views y ViewModels no encuentra nada, y los TODO «Mover ... a la capa Comunicación» ya no existen. El cierre del canal captura TimeoutException (CloseClientSafely, ServerConnectionManager.cs:738). Las vistas de mvvm-resto no reintroducen callbacks.

### BL-062 · Implementar las solicitudes de amistad reales
- Creado: 2026-10-29 13:05
- Por qué: solicitar, aceptar, rechazar y cancelar amistades quedó fuera del paso 4; hoy solo se precargan con el script de demostración. `FriendshipRepository` solo consulta aceptadas. Incluye los estados FRIEND_PENDING y FRIEND_REJECTED, el contrato y la pantalla de amigos.
- Depende de: BL-061
- Estado: Finalizado el 2026-11-11 · commits 6696c4f, 27cb845 y 93fa788
- Nota (2026-11-11 23:55): cierre: contrato y script v4.1 en 6696c4f; servidor (IFriendshipRepository, FriendshipRepository y FriendsService con los estados FRIEND_PENDING y FRIEND_REJECTED) en 27cb845; pantalla de amigos en 93fa788. Cancelar una solicitud enviada no se implementó: pasa a BL-156. El detalle del servidor sigue en BL-152 hasta confirmar sus pruebas y la v4.1 (BL-153).

### BL-078 · Unificar el cierre de canales WCF en el cliente
- Creado: 2026-10-29 16:36
- Por qué: ServerConnectionManager.CloseClientSafely repite la lógica Faulted→Abort / Close con Abort de respaldo que ya tienen LoginScreen, CreateAccountScreen y LeaderboardScreen. Al migrar esas pantallas a la capa de comunicación (BL-036) debe quedar una sola implementación, en el ServerConnectionManager de instancia que vive en App.
- Depende de: BL-036
- Estado: Finalizado el 2026-11-11 · commits 384ef1a y b6dec93
- Nota (2026-11-11 23:55): cierre: CloseClientSafely (ServerConnectionManager.cs:718) es la única implementación del cierre: cuentas (ServerConnectionManager.Accounts.cs:124), chat (ServerConnectionManager.Chat.cs:113), amigos y lobby (ServerConnectionManager.cs:173-174) y la tabla de posiciones (ServerConnectionManager.Leaderboard.cs). Las copias de LoginScreen y CreateAccountScreen se retiraron en 384ef1a y la de LeaderboardScreen en b6dec93; git grep de Abort() fuera de Communication/ no encuentra nada.

### BL-154 · Regenerar el proxy y adaptar el cliente a amigos y modalidad
- Creado: 2026-11-11 19:30
- Por qué: una sola regeneración del proxy (svcutil) con los contratos del Paquete 1 y el paso fijo de BL-057 (apertura 10 s, envío 15 s, cierre 5 s y sin identity en App.config). Después: ServerCallback implementa los callbacks nuevos; ServerConnectionManager gana un parcial para las solicitudes; FriendsScreen y FriendsViewModel muestran añadir por nickname y pendientes con aceptar y rechazar (BL-085, parcial); LobbyScreen muestra el combo de cartas por jugador y lee CardsPerPlayerCount de la sala (BL-082, parcial). Las claves de idioma ya están en el Paquete 1.
- Depende de: Paquete 1
- Estado: Finalizado el 2026-11-11 · commits 69f0776, d3fde5b, 93fa788 y 0fc59c2
- Nota (2026-11-11 23:55): cierre: proxy regenerado en 69f0776 (App.config conserva apertura 10 s, envío 15 s y cierre 5 s, sin <identity>); ServerCallback implementa los callbacks nuevos en d3fde5b; ServerConnectionManager.FriendRequests.cs y FriendsScreen con añadir por nickname y pendientes en 93fa788; combo de cartas por jugador de LobbyScreen en 0fc59c2.

### BL-157 · Amistades: el cliente aún no usa las operaciones nuevas
- Creado: 2026-11-11 21:22
- Por qué: el servidor ya expone SendFriendRequest, AcceptFriendRequest, DeclineFriendRequest, OnFriendRequestReceived, OnFriendAdded y PendingFriendRequests, pero el proxy del cliente no se ha regenerado y FriendsScreen sigue con los controles de añadir y pendientes ocultos (BL-085). Hasta entonces las operaciones solo se prueban con las pruebas del servidor. Un cliente con proxy viejo no conoce los callbacks nuevos: si recibe OnFriendRequestReceived su canal puede fallar, así que los clientes se actualizan juntos.
- Depende de: BL-154
- Estado: Finalizado el 2026-11-11 · commits 69f0776, d3fde5b y 93fa788
- Nota (2026-11-11 23:55): cierre: el proxy se regeneró en 69f0776, ServerCallback implementa OnFriendRequestReceived, OnFriendAdded y los resultados en d3fde5b, y FriendsViewModel envía, acepta y rechaza solicitudes y muestra las pendientes en 93fa788.

### BL-008 · Crear la cuenta Gmail emisora y el archivo de secretos
- Creado: 2026-10-20 10:30
- Por qué: el envío SMTP necesita credenciales; irán en un archivo cifrado fuera de Git. La cuenta Gmail es solo el remitente: los destinatarios siguen la regla RC-E-03. Requiere verificación en dos pasos y contraseña de aplicación (no se pega en chats ni en el repo).
- Depende de: nada
- Estado: Finalizado el 2026-11-15 · commit 1b40829
- Nota (2026-11-15 00:32): cierre parcial por decisión del encargado. El encargado confirmó el 2026-11-15 que la cuenta Gmail emisora existe y que los códigos de verificación del registro ya llegan por correo (SMTP y plantillas en 1b40829; EmailUtility lee EmailConfig.xml en EmailUtility.cs:28). La parte del archivo de secretos fuera de Git (ignorado y cifrado según D-43) NO está hecha: EmailConfig.xml está versionado y en texto plano, y esa parte pasa a BL-171.

### BL-105 · Llevar la modalidad colaborativa al servidor
- Creado: 2026-11-03 23:24
- Por qué: DefaultCardsPerPlayerCount queda fija en 4 hasta que la modalidad llegue al servidor; entonces deberá reflejar la selección colaborativa de 4, 6 u 8 cartas.
- Depende de: cambio de modalidad colaborativa del lobby
- Estado: Finalizado el 2026-11-15 · commit 8b5b84f
- Nota (2026-11-11 23:55): avance. La modalidad ya llega al servidor: estado por sala en SessionRegistry.Readiness.cs:297 y :387 (8b5b84f) y el cliente lee CardsPerPlayerCount de la sala y la cambia desde el combo de LobbyScreen (0fc59c2). DefaultCardsPerPlayerCount (:12) queda solo como valor inicial de la sala. Se cierra junto con BL-151.
- Nota (2026-11-15 00:32): cierre. Evidencia: 8b5b84f está en origin/main (git merge-base --is-ancestor); el estado de la modalidad por sala vive en SessionRegistry.Readiness.cs; el cliente lee CardsPerPlayerCount de la sala y la cambia desde el combo de LobbyScreen desde 0fc59c2 (también en origin/main); el encargado reporta las 144 pruebas de TLMG.Services.Tests en verde (2026-11-15), entre ellas LobbyServiceCardsPerPlayerTests y SessionRegistryCardsPerPlayerTests, y la prueba con 3 clientes. DefaultCardsPerPlayerCount queda solo como valor inicial de la sala. Se cierra junto con BL-151.

### BL-151 · Servidor: operación ChangeCardsPerPlayer (modalidad del lobby)
- Creado: 2026-11-11 19:30
- Por qué: el Paquete 1 (contratos congelados de amigos y modalidad) congeló el contrato (ILobbyManager.ChangeCardsPerPlayer, OperationChangeCardsPerPlayerRequest y Result, CardsPerPlayerValidator) y dejó un cascarón en LobbyService.ChangeCardsPerPlayer (LobbyService.CardsPerPlayer.cs) que responde Common_UnexpectedError. Falta la lógica: estado por sala en SessionRegistry (hoy DefaultCardsPerPlayerCount fija en 4, SessionRegistry.Readiness.cs:10), valores 4, 6 y 8 (D-04), mismo valor = éxito sin cambios y sin reiniciar Listo, valor distinto = todos a No Listo (Regla No. 11) y aviso a la sala por OnLobbyRoomChanged, con el lock único (Estándar §11.1) y los avisos fuera del lock, sin cambiar las firmas que usan las pruebas. Incluye pruebas del registro y del servicio. Al terminar cierra BL-105.
- Depende de: Paquete 1; BL-154 para probarla desde el cliente
- Estado: Finalizado el 2026-11-15 · commit 8b5b84f
- Nota (2026-11-11 20:32): avance en la rama paquete2-modalidad, sin confirmar: ChangeCardsPerPlayer quedó implementado en SessionRegistry.ChangeCardsPerPlayer (SessionRegistry.Readiness.cs) y en LobbyService.ChangeCardsPerPlayer (LobbyService.CardsPerPlayer.cs), con pruebas en TLMG.Services.Tests. Falta compilar, correr las pruebas y probarla con el cliente (BL-154). Sigue pendiente hasta tener el commit; BL-105 se cierra con ella.
- Nota (2026-11-11 23:55): commit encontrado: 8b5b84f (SessionRegistry.ChangeCardsPerPlayer y LobbyService.ChangeCardsPerPlayer), con pruebas LobbyServiceCardsPerPlayerTests y SessionRegistryCardsPerPlayerTests. Sigue pendiente porque no consta que se hayan compilado y corrido las pruebas del servidor; el cliente ya la usa (0fc59c2).
- Nota (2026-11-15 00:32): cierre. Evidencia: 8b5b84f (SessionRegistry.ChangeCardsPerPlayer y LobbyService.ChangeCardsPerPlayer) está en origin/main (git merge-base --is-ancestor); el rango 78e1fa1..8aa6844 adaptó sus pruebas (c78a54f); el encargado reporta las 144 pruebas de TLMG.Services.Tests en verde (2026-11-15); el cliente usa la operación desde 0fc59c2; y el encargado reporta la prueba con 3 clientes. Cierra BL-105.
### BL-187 · Auditar semántica de null y resultados de contratos
- Creado: 2026-11-20 15:07
- Por qué: Los contratos y resultados requieren una revisión explícita de ausencia, error y valores nulos entre cliente y servidor.
- Depende de: Contratos WCF y decisiones de error.
- Responsable: Luis.
- Estado: Finalizado el 2026-11-22 · commit e7a385a
- Nota (2026-11-20): revisión inicial sobre 0829793; requiere confirmar alcance detallado durante la tarea.
- Nota (2026-11-21): implementación sobre acc33c1. Los 18 commits de 720586c a 338b3d0 (por su asunto) normalizan los resultados públicos por área: solicitud, verificación y cambio de contraseña (720586c, 8b61240, c23c8f3), perfil (ab3bff8, 42ac76f), registro y reenvío (e56f998, ade91b7), inicio y cierre de sesión (8d3f02a), recuperación y perfil (7632b67), registros pendientes (92dfc14), amistades y sala (53e6212), chat (505dd1f), partida (043e338), heartbeats de cliente y servidor (14a09c0) y clasificación (338b3d0). Incluyen sus pruebas en test/.
- Nota (2026-11-22): auditoría independiente de solo lectura sobre 338b3d0, sin compilar ni ejecutar pruebas (el informe no está en el repositorio). No halló defectos P0 ni P1. Halló una omisión (GetRecoveryContext devolvía ErrorKey nulo en éxito), documentación que aún decía «null en éxito» y ramas defensivas sin prueba. Verificó por script que las 72 ErrorKeys del servidor existen en los diccionarios es-MX y en-US, que tienen paridad exacta (489 claves cada uno).
- Nota (2026-11-22): decisión D-66 y su implementación. Contrato público de los …Result derivados de OperationResult: éxito con IsSuccess=true y ErrorKey=string.Empty; fallo con IsSuccess=false y ErrorKey no vacía y reconocida por los diccionarios. Un resultado nulo, un estado desconocido o una clave inválida no se interpretan como éxito. T07 no prohíbe null: se distinguen el resultado público, las validaciones locales del cliente (los métodos Get…ErrorKey de InputValidator y ValidationHelper devuelven null si el dato es válido), los validadores de TLMG.Domain (string.Empty si es válido), los datos opcionales (null) y los protocolos internos (eco de chat, MatchDelivery, delegados privados). 8c01c91 fija string.Empty en GetRecoveryContext, documenta el contrato en OperationResult y amplía SessionRegistryMatchExitTests.
- Nota (2026-11-22): validaciones. Declaración del desarrollador, no ejecución de Claude: suite completa en Visual Studio con 1379 de 1379 pruebas aprobadas y 0 fallidas, sobre el código previo a e7a385a e incluyendo las pruebas de invitaciones. Prueba manual integral con tres clientes WPF y un servidor WCF (JugadorTest1, JugadorTest2 y JugadorTest3): autenticación, perfil, amistades, invitaciones, lobby, chat, preparación, juego, resultados y clasificación. Además, un cliente se desconectó sin avisar durante una partida con el servidor activo, volvió a iniciar sesión dentro del período de gracia de 15 s y continuó la partida conservando su turno.
- Nota (2026-11-22): defecto hallado en la prueba integral y corregido en e7f2da6. Tras pulsar No, una nueva invitación del mismo jugador a la misma sala no aparecía, porque el rechazo no llega al servidor y la invitación pendiente se reutilizaba sin avisar. Ahora el servidor vuelve a notificar la misma invitación si pasaron al menos 5 s desde el último aviso (RoomInvitationRecord.LastNotifiedUtc, SessionRegistry.CreateOrFindInvitation) y agrega FriendsServiceRoomInvitationTests. No agrega una operación de rechazo al contrato WCF. El desarrollador confirmó a mano la primera invitación, el rechazo, la nueva invitación tras el intervalo, la aceptación y un solo aviso ante intentos repetidos dentro del intervalo.
- Nota (2026-11-22): estándar. La fuente oficial se actualizó en Google Docs y e7a385a sincroniza la copia exportada docs/Estandar.md (§10.2, contrato de los resultados, y §16.1, validación). e7a385a es un cambio solo documental; los funcionales son 720586c..338b3d0, 8c01c91 y e7f2da6.
- Nota (2026-11-22): cierre del alcance de BL-187, que era revisar la semántica de ausencia, error y null entre cliente y servidor. No afirma la ausencia de otros defectos de comunicación, persistencia o concurrencia. Quedan fuera y abiertas: (a) la creación de la cuenta y el borrado del registro pendiente no son atómicos (FinalizePlayer; BL-094 cubre la carrera de duplicados al guardar, no este borrado); (b) las ramas defensivas nuevas de ChatService, MatchService y FriendsService tienen poca o ninguna prueba y el cliente no tiene proyecto de pruebas (BL-164); (c) la normalización de resultados del cliente está repetida en varias clases de ServerConnectionManager y ProfileViewModel, y los literales de clave siguen duplicados (relacionado con BL-017); (d) tras reiniciar el servidor no se restaura la sesión ni la partida, porque ambas viven en memoria y no hay persistencia. Las deudas (a) a (c) y un rechazo explícito de invitación no tienen entrada propia; se proponen aparte.

## Descartados

### BL-085 · Reactivar los controles ocultos de FriendsScreen
- Creado: 2026-10-29 21:52
- Por qué: las Piezas 3 y 4 ocultaron (Collapsed) buscar, añadir amigo, las pestañas Pendientes y Bloqueados con sus datos de prueba (AstroKitty, SchroBinger, ToxicTabby, contadores 2 y 1) y el botón Bloquear de la plantilla; no tienen servidor detrás. El puntaje que mostraban las tarjetas de prueba se quitó porque Friend no lo trae.
- Depende de: BL-062 (solicitudes de amistad); contratos de bloqueo
- Estado: Descartada el 2026-11-20 · alcance residual absorbido por BL-156 y BL-165
- Nota (2026-11-11 23:55): avance (93fa788). FriendsScreen ya no tiene contadores fijos ni datos de prueba: añadir por nickname y pendientes reales (aceptar y rechazar) están activos, y se retiraron del XAML la pestaña Bloqueados y el botón Bloquear. Pendiente: bloqueados y bloquear/desbloquear (BL-156).
- Nota (2026-11-20): revisión sobre 08297935. Búsqueda por nickname y solicitudes pendientes con aceptar/rechazar están conectadas; el equipo confirmó esos flujos en BL-152. La pestaña y operaciones de bloqueados, bloqueo/desbloqueo y sus pantallas continúan en BL-156; reportes y moderación en BL-165. Se conserva la historia sin duplicar la obligación.
- Nota (2026-11-20): reconciliación R2A sobre 08297935. BL-156 mantiene cancelar, quitar amistad y bloquear/desbloquear con pantallas; BL-165 cubre moderación y reportes. Las solicitudes enviadas, aceptadas y rechazadas se registraron como probadas por el equipo.

### BL-032 · .gitignore, ConnectionStrings.config y archivo cifrado de secretos
- Creado: 2026-10-25 00:49
- Por qué: Estándar §8.3 y §5.2 declaran estas reglas como pendientes. Server/TLMG.Server/App.config lleva la cadena inline; falta ConnectionStrings.config (configSource) y ConnectionStrings.config.example. Reglas de ignorado para ambos y para el archivo DPAPI de SMTP (D-43).
- Depende de: BL-008
- Estado: Descartada el 2026-11-20 · alcance residual absorbido por BL-007/D-43
- Nota (2026-10-25): Cadena de conexión del servidor movida a ConnectionStrings.config y regla de .gitignore hechas en el commit a733631. Pendiente: nombre y patrón del archivo cifrado de SMTP (D-43, BL-008). No cerrada.
- Nota (2026-11-15 00:32): revisado sobre 8aa6844; sin cambio. El .gitignore solo cubre ConnectionStrings.config (.gitignore:432). EmailConfig.xml está versionado y sin .example: ver BL-171.
- Nota (2026-11-20): revisión sobre 08297935. El host usa configSource, .gitignore cubre ConnectionStrings.config y EmailConfig.xml y ambos .example están rastreados; los archivos reales no están rastreados. La externalización de conexión está en a733631; el riesgo SMTP de versionado se cerró en BL-171. El cifrado DPAPI y su decisión continúan en BL-007/D-43; no se comprobó cifrado en el entorno ni se inspeccionaron secretos. El trabajo residual no se duplica aquí.
- Nota (2026-11-20): reconciliación R2A sobre 08297935. La externalización y exclusión fueron verificadas en B1; BL-007 conserva expresamente DPAPI/D-43. No se comprobaron secretos ni cifrado real.

### BL-174 · Diálogos y mensajes de error duplicados en el cliente (inventario previo, por revisar)
- Creado: 2026-11-15 00:32
- Por qué: Inventario de solo lectura sobre 8aa6844: (a) tres rutas para traducir una ErrorKey: ErrorMessageHelper.FindErrorMessage (un solo llamador, MainWindow.xaml.cs:161), LanguageManager.FindText (Application.Current; lo usan 8 ViewModels y DialogManager) y la copia de la clave genérica en cada uno (UnexpectedErrorKey en ErrorMessageHelper.cs:10 y LanguageManager); (b) tres formas de mostrar un error: IDialogManager.ShowError (DialogManager.cs:15; lo usan LobbyViewModel, LeaderboardViewModel y App.xaml.cs:74), StatusMessage.ShowError dentro del formulario (FriendsViewModel.cs:455, ForgotPassword y NewPassword), más tres MessageBox directos en MainWindow.xaml.cs:81, 89 y 160 (BL-142); (c) 36 constantes *ErrorKey con el valor escrito en 13 archivos del cliente, algunas repetidas (Lobby_ChatEmptyError, Common_UnexpectedError); (d) VerificationViewModel usa CreateAccount_DataModelError como error inesperado (VerificationViewModel.cs:26). Relacionada con BL-017 y BL-134. Solo inventario: no se propone ni se hace ningún cambio de código.
- Depende de: BL-017; BL-134
- Estado: Descartada el 2026-11-20 · inventario absorbido por BL-017, BL-018, BL-134 y BL-142
- Nota (2026-11-20): revisión sobre 08297935. Las búsquedas muestran ErrorMessageHelper y LanguageManager (BL-134), literales repetidos (BL-017), CreateAccount_DataModelError (BL-018) y MessageBox directos en MainWindow (BL-142). StatusMessage en formularios e IDialogManager para diálogos tienen propósitos distintos y no demuestran por sí solos duplicación defectuosa. El inventario no añade obligación independiente; BL-187 cubre la revisión contractual de null. Se conservan sus notas históricas.
- Nota (2026-11-20): reconciliación R2A sobre 08297935. BL-017 conserva duplicación de claves, BL-018 texto técnico, BL-134 traducción y BL-142 decisiones de diálogos; diferencias entre formulario y diálogo no son por sí mismas un defecto.

### BL-090 · El ejemplo de LanguageManager del Estándar dice public y el código es internal
- Creado: 2026-10-29 22:53
- Por qué: docs/Estandar.md:1686 (§7.3) declara "public static class LanguageManager"; Client/TLMG.Client/LanguageManager.cs:12 es "internal static class". Corregir el ejemplo del Estándar (el código es correcto: el tipo no se usa fuera del ensamblado).
- Depende de: el encargado (el Estándar se edita en Docs y se reexporta, BL-050)
- Estado: Descartada el 2026-11-18 · corrección editorial del ejemplo del Estándar; el código ya usa visibilidad internal y no queda cambio de software asociado
- Nota (2026-11-18): El descarte se limita a la diferencia editorial del ejemplo; no declara cumplida ninguna obligación académica o documental.
- Nota (2026-11-20): reconciliación R2A sobre 08297935. El ejemplo public de LanguageManager persiste en Estándar §7.3 mientras el código es internal; el descarte no elimina esa corrección editorial, que se incorpora explícitamente a BL-101.

### BL-056 · Registrar en la Bitácora D-57 y la enmienda a D-40
- Creado: 2026-10-25 19:32
- Por qué: la Bitácora vive fuera del repositorio. Falta registrar D-57 (criterio de accesibilidad) y la enmienda a D-40: la referencia de servicio de Visual Studio usa el mismo generador que svcutil.exe. Queda abierta hasta que el encargado lo haga.
- Depende de: el encargado
- Estado: Descartada el 2026-11-06 · el registro de decisiones se hace en BL-114
- Nota (2026-11-20): reconciliación R2A sobre 08297935. La obligación de cotejar D-57 y la enmienda de D-40 se precisa en BL-114; la Bitácora original no está disponible, por lo que no se afirma que estén registradas.

### BL-059 · Registrar en la Bitácora las decisiones D-58 a D-61
- Creado: 2026-10-29 13:08
- Por qué: el Estándar (§3.14, §4.3, §5.3, §10.3, §11.1, §15.1, §16.2, §19.1, §20.2) y el SAD (ADR-08, C-02, C-06, §4.2, §4.3, §7.3) ya citan D-58 a D-61 y ADR-08, pero la Bitácora (fuera del repositorio, BL-056) aún no las contiene: D-58 (SessionRegistry con lock único, sessionToken, el último inicio de sesión gana), D-59 (scripts de demostración en database/seed/), D-60 (ServerConnectionManager) y D-61 (xUnit.net). Relacionada con BL-009 y BL-056.
- Depende de: decisión del encargado sobre la redacción
- Estado: Descartada el 2026-11-06 · el registro de decisiones se hace en BL-114
- Nota (2026-11-20): reconciliación R2A sobre 08297935. BL-114 conserva la revisión de D-58 a D-61 y ADR-08; falta cotejar la Bitácora original.

### BL-068 · Registrar en el Estándar y la Bitácora el estado de salas en SessionRegistry
- Creado: 2026-10-29 15:07
- Por qué: la autorización de lock de D-58 ahora cubre también las salas, las pertenencias, los callbacks y las autorizaciones de entrada; el límite de 6 jugadores por sala vive en `SessionRegistry` (Reglas §1.1.1). Falta reflejarlo en §11.1 y en la Bitácora (fuera del repositorio). Complementa BL-059.
- Depende de: decisión del encargado
- Estado: Descartada el 2026-11-06 · el registro de decisiones se hace en BL-114
- Nota (2026-11-20): reconciliación R2A sobre 08297935. BL-114 conserva registrar estado de salas y autorizaciones; BL-101 incluye cotejar su descripción en §11.1. No se afirma que Bitácora o Estándar ya estén completos.

### BL-074 · Registrar en la Bitácora el canal de amigos como presencia
- Creado: 2026-10-29 15:42
- Por qué: "en línea" significa tener un canal de amigos vivo, no tener sesión, y cerrar ese canal termina la sesión del jugador (ADR-08, D-58). Falta registrarlo en la Bitácora (fuera del repositorio) y reflejarlo en §6.2 del Estándar.
- Depende de: decisión del encargado
- Estado: Descartada el 2026-11-06 · el registro de decisiones se hace en BL-114
- Nota (2026-11-20): reconciliación R2A sobre 08297935. BL-114 conserva el cotejo de presencia del canal de amigos y su evolución; BL-101 conserva cotejar §6.2. La política actual de desconexión debe verificarse antes de copiar la afirmación histórica.

### BL-020 · Cierre del canal en `CreateAccountScreen.CloseRegistrationClient`: excepciones y bloqueo de la UI
- Creado: 2026-10-20 12:32
- Por qué: `Close()` bloquea el hilo de la interfaz hasta el `CloseTimeout` y el método no captura `TimeoutException` ni `CommunicationException` (Estándar §10.2: capturar el tipo más específico). Si el servidor cae con el canal abierto, el cierre puede lanzar una excepción sin controlar.
- Hallazgo (2026-10-20 16:20, análisis de solo lectura): el canal se abre de forma implícita en la primera llamada (`CreateAccountScreen.xaml.cs`, línea 111) y todo ocurre síncrono en el hilo de la interfaz. Ninguno de los dos `App.config` define tiempos límite, así que rigen los valores por defecto de WCF (1 min de apertura, envío y cierre). Con el servidor apagado la ventana se bloquea de unos segundos (conexión rechazada) hasta 1 minuto (puerto que no responde). El mensaje de error se oculta a los 4 s. No lo introdujo la fase 2. Observado en pruebas (2026-10-20 22:06): con el servidor encendido, el clic en "Crear Cuenta" deja la ventana aparentemente congelada unos 4 s antes de pasar a la verificación; con el servidor apagado, el error aparece en unos 5 a 6 s; el texto de error queda en el borde inferior de la ventana y es fácil no verlo.
- Depende de: decisión sobre timeouts del binding en `App.config` (mínimo y sin hilos); fase 2, cambio 3 (lo hace más visible)
- Estado: Descartada el 2026-11-11 · ya no aplica: CloseRegistrationClient y CloseLogInClient se retiraron en 384ef1a y ServerConnectionManager.Accounts.cs:111 captura TimeoutException; lo que queda lo cubren BL-095 (TimeoutException en Leaderboard), BL-078 (cierre unificado de canales) y BL-079 (bloqueo al abrir un canal) · commit 384ef1a
- Nota (2026-10-29 22:53): CloseRegistrationClient ya captura CommunicationException (CreateAccountScreen.xaml.cs:209-213) pero no TimeoutException. El mismo hueco está en LoginScreen.CloseLogInClient (:182) y LeaderboardScreen.CloseLeaderboardClient (:172); ServerConnectionManager.CloseClientSafely sí captura las dos. Con closeTimeout de 5 s (App.config:9) el bloqueo máximo es de 5 s. Se resuelve junto con BL-078.
- Nota (2026-11-11 12:32): estado anterior: En curso (mitigación parcial: timeouts del binding en commit 95279e7, integrado en 9e4365d; cierre del canal en Unloaded en 84e35b8, integrado en 8e260fc). Falta capturar `TimeoutException` y `CommunicationException` en `CloseRegistrationClient` y decidir sobre el bloqueo de la interfaz.
- Nota (2026-11-11 23:55): seguimiento (b6dec93). El hueco que señalaba la nota del 2026-10-29 en LeaderboardScreen.CloseLeaderboardClient quedó cubierto: la consulta y el cierre del canal pasaron a ServerConnectionManager.Leaderboard.cs, que captura TimeoutException al abrir (:37), y CloseClientSafely captura CommunicationException y TimeoutException al cerrar (ServerConnectionManager.cs:733 y :738). La parte pendiente que remitía a BL-095 y BL-078 queda resuelta.
- Nota (2026-11-20): reconciliación R2A sobre 08297935. CloseClientSafely captura CommunicationException y TimeoutException; BL-079 conserva expresamente la validación de bloqueo de interfaz tanto al abrir como al cerrar. No se midió duración en esta revisión.

### BL-024 · Verificar si hay tiempo límite de espera del resultado por callback en el registro
- Creado: 2026-10-20 16:50
- Por qué: la operación de registro es `IsOneWay` y el resultado llega por callback; el `sendTimeout` solo acota el envío, no la espera del resultado. Si el servidor se cuelga después de recibir la solicitud, el botón podría quedar deshabilitado sin límite. Verificar en `CreateAccountScreen`; si se confirma, valorar un `DispatcherTimer` en la interfaz (Estándar §11.4).
- Depende de: BL-020
- Estado: Descartada el 2026-11-11 · obsoleta tras d056740: IUsersManager es Request-Reply y el registro ya no usa callback; el bloqueo de la interfaz queda en BL-079
- Nota (2026-11-20): reconciliación R2A sobre 08297935. IUsersManager expone registro request-reply; el riesgo de espera síncrona en interfaz permanece en BL-079. No se ejecutó prueba de bloqueo.

