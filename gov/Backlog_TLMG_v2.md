# Backlog operativo V2 · revisión preliminar

**Inventario operativo reconciliado con V1.** Las tareas postergadas y descartadas se conservan en el [histórico](Backlog_TLMG.md). Base local: `dev-LS_Angel` / `08297935b6601871278c3f889f324dd57ec34158`, 2026-11-20. El commit de referencia `e01a0bb` no está disponible localmente.

**Alcance de la auditoría:** las 84 entradas del inventario inicial tienen disposición y nota de revisión; ninguna queda en la lista de auditoría pendiente. BL-043 conserva dos ubicaciones originales no verificables por falta del diagnóstico de origen y continúa activo. De los 69 cierres históricos del inventario R2B se revisaron 14 en R2B1; los 55 restantes no fueron verificados por alcance en esa revisión. Esto no convierte los requisitos pendientes en finalizados ni impide organizar el backlog.

## P0 — Seguridad y funcionalidad esencial

### BL-093 · Excepciones en límites WCF
- Prioridad: P0.
- Responsable: Por asignar; Luis coordina decisión documental.
- Estado: Parcial.
- Implementación existente: SafeNotify captura fallos conocidos en callbacks; algunos repositorios controlan errores de datos.
- Pendiente concreto: Definir y probar registro y respuesta para excepciones inesperadas en operaciones y cierres de canal, sin capturas indiscriminadas; verificar si el FaultReason fijo en español de UsersManager se expone al usuario (BL-135).
- Evidencia: ServiceImplementation.FriendsManager.cs; ServiceImplementation.cs.
- Depende de: Decisión del encargado y alineación del Estándar.
- Última revisión: 2026-11-20.

### BL-179 · Privacidad y configuración SMTP
- Prioridad: P0.
- Responsable: Por asignar.
- Estado: Pendiente.
- Implementación existente: EmailUtility registra excepciones SMTP y despachadores registran resultados.
- Pendiente concreto: Eliminar datos personales del mensaje y detalle de excepción, preservar diagnóstico útil y validar configuración incompleta.
- Evidencia: Server/TLMG.Services/Utilities/EmailUtility.cs:99.
- Relacionado: BL-147 trata separación de auditoría; el retiro de SMTP versionado se cerró en BL-171.
- Depende de: Corrección y pruebas de privacidad SMTP.
- Última revisión: 2026-11-20.

### BL-066 · Plazos de autorizaciones
- Prioridad: P0.
- Responsable: Por asignar.
- Estado: Parcial.
- Implementación existente: El barrido evalúa silencio de enlaces y sesiones sin participación activa.
- Pendiente concreto: Definir y evaluar vencimiento UTC de autorizaciones sin temporizador; sesiones huérfanas se atienden en BL-058.
- Evidencia: SessionRegistry.Heartbeat.cs y SessionRegistry.Lobbies.cs.
- Relacionado: BL-072 especifica los casos de autorización no usada.
- Depende de: BL-058 para evaluación de sesiones.
- Última revisión: 2026-11-20.

### BL-072 · Autorización de entrada no usada
- Prioridad: P0.
- Responsable: Por asignar.
- Estado: Pendiente.
- Implementación existente: Se consume al entrar y se limpia al desaparecer sala o sesión.
- Pendiente concreto: Evitar uso posterior de autorización antigua, incluso tras sala llena; verificar plazo y limpieza.
- Evidencia: SessionRegistry.Lobbies.cs:219.
- Depende de: BL-066.
- Última revisión: 2026-11-20.

### BL-044 · Validaciones de cuenta en servidor
- Prioridad: P0, por validación de entradas no confiables.
- Responsable: Por asignar; Luis coordina la interpretación documental.
- Estado: Parcial.
- Implementación existente: validadores de Domain comprueban formato y longitudes; SQL v4.1 coincide en máximos.
- Pendiente concreto: consultar las reglas originales y cubrir en servidor dominios y nombres restringidos si son obligatorios; vocabulario ofensivo se coordina con BL-107.
- Evidencia: `Server/TLMG.Domain/Validators/AccountFieldValidator.cs`; `database/TLMG_DB_v4.1.sql:80`.
- Relacionado: BL-107 trata el vocabulario ofensivo.
- Depende de: Diccionario de reglas original y verificación de base instalada.
- Última revisión: 2026-11-20.

### BL-053 · Segundo factor por inactividad
- Prioridad: P0, por requisito de autenticación.
- Responsable: Por asignar.
- Estado: Pendiente.
- Implementación existente: registro tiene verificación; login guarda last_time en UTC.
- Pendiente concreto: implementar y validar 2FA de login tras 30 días, conservando la última fecha anterior para decidir.
- Evidencia: `Server/TLMG.Services/Accounts/PlayerLogInService.cs`; CU-03 FA-02 citado en V1.
- Depende de: BL-007; política de autenticación.
- Última revisión: 2026-11-20.

### BL-013 · Cuenta SQL de mínimo privilegio
- Prioridad: P0, por acceso de base de datos.
- Responsable: Por asignar.
- Estado: Requiere validación.
- Implementación existente: SQL v4.1 define rol, GRANT y DENY; asignación del usuario está comentada.
- Pendiente concreto: verificar sin cambios ni secretos la identidad usada por el servidor, existencia y membresía del usuario y permisos efectivos en cada base requerida.
- Evidencia: `database/TLMG_DB_v4.1.sql:1228`; no se consultó la instalación.
- Depende de: acceso autorizado al entorno; BL-153.
- Última revisión: 2026-11-20.

### BL-023 · Seguridad y límites del host WCF
- Prioridad: P0, por seguridad de transporte y exposición de recursos.
- Responsable: Por asignar.
- Estado: Requiere validación.
- Implementación existente: includeExceptionDetailInFaults=false; se reportó partida real entre dos PC.
- Pendiente concreto: verificar transporte efectivo, firewall, límites predeterminados y resistencia según despliegue; decidir si requieren cambios sustentados.
- Evidencia: `Server/TLMG.Server/App.config` local modificado declara security mode=None, línea ausente en HEAD; no se verificó protección externa.
- Relacionado: BL-034 trata los tiempos límite del host.
- Depende de: BL-021; revisión de entorno.
- Última revisión: 2026-11-20.

