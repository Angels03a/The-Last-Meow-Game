# The Last Meow · Dominio del juego

Documento de dominio basado en `Reglas_de_The_Last_Meow_V3` y en el borrador anterior de `Dominio del juego.md`.

Vocabulario fijo del dominio:

- **Mano:** cartas que el jugador todavía no ha colocado.
- **Pila:** cartas que el jugador ha colocado boca abajo en la mesa. La última colocada queda arriba.
- **Vidas:** cartas que posee el jugador, es decir, mano más pila. Solo bajan por un castigo. Colocar una carta no
  baja las vidas.
- **Vida extra:** beneficio del minijuego del gatito fugitivo. No es una carta y no cuenta como vida.
- **Gato (Maullido Fatal):** carta peligrosa. El resto son cartas seguras.
- **Retos exitosos:** lo único que cuenta para ganar. Las rondas no se ganan.
- **Ronda:** ciclo que va desde que se colocan las cartas hasta que se resuelve un reto. Se numera de forma
  secuencial solo como referencia, e incluye la ronda que se reinicia por la salida de un jugador.
- **Castigo pendiente:** estado entre un reto fallido y la eliminación de la carta perdida.

---

## 1. Precondiciones y estado inicial (SALA / lobby)

- Mínimo **3** jugadores en la SALA, todos en estado **LISTO**, para lanzar el contador de inicio.
- Máximo **6** jugadores.
- Con todos en LISTO se muestra la pantalla de contador de **3 segundos**.
  - Durante los 3 segundos, cualquier jugador puede dar clic en **Cancelar**.
  - Quien da clic en Cancelar pasa a **NO LISTO**. Los demás siguen en LISTO.
  - Al pasar a NO LISTO cualquier jugador, el contador se cancela de inmediato.
  - **Durante el contador no se puede modificar la cantidad de cartas.**
- Acceso a la sala **solo por invitación** de un amigo que ya esté dentro. No hay salas públicas ni códigos.
- Un jugador bloqueado (en cualquier sentido) por alguien de la sala no puede entrar. El sistema solo muestra
  "No puedes unirte a esta sala."

### Acciones permitidas dentro de la SALA

- **Mensajes**
  - Chat de la sala entre los jugadores presentes, con el nickname de quien escribe.
  - Los jugadores **invitados no pueden usar el chat** (ni en la sala ni en la partida).
- **Amigos**
  - Invitar amigos a unirse a la SALA.
  - Agregar amigos por nickname.
  - Aceptar o rechazar solicitudes de amistad.
  - Ver las solicitudes aún no confirmadas y cancelar las que uno envió.
  - Un invitado solo puede **recibir y aceptar** solicitudes e invitaciones, nunca iniciarlas.
- **Cartas (modalidad)**
  - Cualquier jugador puede cambiar la cantidad de cartas.
    - **4 cartas:** 1 Gato fatal + 3 seguras. Juego normal. Gana con **2** retos exitosos.
    - **6 cartas:** 1 Gato fatal + 5 seguras. Juego normal. Gana con **2** retos exitosos.
    - **8 cartas:** 2 Gatos fatales + 6 seguras. Juego extra. Gana con **4** retos exitosos.
  - Al cambiar la cantidad de cartas, **todos pasan a NO LISTO**. Todos deben volver a dar clic en LISTO.
  - Una vez iniciado el contador de inicio, no se puede modificar la cantidad de cartas.
- **Moderación** (clic derecho sobre un jugador): Reportar y Bloquear siempre disponibles. La expulsión por votación
  requiere el voto de todos los jugadores menos el infractor.

### Gatito fugitivo (solo con 8 cartas seleccionadas)

