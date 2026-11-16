**The Last Meow**

*Reglamento Oficial del Videojuego*

Se busca desarrollar un videojuego como proyecto para la Experiencia Educativa “Tecnologías para la Construcción de Software”, teniendo al “Diseño de Software” como la base fundamentada que se requiere para el diseño, la arquitectura, y la implementación.

El videojuego es una adaptación al juego de cartas inspirado en Skull, de Hervé Marly, con ambientación felina propia, llamado The Last Meow (o “El último Maullido” en español). Este videojuego no se limita solo a contener la misma lógica del juego o al contenido íntegro de las reglas, sino que es una adaptación por completo, contemplando un conjunto de servicios, como mensajería o cifrado de contraseñas, que conforman el sistema completo.

El equipo optó por el nombre **The Last Meow** (en español "El último maullido"), caracterizando la temática de los gatos en dicho juego, inspirado en distintos estilos característicos.

1. **1\. Reglamento**

**1.1. Preparación y Lobby**

***1.1.1. Requisitos:***

•  Cantidad de jugadores mínimos requeridos tanto para que la cuenta regresiva pueda arrancar como para iniciar una partida: 3 (todos en estado "Listo").

•  Cantidad de jugadores máximos para jugar: 6\.

***1.1.2. Inicio:***

Para poder iniciar la partida, desde el lobby, cada jugador tendrá que marcarse como "Listo". La partida no la inicia ningún jugador en particular: comienza automáticamente en cuanto todos alcanzan ese estado (ver 1.1.3).

***1.1.3. Configuración de Partida:***

La elección de la **modalidad de juego** (cantidad de cartas y Gatos con Maullido Fatal) se realiza a través de una selección colaborativa sincronizada en tiempo real con las siguientes opciones:

**1\. 4 (1 Gato con Maullido Fatal)**

**2\. 6 (1 Gato con Maullido Fatal)**

**3\. 8 (2 Gatos con Maullido Fatal)**

•  Cualquier jugador dentro del lobby puede **cambiar la configuración de la partida** entre las opciones disponibles. Al seleccionar una opción, el cambio se refleja de forma instantánea en las pantallas de todos los jugadores de la sala.

•  Cada vez que cualquier jugador **modifica** la cantidad de cartas, el sistema cambiará automáticamente el estado de todos los jugadores de **“Listo”** a **“No Listo”**. Esto garantiza que ningún participante sea forzado a jugar una configuración que no ha confirmado.

•  Cuando **todos** los jugadores alcancen el estado **“Listo”**, se iniciará un temporizador de **3 segundos** antes de comenzar la partida. Si durante este lapso cualquier jugador cambia su estado a **“No Listo”**, el temporizador **se cancela** de inmediato. Durante el temporizador no se puede modificar la cantidad de cartas.

**2\. Mecánica General**

***2.0.1. Acciones por turno:***

En su turno, el jugador activo realiza exactamente una de las siguientes acciones:

**1\. Colocar boca abajo una carta de su mano sobre su propia pila:** La colocación de cartas se ejecutará exclusivamente mediante **un clic** sobre la carta seleccionada dentro de la mano del jugador. Al hacer clic sobre la carta, el sistema disparará una animación de traslación donde la carta se desplazará desde el centro del panel inferior de cartas del jugador hacia su pila correspondiente en el centro de la mesa, posicionándose boca abajo. Solo está disponible si el jugador tiene al menos una carta en su mano.

**2\. Iniciar una "apuesta" (challenge):** declarar un número de cartas que el jugador cree poder voltear sin revelar ningún Gato con Maullido Fatal. La apuesta debe ser mayor a 0 y menor o igual al menor de estos dos valores: el número de cartas boca abajo sobre la mesa, y el número de cartas de la modalidad en curso (4, 6 u 8).

**Mano vacía:** si el jugador no tiene cartas en su mano (todas sus vidas están sobre la mesa), la única acción que puede realizar es abrir una apuesta, y debe declarar **exactamente 1**. No puede pasar el turno. Esto evita que un jugador con pocas cartas se niegue a apostar de forma indefinida. Después de abrir con 1, el resto de la dinámica de apuestas es la normal (2.0.2): otros jugadores pueden subir y él también puede subir o retirarse.

***2.0.2. Dinámica de Apuestas:***

**1\. Inicio de la Apuesta:** Una vez que un jugador inicia una apuesta (declara el número de cartas que cree poder voltear sin revelar un Gato con Maullido Fatal), ningún otro jugador puede colocar más cartas sobre la mesa.

**2\. Ronda de apuestas:** En orden de turno, los jugadores deben elegir entre:

○  **Subir la apuesta:** Declarar un número mayor de cartas a voltear.

○  **Retirarse:** Pasar el turno y abandonar la apuesta actual. Un jugador que se retira no puede volver a participar en la ronda de apuestas en curso.

El **orden de turnos** es el orden en que el servidor registró el ingreso de cada jugador a la sala. Se fija al iniciar la partida y no cambia durante ella. Quien sale de la sala de espera y vuelve a entrar antes de iniciar ocupa el último lugar del orden. Los jugadores eliminados, expulsados o que abandonaron se omiten.

**3\. Resolución del reto:** Este proceso continúa hasta que solo queda un jugador con la apuesta más alta. Ese jugador debe cumplirla siguiendo este orden:

○  Primero, voltea las cartas de su propia pila, de arriba hacia abajo (si aparece un Gato con Maullido Fatal, el reto falla).

○  Después, elige de qué pilas de otros jugadores desea continuar volteando cartas, hasta alcanzar el número prometido. De cada pila ajena se voltea siempre la carta de arriba, es decir, la última que su dueño colocó. El apostador elige **de qué jugador** voltea, nunca qué carta de esa pila.

Las pilas se forman por orden de colocación: cada carta nueva se coloca encima de las anteriores. Las cartas volteadas quedan a la vista hasta que termina la ronda.

**4\. Inicio de cada ronda:** Cada ronda comienza con el **primer jugador activo según el orden de turnos**. Si el primer jugador de la partida fue eliminado, la ronda comienza con el siguiente jugador activo en el orden. Ejemplo: con los jugadores A, B, C y D, si B es eliminado, las rondas siguientes se juegan en el orden A, C, D y comienza A. Si A es eliminado, el orden es B, C, D y comienza B.

***2.0.3. Resultado del reto:***

•  **Éxito:** Si logra voltear la cantidad de cartas prometida sin revelar ningún Gato con Maullido Fatal, suma un reto exitoso a su conteo (2.0.5). Todas las cartas jugadas regresan a las manos de sus dueños y la mesa se reinicia para una nueva ronda.

•  **Fallo:** Si aparece un Gato con Maullido Fatal al voltear (y el apostador no tiene una vida extra que lo absorba, ver 2.0.4.1), el jugador falla el reto. Todas las cartas jugadas regresan a las manos de sus dueños y, después, el apostador pierde **permanentemente una carta**. La carta que se pierde se elige así (ver 2.0.3.1):

○  **Si el Gato era de otro jugador:** el dueño de la pila donde apareció el Gato elige **a ciegas** qué carta del apostador se elimina.

○  **Si el Gato era del propio apostador:** el apostador elige **viendo sus cartas** cuál se elimina.

Cuando la carta ha sido eliminada, la mesa se reinicia para una nueva ronda.

***2.0.3.1. Castigo pendiente:***

Entre el momento en que un reto falla y el momento en que se elimina la carta existe un **castigo pendiente**.

•  **Quién elige:** el dueño de la pila donde apareció el Gato con Maullido Fatal. Si el Gato era del propio apostador, elige el apostador. Solo puede haber un dueño, porque la revelación se detiene en el primer Gato (No. 6).

•  **Qué ve quien elige:**

○  **Elección a ciegas (Gato ajeno):** una ventana superpuesta con el nombre del jugador castigado y todas sus cartas (sus vidas) **boca abajo**, sin ninguna pista sobre cuál es cuál. El orden en que aparecen es aleatorio y distinto en cada castigo. Quien elige selecciona una.

○  **Elección propia (Gato propio):** la misma ventana, pero el jugador castigado ve sus cartas boca arriba y elige cuál eliminar.

•  **Qué ven los demás jugadores:** únicamente que el jugador correspondiente está eligiendo. No ven cartas.

•  **Resultado:** cuando se elige, la ventana se cierra y se elimina la carta. **Solo el jugador castigado** ve qué carta se eliminó (incluido su tipo). Los demás jugadores solo ven que sus vidas bajaron en 1, sin saber de qué tipo era la carta.

•  **Bloqueo:** mientras exista un castigo pendiente nadie puede realizar acciones de juego (colocar, apostar, subir ni retirarse). El chat sigue disponible.

•  **Una sola vida:** si el jugador castigado tiene solo 1 vida, no hay nada que elegir: la carta se elimina de inmediato, sin mostrar la ventana, y se aplica la eliminación (2.0.5).

•  **Tiempo límite:** quien debe elegir dispone de **10 segundos**. Si no elige, el sistema elige **al azar**.