### BL-007 · Verificación, recuperación y secretos
- Prioridad: P0.
- Responsable: Por asignar; Luis coordina decisiones contractuales.
- Estado: Parcial.
- Implementación existente: Existe registro con código y servicios de recuperación de contraseña.
- Pendiente concreto: Validar flujo completo, purga, política de activación y cifrado DPAPI; comprobar con BL-153 la discrepancia entre dbo.SavePendingRegistration invocado y dbo.CreatePendingRegistration versionado. Las decisiones documentales corresponden a Luis.
- Evidencia: PlayerAccountRegistrationService, PasswordRecoveryService; D-42/D-43.
- Relacionado: BL-053, BL-114 y BL-177 son obligaciones relacionadas, no prerrequisitos mutuos.
- Depende de: Decisiones y fuentes de verificación correspondientes.
- Última revisión: 2026-11-20.

### BL-177 · Registro sin código
- Prioridad: P0.
- Responsable: Por asignar; Luis coordina decisiones contractuales.
- Estado: Pendiente.
- Implementación existente: El contrato publica RegisterPlayerAccount y el servicio llama a CreateAccount.
- Pendiente concreto: Impedir la creación de cuentas sin verificar el código y validar la operación expuesta.
- Evidencia: IUsersManager.cs; PlayerAccountRegistrationService.cs.
- Relacionado: BL-007 coordina el flujo de verificación.
- Depende de: Decisión de contrato y regla de verificación.
- Última revisión: 2026-11-20.

### BL-111 · Sesión de invitados
- Prioridad: P0.
- Responsable: Compañera.
- Estado: Pendiente.
- Implementación existente: Existen flujos de cliente para invitado; revisar alcance de servidor.
- Pendiente concreto: Permitir participación con sesión real y restricciones de invitado.
- Evidencia: Histórico BL-111; reglas V3.
- Depende de: Autorización y contratos de sala y partida.
- Última revisión: 2026-11-20.

### BL-058 · Sesiones abandonadas
- Prioridad: P0, por la validez de tokens y la limpieza de sesiones.
- Responsable: Por asignar; Luis coordina decisiones contractuales.
- Estado: Requiere validación.
- Por qué sigue vigente: el barrido de silencio debe comprobarse también para sesiones huérfanas y limpieza interna.
- Evidencia actual: `Server/TLMG.Services/Sessions/SessionRegistry.Heartbeat.cs`; el desarrollador comunica pruebas funcionales de conexión, reconexión y abandono.
- Falta para cerrar: corregir o verificar el vencimiento de sesiones sin canal: TryFindSessionByToken actualiza LastActivityUtc al consultar identidad; comprobar limpieza y respuesta de logout perdida.
- Depende de: ADR-04 y D-54; latidos.
- Última revisión: 2026-11-20.

## P1 — Funcionalidad y validación

### BL-166 · Resultados y pantallas de demostración
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Implementación existente: GameOver recibe ganador y clasificación de MatchView; panel demo se oculta con MatchId.
- Pendiente concreto: Comprobar accesibilidad de comandos demo y sustituir datos o causas simulados de RoundResult y MatchClosed en flujos que deban ser reales.
- Evidencia: GameplayViewModel; RoundResultViewModel; MatchClosedViewModel.
- Depende de: BL-046, BL-116 y BL-117.
- Última revisión: 2026-11-20.

### BL-180 · Avatares en lugares vacíos
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Requiere validación.
- Implementación existente: El estilo colapsa lugares con contenido null; afc804b está integrado.
- Pendiente concreto: En partida de tres confirmar dos rivales visibles y otros lugares vacíos.
- Evidencia: GameplayScreen.xaml:18; MatchBoardHelper.
- Relacionado: BL-181 puede validarse en la misma partida.
- Depende de: Clientes reales para validación visual.
- Última revisión: 2026-11-20.

### BL-181 · Estabilidad de mesa y barra
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Requiere validación.
- Implementación existente: afc804b reserva espacio de pilas y existen límites visuales.
- Pendiente concreto: En la partida de BL-180 variar pilas y comprobar estabilidad sin ocultar cartas.
- Evidencia: GameplayScreen.xaml; GlobalStyles.xaml.
- Relacionado: BL-180 puede validarse en la misma partida.
- Depende de: Clientes reales para validación visual.
- Última revisión: 2026-11-20.

### BL-015 · Puntos del ranking general
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Requiere validación.
- Implementación existente: Repositorio, servicio y cliente transportan TotalPoints hasta ScoreLabel.
- Pendiente concreto: Comprobar visualmente con base que tenga puntuaciones reales; posición personal corresponde a BL-091.
- Evidencia: PlayerRepository; LeaderboardService; LeaderboardScreen.xaml.
- Depende de: BL-091 y BL-185.
- Última revisión: 2026-11-20.

### BL-045 · Avatares y rutas de perfil
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Implementación existente: Perfil y ranking calculan avatar desde catálogo con fallback.
- Pendiente concreto: Corregir o validar enlace AvatarImagePath ausente del modelo y compatibilidad de rutas heredadas; decidir estándar de recursos sin migración amplia.
- Evidencia: LeaderboardScreen.xaml:32; PlayerRankEntry.cs; SQL v4.1.
- Depende de: BL-103 y decisión del equipo.
- Última revisión: 2026-11-20.

### BL-082 · Controles reales de Lobby
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Implementación existente: Chat, modalidad, cuenta regresiva y lista de jugadores con estado están conectados.
- Pendiente concreto: Definir y validar presentación de perfil y panel de listos sin activar simulaciones; votación está en BL-161.
- Evidencia: LobbyScreen.xaml; LobbyViewModel.cs.
- Depende de: BL-103, BL-161 y BL-165.
- Última revisión: 2026-11-20.

### BL-107 · Vocabulario ofensivo
- Prioridad: P1.
- Responsable: Compañera para funcionalidad; Luis coordina reglas documentales.
- Estado: Pendiente.
- Implementación existente: Chat y nickname se validan por caracteres y longitud en servidor.
- Pendiente concreto: Consultar reglas y lista aprobada de RC-M-01/RC-N-01 y aplicar validación de servidor.
- Evidencia: ChatValidator; AccountFieldValidator; histórico BL-107.
- Relacionado: BL-044 conserva otras reglas de validación.
- Depende de: Diccionario original y definición de reglas.
- Última revisión: 2026-11-20.