- Solo ocurre cuando la modalidad elegida es de **8 cartas**. Con 4 o 6 cartas aparece la pantalla de carga normal.
- Se muestra cuando **todos están en LISTO** (y pasa el contador), en lugar de la pantalla de carga normal.
- Dura 15 segundos. Todos ven correr al gatito y cualquiera puede intentar atraparlo con un toque.
- **Solo hay una oportunidad.** Si alguien lo atrapa, desaparece para todos. El servidor decide quién fue primero.
- Quien lo atrapa recibe **1 vida extra** válida solo en esa partida. Si nadie lo atrapa, nadie recibe nada.
- No se acumula, no se regala, no se guarda para otra partida.

### Alternativas del lobby

- **1 o 2 jugadores en la SALA:** la sala se mantiene abierta esperando más jugadores. Se pueden realizar las mismas
  acciones (mensajes, amigos, cartas), excepto iniciar la partida.
- **Un jugador pierde la conexión en la SALA:** solo ese jugador sale. La sala sigue mientras tenga alguien dentro.
  Para volver debe iniciar sesión de nuevo y ser invitado otra vez. Entra al **final** del orden de turnos.
- **Orden de turnos:** es el orden en que el servidor registró el ingreso de cada jugador a la sala. Se congela al
  iniciar la partida. El primer jugador en la sala tiene el primer turno.

---

## 2. Sugerencias de diseño

![[Panel izquierdo.png|388]]

### 2.1. Se agrega

- Botón **Opciones** en el panel izquierdo con:
  - Selección de idioma (es-MX / en-US, cambio en caliente).
  - Audio y sonido.

### 2.2. NO se incluye (no debe ir)

- **Botón Rendirse.** No existe en el reglamento. Ya existen **Retirarse** (dentro de una apuesta) y **Salir**
  (abandono). Tener un tercero duplicaría las formas de salir.
- **Botón Revelar cartas.** La revelación la dirige el servidor: el apostador solo elige de qué jugador voltear.

---

## 3. Juego a nivel de interfaz

![[Dominio del juego.png|720]]

La pantalla de juego se divide en 5 zonas.

### 3.1. Derecha: chat de la partida

- Permite enviar y recibir mensajes con la misma presentación que el chat de la SALA, más pequeño y lateral.
- El chat de la SALA y el del juego son **distintos**. Al pasar de SALA a juego, el chat del juego inicia en blanco.
- Los jugadores invitados no pueden escribir ni usar este chat.

### 3.2. Inferior: baraja del jugador

- La cantidad de cartas de cada jugador al inicio es igual a la modalidad (4, 6 u 8). Esa cantidad son sus **vidas**.
- **Lado izquierdo:** avatar (por defecto o el de Mi Perfil) y nombre (el del login o el modificado en Mi Perfil).
  - Junto al avatar, un número pequeño con las **vidas** del jugador (mano más pila). **Solo baja por un castigo.**
    - Colocar una carta en la mesa no cambia este número.
    - Aparte, la mano muestra cuántas cartas quedan **en la mano**, que sí baja al colocar y vuelve a subir al
      terminar la ronda.
- **Lado derecho: controles de apuesta**
  - Contador de cartas a apostar. Valores válidos: de **1** hasta el **menor** entre las cartas boca abajo en la
    mesa y la cantidad de la modalidad (4, 6 u 8). Cualquier valor fuera de ese rango está deshabilitado (No. 8). El
    0 no existe.
  - Al subir una apuesta solo se permiten valores mayores que la apuesta actual.
  - **Mano vacía:** si el jugador no tiene cartas en la mano, el contador queda fijo en **1** y la única acción
    disponible es apostar.
  - **Botón Apostar:** abre la apuesta o la sube. Está ligado al contador. Para abrirla, el jugador debe haber
    colocado al menos una carta propia en la ronda.
  - **Botón Retirarse:** solo aparece durante una ronda de apuestas.
  - Los botones solo están activos en el turno del jugador (No. 1).
- **Colocar una carta:** un clic sobre la carta de la mano. Animación de traslación desde el panel inferior hacia su
  pila en el centro de la mesa, boca abajo.