•  **Cambios en la sala durante el castigo:** si quien debe elegir es expulsado de la partida, el sistema elige al azar de inmediato. Si el jugador castigado es expulsado o abandona, el castigo se cancela.

•  **Vida extra:** si el apostador tenía la vida extra (2.0.4.1) y el Gato revelado fue el primero, la vida extra se consume y **no** hay castigo pendiente.

•  **Después del castigo:** si el jugador castigado se queda con 0 vidas, queda eliminado (2.0.5). Después se comprueba si la partida termina (No. 7 y No. 9b); si no, comienza una nueva ronda (2.0.2, punto 4).

***2.0.4. Variantes de Juego:***

El juego ofrece distintas configuraciones que modifican la dificultad y el número de cartas en juego:

•  **Modo 4 Cartas:** cada jugador recibe 4 cartas — 1 Gato con Maullido Fatal y 3 seguras.

•  **Modo 6 Cartas:** cada jugador recibe 6 cartas — 1 Gato con Maullido Fatal y 5 seguras.

•  **Modo 8 Cartas:** cada jugador recibe 8 cartas — 2 Gatos con Maullido Fatal y 6 seguras.

***2.0.4.1. Vida Extra: El gatito Fugitivo***

Variante que añade diversión antes de cada partida, sin alterar las reglas centrales del juego. Este minijuego SÓLO existe en la modalidad de 8 cartas; en las modalidades de 4 y 6 cartas no se juega y se presenta la pantalla de carga normal.

**¿Cómo funciona?**

En lugar de la pantalla normal de carga (exclusivamente en el modo de 8 cartas), todos los jugadores ven durante **15 segundos** a un gatito corriendo por la pantalla. Cualquier jugador puede intentar atraparlo tocándolo mientras está en la pantalla, pero es muy rápido y solo hay una oportunidad, en cuanto alguien lo atrapa, desaparece para todos.

**Si nadie logra atraparlo en los 15 segundos:** la partida continúa normal y nadie recibe ningún beneficio.

**Recompensa:** El jugador que atrape al gatito obtiene **1 vida extra**, válida solo durante esa partida.

•  Con vida extra, el **primer** Gato con Maullido Fatal que revele en esa partida NO hace fallar el reto: consume la vida extra automáticamente y el jugador CONTINÚA revelando cartas hasta completar las prometidas.

•  Si completa las cartas prometidas sin revelar otro Gato con Maullido Fatal, el reto es exitoso.

•  Si revela un **segundo** Gato con Maullido Fatal (posible únicamente en la modalidad de 8 cartas), el reto falla normalmente y se aplica el castigo pendiente de 2.0.3.1.

•  Si el jugador no tiene vida extra, el primer Gato con Maullido Fatal falla el reto de manera ordinaria.

**Nota:** la vida extra **no es una carta**. No cuenta como vida para la eliminación (2.0.5): las vidas de un jugador son únicamente sus cartas, y la vida extra solo absorbe el primer Gato con Maullido Fatal que revele.

**Límites:**

•  Solo un jugador puede obtener la vida extra por partida, nunca más de uno.

•  No se acumula.

•  No puede regalarse ni transferirse a otro jugador.

•  No se guarda para partidas futuras, si la partida termina sin haberse usado, simplemente se pierde.

***2.0.5. Condiciones de Victoria y Eliminación***

•  **Eliminación:** Un jugador perderá automáticamente y quedará eliminado de forma definitiva de la partida en el momento en que su número de **vidas** sea igual a cero. Las vidas son las cartas del jugador, tanto las que están en su mano como las que están en su pila sobre la mesa. Esta condición se alcanza de forma progresiva cada vez que el jugador falla un reto al revelar un Gato con Maullido Fatal y pierde una carta; al perder su última vida, el sistema lo retirará de la contienda de manera inmediata e irreversible. Colocar cartas sobre la mesa no reduce las vidas: un jugador con la mano vacía pero con cartas en su pila sigue en la partida.

•  **Victoria:** Gana la partida el primer jugador que acumule los retos exitosos que exige su modalidad.

| Modalidad | Retos exitosos para ganar la partida |
| ----- | :---: |
| 4 cartas | 2 |
| 6 cartas | 2 |
| 8 cartas | 4 |

Una ronda es el ciclo que va desde que se colocan las cartas hasta que se resuelve un reto. Al terminar cada ronda, las cartas vuelven a las manos de sus dueños (salvo la carta eliminada por el castigo) y la mesa se reinicia. Las rondas no se ganan: se numeran de forma secuencial solo como referencia, e incluyen la ronda que se reinicia por la salida de un jugador (4.0.6). Lo único que cuenta para ganar son los retos exitosos acumulados por cada jugador, y ese conteo NO se reinicia en ningún momento de la partida.