### BL-155 · Abuso de solicitudes de amistad
- Prioridad: P1.
- Responsable: Por asignar.
- Estado: Requiere decisión.
- Implementación existente: Se valida sesión y SQL v4.1 impone unicidad de pareja.
- Pendiente concreto: Decidir respuesta que limita enumeración y límites proporcionados de solicitudes y reenvíos.
- Evidencia: FriendsService.cs; database/TLMG_DB_v4.1.sql.
- Depende de: BL-152 y decisión del encargado.
- Última revisión: 2026-11-20.

### BL-147 · Auditoría y operación del servidor
- Prioridad: P1.
- Responsable: Por asignar.
- Estado: Pendiente.
- Implementación existente: Archivo y consola reciben logs operativos del root.
- Pendiente concreto: Diferenciar auditoría de acciones validadas y log operativo, comprobar eventos y privacidad.
- Evidencia: Server/TLMG.Server/log4net.config; Program.cs.
- Relacionado: BL-033 y BL-179 tratan niveles y privacidad independientemente.
- Depende de: BL-093 para diagnóstico de errores.
- Última revisión: 2026-11-20.

### BL-148 · Arranque sin SQL
- Prioridad: P1.
- Responsable: Por asignar; decisión del encargado.
- Estado: Requiere decisión.
- Implementación existente: El fallo del calentamiento EF se registra y permite continuar el arranque.
- Pendiente concreto: Elegir política de arranque y probar operaciones y mensajes con BD inaccesible; no se ha probado modo degradado integral.
- Evidencia: Server/TLMG.Server/Program.cs.
- Depende de: Decisión del encargado y BL-111 para invitados.
- Última revisión: 2026-11-20.

### BL-087 · Presentación de invitaciones
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Implementación existente: MessageBox muestra texto traducido; Yes solicita aceptación.
- Pendiente concreto: Resolver localización de botones y tratamiento de avisos simultáneos; el rechazo al servidor se atiende en BL-069.
- Evidencia: MainWindow.xaml.cs:95.
- Relacionado: BL-142 trata responsabilidades de MainWindow.
- Depende de: BL-069 para rechazo funcional.
- Última revisión: 2026-11-20.

### BL-092 · Validación de historial de navegación
- Prioridad: P1.
- Responsable: Compañera para pruebas funcionales; Luis para decisión arquitectónica.
- Estado: Requiere validación.
- Implementación existente: GoBack y navegación sin historial cubren los regresos originales.
- Pendiente concreto: Verificar secuencias restantes y pila, especialmente RoundResult y MatchClosed; no se midió memoria.
- Evidencia: NavigationManager.cs; MainWindow.xaml.cs; ViewModels de partida.
- Depende de: BL-141 y BL-143.
- Última revisión: 2026-11-20.

### BL-069 · Ciclo de invitaciones de sala
- Prioridad: P1.
- Responsable: Por asignar.
- Estado: Parcial.
- Implementación existente: Existen envío, aceptación y limpieza por sala o sesión.
- Pendiente concreto: Cubrir rechazo, cancelación, vencimiento y notificación de invitaciones repetidas.
- Evidencia: IRoomInvitationManager; SessionRegistry.Friends.cs.
- Depende de: BL-058 y decisión de contrato.
- Última revisión: 2026-11-20.

### BL-077 · Aviso de fallo de amigos
- Prioridad: P1.
- Responsable: Por asignar.
- Estado: Parcial.
- Implementación existente: FriendsViewModel muestra el error mientras está activo; hay intentos de recuperación de canal.
- Pendiente concreto: Verificar y cubrir aviso y recuperación cuando la respuesta falla fuera de Amigos, incluyendo pérdida de avisos.
- Evidencia: FriendsViewModel.cs; ServerConnectionManager.Recovery.cs.
- Depende de: BL-058.
- Última revisión: 2026-11-20.

### BL-083 · Recuperar sala actual
- Prioridad: P1.
- Responsable: Por asignar.
- Estado: Parcial.
- Implementación existente: GetRecoveryContext devuelve contexto de partida recuperable autorizada.
- Pendiente concreto: Resolver el estado local perdido en sala antes de partida conforme a V3 y validar los casos de token y abandono.
- Evidencia: SessionRegistry.Matches.cs:63; Dominio V3 §1.
- Depende de: Decisión del encargado y BL-058.
- Última revisión: 2026-11-20.

### BL-089 · Presentación de invitados
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Implementación existente: FriendsScreen oculta controles y muestra aviso de invitado.
- Pendiente concreto: Revisar aviso repetido y disponibilidad de Invitar Amigos al integrar sesión real.
- Evidencia: LobbyViewModel.cs:337; LobbyScreen.xaml:199.
- Depende de: BL-111.
- Última revisión: 2026-11-20.

### BL-048 · Metadatos WCF e identidad
- Prioridad: P1.
- Responsable: Por asignar; Luis documenta la decisión de despliegue.
- Estado: Requiere validación.
- Implementación existente: las rutas identificadas están ignoradas y sus archivos no están presentes localmente ni rastreados.
- Pendiente concreto: verificar exposición histórica y reglas de regeneración e identidad de despliegue sin divulgar identificadores.
- Evidencia: `.gitignore:437`; histórico BL-048.
- Depende de: BL-021 y BL-023.
- Última revisión: 2026-11-20.

### BL-052 · Política de login para cuentas sancionadas
- Prioridad: P1.
- Responsable: Por asignar; decisión de producto y moderación con la compañera.
- Estado: Requiere decisión.
- Implementación existente: login rechaza estados distintos de USER_ACTIVE con error de credenciales.
- Pendiente concreto: definir si y cuándo distinguir suspensión y bloqueo tras verificar identidad, sin presumir una política.
- Evidencia: `Server/TLMG.Services/Accounts/PlayerLogInService.cs:109`; SQL v4.1.
- Depende de: decisión del equipo y BL-165.
- Última revisión: 2026-11-20.

### BL-060 · Seed de amistades fuera de producción
- Prioridad: P1.
- Responsable: Por asignar.
- Estado: Requiere decisión.
- Implementación existente: el script se identifica como herramienta de demostración y modifica amistades al ejecutarse manualmente.
- Pendiente concreto: decidir si se conserva para desarrollo y comprobar que no forme parte de la instalación ni del paquete final.
- Evidencia: `database/seed/TLMG_Seed_FriendshipDemo.sql`; búsqueda local de invocaciones.
- Depende de: revisión del paquete de entrega.
- Última revisión: 2026-11-20.