### 3.3. Superior: datos de la partida

- Nombre del juego (español e inglés).
- **Ronda actual**, numerada de forma secuencial. Una ronda reiniciada por la salida de un jugador cuenta como una
  ronda nueva.
- Nickname de cada jugador, sus **vidas** y sus **retos exitosos**.
- Opción de **Salir** (equivale a abandono inmediato, ver 5.2).

### 3.4. Inferior derecho: debajo del chat y a la derecha de la baraja

- Sin botones por ahora. Queda reservado.

### 3.5. Central: juego y experiencia

- **Experiencia visual**
  - Animación de colocar una carta boca abajo en la mesa.
  - La carta que se pierde sale de la baraja con animación.
  - Cuando termina una ronda, **todas** las cartas jugadas regresan con animación a las manos de sus dueños.
  - Ventana superpuesta (pop-up) del castigo: cartas **boca abajo** y sin conocer el tipo, salvo el castigado cuando
    elige sobre su propio Gato.
  - Animación de la revelación de cartas y del Maullido Fatal.
  - Desplazamientos y animaciones del gatito fugitivo.
  - Pantalla de espera ("esperando a jugador X") durante la pausa de reconexión de 15 segundos.
- **Lógica del juego:** constituida por las reglas V3 del reglamento, adaptación digital de Skull. El servidor es
  autoritativo: arbitra turnos, apuestas, castigos y cartas. El cliente solo muestra y envía intenciones.

---

## 4. Lógica pura (reglas que el motor debe cumplir)

### 4.1. Estado de un jugador

| Dato | Descripción |
|---|---|
| Mano | Cartas aún no colocadas. Solo el dueño las ve. |
| Pila | Cartas colocadas, boca abajo. Se apilan: la última queda arriba. Solo el dueño conoce su contenido. |
| Vidas | Mano + pila. Llegan a 0 → eliminado. |
| Retos exitosos | Se acumulan y nunca se reinician. |
| Vida extra | Verdadero/falso, solo modalidad 8, la consume el primer Gato revelado. |
| Tipo de carta | Seguro o Gato. Viaja al cliente solo para las cartas del propio jugador o las ya reveladas. |

### 4.2. Flujo de una ronda

1. **Colocación (por turnos).** Cada turno, el jugador activo elige **una** de dos acciones: colocar una carta de su
   mano o abrir una apuesta (solo si ya colocó al menos una carta propia en la ronda). Con la mano vacía solo puede abrir una apuesta de **exactamente 1**. No hay turno
   pasivo.
2. **Apuesta.** Quien abre declara un número de 1 hasta min(cartas en mesa, modalidad). Nadie más coloca. En orden de
   turno, cada jugador **sube** o se **retira**. El que se retira no vuelve a esa apuesta. La apuesta no se cancela
   (No. 3), salvo por la salida de un jugador a mitad de ronda.
3. **Resolución.** Queda un solo jugador con la apuesta más alta. Voltea:
   - **a)** Primero su propia pila, de arriba hacia abajo.
   - **b)** Después elige de qué jugador voltear, siempre la carta de arriba de esa pila. La elección se hace
     **antes de cada volteo** (puede alternar o repetir pila). Tras voltear, la siguiente carta sin voltear pasa a
     ser la de arriba. Elegir una pila sin cartas por voltear se rechaza con `ErrorKey` y no cambia el estado.
   - **c)** Se detiene al llegar al número prometido (éxito) o al revelar un Gato (fallo). Con vida extra, el
     primer Gato (propio o ajeno) la consume, **cuenta como una de las cartas prometidas** y la revelación continúa.
4. **Éxito:** +1 reto exitoso. **Todas** las cartas regresan a sus dueños. Si llega a la cantidad de la modalidad,
   **gana al instante** (No. 7).