**3\. Restricciones del Dominio de Juego**

| Identificador | Nombre de la Restricción | Descripción de la Restricción |
| :---: | :---- | :---- |
| **No. 1** | Exclusividad de Turno | Solo el jugador asignado en el turno actual tiene permitido realizar acciones (colocar carta, apostar, subir o retirarse). Cualquier entrada fuera del turno será rechazada. |
| **No. 2** | Integridad de Información | Las cartas en mano y en las pilas boca abajo deben permanecer ocultas para todos los jugadores excepto para el dueño. El sistema nunca envía información de cartas ocultas a otros jugadores. |
| **No. 3** | Inmutación de la Apuesta | Una vez que un jugador inicia o eleva una apuesta, esta no puede ser cancelada, modificada o revertida por ningún motivo, salvo que un jugador sea expulsado del grupo a mitad de la ronda (por desconexión no recuperada, por abandono o por votación): en ese caso la ronda en curso se reinicia y la apuesta abierta se anula (4.0.6). |
| **No. 4** | Condición de Eliminación | La eliminación de un jugador es un evento inmediato y permanente. Ocurre en el momento exacto en que sus vidas llegan a cero, es decir, cuando ya no le queda ninguna carta ni en su mano ni en su pila sobre la mesa, tras haber perdido progresivamente cada una por castigos de resoluciones fallidas frente a un Gato con Maullido Fatal. Tener la mano vacía no elimina a un jugador. No podrá reincorporarse en las rondas posteriores. |
| **No. 5** | Configuración de la partida | La configuración de la partida (cantidad de cartas y gatos) es inmodificable una vez que se inicia el juego. |
| **No. 6** | Validación de apuesta | El orden revelado de cartas es obligatorio: primero la pila del apostador, luego las pilas ajenas. Dentro de cada pila se voltea siempre la carta de arriba (la última colocada). La revelación se detiene inmediatamente al revelar un Gato con Maullido Fatal (salvo que el apostador posea la vida extra de la Regla 2.0.4.1, en cuyo caso el primer Gato se consume y la revelación continúa), determinando el fallo del reto, y se detiene también en el momento en que se alcanza el número prometido. |
| **No. 7** | Integridad de Fin de la partida | La partida termina automáticamente en el momento en que un jugador alcanza los retos exitosos que exige su modalidad (2 en las modalidades de 4 y 6 cartas, 4 en la modalidad de 8 cartas) o si queda un único jugador en pie tras la eliminación de todos los demás. Una vez que se alcanza el número de cartas apostadas y el reto es exitoso, la resolución debe terminar inmediatamente. Prohíbe explícitamente seguir volteando cartas una vez cumplida la apuesta. Esto evita la "codicia" que arruinaría la victoria del jugador. |
| **No. 8** | Validación de Apuesta | Una apuesta debe ser mayor a 0 y menor o igual al menor entre: el total de cartas boca abajo en la mesa y el número de cartas de la modalidad (4, 6 u 8). Se debe deshabilitar visualmente cualquier valor fuera de este rango. |
| **No. 9a** | Invariante de lobby | La partida sólo puede iniciar si hay al menos 3 jugadores en el lobby y todos están en estado Listo. |
| **No. 9b** | Invariante de partida activa | Una nueva ronda sólo puede iniciar si hay al menos 2 jugadores activos, entendiendo por activo al jugador con al menos 1 vida. Si queda un único jugador activo, la partida termina inmediatamente con su victoria (concordante con No. 7 y RF-38). |
| **No. 10** | Expulsión por votación | Un jugador activo podrá ser expulsado del grupo si y solo si se alcanza un consenso de votos a favor, donde todos los demás jugadores excepto el infractor deben votar por su expulsión del grupo, ya sea en el lobby o durante la partida. |
| **No. 11** | Reinicio de confirmación por cambio de modalidad de juego para iniciar juego | Cualquier alteración de la modalidad de juego en el Lobby, transicionará el estado de un jugador “Listo” a “No Listo”. La condición de inicio de juego requiere que todos los jugadores estén en “Listo” para iniciar el juego. |
| **No. 12** | Bloqueo de un jugador | No se permitirá el acceso a una sala a un jugador si existe un bloqueo activo con cualquiera de los jugadores presentes en la sala, sin importar quién bloqueó a quién (el bloqueo es bidireccional en el acceso a salas). |
| **No. 13** | Límite de colocación de cartas | Un jugador sólo puede colocar una carta por turno, tomada de su mano, y no puede colocar más cartas de las que tiene en ella (o 0 si decide abrir apuesta). El sistema debe deshabilitar la acción de colocar si el jugador no tiene cartas en su mano; en ese caso, su única acción es abrir una apuesta de exactamente 1 (2.0.1). Tener la mano vacía no implica eliminación (No. 4). |
| **No. 14** | Acceso a salas por invitación | No existen salas públicas ni códigos de sala. Un jugador sólo puede unirse a una sala si fue invitado por un jugador presente en ella que forme parte de su lista de amigos. La amistad no necesita ser transitiva: dos jugadores sin relación de amistad pueden coincidir en la misma sala si cada uno fue invitado por un amigo suyo. Los jugadores invitados constituyen la única excepción al requisito de amistad previa: un jugador registrado puede enviarles una solicitud de amistad por su nickname generado o su número de jugador, y una vez aceptada puede invitarlos a la sala. El invitado nunca puede iniciar esa relación, sólo aceptarla. |
| **No. 15** | Integridad del castigo pendiente | Mientras exista un castigo pendiente (2.0.3.1) no se admite ninguna acción de juego. Solo quien debe elegir puede elegir, solo una vez y solo entre las cartas del castigado. Las cartas del castigado nunca se revelan a quien elige a ciegas, y el tipo de la carta eliminada solo lo conoce el castigado. |