### BL-018 · Mensaje técnico de registro
- Prioridad: P1.
- Responsable: Por asignar.
- Estado: Pendiente.
- Implementación existente: registro y verificación usan CreateAccount_DataModelError.
- Pendiente concreto: sustituir el texto técnico por uno comprensible en ambos idiomas y cotejar el Excel original.
- Evidencia: `Client/TLMG.Client/Languages/Strings.es-MX.xaml:197`; VerificationViewModel.
- Depende de: acceso al Excel para su actualización; independiente de la refactorización de BL-017.
- Última revisión: 2026-11-20.

### BL-094 · Identificar duplicados al guardar
- Prioridad: P1.
- Responsable: Por asignar.
- Estado: Pendiente.
- Implementación existente: SQL v4.1 declara unicidad; el repositorio registra excepciones y devuelve bool.
- Pendiente concreto: distinguir correo o nickname duplicado de fallos de datos ante la carrera entre consulta e inserción; conservar logs y validar el caso.
- Evidencia: `Server/TLMG.DataAccess/Repositories/PlayerRepository.cs:75`; `Server/TLMG.Services/Accounts/PlayerAccountRegistrationService.cs:240`; `database/TLMG_DB_v4.1.sql:89`.
- Depende de: verificar despliegue de restricciones; BL-153.
- Última revisión: 2026-11-20.

### BL-143 · Regreso desde verificación de registro
- Prioridad: P1.
- Responsable: Compañera para la validación funcional; Luis decide la composición de Loading mediante BL-169.
- Estado: Requiere validación.
- Implementación existente: ScreenFactory cubre Leaderboard, Moderation, Gameplay y ambos flujos de Verification; el regreso del registro navega a CreateAccount.
- Pendiente concreto: comprobar qué ve el jugador al regresar desde la verificación con un registro pendiente. La construcción directa de Loading requiere decisión arquitectónica, no se considera por sí misma un fallo funcional.
- Evidencia: `Client/TLMG.Client/Navigation/ScreenFactory.cs:275`; `Client/TLMG.Client/MainWindow.xaml.cs:40`.
- Depende de: BL-007 y BL-169; BL-118 para transición por modalidad.
- Última revisión: 2026-11-20.

### BL-021 · Conexión entre dos equipos
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Requiere validación.
- Implementación existente: El desarrollador jugó una partida con cliente y servidor en PC distintas.
- Pendiente concreto: Confirmar los puntos de seguridad y latencia originales que correspondan al despliegue; no repetir la partida ya informada.
- Evidencia: BL-021 histórico y reporte del desarrollador.
- Depende de: Decisión de despliegue.
- Última revisión: 2026-11-20.

### BL-079 · Respuesta con servidor apagado
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Requiere validación.
- Implementación existente: El desarrollador confirma error, desconexión y retorno al login.
- Pendiente concreto: Comprobar duración y capacidad de interacción durante la apertura síncrona. Comprobar también la capacidad de interacción durante cierre síncrono de canales (BL-020).
- Evidencia: BL-079 histórico y reporte del desarrollador.
- Depende de: Decisión sobre espera; BL-020.
- Última revisión: 2026-11-20.

### BL-133 · Comandos y CanExecute
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Requiere validación.
- Implementación existente: El histórico registra pruebas de varias pantallas, login, registro y Gameplay.
- Pendiente concreto: Confirmar los CanExecute no acreditados y los escenarios no cubiertos; invitados se lleva en BL-111.
- Evidencia: BL-133 histórico.
- Depende de: Fase 6 y BL-164.
- Última revisión: 2026-11-20.

### BL-091 · Posición y puntos propios
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Implementación existente: El leaderboard consulta datos del servidor.
- Pendiente concreto: Mostrar posición y puntos reales de la sesión.
- Evidencia: LeaderboardViewModel; BL-091 histórico.
- Depende de: BL-103 y BL-185.
- Última revisión: 2026-11-20.

### BL-103 · Perfil con estadísticas reales
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Implementación existente: Existe operación de perfil.
- Pendiente concreto: Completar puntos, rango y racha con datos autoritativos.
- Evidencia: PlayerProfileService; BL-103 histórico.
- Depende de: BL-185.
- Última revisión: 2026-11-20.

### BL-106 · Retención y purga de chat
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Pendiente.
- Implementación existente: Hay servicio de chat.
- Pendiente concreto: Cubrir retención durante 24 horas y purga según requisito.
- Evidencia: BL-106 histórico; ChatService.
- Depende de: Diseño de persistencia de chat.
- Última revisión: 2026-11-20.

### BL-153 · Despliegue de SQL v4.1
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Requiere validación.
- Implementación existente: El equipo confirma operaciones de amistad con persistencia en una base.
- Pendiente concreto: Verificar versión, permisos y aplicación en bases requeridas; comprobar con BL-007 la disponibilidad y compatibilidad de dbo.SavePendingRegistration, invocado por el repositorio y ausente de los scripts versionados. Luis registra la decisión documental.
- Evidencia: database/TLMG_DB_v4.1.sql.
- Depende de: BL-114.
- Última revisión: 2026-11-20.

### BL-156 · Operaciones de amistad y bloqueo
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Pendiente.
- Implementación existente: Enviar, aceptar y rechazar solicitudes ya funcionan.
- Pendiente concreto: Cancelar solicitud, quitar amigo y bloquear o desbloquear.
- Evidencia: BL-156 histórico.
- Relacionado: BL-085 fue absorbido y BL-071 depende de esta funcionalidad.
- Depende de: BL-152 ya implementado; contratos de bloqueo.
- Última revisión: 2026-11-20.

### BL-165 · Moderación real
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Pendiente.
- Implementación existente: Existe interfaz de moderación.
- Pendiente concreto: Implementar reportes y bloqueos reales y su integración.
- Evidencia: ModerationViewModel; BL-165 histórico.
- Depende de: BL-156 y decisión de contrato.
- Última revisión: 2026-11-20.

### BL-185 · Persistencia de resultados
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Pendiente.
- Implementación existente: El leaderboard lee total_points; el motor mantiene estado de partida.
- Pendiente concreto: Persistir resultados finales y estadísticas acumuladas de registrados.
- Evidencia: PlayerRepository; MatchState; BL-185 histórico.
- Relacionado: BL-091 y BL-103 consumirán resultados o estadísticas cuando correspondan.
- Depende de: Reglas V3 y decisión de persistencia.
- Última revisión: 2026-11-20.