5. **Fallo:** las cartas regresan a sus dueños y se abre el **castigo pendiente**. Elige la carta que se pierde:
   - el dueño de la pila donde estaba el Gato, **a ciegas**, si el Gato era ajeno;
   - el propio apostador, **viendo sus cartas**, si el Gato era suyo;
   - el sistema, **al azar** (misma probabilidad para cada carta del castigado, incluido el Gato), si pasan 10
     segundos o quien debe elegir fue expulsado;
   - si el castigado tiene 1 sola vida, no hay elección y queda eliminado de inmediato;
   - si quien elige se desconecta, sus 10 segundos siguen corriendo. Si se desconecta el castigado, el castigo se
     suspende durante la pausa de 15 segundos y, si no regresa, se cancela;
   - si alguien sale durante un castigo pendiente, la ronda **no** se reinicia: se resuelve o cancela el castigo y
     empieza la siguiente (el contador de rondas sube una vez).
6. **Siguiente ronda.** Si el juego no terminó, empieza el **primer jugador activo del orden fijo**.

### 4.3. Fin de la partida y orden final

- Gana quien llega primero a sus retos exitosos, o el último jugador que queda activo.
- **Posiciones sin empates**, en este orden:
  1. Ganador.
  2. Sobrevivientes por: más retos exitosos, más vidas, quien llegó antes a su número de retos y, por último, orden
     de turnos.
  3. Eliminados por quedarse sin vidas: el eliminado más tarde queda mejor.
  4. Quienes abandonaron o fueron expulsados (0 puntos): al final. Quien salió más tarde queda mejor. Si dos salidas
     coinciden, queda mejor quien comenzó su desconexión más tarde y, si aún empatan, quien va antes en el orden de
     turnos.
- Un **jugador eliminado** pasa a ser espectador con la vista pública (nunca ve cartas ocultas) y puede salir sin
  penalización. Si sale después de eliminado, **conserva** los puntos de su posición.
- Un **jugador invitado** ocupa su posición en la tabla, aunque cobre 0. Los demás conservan la suya.
- Si todos los demás abandonan, el jugador restante gana de inmediato y recibe los puntos normales de ganador.

### 4.4. Escenarios

Convención: se juega la modalidad de 4 cartas (3 seguras + 1 Gato por jugador). Orden de turnos **C, B, A** (C creó
la sala e invitó a B, B invitó a A). Una carta segura se escribe `S` y el Gato `G`.

#### Escenario 1. Eliminación del jugador A (castigo ciego y propio)

**Ronda 1: A pierde contra el Gato de B (A queda con 3 vidas)**

1. T1 C coloca `S`. T2 B coloca `S`. T3 A coloca `S`. T4 C coloca `S`.
2. T5 B coloca `G` (queda arriba de su pila: `S`, `G`).
3. T6 A abre una apuesta de **2** (hay 5 cartas en mesa; 2 ≤ min(5, 4)). C y B se retiran.
4. Resolución: A voltea su propia carta de arriba (`S`, lleva 1). Elige la pila de B y voltea su carta de arriba: `G`.
   El reto **falla**. La revelación se detiene.
5. Todas las cartas regresan a sus manos. Castigo pendiente: elige **B** (dueño del Gato) y ve las **4 cartas de A**
   boca abajo, en orden aleatorio. Elige una y se elimina una `S`.
6. A ve que perdió una `S`. C y B solo ven que A tiene una vida menos.

Estado: A 3 vidas (2 `S`, 1 `G`). B y C, 4 vidas.

**Ronda 2: A pierde con su propio Gato (A queda con 2 vidas)**

Empieza **C** (primer jugador activo del orden), no A.

1. C `S`, B `S`, A coloca `G`, C `S`, B `S`.
2. A abre una apuesta de **2**, esperando que alguien suba. C y B se retiran.
3. A voltea su carta de arriba: `G`. Falla.
4. Castigo pendiente: elige **A viendo sus 3 cartas** (todas regresaron a su mano). Decide eliminar una `S` y
   conservar su Gato. B y C solo ven "A está eligiendo".