***3.0.1. Reglas para jugadores no registrados o invitados:***

Los jugadores invitados pueden unirse a una sala, jugar una partida completa y ganarla siguiendo las mismas reglas de *The Last Meow*, sin necesidad de crear una cuenta, proporcionar correo, contraseña ni iniciar sesión. Sin embargo, presentan las siguientes restricciones de alcance:

•  **Identidad y Acceso:** No tienen perfil editable ni credenciales de acceso. Su nickname es fijo y generado automáticamente por el sistema con el formato **"Jugador N"**, acompañado de un número de jugador único para la sesión. Se les invita a las salas mediante ese nickname generado o su número de jugador.

•  **Relaciones y Amistades:** Un jugador invitado SÍ dispone de una lista de amigos durante su sesión, pero:

○  NO puede enviar solicitudes de amistad ni agregar a nadie por iniciativa propia.

○  SÍ puede recibir y aceptar solicitudes de amistad.

○  SÍ puede recibir y aceptar invitaciones a una sala.

○  Toda esa relación es temporal: existe únicamente durante su sesión y desaparece al cerrarla, sin quedar registrada en ninguna parte.

•  **Persistencia y Puntuación:** Sus victorias y puntos de ronda NO se almacenan al terminar la sesión. No cuentan con historial permanente de partidas jugadas ni aparecen en el *leaderboard* de jugadores registrados. Si un jugador invitado gana una partida, aparecerá en la primera posición de la tabla de resultados, pero se le otorgarán **0 puntos**.

•  **Comunicación:** Tienen estrictamente prohibido el uso del chat colaborativo (tanto en lobby como en partida).

**4\. Sistema de Comunicación y Moderación**

Como parte de los servicios del sistema de *The Last Meow*, se implementará un entorno de comunicación y un sistema de moderación para garantizar la integridad de la comunidad y el respeto entre los usuarios.

***4.0.1. Chat Colaborativo:***

Los jugadores tendrán a su disposición un chat colaborativo. Este chat estará activo en:

•  **El Lobby (Sala de espera):** Para interactuar y coordinarse antes de que todos los jugadores confirmen su estado “Listo” e inicie la partida automáticamente (ver 1.1.3).

•  **Durante la partida:** Para fomentar la interacción, las apuestas y la dinámica de engaño (bluffing) propia del juego.

***4.0.2. Sistema de Sanciones (Baneos):***

Para penalizar las infracciones, el sistema de moderación registrará el estado de la cuenta de los infractores aplicando las siguientes medidas:

•  **Expulsión (Kick):** Remoción inmediata de un jugador de la sala de espera o de la partida en curso. (Ver punto 4.0.4.).

•  **Baneo Temporal:** Si un jugador acumula una cantidad de **tres reportes** por parte de otros usuarios, el sistema cambiará el estado de su cuenta a "suspendida", impidiéndole iniciar sesión y jugar por un periodo de tiempo establecido de **tres días**.

•  **Baneo Permanente:** A la quinta suspensión acumulada, la cuenta pasa a estado 'Baneada' de forma permanente.