### BL-186 · Indicador de vida extra
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Pendiente.
- Implementación existente: El dominio mantiene HasExtraLife.
- Pendiente concreto: Mostrar el beneficio y su consumo en Gameplay.
- Evidencia: MatchPlayer.HasExtraLife; BL-186 histórico.
- Depende de: BL-119 y vista de partida.
- Última revisión: 2026-11-20.

### BL-071 · Actualización de amistades en canales
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Por qué sigue vigente: la eliminación de amistades aún debe reflejarse en los canales.
- Evidencia actual: `FriendsService` llama a `AddFriendshipToOnlineChannels` al aceptar.
- Falta para cerrar: cubrir y verificar la eliminación.
- Depende de: BL-156.
- Última revisión: 2026-11-20.

### BL-118 · Navegación según modalidad
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Por qué sigue vigente: la carga normal de cuatro y seis cartas no está acreditada.
- Evidencia actual: el desarrollador confirma que varios clientes ven el minijuego de ocho cartas; el histórico registra navegación directa a Gameplay en cuatro y seis.
- Falta para cerrar: implementar o verificar la transición correspondiente y sus tiempos.
- Depende de: BL-046 y BL-117.
- Última revisión: 2026-11-20.

### BL-119 · Presentación del gatito
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Requiere validación.
- Por qué sigue vigente: la reproducción de segmentos necesita comprobación visual y de sincronización.
- Evidencia actual: El desarrollador informó una captura multicliente correcta con continuación; la presentación y sincronización específicas siguen por verificar en BL-184.
- Falta para cerrar: comprobar los requisitos de presentación y tiempos descritos en el histórico.
- Depende de: BL-117 y validación del encargado.
- Última revisión: 2026-11-20.

### BL-161 · Expulsión por votación
- Prioridad: P1, por su efecto sobre sala y partida.
- Responsable: Compañera.
- Estado: Pendiente.
- Por qué sigue vigente: las pantallas referencian el trabajo y el dominio exige unanimidad entre los demás jugadores.
- Evidencia actual: `GameplayScreen.xaml.cs`, `LobbyScreen.xaml.cs` y `docs/Dominio_del_juego_v3.md` §2.
- Falta para cerrar: implementar y validar el flujo y las consecuencias en sala y partida.
- Depende de: BL-165 e integración de sala y partida.
- Última revisión: 2026-11-20.

### BL-182 · Regresiones manuales de partida
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Por qué sigue vigente: quedan escenarios multicliente y de interfaz sin acreditar.
- Evidencia actual: el desarrollador informa pruebas funcionales de conexión, reconexión y abandono; el histórico documenta otra prueba con tres jugadores.
- Falta para cerrar: cinco y seis jugadores, caída aislada del canal, idioma, errores y regresiones enumeradas en el histórico.
- Depende de: escenarios y entorno de prueba.
- Última revisión: 2026-11-20.

### BL-184 · Validación multicliente del gatito
- Prioridad: P1.
- Responsable: Compañera.
- Estado: Parcial.
- Por qué sigue vigente: el flujo principal informado no acredita captura simultánea, escape, recuperación ni sincronización bajo latencia.
- Evidencia actual: el desarrollador confirma que tres clientes jugaron, capturaron al gatito y terminaron la partida con ganador.
- Falta para cerrar: captura simultánea, escape, recuperación, suscripción tardía, sincronización bajo latencia, idioma y errores excepcionales; la transición principal con ganador fue informada como probada.
- Depende de: BL-058, BL-118 y BL-119 para los escenarios indicados en el histórico.
- Última revisión: 2026-11-20.

## P2 — Contratos, calidad y estándar

### BL-005 · Generación asíncrona del proxy WCF
- Prioridad: P2.
- Responsable: Por asignar; Luis coordina la decisión documental.
- Estado: Requiere decisión.
- Implementación existente: 563cd51 retiró las operaciones asíncronas del proxy histórico; el cliente actual expone operaciones síncronas.
- Pendiente concreto: Decidir si la generación Task actual se autoriza frente a ADR-03 o si se regenera conforme a la regla; no editar archivos generados manualmente.
- Evidencia: Reference.svcmap activa GenerateTaskBasedAsynchronousMethod; Reference.cs expone operaciones Task<T>.
- Depende de: Decisión del encargado y regla aplicable.
- Última revisión: 2026-11-20.



### BL-178 · Referencias de comentarios de chat
- Prioridad: P2.
- Responsable: Compañera.
- Estado: Parcial.
- Implementación existente: El TODO de Gameplay cita BL-161 existente.
- Pendiente concreto: Corregir trazabilidad del TODO de ChatService y revisar formato; el ámbito funcional permanece en BL-106.
- Evidencia: ChatService.cs:16; GameplayScreen.xaml.cs:106.
- Depende de: BL-106, BL-161 y BL-189.
- Última revisión: 2026-11-20.

### BL-033 · Nivel de log para entrega
- Prioridad: P2.
- Responsable: Por asignar.
- Estado: Parcial.
- Implementación existente: El servidor referencia ConsoleAppender.
- Pendiente concreto: Definir y verificar configuración efectiva de entrega sin DEBUG/TRACE para cliente y servidor.
- Evidencia: Ambos log4net.config; Estándar §10.3.
- Relacionado: BL-147 trata la separación de auditoría y operación.
- Depende de: Decisión de configuración por entorno.
- Última revisión: 2026-11-20.

### BL-043 · Estilo del diagnóstico histórico
- Prioridad: P2.
- Responsable: Por asignar.
- Estado: Parcial.
- Implementación existente: nombres, retornos, literal de puntos y uso de cultura históricos fueron corregidos o reemplazados.
- Pendiente concreto: agregar param y returns a los tres métodos documentados de ValidationHelper; corregir las 80 líneas de GlobalStyles que superan 120 caracteres preservando el comportamiento. Verificar necesidad de System.Web antes de retirarlo.
- Evidencia: `Client/TLMG.Client/ValidationHelper.cs:33`; `Client/TLMG.Client/Styles/GlobalStyles.xaml`; `Server/TLMG.Services/TLMG.Services.csproj:52`.
- Depende de: BL-027; diagnóstico original para identificar dos ternarios históricos; decisiones de claves en BL-017/BL-187 y MVVM en BL-041.
- Última revisión: 2026-11-20.