Estado: A 2 vidas (1 `S`, 1 `G`).

**Ronda 3: A pierde contra el Gato de C (A queda con 1 vida)**

1. C `S`, B `S`, A coloca su única `S` (su mano queda con `G`), C coloca `G`, B `S`.
2. A abre una apuesta de **2**. C y B se retiran.
3. A voltea su carta de arriba (`S`) y después elige a C: su carta de arriba es `G`. Falla.
4. Castigo pendiente: elige **C**, a ciegas, entre las 2 cartas de A. Elimina la `S`.
5. A ve que perdió la `S` y que conserva el `G`.

Estado: A 1 vida (`G`).

**Ronda 4: eliminación de A**

1. C `S`, B `S`, A coloca `G` (su mano queda **vacía**, sigue vivo con 1 vida en la mesa), C `S`, B `S`.
2. En el turno siguiente de A, solo puede abrir una apuesta de **exactamente 1**. C y B se retiran.
3. A voltea su carta de arriba: `G`. Falla.
4. El castigado tiene 1 vida: **no hay pop-up**, la carta se elimina y A queda eliminado.
5. Quedan C y B (2 jugadores activos, la partida continúa). La ronda 5 empieza con **C**, el primero del orden.

Posición de A: 3.º. Los puntos se calculan al terminar la partida.

#### Escenario 2. Victoria por retos exitosos (3 jugadores, modalidad de 4)

Orden A, B, C. Se necesitan **2** retos exitosos.

1. Ronda 1: A `S`, B `S`, C `S`, A abre una apuesta de **3** (hay 3 cartas en mesa). B y C se retiran.
2. A voltea su carta de arriba (`S`), elige a B (`S`) y a C (`S`). Llega a 3 sin Gatos: **éxito**. A tiene 1 reto.
   Las cartas regresan y nadie pierde vidas.
3. Ronda 2: empieza A (primer jugador). Repite la apuesta de 3 y vuelve a cumplirla. A llega a 2 retos.
4. La resolución termina **en el instante** de cumplir la apuesta. La partida termina.
5. Tabla: A 1.º. B y C siguen vivos, con 0 retos y 4 vidas. Gana el desempate el que aparece antes en el orden de
   turnos: B 2.º, C 3.º.
6. Puntos (3 jugadores, modo 4): A = 2 + 1 = **3**, B = 2 − 1 + 0 = **1**, C = 2 − 2 + 0 = **0**.

#### Escenario 3. Rondas de apuesta con subidas (4 jugadores)

Orden A, B, C, D.

1. Tras varias colocaciones hay 8 cartas en mesa. A abre una apuesta de **2**.
2. B sube a **3**. C se retira. D se retira. A debe subir o retirarse: se retira.
3. Solo queda B con la apuesta más alta (**3**). B debe cumplirla: voltea su propia pila de arriba hacia abajo y, si
   falta, elige de qué jugador voltear la carta de arriba.
4. A y C ya no pueden volver a esa apuesta. Cualquier valor mayor a min(8, 4) = 4 está deshabilitado.

#### Escenario 4. Vida extra (modalidad de 8 cartas)

1. Con 8 cartas seleccionadas y todos en LISTO, todos ven el gatito fugitivo 15 segundos. **A** lo atrapa primero:
   recibe 1 vida extra.
2. En una ronda, A apuesta **3** con la pila `S`, `G`, `G` (arriba el segundo Gato).
3. A voltea su carta de arriba: `G`. **Primer Gato:** consume la vida extra y no hay fallo. Cuenta como 1 de las 3
   cartas prometidas. La revelación continúa.
4. A voltea la siguiente: `G`. **Segundo Gato:** el reto falla y se abre el castigo pendiente (propio, con cartas a
   la vista).
