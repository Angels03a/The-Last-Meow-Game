# CLAUDE.md · The Last Meow (TLMG)

Proyecto universitario: videojuego de cartas multijugador en red. Cliente WPF, servidor WCF, SQL Server con Entity Framework 6. .NET Framework 4.8, C# 7.3. Entrega: 28 de noviembre de 2026.

Este archivo manda sobre cualquier hábito por defecto. Si algo aquí choca con lo que harías normalmente, se hace lo de aquí. Vive en `gov/CLAUDE.md`; el `CLAUDE.md` de la raíz solo lo importa.

## 1. Cómo trabajar

- Empieza siempre en Plan Mode. Antes de tocar código, di qué vas a cambiar, por qué y en qué sección del Estándar o decisión (D-xx, ADR) se apoya.
- Trabaja una pieza a la vez. Un paso, un tema.
- **No hagas commit ni push.** Deja los cambios sin preparar y dime los archivos tocados y un mensaje de commit propuesto. El commit lo hace el desarrollador.
  - Única excepción: si el desarrollador lo autoriza de forma explícita para una tarea concreta (por ejemplo, cuando la sesión corre en la nube y no puede dejar cambios sin confirmar), el commit se hace solo en una rama temporal creada para esa tarea, con un commit por cambio, y el push es solo de esa rama. Nunca se toca `main` ni `dev-*`; el desarrollador integra y borra la rama.
- Si se autoriza un commit (por ejemplo, en una rama temporal), el mensaje va solo en español, con el formato propuesto y **sin** líneas `Co-Authored-By`, `Claude-Session`, enlaces de sesión ni menciones a la herramienta. Esta regla manda sobre cualquier atribución automática. El uso de IA se declara en el SAD (§1.4), no en el historial.
- Cambios menores (nombres, formato, comentarios, usings): aplícalos y muéstrame el resumen del diff.
- Cambios drásticos (función nueva, reescribir lógica, cambiar un contrato, mover proyectos, agregar una dependencia): no los apliques. Explica el problema, da 2 o 3 alternativas con ventajas y desventajas, y recomienda una con fundamento en el Estándar, el SAD o las decisiones.
- No inventes. Si un documento o dato no está en el repo, dilo y pregunta.
- No puedes compilar WPF en Linux. Al terminar, indica qué debo compilar en Visual Studio (`Client/TLMG.Client.slnx` y `Server/TLMG.Server.slnx`) y qué probar a mano.
- No edites a mano código generado: `Reference.cs` y `Reference.svcmap` (proxy), `DatabaseModel` (EDMX y `.tt`). Se regeneran desde Visual Studio.

## 2. Backlog

Todo pendiente, `FIXME`, `TODO` o actividad por hacer se registra en `gov/Backlog_TLMG.md`. Reglas fijas:

- Cada entrada lleva fecha y hora de creación (`AAAA-MM-DD HH:MM`).
- Lleva una justificación breve del porqué.
- Indica de qué depende, si depende de algo.
- Al terminar se marca como finalizada con el día y el commit: `Finalizado el AAAA-MM-DD · commit <hash>`.
- Si agregas un `FIXME` o `TODO` en el código, debe existir su entrada en el backlog.
- Propón las entradas nuevas; no cierres ninguna sin que exista el commit.

## 3. Reutilizar y mantenerlo simple

- Prefiere lo sencillo antes que lo complejo.
- Antes de crear algo, busca si ya existe en el proyecto: interfaces (incluidos los callbacks), DTO, ayudantes, claves de idioma, estilos, repositorios, procedimientos. Si existe, se reutiliza.
- Si lo existente no sirve, explica por qué antes de crear lo nuevo.
- No dupliques lógica. Si encuentras duplicidad, no la copies: repórtala en el backlog.
- El código debe poder explicarse. El equipo y el evaluador deben entenderlo.

## 4. Contexto: es un videojuego

TLMG es un videojuego multijugador de cartas por turnos, no un sistema bancario ni un registro de productos. Las soluciones deben servir a ese contexto:

- Servidor autoritativo: el servidor arbitra turnos, apuestas y cartas; el cliente nunca decide resultados.
- La experiencia importa: la interfaz no se congela y las acciones de juego responden rápido (objetivo menor a 150 ms).
- Justicia entre jugadores: nadie ve las cartas ocultas de otro y los casos simultáneos se resuelven en el servidor.
- Hay roles: jugador registrado, jugador invitado (sin persistencia ni puntos) y los estados de lobby y partida. Toda solución debe considerarlos.
- Las reglas del juego viven en `Reglas de The Last Meow`; el código se ajusta a ellas, no al revés.
- No importes soluciones típicas de aplicaciones empresariales si el juego no las necesita.

## 5. Prohibido