### BL-162 · Aviso de rechazo de amistad
- Prioridad: P2.
- Responsable: Compañera; decisión de privacidad por asignar.
- Estado: Requiere decisión.
- Implementación existente: El rechazo cambia estado y responde al receptor.
- Pendiente concreto: Decidir si actualizar al remitente de inmediato y cómo hacerlo sin exposición innecesaria.
- Evidencia: FriendsService.DeclineFriendRequest; IFriendshipManagerCallback.
- Depende de: BL-152 y BL-155.
- Última revisión: 2026-11-20.

### BL-163 · Aviso de solicitud fuera de Amigos
- Prioridad: P2.
- Responsable: Compañera.
- Estado: Requiere decisión.
- Implementación existente: El manager guarda solicitudes con lista inicializada y Amigos las presenta al activarse.
- Pendiente concreto: Decidir aviso inmediato y comprobar recuperación si la lista no está inicializada.
- Evidencia: ServerConnectionManager.FriendRequests.cs; FriendsViewModel.cs.
- Relacionado: BL-087 y BL-142 tratan presentación y organización relacionadas.
- Depende de: Decisión sobre aviso inmediato.
- Última revisión: 2026-11-20.

### BL-150 · Alcance y privacidad del log cliente
- Prioridad: P2.
- Responsable: Por asignar.
- Estado: Requiere validación.
- Implementación existente: App inicializa log4net; hay diagnóstico local y de comunicación.
- Pendiente concreto: Validar eventos y detalles de excepciones sin datos sensibles y documentar que la evidencia autoritativa corresponde al servidor.
- Evidencia: Client/TLMG.Client/App.xaml.cs; log4net.config.
- Depende de: BL-147.
- Última revisión: 2026-11-20.

### BL-034 · Timeouts del servidor y host
- Prioridad: P2.
- Responsable: Por asignar.
- Estado: Requiere decisión.
- Implementación existente: el cliente local declara 10/15/5 segundos; el binding del servidor no explicita timeouts.
- Pendiente concreto: evaluar valores efectivos y decidir los adecuados para servidor y host sin asumir que deban coincidir con cliente.
- Evidencia: `docs/Estandar.md:3211`; bindings locales de Client y Server.
- Depende de: BL-021 y BL-023.
- Última revisión: 2026-11-20.

### BL-037 · Frontera Services y DataAccess
- Prioridad: P2.
- Responsable: Por asignar.
- Estado: Parcial.
- Implementación existente: los repositorios inspeccionados capturan excepciones de infraestructura; no se encontraron tipos EF/SQL directos en C# de Services.
- Pendiente concreto: verificar si las referencias y targets EF de Services son necesarios en compilación y ejecución; retirar o justificar según Estándar. La carrera de duplicados se atiende en BL-094.
- Evidencia: `Server/TLMG.Services/TLMG.Services.csproj:3`; repositorios DataAccess.
- Depende de: decisión técnica; BL-004 y BL-094 para sus alcances separados.
- Última revisión: 2026-11-20.

### BL-017 · Organización de ErrorKey
- Prioridad: P2.
- Responsable: Luis para decisiones contractuales; implementación por asignar.
- Estado: Parcial.
- Implementación existente: ErrorKeys agrupa claves de servidor y OperationResult transporta errores.
- Pendiente concreto: evaluar literales duplicados y decidir centralización sin unificar errores de distinto significado.
- Evidencia: `Server/TLMG.Domain/Constants/ErrorKeys.cs`; ViewModels y servicios.
- Depende de: inventario y decisión del encargado; BL-187 para semántica contractual.
- Última revisión: 2026-11-20.

### BL-134 · Traducción y pruebas de cliente
- Prioridad: P2.
- Responsable: Luis para evaluación contractual; pruebas de cliente por asignar.
- Estado: Requiere validación.
- Implementación existente: ErrorMessageHelper y LanguageManager buscan recursos con fallback similar.
- Pendiente concreto: determinar qué dependencia de recursos WPF afecta las pruebas de BL-164 y decidir si conviene unificar sin alterar el alcance de búsqueda.
- Evidencia: `Client/TLMG.Client/ErrorMessageHelper.cs`; `Client/TLMG.Client/LanguageManager.cs`.
- Depende de: BL-164; BL-187 para semántica de null.
- Última revisión: 2026-11-20.

### BL-144 · Comentarios XML y título informativo
- Prioridad: P2.
- Responsable: Por asignar.
- Estado: Parcial.
- Implementación existente: clase, constructor, propiedad ScreenFactory y ShowExistingScreen tienen documentación; ShowMessage usa MainWindow_Title.
- Pendiente concreto: documentar las cuatro sobrecargas de navegación, GoBack y ClearHistory; incorporar y usar el título informativo acordado en ambos idiomas.
- Evidencia: `Client/TLMG.Client/Navigation/NavigationManager.cs`; `DialogManager.cs`; Estándar §12.
- Depende de: nada.
- Última revisión: 2026-11-20.

### BL-176 · Diagnósticos de Visual Studio
- Prioridad: P2.
- Responsable: Compañera para validación final; correcciones por asignar.
- Estado: Requiere validación.
- Implementación existente: el caso concreto de xUnit2013 señalado usa Assert.Single.
- Pendiente concreto: consultar la lista actual de diagnósticos, identificar ubicaciones y distinguir advertencias de sugerencias antes de actuar.
- Evidencia: `test/TLMG.Services.Tests/FriendsServiceAcceptRequestTests.cs:65`; lista actual no consultada.
- Depende de: lista de errores de Visual Studio.
- Última revisión: 2026-11-20.

### BL-039 · Infraestructura y rutas de prueba
- Prioridad: P2.
- Responsable: Compañera.
- Estado: Parcial.
- Implementación existente: Existen xUnit y pruebas Domain y Services; hay pruebas de rutas.
- Pendiente concreto: Verificar pruebas explícitas de extremos de túneles y escondites; pruebas WPF en BL-164.
- Evidencia: FugitiveKittenRouteTests y BL-039 histórico.
- Depende de: BL-116.
- Última revisión: 2026-11-20.

### BL-041 · Migración MVVM
- Prioridad: P2.
- Responsable: Luis.
- Estado: Parcial.
- Implementación existente: Existen ViewModels, fábrica y comandos; FugitiveKitten se migró.
- Pendiente concreto: Revisar el alcance completo pendiente y la decisión de Loading.
- Evidencia: ScreenFactory y BL-041 histórico.
- Depende de: BL-169.
- Última revisión: 2026-11-20.