5. La vida extra ya no existe para el resto de la partida. No cuenta como vida.

#### Escenario 5. Desconexión de un jugador a mitad de ronda

Orden A, B, C, D. Es la ronda 6. A abrió una apuesta de 2 y B sube a 3.

1. La conexión de **D** se cae. La partida se pausa 15 segundos y los demás ven "esperando a jugador D".
2. **Caso a:** D inicia sesión y vuelve antes de 15 segundos. Continúa con normalidad, con sus cartas y su lugar.
3. **Caso b:** D regresa a los 16 segundos. Ya fue expulsado: va al **menú principal**, sin volver a la sala.
4. En el caso b, D sale por completo (cartas, turno y puntuación). La **ronda en curso se reinicia**: las cartas
   colocadas regresan a sus manos, la apuesta se anula y empieza la ronda con el primer jugador activo (A). La ronda
   reiniciada cuenta como la **ronda 7** en el contador.
5. Los retos ya resueltos y las vidas de los demás no cambian. D recibe 0 puntos y queda en la última posición.
6. Si D es un **jugador invitado** y pierde la sesión, no puede volver: al reingresar es un invitado nuevo con otro
   número (por ejemplo, de Jugador 3 a Jugador 4).

#### Escenario 6. Jugador invitado que gana

1. Tres jugadores en modalidad de 6 cartas. G es invitado, R1 y R2 son registrados. Orden G, R1, R2.
2. G gana tras 2 retos exitosos. Aparece en **1.º** de la tabla, pero recibe **0 puntos**.
3. R1 y R2 conservan su posición: base de 3 jugadores = 2. R1 (2.º) = 2 − 1 + 1 = **2**. R2 (3.º) = 2 − 2 + 1 = **1**.
4. G no queda en el marcador ni en historial. R1 y R2 guardan sus puntos.

#### Escenario 7. Abandono voluntario

1. Durante la ronda, **B** pulsa Salir. Se trata como abandono inmediato, sin pausa.
2. B va al menú principal. Recibe 0 puntos. La ronda en curso se reinicia y cuenta como ronda nueva.
3. Si quedan 2 jugadores activos, continúa. Si queda uno solo, la partida termina con su victoria.
4. La base de puntos usa solo a quienes no abandonaron. Con 3 jugadores iniciales y 1 que abandona, quedan 2: se usa
   el mínimo de la base (2 puntos).

#### Escenario 8. Puntaje con un abandono (5 jugadores, modalidad de 6)

1. Se inicia con 5 jugadores. Uno abandona a mitad de partida. Quedan **4 contados** → base = 4 − 1 = **3**.
2. Gana P1. Bonos de modalidad de 6 cartas: ganador +2, no ganadores +1.
3. P1 (1.º) = 3 + 2 = **5**. P2 (2.º) = 3 − 1 + 1 = **3**. P3 (3.º) = 3 − 2 + 1 = **2**. P4 (4.º) = 3 − 3 + 1 = **1**.
4. El jugador que abandonó queda en 5.º con **0 puntos**. Distribución: 5 / 3 / 2 / 1 / 0.

---

## 5. Sistema de puntaje

### 5.1. Reglas vigentes

- **Base = jugadores contados − 1**, con mínimo 2. Los contados son quienes no abandonaron ni fueron expulsados.

| Jugadores contados | 2 | 3 | 4 | 5 | 6 |
|:---:|:---:|:---:|:---:|:---:|:---:|
| Base | 2 (mínimo) | 2 | 3 | **4** | **5** |

- Bono: ganador +1 / +2 / +3 (4 / 6 / 8 cartas). No ganadores 0 / +1 / +2.
- Posición k: base − (k − 1) + bono. Nunca menos de 0.
- Con esta base, los puntos de las posiciones contadas bajan siempre de 1 en 1 y nunca se repiten. Ejemplo de 6
  jugadores en modalidad de 4 cartas: 6 / 4 / 3 / 2 / 1 / 0.