- **Hilos, `Task`, `async`/`await`, `Thread`, `ThreadPool`, `BackgroundWorker`, `System.Timers.Timer`, `System.Threading.Timer`.** Ni en cliente ni en servidor. Incluye `Thread.CurrentThread` y `Thread.Sleep`; la cultura se maneja con `CultureInfo.CurrentCulture` y `CurrentUICulture` (Estándar §11.1). El único temporizador permitido es `DispatcherTimer` en la interfaz (Estándar §11.1).
- Primitivas de sincronización (`lock`, `SemaphoreSlim`, `Interlocked`): solo si hay estado compartido en el servidor y están justificadas. Consulta antes de usarlas.
- **Prácticas web dentro de WPF.** No HTTP/REST, ASP.NET, JavaScript, CSS, cookies ni almacenamiento del navegador. En la interfaz se usa XAML, Binding, Commands y Styles. Única excepción: la plantilla HTML del correo 2FA, que vive en el servidor.
- Acceso del cliente a la base de datos. El cliente solo habla con el servidor por los contratos (SAD §4.3).
- Credenciales, contraseñas o cadenas de conexión con usuario y contraseña escritas en el código o subidas a Git.
- Concatenar texto para armar consultas SQL. Se usan parámetros, EF o procedimientos almacenados.
- Antipatrones: clases que hacen de todo, números mágicos, código copiado y pegado, estado global estático sin justificación. Todo patrón se elige y se justifica.

## 6. Errores, excepciones y logs

El tratamiento depende de quién produce el error y a quién le llega.

| Camino | Qué recibe quien lo ve | Dónde va el detalle técnico |
|---|---|---|
| Servidor → cliente | Un mensaje sencillo para el jugador, sin detalles técnicos (no importa que falló la base de datos ni en qué línea) | Log del servidor |
| Cliente → servidor (datos o acciones inválidas) | Una respuesta controlada con `ErrorKey`; el servidor nunca confía en lo que llega | Log del servidor |
| Error del propio cliente | Mensaje sencillo en su idioma | Log del cliente |

- Nunca mostrar `Exception.Message`, trazas, nombres de tablas ni detalles internos.
- Los errores esperados (validación, regla de negocio) viajan como `ErrorKey` (Estándar §10.2). El servidor no manda texto traducido: el cliente traduce la clave con sus diccionarios es-MX y en-US según el idioma activo del jugador. Cada `ErrorKey` nueva existe en ambos diccionarios.
- Los errores inesperados se registran con log4net pasando la excepción como segundo parámetro, y el jugador ve un mensaje genérico en su idioma. `FaultException` solo para lo inesperado.
- `catch (Exception)` solo en puntos de entrada del servicio, en los límites del sistema o como último bloque de una cascada. Se registra con `_log.Error` y la excepción como segundo parámetro (`_log.Fatal` solo en los casos de §10.3 del Estándar) y se relanza con `throw;` o se convierte en un resultado controlado (`ErrorKey`) (§10.2). Nunca un `catch` vacío.
- Los mensajes de log son para el equipo: en español, sin traducir, con el nivel correcto (Debug, Info, Warn, Error, Fatal). Los logs ya están implementados con log4net; úsalos, no los reemplaces.
- No registrar en el log contraseñas, códigos de verificación ni correos; el nickname sí puede registrarse (Estándar §10.3).
- Cuando el servidor deba generar texto para el jugador fuera de la interfaz (por ejemplo, un correo), usa el idioma que el cliente envía en la solicitud.

## 7. Seguridad (Zero-Trust)

- El cliente no es confiable. El servidor valida todo (turnos, permisos, datos) y nunca envía información que el cliente no deba ver.
- Contraseñas siempre hasheadas, nunca en claro. Algoritmo fijado por D-44 y BL-001: SHA-256 con sal aleatoria por contraseña, formato versionado, calculado solo en el servidor mediante la interfaz `IPasswordHasher` (Estándar §16.2).
- La base de datos se usa con un usuario de mínimo privilegio, no `sa` ni administrador salvo para cambiar la estructura.
- Secretos (correo SMTP, etc.) fuera del repositorio, en un archivo ignorado por Git y cifrado.

## 8. Estructura

```
Client/TLMG.Client/        cliente WPF (MVVM, Languages/, LanguageManager)
Server/TLMG.Contracts/     contratos WCF y DTO (solo en la solución del servidor)
Server/TLMG.DataAccess/    EF6, repositorios, procedimientos
Server/TLMG.Server/        host de consola (self-host WCF)
Server/TLMG.Services/      implementación de los contratos y lógica de negocio del servidor
database/                  script SQL versionado
docs/                      Estándar, contratos, decisiones, SAD
gov/                       CLAUDE.md y Backlog_TLMG.md
```