### BL-051 · Decisión sobre tiempos medidos
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere decisión.
- Implementación existente: Se midieron ~3 s primer login, ~1.8 s con servidor preparado y ~1.42 s ranking.
- Pendiente concreto: Decidir si los tiempos son aceptables para la demostración.
- Evidencia: BL-051 histórico, medición de 2026-11-11.
- Depende de: Decisión del encargado.
- Última revisión: 2026-11-20.

### BL-187 · Semántica de null y resultados
- Prioridad: P2.
- Responsable: Luis.
- Estado: Pendiente.
- Implementación existente: Existen contratos WCF y resultados controlados.
- Pendiente concreto: Auditar ausencia, errores y null entre clientes y servicios.
- Evidencia: Contratos WCF; BL-187 histórico.
- Depende de: Contratos y decisiones de error.
- Última revisión: 2026-11-20.

### BL-189 · Comentarios restantes en inglés
- Prioridad: P2.
- Responsable: Compañera.
- Estado: Pendiente.
- Implementación existente: Hubo limpieza previa de comentarios de jugabilidad.
- Pendiente concreto: Inventariar y corregir los restantes fuera de ese alcance.
- Evidencia: BL-189 histórico; Estándar §12.
- Depende de: Inventario de comentarios.
- Última revisión: 2026-11-20.

### BL-190 · SonarQube final
- Prioridad: P2.
- Responsable: Compañera.
- Estado: Pendiente.
- Implementación existente: Existe análisis previo de una revisión anterior.
- Pendiente concreto: Revalidar el commit definitivo y registrar incidencias.
- Evidencia: BL-190 histórico.
- Depende de: Commit definitivo y entorno SonarQube.
- Última revisión: 2026-11-20.

### BL-115 · Referencias a validadores en el Estándar
- Prioridad: P2, pues la discrepancia observada es documental.
- Responsable: Luis.
- Estado: Parcial.
- Por qué sigue vigente: el Estándar describe ubicaciones contradictorias.
- Evidencia actual: validadores en `Server/TLMG.Domain/Validators`; nota histórica de §4.3 y §16.1.
- Falta para cerrar: corregir y verificar las referencias del Estándar.
- Depende de: BL-040.
- Última revisión: 2026-11-20.

## P2 — Documentación y arquitectura

### BL-012 · Documento de contratos WCF
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere validación.
- Implementación existente: Hay contratos y callbacks de cuentas, amigos, sala, partida y contraseña.
- Pendiente concreto: Recuperar documento original y actualizar roles, operaciones y estado real de invitados.
- Evidencia: Server/TLMG.Contracts; histórico BL-012.
- Depende de: BL-026 y BL-111.
- Última revisión: 2026-11-20.

### BL-009 · Ratificación de sincronización
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere decisión.
- Implementación existente: D-58 autoriza lock de SessionRegistry; 2FA usa persistencia.
- Pendiente concreto: Consultar ADR-05 y decisiones originales; ratificar o corregir contradicciones con implementación y D-42.
- Evidencia: docs/Estandar.md:3152; VerificationCodeStore.
- Depende de: BL-007 y BL-026.
- Última revisión: 2026-11-20.

### BL-010 · ADR-07 y chat
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere validación.
- Implementación existente: El Estándar registra D-55 y el chat valida y limita mensajes.
- Pendiente concreto: Consultar ADR-07 original y confirmar si refleja comunicación vigente.
- Evidencia: docs/Estandar.md:4402; ChatService.
- Depende de: BL-009 y ADR-07 original.
- Última revisión: 2026-11-20.

### BL-011 · Drivers y documentos de hito
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere validación.
- Implementación existente: La implementación 2FA usa repositorio y procedimientos.
- Pendiente concreto: Recuperar Drivers priorizados_V2 y Equipo3_Hito1_V2 y cotejar referencias a ADR-05.
- Evidencia: Histórico BL-011; originales no disponibles.
- Depende de: BL-009 y BL-026.
- Última revisión: 2026-11-20.

### BL-026 · Fuentes documentales
- Prioridad: P2.
- Responsable: Luis.
- Estado: Parcial.
- Implementación existente: Estándar y dominio Markdown y reglas V3 DOCX están versionados.
- Pendiente concreto: Recuperar SAD, Bitácora y contratos documentales, acordar formato y corregir referencias.
- Evidencia: docs/; git 7110f29; gov/CLAUDE.md §14.
- Depende de: Originales y decisión del encargado.
- Última revisión: 2026-11-20.

### BL-114 · Bitácora y decisiones
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere validación.
- Implementación existente: El Estándar y el código reflejan varias decisiones de navegación, Domain y Match.
- Pendiente concreto: Cotejar Bitácora original y SAD; registrar decisiones aprobadas y enmiendas sin asignar números de antemano. Cotejar D-57, enmienda D-40, D-58 a D-61 y ADR-08, salas/autorizaciones y presencia (BL-056, BL-059, BL-068 y BL-074). Consultar al encargado sobre D-66 y §17.1: firmas que usan OperationResult directamente (BL-109).
- Evidencia: docs/Estandar.md:4405; INavigationManager; IMatchConnection.
- Relacionado: BL-081, BL-108, BL-142 y BL-169 aportan decisiones o propuestas para cotejar, sin bloquear la consulta de la Bitácora.
- Depende de: BL-026 y originales de Bitácora y SAD.
- Última revisión: 2026-11-20.

### BL-170 · Declaración de IA en SAD
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere validación.
- Implementación existente: El Estándar tiene declaración propia; SAD no está disponible.
- Pendiente concreto: Revisar SAD §1.4 y respaldar actividades, revisión humana y pruebas efectivamente realizadas.
- Evidencia: gov/CLAUDE.md §1; docs/Estandar.md §21.
- Depende de: BL-026 y SAD original.
- Última revisión: 2026-11-20.

### BL-081 · Estándar de comunicación
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere decisión.
- Implementación existente: Closed y Faulted comparten OnServerChannelEnded; apertura y cierre capturan errores de comunicación y timeout.
- Pendiente concreto: aclarar sufijo de implementación de callback y tratamiento de errores en eventos de canal conforme a la decisión del encargado, sin relanzamiento indiscriminado.
- Evidencia: `Client/TLMG.Client/Communication/ServerConnectionManager.cs:677`; `docs/Estandar.md` §§3.14 y 10.2.
- Relacionado: BL-093 trata límites del servidor y BL-114 registra decisiones.
- Depende de: Decisión del encargado.
- Última revisión: 2026-11-20.