- Eliminado por cartas: cobra por su posición. Abandono y expulsión: 0. Invitado: 0 siempre. Los que abandonan
  reciben 0 aunque su posición sea inferior.

### 5.2. Salida voluntaria y abandono

- Botón Salir = abandono inmediato. 0 puntos, última posición, reinicio de la ronda en curso.

### 5.3. Marcador (leaderboard)

- Se ordena por puntos acumulados, de mayor a menor. Solo jugadores registrados.
- Si dos jugadores tienen el mismo total, queda por encima quien llegó primero a ese total. Quien llega después con el
  mismo total se coloca en la posición siguiente.
- Ejemplo: A llegó a 40 puntos y está en el 5.º lugar. Cuando B también llega a 40, B ocupa el 6.º.

---

## 6. Decisiones tomadas (antes puntos por decidir)

1. **Botón Rendirse.** Se quita. No debe ir.
2. **Botón Revelar cartas.** Se quita. No debe ir.
3. **Contador junto al avatar.** Son las **vidas** (mano más pila).
4. **Valor máximo de la apuesta.** De 1 hasta el menor entre las cartas en la mesa y la modalidad. El 0 no existe.
5. **Ronda actual en pantalla.** Sí se cuentan las rondas, incluidas las reiniciadas por una desconexión. Cada ronda
   reiniciada cuenta como una ronda nueva.
6. **Cartas tras un reto exitoso.** Regresan **todas** las cartas jugadas.
7. **Cambio de cartas durante el contador.** Se bloquea.
8. **Gatito fugitivo.** Solo con 8 cartas seleccionadas. Al estar todos en LISTO aparece el gatito, en lugar de la
   pantalla de carga normal.
9. **Castigo.** Se cambia al pop-up, basado en Skull. Cada turno es una sola acción.
10. **Puntaje.** Se adopta Base = jugadores contados − 1 (mínimo 2).
11. **Marcador.** El que llega después con los mismos puntos va en la posición siguiente.
12. **Abrir apuesta (H-01).** Exige haber colocado al menos una carta propia en la ronda.
13. **Elección de pila (H-02).** Se elige el jugador antes de cada volteo.
14. **Siguiente carta (H-03).** Tras voltear, la siguiente sin voltear pasa a ser la de arriba.
15. **Pila sin cartas (H-04).** Elegirla se rechaza con `ErrorKey` y no cambia el estado.
16. **Vida extra (H-05).** El Gato que la consume cuenta como carta prometida.
17. **Desconexión en castigo (H-06).** A quien elige le sigue corriendo su límite de 10 s. Si se desconecta el
    castigado, el castigo se suspende durante la pausa de 15 s y, si no regresa, se cancela.
18. **Salida en castigo (H-07).** No reinicia la ronda. El contador sube una vez.
19. **Azar (H-08).** Uniforme sobre todas las cartas del castigado, incluido el Gato, en ambos modos.
20. **Eliminado (H-09).** Espectador con vista pública. Puede salir sin penalización.
21. **Invitado a mitad de tabla (H-10).** Conserva su posición y cobra 0.
22. **Salidas simultáneas (H-11).** Por el inicio de la desconexión y, si empatan, por el orden de turnos.
23. **Eliminado que sale (H-12).** Conserva los puntos de su posición.
24. **Todos abandonan (H-13).** El restante gana con puntos normales. Riesgo anotado: dos cuentas amigas podrían
    abandonar a propósito para dar puntos a una tercera.

---

## Notas del autor

Revisión
- método enviar, y callback notificar recepción
- no es necesario el de respuestas, sino solo por el mensaje
- versión con diccionarios, explícito, que se explique la definición del diccionario y en donde se agrega el callback,
  sin métodos innecesarios.

![[Recording 20261113130349.m4a]]
