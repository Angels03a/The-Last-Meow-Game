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

### BL-001 · Decidir el algoritmo de hash de contraseña
- Creado: 2026-10-20 10:30
- Por qué: hoy la contraseña se guarda en claro; el SAD dice SHA-256 + salt y se propuso PBKDF2. Bloquea login y registro seguros.
- Depende de: decisión del equipo y del encargado
- Estado: Pendiente

### BL-002 · Renombrar el contrato de registro a `IUsersManager`
- Creado: 2026-10-20 10:30
- Por qué: `IAccountRegistrationService` no sigue D-41 ni §3.14. Incluye callback, archivo parcial, endpoint y regenerar el proxy.
- Depende de: acuerdo con Nicole sobre quién lo hace
- Estado: Pendiente

### BL-003 · Renombrar `AccountRegistrationManager`
- Creado: 2026-10-20 10:30
- Por qué: `Manager` no aplica a lógica de negocio (§3.14); falta decidir el sufijo (D-42).
- Depende de: BL-002
- Estado: Pendiente

### BL-004 · Mover `Server/Services` a `Server/TLMG.Services` y pasar a PackageReference
- Creado: 2026-10-20 10:30
- Por qué: no sigue la estructura `Server/TLMG.X/`, usa `packages.config` y el ensamblado se llama `Services`.
- Depende de: BL-002
- Estado: Pendiente

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
- Por qué: el envío SMTP necesita credenciales; irán en un archivo cifrado fuera de Git.
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
- Por qué: usings sin agrupar en `CreateAccountScreen` (§9.4); líneas de más de 120 columnas en `PlayerAccountRegistrationManager` (§9.2) y espacios al final de línea en varios archivos; `OperationRegisterPlayerAccountResult` sin `<summary>` y con la llave en la misma línea (§12.2, §9.6); `FIXME` duplicado del hash de contraseña (ligar a BL-001); `<WCFMetadata Include="Connected Services\" />` sin uso en `TLMG.Server.csproj` y `TLMG.Contracts.csproj`; DTOs sin sufijo `Data` (D-41); proxy `TLMGService` y carpeta `Communication/` sin uso (solo se borran con autorización de Martín).
- Depende de: coordinación con Nicole
- Estado: Pendiente

---

## Finalizados

### BL-025 · Reparar `origin/main` (marcadores de conflicto en 74e5f9a)
- Creado: 2026-10-20 22:15
- Por qué: `main` se subió con marcadores de conflicto en 4 archivos y no compila. `dev-LS_Angel` ya contiene el contenido válido de los commits de armasstella (reproducidos en 25aae1c y 53c0e8b; integrados en bb13661) y la fase 2 (8e260fc, 9e4365d). Plan: registrar `74e5f9a` en `dev-LS_Angel` con `merge -s ours` (el árbol no cambia) y avanzar `main` por fast-forward, sin forzar historia.
- Depende de: nada
- Estado: Finalizado el 2026-10-20 · commit 17a2738 (merge -s ours de main y avance de main por fast-forward, de 74e5f9a a 17a2738)