Las sanciones aplicadas por el sistema son completamente automáticas a partir de la acumulación de 3 reportes válidos. En esta versión del sistema no existe un proceso de apelación ni de revisión humana de sanciones, asumiéndose como un riesgo aceptado de diseño (ver Q-07 del catálogo de preguntas del Hito 1).

*Nota:* A la quinta suspensión acumulada, la cuenta pasa a estado 'Baneada' de forma permanente.

***4.0.3. Mecánica de Expulsión por Votación en Partida:***

Con el fin de salvaguardar un entorno justo y libre de toxicidad, el sistema cuenta con un protocolo de expulsión directa:

•  **Causas válidas:** Un jugador puede ser sometido a votación de expulsión por:

**1\. Conducta inapropiada o toxicidad:** Hostigamiento, faltas de respeto o lenguaje obsceno reiterado a través del chat colaborativo.

**2\. Abuso de mecánicas:** Aprovechamiento malintencionado de vulnerabilidades lógicas, bloqueos intencionales o retraso abusivo del juego.

•  **Criterio de aprobación:** Para hacer efectiva la expulsión inmediata de la partida, se requiere que todos los jugadores presentes menos uno emitan un voto de expulsión contra el jugador en cuestión.

•  **Efecto:** Al alcanzarse el número requerido de jugadores que deben votar, el jugador infractor será expulsado de la partida de forma automática. La expulsión queda registrada en el historial del jugador por trazabilidad, pero NO suma al contador de reportes ni puede producir una suspensión por sí misma. Si además hubo conducta sancionable, cualquier jugador debe reportar al infractor de forma explícita para que cuente hacia el sistema de sanciones.

***4.0.4. ¿Cómo reportar o bloquear?:***

El **menú de interacción entre jugadores**, accesible mediante **clic derecho sobre el avatar o el nombre del jugador**, mantendrá habilitadas las opciones de **“Reportar”** y **“Bloquear”** en todo momento:

•  **En el lobby:** Permite mitigar la toxicidad temprana en el chat previo al juego y penalizar conductas obstructivas (como el lobby hostage, donde un jugador se rehúsa intencionalmente a marcar “Listo” para congelar la sala).

•  **Durante el juego:** Permite sancionar en tiempo real faltas al chat, actitudes antideportivas o abuso comprobado de mecánicas.

***4.0.5. Bloqueo de Jugadores:***

El bloqueo es una medida unilateral de protección. Cuando un jugador A bloquea a un jugador B:

**1\.** B no puede unirse a una sala donde esté A.

**2\.** A tampoco puede unirse a una sala donde esté B. El bloqueo impide la coincidencia en ambos sentidos, aunque sólo A lo haya solicitado.

**3\.** B no puede enviar solicitudes de amistad a A.

**4\.** B no puede invitar a A a una sala.

**5\.** A conserva todas sus capacidades sobre B, ya que fue quien bloqueó.

**6\.** Si existía una amistad entre ambos, se conserva en el sistema pero deja de listarse en la pestaña "Amigos" y pasa a listarse en "Bloqueados" mientras el bloqueo esté activo.

**7\.** El bloqueo NO tiene efecto retroactivo: si ambos ya se encuentran en la misma sala cuando A bloquea a B, ambos permanecen en la sala hasta que alguno salga o termine la partida. El bloqueo surte efecto para futuros intentos de coincidir.

**8\.** Si A desbloquea a B: la amistad previa (si existía) vuelve a listarse con normalidad en "Amigos", se eliminan las restricciones de sala y se permite la interacción mutua.

Al denegar el acceso a una sala por efecto de un bloqueo, el sistema muestra de forma neutra y sin revelar motivos: "No puedes unirte a esta sala."

***4.0.6. Protocolo de Abandono y Desconexión:***

•  **Desconexión involuntaria:** Si un jugador pierde la conexión a la red durante el juego, se mantendrá una pausa preventiva durante un lapso de 15 segundos. Si el tiempo expira sin reconexión exitosa, el sistema procederá a expulsarlo del grupo para no entorpecer el avance de la partida.

•  **Partida Abandonada por Inactividad:** A fin de no saturar los recursos del servidor con partidas abandonadas en las que no hay límite de tiempo por acción individual, si la sala acumula 10 minutos continuos sin jugadas registradas, el juego se cerrará automáticamente para todos los jugadores sin penalización.

•  **Desconexión durante un castigo pendiente:** si quien debe elegir pierde la conexión mientras hay un castigo pendiente, rige el tiempo límite de 10 segundos de 2.0.3.1: si no se reconecta y elige a tiempo, el sistema elige al azar. Si es el jugador castigado quien se desconecta, se aplica la pausa preventiva de 15 segundos ya descrita.