### BL-142 · Responsabilidades de MainWindow
- Prioridad: P2.
- Responsable: Luis para decisión arquitectónica; implementación por asignar.
- Estado: Requiere decisión.
- Implementación existente: MainWindowViewModel hospeda la pantalla; MainWindow atiende eventos y diálogos.
- Pendiente concreto: Definir separación justificada y tratamiento de mensajes formateados; invitaciones se llevan en BL-087.
- Evidencia: MainWindow.xaml.cs; DialogManager.cs.
- Relacionado: BL-087 y BL-163 tratan avisos funcionales relacionados.
- Depende de: Decisión arquitectónica del encargado.
- Última revisión: 2026-11-20.

### BL-145 · Cambio de idioma y ciclo de vida
- Prioridad: P2.
- Responsable: Compañera para validación funcional; decisión técnica por asignar.
- Estado: Requiere validación.
- Implementación existente: Los diccionarios se cambian y las vistas usan recursos dinámicos; se restaura la misma instancia de contenido.
- Pendiente concreto: Comprobar Loaded/Unloaded, estado y suscripciones al cambiar idioma y decidir si se necesita la recarga.
- Evidencia: LanguageManager.cs:141.
- Depende de: BL-041; pruebas funcionales.
- Última revisión: 2026-11-20.

### BL-169 · Decisión MVVM de Loading
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere decisión.
- Implementación existente: ChangePassword tiene ViewModel; Loading anima y emite evento desde la vista.
- Pendiente concreto: Consultar al encargado una excepción documentada o solución conforme a §7.1, sin crear un ViewModel vacío por uniformidad.
- Evidencia: LoadingScreen.xaml.cs; docs/Estandar.md:1538.
- Relacionado: BL-143 cubre validación de navegación; BL-114 registra la decisión.
- Depende de: Decisión del encargado sobre Loading.
- Última revisión: 2026-11-20.

### BL-084 · Capacidad fija de sala
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere decisión.
- Implementación existente: Cliente y servidor utilizan 6, el máximo de V3; el servidor aplica el límite.
- Pendiente concreto: Decidir si aceptar la constante visual fija o publicar capacidad, sin cambio de contrato innecesario.
- Evidencia: LobbyViewModel.cs:17; ValidationRules.cs:45.
- Depende de: Decisión del encargado.
- Última revisión: 2026-11-20.

### BL-049 · Ejemplo de callback en el Estándar
- Prioridad: P2.
- Responsable: Luis.
- Estado: Requiere decisión.
- Implementación existente: D-62 regula Result y On; los callbacks actuales incluyen `[ServiceContract]`.
- Pendiente concreto: consultar al responsable y ratificar o aclarar el ejemplo de §3.10 respecto al atributo.
- Evidencia: `docs/Estandar.md:855`; `Server/TLMG.Contracts/IFriendsManagerCallback.cs`; commit `53226c4`.
- Depende de: decisión del encargado y fuente editable del Estándar.
- Última revisión: 2026-11-20.

### BL-080 · Abstracción de comunicación por área
- Prioridad: P2.
- Responsable: Luis, para documentar la decisión; cambios de implementación por asignar si fueran necesarios.
- Estado: Requiere decisión.
- Implementación existente: interfaces por área inyectadas en ViewModels; chat se expone por `ILobbyConnection`.
- Pendiente concreto: decidir si la agrupación del chat satisface D-70 y las necesidades de pruebas; no se propone una interfaz monolítica.
- Evidencia: `Client/TLMG.Client/Communication/ILobbyConnection.cs`; `docs/Estandar.md:1131`; commit `cefa0a3`.
- Depende de: decisión del encargado; necesidades de pruebas.
- Última revisión: 2026-11-20.

### BL-031 · Fechas del historial SQL
- Prioridad: P2.
- Responsable: Luis.
- Estado: Parcial.
- Implementación existente: v4.1 incluye encabezado versionado; v4 retiró los datos de demostración según V1.
- Pendiente concreto: recuperar fechas documentadas de v1, v2, v3 y v3.1 y completar el encabezado; no deducirlas de commits de traslado.
- Evidencia: `database/TLMG_DB_v4.1.sql:10`; historial `80d0054` y `c16351b`.
- Depende de: fuente verificable de fechas.
- Última revisión: 2026-11-20.

### BL-101 · Defectos del Estándar
- Prioridad: P2.
- Responsable: Luis.
- Estado: Pendiente.
- Implementación existente: existen ejemplos y tablas en el Estándar; la lista de incidencias está en V1.
- Pendiente concreto: corregir o ratificar los ejemplos, formato, nombres, estados, tabla de logs y reglas de excepciones detallados en V1; revisar puntuación en la fuente editable y reexportar. Incluir ejemplo public/internal de LanguageManager (BL-090) y cotejar estado de salas y presencia en §§11.1 y 6.2 (BL-068/BL-074).
- Evidencia: `docs/Estandar.md:1330`, `:1464`, `:1527`, `:2399`, `:2573`, `:3162` y `:3196`.
- Depende de: encargado y fuente editable del Estándar.
- Última revisión: 2026-11-20.

### BL-110 · Diccionarios y Excel
- Prioridad: P2.
- Responsable: Por asignar; Luis coordina la alineación documental.
- Estado: Parcial.
- Implementación existente: 489 claves únicas por idioma, con paridad exacta.
- Pendiente concreto: comprobar usos dinámicos y resolver claves redundantes o sin uso; consultar el Excel original y alinear sus claves.
- Evidencia: `Client/TLMG.Client/Languages/Strings.es-MX.xaml` y `Strings.en-US.xaml`; Excel no encontrado en el repositorio.
- Depende de: acceso a `TLMG_Diccionario_v3.xlsx`.
- Última revisión: 2026-11-20.

### BL-188 · Cuaderno de estudio técnico
- Prioridad: P2.
- Responsable: Luis.
- Estado: Pendiente.
- Implementación existente: Hay código y documentación parcial del proyecto.
- Pendiente concreto: Preparar el cuaderno integral para estudiar y defender el proyecto.
- Evidencia: BL-188 histórico.
- Depende de: BL-026; BL-114.
- Última revisión: 2026-11-20.

Las decisiones y fuentes pendientes se detallan en las entradas de esta sección.

## Postergados — Mejoras no obligatorias actualmente

Las entradas postergadas se conservan en V1 y no forman parte de las tareas activas.