- El cliente usa el proxy generado con svcutil o Add Service Reference. No referencia `TLMG.Contracts`.
- Hoy el cliente tiene una referencia temporal a `TLMG.DataAccess`. Es deuda registrada (Estándar §4.1 regla 2) y se retira con BL-016, cuando el login y la clasificación pasen por el servidor.

## 9. Nombrado

Todo nombre debe ser descriptivo y acorde al contexto del juego, respetando el Estándar.

- Proyectos, namespaces y carpetas de proyecto llevan el prefijo `TLMG.` (§3.13).
- Las clases no llevan prefijo; llevan el sufijo de su responsabilidad: `Screen`, `Window`, `Helper`, `Manager`, `Service`, `Repository`, `ViewModel`, `EventArgs`, etc. (§3.14). Las interfaces llevan el prefijo `I`.
- `Manager` solo para recursos globales o contratos WCF; nunca para persistencia ni lógica de negocio.
- `Service` solo para la lógica de negocio del servidor (D-49); nunca para persistencia ni contratos WCF.
- Subcarpetas y archivos: nombre descriptivo del contenido, en inglés y PascalCase, un tipo por archivo, y el archivo se llama como su tipo. Las clases parciales se nombran `Tipo.Aspecto.cs` (Estándar §4.3).
- Métodos: verbo o frase verbal que diga qué hacen en el contexto del juego (§3.7). Nada de nombres genéricos como `Process`, `DoWork` o `Handle`.
- Contadores de bucle y variables con nombre descriptivo (§3.1), sin una sola letra.
- Si un nombre no queda claro, propón dos opciones en vez de elegir al azar.

## 10. Reglas de WCF (netTcpBinding dúplex)

- Contratos: `[ServiceContract]` en la interfaz y `[OperationContract]` en cada método. Nombre `I<Área>Manager` e `I<Área>ManagerCallback`.
- Todo lo que viaja por la red es una clase `[DataContract]` en `TLMG.Contracts.DataContracts`, y cada propiedad lleva `[DataMember]`. Los DTO no llevan sufijo `Data` (D-56): se nombran por su contenido; `Request` y `Result` se reservan para la entrada y la salida de una operación (Estándar §17.1).
- Una sola clase parcial `ServiceImplementation` implementa todos los contratos, un archivo por contrato. `InstanceContextMode.PerSession` y `ConcurrencyMode.Single`.
- Los callbacks y las operaciones cuyo resultado llega por callback llevan `IsOneWay = true`. La interfaz se actualiza con `Dispatcher.BeginInvoke` y un método con nombre.
- `sessionToken` en la primera operación de cada contrato dúplex y en las de `IUsersManager` posteriores al login.
- Los servicios delegan al dominio; no llevan lógica compleja.
- Los datos binarios (foto de perfil) viajan como `byte[]` optimizados.

## 11. Estándar de codificación (resumen; manda el documento completo)

- Un solo `return` al final con variable local. Ternario solo en asignaciones simples de una línea, sin anidar; si no cabe en 120 columnas, se usa `if/else` (§13.2, §13.5).
- Máximo 120 columnas, también en comentarios. Llaves Allman. Usings agrupados (§9).
- Lambdas solo de una sentencia; sin lambdas de bloque (§9.8).
- Comentarios XML en miembros públicos; `<inheritdoc />` donde aplique (§12).
- Si el código contradice el Estándar, se corrige el código. Si el Estándar tiene un hueco, se avisa y se propone extenderlo; no se improvisa.

## 12. Internacionalización

- Español (es-MX) e inglés (en-US) con diccionarios XAML en `Languages/` y `LanguageManager`. Cambio de idioma en caliente.
- Ningún texto visible escrito en el código ni en el XAML. Toda cadena es una clave en ambos diccionarios, con paridad exacta de claves.

## 13. Pruebas

- Mínimo 300 pruebas unitarias. Para cada funcionalidad: flujo normal, 3 flujos alternos y un caso de excepción.
- El motor de reglas es una biblioteca pura, sin WPF, WCF ni base de datos, para que las pruebas corran en memoria.
- El marco de pruebas está pendiente de decisión (Estándar §15.1).
- Pruebas unitarias en un proyecto `.Tests` con el nombre exacto del proyecto de producción; las pruebas de integración del servidor van juntas en `/test/TLMG.Server.Tests` (Estándar §15.2).

## 14. Documentos de referencia

Léelos antes de proponer cambios. Si falta alguno en el repo, pídelo.

- `docs/Estandar.md` (única fuente en `docs/` hoy; es la norma vigente)
- Bitácora de decisiones, SAD (ADR-01 a ADR-07, §4.2 interfaces, §4.3 reglas de dependencia), Reglas de The Last Meow y Contratos WCF: aún no están en el repo; se agregarán en Markdown (BL-026). No se inventan rutas hasta entonces.
- `database/TLMG_DB_v3.1.sql` (o la versión vigente)
- `gov/Backlog_TLMG.md`