•  **Reconexión a tiempo (jugador registrado):** si el jugador pierde la conexión por un error externo, vuelve a iniciar sesión y regresa dentro de los 15 segundos, continúa con normalidad, con sus cartas y su lugar en el orden de turnos. La partida estuvo en pausa durante ese lapso y los demás jugadores ven una pantalla de espera.

•  **Reconexión tardía:** si el jugador regresa después de los 15 segundos, ya fue expulsado: vuelve al **menú principal** y no se reincorpora a la sala ni a la partida.

•  **Jugador invitado:** solo puede reconectarse mientras su sesión siga viva. Un invitado no conserva información: si pierde la sesión, se considera que dejó la partida y, al volver a entrar, es un jugador invitado nuevo con un número nuevo (por ejemplo, si era Jugador 3, ahora puede ser Jugador 4). No recupera su lugar.

•  **Salida voluntaria:** el jugador que sale con el botón de salir regresa al menú principal y se trata como abandono inmediato, sin pausa de 15 segundos: recibe 0 puntos (5.0.2) y se aplica el reinicio de la ronda en curso descrito más abajo.

•  **Desconexión en la sala de espera:** si un jugador pierde la conexión antes de iniciar la partida, solo ese jugador sale de la sala; la sala continúa mientras tenga al menos un jugador dentro. Para volver, debe iniciar sesión de nuevo y ser invitado otra vez, y al entrar ocupa el último lugar del orden.

•  **Efecto en la ronda en curso:** si un jugador es expulsado del grupo (porque no se reconectó a tiempo, por abandono o por votación, No. 10\) mientras hay una ronda en curso, sale por completo de la partida: sus cartas, su turno y su puntuación dejan de existir. La **ronda en curso se reinicia**: las cartas que los demás habían colocado regresan a sus manos, se anula cualquier apuesta abierta y la mesa queda vacía. La ronda reiniciada cuenta como una ronda nueva en el contador de rondas. Después comienza una nueva ronda según 2.0.2, punto 4\. Solo se reinicia la ronda en curso: los retos ya resueltos, las vidas y los retos exitosos de los demás jugadores no cambian. Si ya existía un castigo pendiente, se aplica 2.0.3.1, porque el reto ya estaba resuelto. Si tras la salida queda un único jugador activo, la partida termina con su victoria (No. 9b).

•  **Efecto del cierre por inactividad:** cuando el juego se cierra por inactividad no hay ganador, no se reparten puntos y ningún jugador recibe penalización. Que un jugador se desconecte y sea expulsado del grupo no detiene la partida: los demás continúan jugando, y esa persona queda con 0 puntos (5.0.2).

**5\. Sistema de Puntaje**

Los puntos se otorgan al terminar la partida y son lo único que persiste en el perfil y marcador del jugador.

***5.0.1. Cálculo de Puntos:***

•  **Puntos del ganador (1.er lugar):**

*Puntos \= Base por número de jugadores \+ Bono por modalidad*

•  **Base por número de jugadores:**

○  3 jugadores: **2 puntos**

○  4 jugadores: **3 puntos**

○  5 jugadores: **4 puntos**

○  6 jugadores: **5 puntos**

•  **Bono por modalidad:**

○  Ganador: 4 cartas: **\+1 punto**; 6 cartas: **\+2 puntos**; 8 cartas: **\+3 puntos**.

○  No ganadores: 4 cartas: **0 puntos**; 6 cartas: **\+1 punto**; 8 cartas: **\+2 puntos**.

•  **Puntos de no ganadores:** La base desciende 1 punto por cada posición posterior al primer lugar (2.º lugar \= Base \- 1, 3.er lugar \= Base \- 2, y así sucesivamente). El bono de no ganador correspondiente a la modalidad se suma después al valor resultante de la base.

•  **Límite inferior:** El total de puntos obtenidos nunca baja de 0 (no existen puntos negativos).

•  **Número de jugadores para la base:** la base se calcula con el número de jugadores que **no abandonaron ni fueron expulsados** al terminar la partida, es decir: el ganador, los sobrevivientes y los eliminados por quedarse sin cartas. Quienes abandonaron o fueron expulsados no se cuentan. Ejemplo: se inicia con 5 jugadores, uno se desconecta y es expulsado; la base es la de 4 jugadores (3 puntos).

•  **Mínimo de la base:** si, al descontar a quienes abandonaron o fueron expulsados, quedan menos de 3 jugadores contados (es decir, 2), la base es la de 3 jugadores: **2 puntos**. En general, la base es igual al número de jugadores contados menos 1 (con mínimo de 2), de modo que los puntos de las posiciones contadas nunca se repiten.

***5.0.2. Casos Particulares:***

•  **Jugador eliminado:** Un jugador eliminado por quedarse sin cartas SÍ recibe los puntos correspondientes a su posición final en la tabla de resultados.

•  **Abandono de partida:** Un jugador que abandona la partida, o que es expulsado del grupo por desconexión (4.0.6), recibe **0 puntos**. La partida continúa para los demás y esa persona ocupa una de las últimas posiciones de la tabla (5.0.4).

•  **Expulsión por votación:** Un jugador expulsado por votación recibe **0 puntos** y ocupa una de las últimas posiciones de la tabla (5.0.4).

•  **Jugador invitado:** Un jugador invitado recibe **0 puntos siempre**, aunque gane la partida.

•  **Cierre por inactividad:** Si la partida se cierra por inactividad (4.0.6) no hay ganador ni tabla de resultados, y nadie recibe puntos.

***5.0.3. Tablas de Ejemplo:***

**Ejemplo 1: 3 jugadores en modo 4 cartas (Base \= 2; Bono ganador \= \+1; Bono no ganador \= 0\)**

| Posición | Jugador | Cálculo | Total de puntos |
| :---: | :---- | :---- | :---: |
| 1.º | Ganador | Base (2) \+ Bono (1) | **3** |
| 2.º | Segundo | Base \- 1 (1) \+ Bono (0) | **1** |
| 3.º | Tercero | Base \- 2 (0) \+ Bono (0) | **0** |

*(Distribución: 3 / 1 / 0\)*

**Ejemplo 2: 6 jugadores en modo 8 cartas (Base \= 5; Bono ganador \= \+3; Bono no ganador \= \+2)**

| Posición | Jugador | Cálculo | Total de puntos |
| :---: | :---- | :---- | :---: |
| 1.º | Ganador | Base (5) \+ Bono (3) | **8** |
| 2.º | Segundo | Base \- 1 (4) \+ Bono (2) | **6** |
| 3.º | Tercero | Base \- 2 (3) \+ Bono (2) | **5** |
| 4.º | Cuarto | Base \- 3 (2) \+ Bono (2) | **4** |
| 5.º | Quinto | Base \- 4 (1) \+ Bono (2) | **3** |
| 6.º | Sexto | Base \- 5 (0) \+ Bono (2) | **2** |

*(Distribución: 8 / 6 / 5 / 4 / 3 / 2\)*

***5.0.4. Posición final en la tabla de resultados:***

Las posiciones se asignan siempre de forma que **no hay empates**: cada jugador ocupa una posición distinta. Se ordenan así:

**1\. 1.er lugar:** el ganador de la partida (2.0.5 y No. 7).

**2\. Jugadores que siguen en la partida al terminar** (la partida terminó porque otro jugador alcanzó los retos exitosos de su modalidad). Se ordenan por:

○  Más retos exitosos acumulados.

○  Si empatan, más vidas.

○  Si siguen empatando, quien alcanzó **primero** su número actual de retos exitosos. Como solo se resuelve un reto por ronda, dos jugadores nunca lo alcanzan en la misma ronda.

○  Si aún no se decide (por ejemplo, ninguno ha tenido retos exitosos y tienen las mismas vidas), queda mejor posicionado quien aparece antes en el orden de turnos (2.0.2).

**3\. Jugadores eliminados por quedarse sin vidas:** quien fue eliminado más tarde queda mejor posicionado. Nunca se elimina a dos jugadores en la misma ronda, así que el orden es único.

**4\. Jugadores que abandonaron o fueron expulsados** (0 puntos): ocupan las últimas posiciones. Quien salió más tarde queda mejor posicionado. Los jugadores que estaban por debajo de quien se fue suben una posición. Ejemplo con cuatro jugadores: si quien iba 3.º se desconecta, el que iba 4.º pasa a ser 3.º y el que se fue pasa a ser 4.º. Estos jugadores no cuentan para el número de jugadores de la base (5.0.1).

Una vez asignadas las posiciones, los puntos se calculan según 5.0.1.

***5.0.5. Orden en el marcador (leaderboard):***

El marcador ordena a los jugadores registrados por sus puntos acumulados, de mayor a menor. Si dos jugadores tienen el mismo total, queda por encima quien llegó primero a ese total, y quien llega después con el mismo total se coloca en la posición siguiente. Ejemplo: A llegó a 40 puntos y ocupa el 5.º lugar; cuando B también llega a 40 puntos, B ocupa el 6.º lugar. Los jugadores invitados no aparecen en el marcador.