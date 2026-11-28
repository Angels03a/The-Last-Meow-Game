Tecnologías para la Construcción de Software

*Estándar de Codificación para CSharp*

Equipo 3

[**1\. Introducción	5**](#introducción)

[1.1. Problemática	5](#problemática)

[1.2. Propósito del documento	5](#propósito-del-documento)

[1.3. Alcance (tecnologías y plataforma objetivo)	5](#alcance-\(tecnologías-y-plataforma-objetivo\))

[1.4. Idioma de desarrollo	6](#idioma-de-desarrollo)

[**2\. Objetivos del estándar	7**](#objetivos-del-estándar)

[2.1. Mantenibilidad	7](#mantenibilidad)

[2.2. Detección temprana de errores	7](#detección-temprana-de-errores)

[2.3. Rendimiento en tiempo de ejecución	7](#rendimiento-en-tiempo-de-ejecución)

[**3\. Reglas de nombrado	7**](#reglas-de-nombrado)

[3.1. Variables locales	7](#variables-locales)

[3.2. Campos privados	8](#campos-privados)

[3.3. Propiedades	9](#propiedades)

[3.4. Constantes y valores de solo lectura (readonly)	10](#constantes-y-valores-de-solo-lectura-\(readonly\))

[3.5. Booleanos	11](#booleanos)

[3.6. Colecciones y arreglos	12](#colecciones-y-arreglos)

[3.7. Métodos	13](#métodos)

[3.8. Eventos y delegados	14](#eventos-y-delegados)

[3.9. Clases	15](#clases)

[3.10. Interfaces	17](#interfaces)

[3.11. Enumeraciones (enums)	19](#enumeraciones-\(enums\))

[3.12. Structs	20](#structs)

[3.13. Namespaces	21](#namespaces)

[3.14. Sufijos por responsabilidad (según capas del sistema) y componentes WCF/UI	22](#sufijos-por-responsabilidad-\(según-capas-del-sistema\)-y-componentes-wcf/ui)

[**4\. Organización del proyecto	24**](#organización-del-proyecto)

[4.1. Arquitectura general (capas del cliente y del servidor)	24](#arquitectura-general-\(capas-del-cliente-y-del-servidor\))

[4.2. Separación entre el motor de reglas y la infraestructura	27](#separación-entre-el-motor-de-reglas-y-la-infraestructura)

[4.3. Estructura de carpetas y namespaces	28](#estructura-de-carpetas-y-namespaces)

[4.4. Gestión de recursos gráficos (nomenclatura y ubicación)	29](#gestión-de-recursos-gráficos-\(nomenclatura-y-ubicación\))

[**5\. Capa de datos y persistencia	30**](#capa-de-datos-y-persistencia)

[5.1. Acceso a datos con Entity Framework y repositorios	30](#acceso-a-datos-con-entity-framework-y-repositorios)

[5.2. Configuración externa (archivos de settings y credenciales)	31](#configuración-externa-\(archivos-de-settings-y-credenciales\))

[5.3. Versionado del script de base de datos	32](#versionado-del-script-de-base-de-datos)

[**6\. Capa de lógica de negocio y de juego	33**](#capa-de-lógica-de-negocio-y-de-juego)

[6.1. Responsabilidades de los servicios y del dominio	33](#responsabilidades-de-los-servicios-y-del-dominio)

[6.2. Patrones de diseño utilizados y su justificación	34](#patrones-de-diseño-utilizados-y-su-justificación)

[**7\. Capa de presentación/UI	36**](#capa-de-presentación/ui)

[7.1. Separación entre lógica y vista	36](#separación-entre-lógica-y-vista)

[7.2. Nombrado de elementos de interfaz	38](#nombrado-de-elementos-de-interfaz)

[7.3. Localización de textos y alcance de claves	40](#localización-de-textos-y-alcance-de-claves)

[**8\. Control de versiones	44**](#control-de-versiones)

[8.1. Herramienta y repositorio remoto	44](#herramienta-y-repositorio-remoto)

[8.2. Política de commits	44](#política-de-commits)

[8.3. Archivos ignorados (.gitignore)	44](#archivos-ignorados-\(.gitignore\))

[**9\. Estilo de código	44**](#estilo-de-código)

[9.1. Indentación	44](#indentación)

[9.2. Límite de columna	45](#límite-de-columna)

[9.3. Uso de espacios en blanco (Vertical y horizontal)	46](#uso-de-espacios-en-blanco-\(vertical-y-horizontal\))

[9.4. Organización del archivo (Usings, namespace, clase)	47](#organización-del-archivo-\(usings,-namespace,-clase\))

[9.5. Orden de miembros dentro de la clase	48](#orden-de-miembros-dentro-de-la-clase)

[9.6. Uso de llaves	49](#uso-de-llaves)

[9.7. Uso de var vs tipos explícitos	50](#uso-de-var-vs-tipos-explícitos)

[9.8. Expresiones lambda y LINQ (reglas de nombres y sintaxis)	51](#expresiones-lambda-y-linq-\(reglas-de-nombres-y-sintaxis\))

[**10\. Prácticas de programación	54**](#prácticas-de-programación)

[10.1. Uso de override y anotaciones de atributos	54](#uso-de-override-y-anotaciones-de-atributos)

[10.2. Manejo de excepciones (jerarquía, bloques catch)	55](#manejo-de-excepciones-\(jerarquía,-bloques-catch\))

[10.3. Manejo de logs y categorización de errores	59](#manejo-de-logs-y-categorización-de-errores)

[Mecanismo de logging	59](#mecanismo-de-logging)

[Trace	61](#trace)

[Debug	62](#debug)

[Information	62](#information)

[Warning	63](#warning)

[Error	63](#error)

[Critical (Fatal)	64](#critical-\(fatal\))

[10.4. Prohibición de números mágicos	65](#prohibición-de-números-mágicos)

[Temporizadores del sistema	65](#temporizadores-del-sistema)

[10.5. Límite de operadores por expresión	66](#límite-de-operadores-por-expresión)

[10.6. Complejidad ciclomática máxima	67](#complejidad-ciclomática-máxima)

[10.7. Uso de nullable reference types / manejo de nulos	68](#uso-de-nullable-reference-types-/-manejo-de-nulos)

[10.8. Uso de propiedades vs campos públicos	68](#uso-de-propiedades-vs-campos-públicos)

[10.9. Principios de Código y Diseño	69](#principios-de-código-y-diseño)

[**11\. Comunicación en tiempo real y rendimiento	71**](#comunicación-en-tiempo-real-y-rendimiento)

[11.1. Comunicación en tiempo real con WCF y DispatcherTimer	71](#comunicación-en-tiempo-real-con-wcf-y-dispatchertimer)

[**12\. Comentarios	75**](#comentarios)

[12.1. Comentarios de implementación (bloque, línea, finales)	75](#comentarios-de-implementación-\(bloque,-línea,-finales\))

[12.2. Comentarios de documentación (XML, doc comments)	76](#comentarios-de-documentación-\(xml,-doc-comments\))

[12.3. Comentarios especiales (TODO, FIXME, HACK, UNDONE)	79](#comentarios-especiales-\(todo,-fixme,-hack,-undone\))

[**13\. Estructuras de control	80**](#estructuras-de-control)

[13.1. Sentencias condicionales	80](#sentencias-condicionales)

[13.2. Uso de return	80](#uso-de-return)

[13.3. Bucles	81](#bucles)

[13.4. Sentencias switch	82](#sentencias-switch)

[13.5. Uso del operador ternario	83](#uso-del-operador-ternario)

[13.6. Pattern matching	84](#pattern-matching)

[**14\. Manejo de eventos y arquitectura reactiva	85**](#manejo-de-eventos-y-arquitectura-reactiva)

[14.1. Convenciones para eventos y delegados	85](#convenciones-para-eventos-y-delegados)

[14.2. Suscripción y desuscripción (prevención de memory leaks)	85](#suscripción-y-desuscripción-\(prevención-de-memory-leaks\))

[**15\. Pruebas	86**](#pruebas)

[15.1. Framework de pruebas	86](#framework-de-pruebas)

[15.2. Estructura de proyectos de prueba y nomenclatura de clases	88](#estructura-de-proyectos-de-prueba-y-nomenclatura-de-clases)

[15.3. Estructura de un test (Arrange-Act-Assert)	89](#estructura-de-un-test-\(arrange-act-assert\))

[15.4. Pruebas unitarias de lógica pura vs pruebas de integración con infraestructura	91](#pruebas-unitarias-de-lógica-pura-vs-pruebas-de-integración-con-infraestructura)

[15.5. Uso de mocks/simulación de dependencias de infraestructura	94](#uso-de-mocks/simulación-de-dependencias-de-infraestructura)

[**16\. Validación de entrada y seguridad	97**](#validación-de-entrada-y-seguridad)

[16.1. Centralización de validaciones	97](#centralización-de-validaciones)

[16.2. Manejo de datos sensibles	97](#manejo-de-datos-sensibles)

[16.3. Prevención de trampas y manipulación de datos del cliente	97](#prevención-de-trampas-y-manipulación-de-datos-del-cliente)

[**17\. DTO y contratos de datos	98**](#dto-y-contratos-de-datos)

[17.1. Estructura de los DTO	98](#estructura-de-los-dto)

[17.2. Serialización	98](#serialización)

[**18\. Navegación entre pantallas y retroalimentación	98**](#navegación-entre-pantallas-y-retroalimentación)

[18.1. Navegación entre pantallas	98](#navegación-entre-pantallas)

[18.2. Mensajes y retroalimentación al jugador	98](#mensajes-y-retroalimentación-al-jugador)

[**19\. Dependencias externas y seguridad	99**](#dependencias-externas-y-seguridad)

[19.1. Librerías y paquetes utilizados	99](#librerías-y-paquetes-utilizados)

[**20\. Anexos	99**](#anexos)

[20.1. Glosario de términos	99](#glosario-de-términos)

[20.2. Documentación de decisiones de arquitectura	100](#documentación-de-decisiones-de-arquitectura)

[20.3. Referencias externas consultadas	103](#referencias-externas-consultadas)

[**21\. Declaración de uso de IA	107**](#declaración-de-uso-de-ia)

1. # Introducción {#introducción}

   1. ## Problemática {#problemática}

Se requiere realizar un proyecto de desarrollo de videojuegos de forma estructurada y organizada, cada estudiante tiende a tener distintos hábitos de codificación distintos, adquirido en EE anteriores o de forma autónoma con proyectos personales. Sin un acuerdo o trabajo lineal y estructurado.

Por ello, existe una falta de uniformidad que podría generar conflictos durante el desarrollo de este proyecto colaborativo. Además, en el contexto de un videojuego The Last Meow Game(TLMG), inspirado en Skull- inspired bluffing game, se requieren conocer ciertas restricciones del dominio, por ello la ausencia de un estándar dificulta y hace más propenso a cometer errores y tomar riesgos sin previamente seguir un documento con gran peso en el desarrollo.

2. ## Propósito del documento {#propósito-del-documento}

El propósito de este documento, pretende ser la base de un estándar de codificación en C\# enfocado al proyecto de videojuego de la Experiencia Educativa Tecnologías para la Construcción de software. El estándar define y aclara reglas de nombrado, estructura del código, manejo de errores y excepciones, comentarios, pruebas unitarias y buenas prácticas orientadas a los videojuegos, adaptando convenciones oficiales de C\# y .NET.

El documento busca que independientemente de cualquier integrante escriba una clase, método o una prueba sea entendible, predecible y mantenible por cualquier otro integrante. De tal forma facilita la comprensión de código ajeno, facilitando la detección de errores.

Los nombres de los ejemplos son ilustrativos y pertenecen al dominio de TLMG.

3. ## Alcance (tecnologías y plataforma objetivo) {#alcance-(tecnologías-y-plataforma-objetivo)}

**Mantenimiento.** Solo podrán incorporarse dependencias aprobadas para el proyecto. Registrar explícitamente los archivos nuevos o movidos en el csproj clásico con la acción correspondiente a su tipo.

Cubre el cliente WPF, el servidor WCF, el acceso a datos y las pruebas. Por lo tanto aplica a:

* Código de las dos soluciones: cliente (WPF) y servidor (WCF, lógica de negocio y acceso a datos).

* Proyectos de pruebas unitarias y de integración.

* Código propio del equipo como DTOs.

* El código generado automáticamente queda excluido de las métricas y no deberá editarse manualmente. Esto comprende Reference.svcmap, las plantillas .tt y los siguientes artefactos: archivos \*.g.cs y \*.Designer.cs, el modelo de Entity Framework y el proxy generado por una referencia de servicio WCF ([Reference.cs](http://Reference.cs)).

* Plataforma: C\# 7.3 sobre .NET Framework 4.8; WPF, WCF (netTcpBinding dúplex), SQL Server con Entity Framework 6 Database First, log4net y CommunityToolkit.Mvvm 8.4.2

  4. ## Idioma de desarrollo {#idioma-de-desarrollo}

Se establecen dos criterios distintos en cuanto al contenido entre la parte de documentación oficial del proyecto y el mismo código.

* Código. El código se escribirá por completo en inglés, esto se respetará por completo, desde clases métodos hasta variables sin excepción.

```c#
public class RoundResolver
{
    private int _activePlayerCount;

    public void ResolveRound()
    {
    }
}

```

Ejemplo incorrecto (identificadores en español)

```c#
public class ResolvedorDeRonda
{
    private int _jugadoresActivos;

    public void ResolverRonda()
    {
    }
}
```

* La documentación XML de los contratos públicos entre componentes deberá redactarse en inglés (§12.2).
* **Documentación del proyecto y commits.** Este estándar, la documentación en `docs/` y los mensajes de commit se escriben en español, por ser el idioma principal del equipo.  

2. # Objetivos del estándar {#objetivos-del-estándar}

   1. ## Mantenibilidad {#mantenibilidad}

      Garantizar que cualquier integrante del equipo pueda comprender, depurar y extender el código escrito por otro compañero sin fricción. La homogeneidad visual y estructural reduce el costo de mantenimiento a lo largo de todo el ciclo de vida del software.

   2. ## Detección temprana de errores {#detección-temprana-de-errores}

   Establecer directrices estrictas de tipado, validación de argumentos y manejo exhaustivo de excepciones que permitan al compilador de C\# y a las pruebas automatizadas detectar defectos antes de que alcancen el entorno de ejecución.

   3. ## Rendimiento en tiempo de ejecución {#rendimiento-en-tiempo-de-ejecución}

   Evitar los bloqueos de la interfaz mediante DispatcherTimer y la comunicación por callbacks (S11.1).

   

3. # Reglas de nombrado {#reglas-de-nombrado}

**Nombres nuevos.** Evitar abreviaturas arbitrarias y nombres como Data, Info, Outcome, Request o Reply si ocultan la responsabilidad. Conservar convenciones técnicas como DTO, EventArgs y WCF. En §17.1, Request y Result identifican la función de un contrato concreto. Deberán reutilizarse los componentes adecuados existentes, incluidos contratos, callbacks, estilos, recursos, repositorios y procedimientos. No introducir nuevos tipos Operation…Request, Operation…Result ni envoltorios genéricos. Si se requiere un nuevo DTO específico o una modificación de un contrato público, deberá justificarse su necesidad, evaluarse la compatibilidad WCF y coordinarse la aprobación previa del cambio.

   1. ## Variables locales {#variables-locales}

Los nombres de variables locales deberán escribirse utilizando camelCase, es decir, iniciando con minúscula y capitalizando la primera letra de cada palabra interna subsecuente adecuándose al dominio.

Ejemplo correcto

```c#
int playerScore = MaxPlayerScore;
```

Ejemplo incorrecto (no es camelCase)

```c#
int PlayerScore = MaxPlayerScore;
```

Los nombres deberán ser descriptivos y con significado, priorizando la comprensión del propósito de la variable por encima de la brevedad del identificador. Esto incluye los contadores de los bucles for, que también llevan un nombre descriptivo (por ejemplo, cardIndex) y no i, j ni k (D-34).

Ejemplo correcto

```c#
int cardCount = InitialCardCount;
```

Ejemplo incorrecto

```c#
int cc = InitialCardCount;
```

Deberá usarse la palabra clave *var* únicamente cuando el tipo de la variable sea evidente a partir del lado derecho de la asignación. Para consultar la regla completa y sus excepciones, véase la sección 9.7 (Uso de var vs tipos explícitos).

Ejemplo correcto

```c#
var playerName = "Hero";
```

Ejemplo incorrecto

```c#
var result = ResolveRound();
```

2. ## Campos privados {#campos-privados}

Los campos privados deberán nombrarse utilizando camelCase consistente con la regla general del lenguaje.

Ejemplo correcto

```c#
private int _currentScore;
```

Ejemplo incorrecto

```c#
private int CurrentScore;
```

Los campos privados (y también los internos) deberán prefijarse con un guión bajo (\_).

Ejemplo correcto

```c#
private int _playerCount;
```

Ejemplo incorrecto

```c#
private int playerCount;
```

Un campo privado (o interno) estático sigue la misma regla de la sección anterior (camelCase con prefijo de guión bajo), sin prefijos adicionales como s\_ o t\_. Esa notación es una práctica obsoleta heredada de C; en C\# resulta innecesaria porque el soporte del IDE (IntelliSense) ya identifica un miembro como estático.

Ejemplo correcto

```c#
private static int _activeCardCount;
```

Ejemplo incorrecto

```c#
private static int s_activeCardCount;
```

Todo campo privado (\_campo) que deba exponerse fuera de la clase deberá hacerlo mediante una propiedad pública en PascalCase basada en el mismo nombre, nunca mediante un campo público adicional.

Ejemplo correcto

```c#
private int _score;

public int Score => _score;
```

Ejemplo incorrecto

```c#
private int _score;

public int score;
```

3. ## Propiedades {#propiedades}

Capitalización (PascalCase): las propiedades, al ser miembros públicos de un tipo, deberán nombrarse utilizando PascalCase.

Ejemplo correcto

```c#
public int PlayerScore { get; set; }
```

Ejemplo incorrecto

```c#
public int playerScore { get; set; }
```

Uso de sustantivo, frase nominal o adjetivo: el nombre de una propiedad deberá ser un sustantivo, una frase nominal o un adjetivo, nunca un verbo, ya que las propiedades representan datos y no acciones. Adicionalmente, se prohíbe que el nombre de una propiedad coincida con el de un método Get correspondiente, ya que ello genera confusión sobre cuál miembro utilizar.

Ejemplo correcto

```c#
public int Score { get; private set; }
```

Ejemplo incorrecto (nombre de verbo)

```c#
public int CalculateScore { get; private set; }
```

Para propiedades booleanas, véase S3.5.

Ejemplo correcto

```c#
public bool IsGameOver { get; private set; }
```

Ejemplo incorrecto (frase negativa)

```c#
public bool IsNotGameOver { get; private set; }
```

4. ## Constantes y valores de solo lectura (readonly) {#constantes-y-valores-de-solo-lectura-(readonly)}

Criterio de elección (const vs static readonly): Deberá usarse const únicamente para valores constantes en tiempo de compilación y de tipos primitivos o inmutables (como int, float, string o bool). Cuando el valor requiera evaluación en tiempo de ejecución, sea un tipo de referencia mutable o dependa de la instanciación de un objeto, deberá declararse como static readonly.

Reglas de nombrado: Las constantes (const) y las propiedades de solo lectura o campos static readonly públicos deberán nombrarse utilizando PascalCase. Por otro lado, los campos static readonly privados o internos deberán nombrarse utilizando camelCase prefijados con un guión bajo (\_), alineándose con la regla de campos privados (sección 3.2).

Ejemplo correcto

```c#
private const int MaxLoginAttempts = 3;

public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

private static readonly ILog _log = LogManager.GetLogger(typeof(LoginScreen));
```

Ejemplo incorrecto

```c#
private const int max_login_attempts = 3;          // Violates PascalCase. 

public static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(30); // Should be PascalCase. 

private static readonly string defaultLanguageCode = "es-MX"; // Missing the _ prefix. 

private const TimeSpan MatchTimeout = TimeSpan.FromSeconds(30); // TimeSpan cannot be const. 
```

5. ## Booleanos {#booleanos}

Los identificadores booleanos deberán nombrarse utilizando una frase afirmativa y no una negativa, con el fin de evitar dobles negaciones al evaluarlos dentro de una condición.

Ejemplo correcto

```c#
public bool CanStartMatch { get; private set; }
```

Ejemplo incorrecto

```c#
if (!cannotStartMatch)

public bool CannotStartMatch { get; private set; }
```

De forma opcional, un identificador booleano podrá prefijarse con Is, Can o Has, únicamente cuando dicho prefijo aporte claridad adicional sobre el estado que representa.

Ejemplo correcto

```c#
public bool IsGameOver { get; private set; }
```

Aplicación por nivel de acceso: conforme a lo ya establecido en las secciones 3.2 y 3.3, un booleano público (propiedad) seguirá PascalCase, mientras que un campo booleano privado seguirá camelCase con prefijo \_; en ambos casos aplican las reglas 1 y 2 anteriores.

Ejemplo correcto

```c#
private bool _isPaused;

public bool IsPaused { get; private set; }
```

Ejemplo incorrecto

```c#
private bool IsPaused;

public bool _cannotStartMatch { get; private set; }
```

6. ## Colecciones y arreglos {#colecciones-y-arreglos}

Una propiedad (o, por extensión, un campo o variable) que representa una colección deberá nombrarse con una frase en plural que describa los elementos que contiene.

Ejemplo correcto

```c#
public List<Card> ActiveCards { get; private set; }

private readonly List<Card> _handCards;

public IReadOnlyList<Card> HandCards { get; }
```

Ejemplo incorrecto

```c#
public List<Card> CardList { get; private set; }

private readonly List<Card> HandCard;

public IReadOnlyList<Card> _handCard { get; }
```

Conforme a lo ya establecido en las secciones 3.1, 3.2 y 3.3, una colección pública (propiedad) seguirá PascalCase, y una colección privada (campo) seguirá camelCase con prefijo \_; en ambos casos, el nombre deberá construirse en plural según la regla anterior.

Ejemplo correcto

```c#
private readonly List<Card> _playerHandCards;

public IReadOnlyList<Card> PlayerHandCards { get; }
```

Ejemplo incorrecto

```c#
private readonly List<Card> PlayerHandCard;

public IReadOnlyList<Card> _playerHandCard { get; }
```

7. ## Métodos {#métodos}

Los nombres de métodos deberán escribirse utilizando PascalCase, de forma consistente con el resto de los miembros públicos de un tipo.

Ejemplo correcto

```c#
public int CalculateScore(int baseScore, float multiplier)
{
    int score = (int)(baseScore * multiplier);
    return score;
}

public void AddScore(int scoreAmount)
{
}
```

Ejemplo incorrecto

```c#
public int calculateScore(int baseScore, float multiplier)
{
    return (int)(baseScore * multiplier);
}

public void addScore(int scoreAmount)
{
}
```

El nombre de un método deberá ser un verbo o una frase verbal que describa la acción que realiza, ya que típicamente actúan sobre datos, y usar un verbo facilita a otros desarrolladores comprender qué hace el método.

Ejemplo correcto

```c#
public int CalculateScore(int baseScore, float multiplier)
{
    int score = (int)(baseScore * multiplier);
    return score;
}
```

Ejemplo incorrecto (nombre de sustantivo)

```c#
public int ScoreCalculation(int baseScore, float multiplier)
{
    return (int)(baseScore * multiplier);
}
```

Los parámetros de un método deberán nombrarse utilizando camelCase, siguiendo la misma convención aplicada a las variables locales.

Ejemplo correcto

```c#
public void AddScore(int scoreAmount)
{
}
```

Ejemplo incorrecto

```c#
public void AddScore(int ScoreAmount)
{
}
```

Excepción: los métodos de un contrato callback de WCF pueden nombrarse con el prefijo "On" (S3.10) y los que entregan un resultado se nombran con el resultado (por ejemplo, CreateRoomResult).

8. ## Eventos y delegados {#eventos-y-delegados}

Los eventos deberán nombrarse utilizando PascalCase y un verbo o frase verbal que describa la acción asociada. Todo evento público deberá declararse utilizando el tipo genérico EventHandler o EventHandler\<TEventArgs\>, según requiera o no transmitir información asociada al evento; se prohíbe el uso de delegados genéricos Action para la exposición de eventos públicos.

Ejemplo correcto

```c#
public event EventHandler PlayerDisconnected;
```

Ejemplo incorrecto

```c#
public event Action PlayerDisconnected;
```

Cuando un evento represente una acción que ocurre en dos momentos (antes y después), deberá usarse el gerundio para el evento previo y el pasado para el evento posterior, sin recurrir a prefijos o sufijos como "Before" o "After".

Ejemplo correcto

```c#
public event EventHandler MatchStarting;

public event EventHandler MatchStarted;
```

Ejemplo incorrecto

```c#
public event EventHandler BeforeMatchStart;

public event EventHandler AfterMatchStart;
```

Los métodos manejadores de eventos y las firmas de los delegados deberán incluir siempre los parámetros estándar de .NET: sender (que representa el objeto origen que emite el evento) y e o un argumento derivado de EventArgs (que contiene los datos asociados al evento).

Ejemplo correcto

```c#
public event EventHandler<ScoreEventArgs> PlayerScored;
```

Ejemplo incorrecto

```c#
public event Action<int> PlayerScored;
```

Una clase que transporta datos asociados a un evento deberá nombrarse con el sufijo EventArgs.

Ejemplo correcto

```c#
public class ScoreEventArgs : EventArgs
{
}
```

Ejemplo incorrecto

```c#
public class ScorePayload : EventArgs
{
}
```

9. ## Clases {#clases}

Todos los nombres de clases deberán escribirse utilizando PascalCase, es decir, iniciando cada palabra que compone el identificador con mayúscula y sin usar guiones ni guiones bajos.

Ejemplo correcto

```c#
public class PlayerHand
{
}
```

Ejemplo incorrecto

```c#
public class playerHand
{
}
```

Asegurar que el nombre de la clase describa "qué es" la entidad que representa (un objeto o concepto), y no "qué hace" (comportamiento), reservando los verbos para métodos.

Los nombres de clases deberán ser sustantivos o frases nominales, nunca verbos ni frases verbales.

Ejemplo correcto

```c#
public class RoundResolver
{
}
```

Ejemplo incorrecto (nombre de verbo)

```c#
public class ResolveRound
{
}
```

No se debe anteponer ningún prefijo al nombre de una clase.

Ejemplo correcto

```c#
public class LanguageManager
{
}
```

Ejemplo incorrecto

```c#
public class CLanguageManager
{
}
```

Se recomienda que las clases derivadas terminen con el nombre de su clase base cuando esta pertenezca a una categoría reconocida del framework.

Ejemplo correcto (hereda de Exception)

```c#
public class RoomFullException : Exception
{
}
```

Ejemplo incorrecto (no hereda de Exception pero usa el sufijo)

```c#
public class PlayerBannedException
{
}
```

Los identificadores de clase no deben contener dos guiones bajos consecutivos, ya que esa notación está reservada para identificadores generados por el compilador.

Ejemplo correcto

```c#
public class PlayerHand
{
}
```

Ejemplo incorrecto

```c#
public class Player__Hand
{
}
```

Ejemplo de clase con responsabilidad única:

```c#
public class GameSession
{
    public string SessionId { get; private set; }

    public DateTime StartTime { get; private set; }

    public bool IsActive { get; private set; }

    public void StartSession()
    {
        StartTime = DateTime.UtcNow;
        IsActive = true;
    }
}
```

10. ## Interfaces {#interfaces}

Los nombres de interfaces deberán escribirse en PascalCase y prefijarse con la letra "I", con el fin de indicar de forma explícita que se trata de una interfaz.

Ejemplo correcto

```c#
public interface IEmailSender
{
}
```

Ejemplo incorrecto

```c#
public interface EmailSender
{
}
```

Cuando exista una clase que sea la implementación estándar de una interfaz, ambos nombres deberán diferir únicamente en el prefijo "I" de la interfaz, para dejar clara su relación.

Ejemplo de contrato de servicio y de callback en WCF (D-38):

```c#
[ServiceContract(CallbackContract = typeof(IChatManagerCallback))]
public interface IChatManager
{
    [OperationContract(IsOneWay = true)]
    void SendMessage(string message);
}

public interface IChatManagerCallback
{
    [OperationContract(IsOneWay = true)]
    void OnMessageReceived(string message);
}
```

Los métodos de un contrato callback de WCF (I…ManagerCallback) que avisan de un suceso iniciado por el servidor llevan el prefijo "On" seguido de lo ocurrido en participio (por ejemplo, OnLobbyRoomChanged, OnRoomInvitationReceived). Los métodos que entregan el resultado de una operación del cliente se nombran con el resultado (por ejemplo, CreateRoomResult) y no llevan "On".

Ejemplo correcto

```c#
[OperationContract(IsOneWay = true)]
void OnLobbyRoomChanged(LobbyRoom lobbyRoom);
```

Ejemplo incorrecto (aviso del servidor sin prefijo "On")

```c#
[OperationContract(IsOneWay = true)]
void LobbyRoomChanged(LobbyRoom lobbyRoom);
```

Excepción: la implementación de los contratos de servicio WCF es una sola clase parcial (por ejemplo, ServiceImplementation) que implementa todos los contratos, porque un ServiceHost aloja una única clase. Cada contrato se implementa en su propio archivo parcial.

Ejemplo correcto

```c#
public interface IPlayerHand
{
}

public class PlayerHand : IPlayerHand
{
}
```

Ejemplo incorrecto (el nombre de la clase no corresponde con la interfaz)

```c#
public class PlayerCardStorage : IPlayerHand
{
}
```

Excepción: una clase puede implementar varias interfaces de rol además de su interfaz principal (por ejemplo, SessionRegistry implementa ISessionRegistry e ISessionIdentityProvider). En ese caso el nombre de la clase corresponde a la interfaz principal. 

11. ## Enumeraciones (enums) {#enumeraciones-(enums)}

**Enumeraciones nuevas.** En enums de resultados, reservar el valor 0 para Unknown o un estado de fallo. En enumeraciones [Flags] se permite None = 0 cuando representa ausencia de banderas. Analizar compatibilidad antes de alterar valores serializados existentes.

Tanto el nombre del tipo enumerado como el de cada uno de sus valores deberán escribirse en PascalCase.

Ejemplo correcto

```c#
public enum RoomState
{
    Waiting,
    InMatch,
    Closed
}
```

Ejemplo incorrecto

```c#
public enum roomState
{
    waiting,
    inMatch,
    closed
}
```

Una enumeración simple, donde solo puede seleccionarse un valor a la vez, deberá nombrarse en singular; una enumeración de tipo "flags", donde pueden combinarse varios valores simultáneamente, deberá nombrarse en plural.

Ejemplo correcto

```c#
public enum RoomState
{
    Waiting,
    InMatch,
    Closed
}

[Flags]
public enum NotificationChannels
{
    None = 0,
    Email = 1,
    Chat = 2
}
```

Ejemplo incorrecto

```c#
public enum RoomStates
{
    Waiting,
    InMatch,
    Closed
}
```

No se recomienda agregar la palabra "Enum" como sufijo al nombre del tipo, ya que resulta redundante se recomienda no agregar "Enum" después del identificador de la enumeración, señalando que, por ejemplo, es preferible el nombre Day sobre DayEnum.

Ejemplo correcto

```c#
public enum PlayerRole
{
    Registered,
    Guest
}
```

Ejemplo incorrecto

```c#
public enum PlayerRoleEnum
{
    Registered,
    Guest
}
```

12. ## Structs {#structs}

Los nombres de structs deberán escribirse utilizando PascalCase, de la misma forma que las clases, interfaces y delegados.

Ejemplo correcto

```c#
public struct CardPosition
{
}
```

Ejemplo incorrecto

```c#
public struct cardPosition
{
}
```

El nombre de un struct deberá ser un sustantivo o una frase nominal, siguiendo el mismo criterio aplicado a las clases, ya que ambos representan una entidad o un valor y no una acción.

Ejemplo correcto

```c#
public struct RoundTimeRange
{
}
```

Ejemplo incorrecto

```c#
public struct CalculateRoundTime
{
}
```

13. ## Namespaces {#namespaces}

Los componentes de un namespace deberán escribirse en PascalCase, separando cada componente jerárquico mediante un punto.

Ejemplo correcto

```c#
namespace TLMG.Client.Views
{
}

namespace TLMG.Services.Accounts
{
}
```

Ejemplo incorrecto

```c#
namespace skillsnlearning.tlmg.gameplay
{
}
```

*Se define la plantilla general:*

Company.(Product\|Technology)\[.Feature\]\[.Subnamespace\]

Se indica que el nombre del namespace debe prefijarse con el nombre de la empresa o sigla del proyecto (por ejemplo, TLMG) precisamente para evitar que namespaces de distintas soluciones coincidan.

Ejemplo correcto

```c#
namespace TLMG.Server.Services
{
}
```

Ejemplo incorrecto

```c#
namespace Accounts
{
}
```

El segundo nivel del namespace deberá corresponder a un nombre de producto o tecnología estable e independiente de la versión, y no deberá basarse en la estructura organizacional interna del equipo (por ejemplo, el nombre de un área, equipo o squad), ya que este tipo de nombres suele ser efímero.

Ejemplo correcto

```c#
namespace TLMG.Contracts
{
}
```

Ejemplo incorrecto

```c#
namespace TLMG.EquipoGameplay2026.Accounts
{
}
```

14. ## Sufijos por responsabilidad (según capas del sistema) y componentes WCF/UI {#sufijos-por-responsabilidad-(según-capas-del-sistema)-y-componentes-wcf/ui}

Los nombres de las clases deberán reflejar de forma explícita su responsabilidad principal mediante el uso de sufijos estandarizados según la capa a la que pertenecen. Para los contratos de servicio y callback en WCF, la interfaz principal debe llevar el sufijo Manager prefijado con I (ej. IUsersManager) y su callback correspondiente el sufijo ManagerCallback prefijado con I (ej. IUsersManagerCallback). Asimismo, para las clases que transportan datos de eventos se utilizará el sufijo EventArgs, y para los delegados manejadores el sufijo EventHandler.

| Sufijo | Cuándo se usa | Ejemplo |
| :---- | :---- | :---- |
| Screen | Pantalla WPF | LoginScreen |
| Window | Ventana principal o secundaria | MainWindow |
| Helper | Utilidades estáticas sin estado | ValidationHelper |
| Manager | Recurso global de la aplicación (LanguageManager) o contrato de servicio WCF (IUsersManager); nunca persistencia | LanguageManager, IUsersManager |
| Repository | Acceso a persistencia | PlayerRepository |
| Service | Lógica del negocio del Servidor | PlayerAccountRegistrationService |
| Connection | Interfaz de un área de la conexión del cliente con el servidor (cuentas, amigos, lobby, chat); la implementa ServerConnectionManager (D-70)  | IAccountsConnection  |
| Exception | Excepciones personalizadas | PlayerNotFoundException |
| Attribute | Atributos personalizados | GameTagAttribute |
| EventArgs | Clases con datos de eventos | ScoreEventArgs |
| EventHandler | Delegados manejadores de eventos | PlayerDisconnectedEventHandler |
| ViewModel | Lógica de presentación de una pantalla MVVM | LoginViewModel |
| Request | DTO con los datos de entrada de una operación del contrato (S17.1; D-56)  | OperationRegisterPlayerAccountRequest (contrato existente)  |
| Result | DTO con el resultado de una operación del contrato (S17.1) | OperationRegisterPlayerAccountResult (contrato existente) |
| Registry | Estado compartido en memoria entre las sesiones del servidor; su acceso se protege conforme a S11.1  | SessionRegistry |
| Validator | Clase estática de TLMG.Domain.Validators que valida formato o reglas en memoria y, en sus métodos que devuelven ErrorKey, utiliza string.Empty cuando el dato es válido. No accede a datos, sesión ni red, y no lanza excepciones por entrada inválida  | PlayerProfileValidator  |
| Dispatcher  | Coordina generar, guardar y enviar algo (por ejemplo, un código por correo). No decide reglas  | PasswordResetDispatcher  |
| Store | Guarda o transforma registros de verificación sobre un repositorio  | VerificationCodeStore  |
| Hasher | Calcula y verifica hashes  | PasswordHasher  |
| Catalog | Lista cerrada de valores válidos  | AvatarCatalog  |
| Provider | Interfaz estrecha de lectura sobre un componente grande  | ISessionIdentityProvider  |
| Connection | Interfaz de la capa Comunicación del cliente, una por área (D-70)  | IProfileConnection  |
| Lookup / Status / Record | …Status es el enum del resultado de una operación; …Lookup es la clase con Status y los datos; …Record son datos tal como se guardan  | PasswordHashLookup, PasswordChangeStatus, PendingRegistrationRecord  |
| NavigationParameter | Datos que viajan entre pantallas  | VerificationNavigationParameter  |
| Behavior | Comportamiento adjunto de WPF  | PasswordBindingBehavior  |
| Factory | Crea objetos de otra capa para que quien los usa no conozca su construcción | ScreenFactory |

Manager nunca se usa para persistencia; para eso Repository.

Service se usa para la lógica de negocio del servidor. No accede directamente a la base de datos, tarea de Repository, ni implementa contratos WCF, tarea de ServiceImplementation.

Connection se usa en el cliente para la interfaz de un área de la conexión con el servidor. La implementa ServerConnectionManager, que conserva la sesión y los canales (D-60), y cada ViewModel recibe únicamente la interfaz de su área (D-70). No define contratos WCF, que son Manager, ni lógica de negocio, que es Service y solo existe en el servidor.

Ejemplo correcto

```c#
public interface IAccountsConnection
{
}

public partial class ServerConnectionManager : IAccountsConnection
{
}

```

Ejemplo incorrecto (Service se reserva para la lógica del servidor)

```c#
public interface ILoginService
{
}

```

Un sufijo asociado a un tipo base o interfaz del framework (como Exception, Attribute, EventArgs o EventHandler) deberá utilizarse únicamente en tipos que efectivamente extiendan esa clase base o implementen esa interfaz.

Ejemplo correcto

```c#
public class RoomFullException : Exception
{
}
```

Ejemplo incorrecto (usa el sufijo reservado sin heredar de Exception)

```c#
public class RoomFullException
{
}
```

Una clase responsable de las operaciones de persistencia de una entidad (guardar, consultar, actualizar o eliminar registros) deberá nombrarse con el sufijo Repository, implementando una interfaz correspondiente.

Ejemplo correcto

```c#
public interface IPlayerRepository
{
}

public class PlayerRepository : IPlayerRepository
{
}
```

Ejemplo incorrecto (Manager no se usa para persistencia)

```c#
public class PlayerManager
{
}
```

4. # Organización del proyecto {#organización-del-proyecto}

   1. ## Arquitectura general (capas del cliente y del servidor) {#arquitectura-general-(capas-del-cliente-y-del-servidor)}

   TLMG se construye en dos soluciones independientes: la del cliente (aplicación WPF) y la del servidor (consola con host WCF, lógica de negocio, dominio y acceso a datos). Ambas se comunican únicamente mediante los contratos WCF y no comparten ensamblados: el cliente usa un proxy generado con svcutil.exe (D-40) y el proyecto TLMG.Contracts existe solo en la solución del servidor. La descripción completa de los componentes y sus interfaces está en el SAD (SAD S4.1 y S4.2); esta sección fija cómo se refleja esa arquitectura en el código.

   Las capas del cliente son las siguientes:

| Capa | Qué contiene | Tipos habituales (S3.14) | Puede usar | No puede usar |
| :---- | :---- | :---- | :---- | :---- |
| Vista | XAML y code-behind limitado a comportamiento visual | Screen, Window | Su ViewModel, diccionarios de recursos | Proxy WCF, Entity Framework, reglas de negocio |
| ViewModel | Estado y comandos de una pantalla | ViewModel | Capa de comunicación, LanguageManager, Helper | Tipos de vista (Window, controles, MessageBox), Entity Framework |
| Comunicación | Proxy generado con svcutil.exe e implementación de los callbacks | Implementaciones de I…ManagerCallback | Contratos expuestos por el proxy | Controles de la vista, Entity Framework |
| Recursos e idioma | Diccionarios XAML y gestor de idioma | Manager | Diccionarios de recursos locales | Todo lo demás |

Las capas del servidor son las siguientes:

| Capa (proyecto) | Qué contiene | Tipos habituales (S3.14) | Restricción principal |
| :---- | :---- | :---- | :---- |
| Host (TLMG.Server) | Consola de arranque, ServiceHost y configuración de endpoints | Program | No contiene lógica de negocio ni accede a datos; solo levanta el servicio |
| Contratos (TLMG.Contracts) | Interfaces WCF y DTO | I…Manager, I…ManagerCallback, DTO (S17.1) | No depende de Services, DataAccess ni Domain |
| Lógica de negocio (TLMG.Services) | Implementación de los contratos WCF y lógica de negocio del servidor | ServiceImplementation (parcial), …Service | ServiceImplementation solo delega; las clases …Service no implementan contratos WCF ni usan Entity Framework |
| Dominio (TLMG.Domain) | Motor de reglas de juego | Clases de reglas | No depende de ningún otro proyecto ni de infraestructura (ver S4.2) |
| Acceso a datos (TLMG.DataAccess) | Entity Framework 6, entidades, repositorios | …Repository | El contexto de EF no sale de esta capa |

Se cumplen las siguientes reglas de dependencia:

1. Las dependencias van del host hacia la persistencia. Una capa inferior nunca referencia a una superior.  
2. El cliente no referencia Entity Framework ni TLMG.DataAccess y nunca se conecta a SQL Server. Toda operación pasa por los contratos WCF.  
3. ServiceImplementation implementa los contratos WCF y delega en las clases …Service; no contiene reglas ni consultas.

Ejemplo correcto (ServiceImplementation delega en la capa de lógica de negocio)

```c#
public partial class ServiceImplementation : IUsersManager
{
    private readonly IPlayerAccountRegistrationService _registrationService;

    public void RegisterPlayerAccount(string nickname, string email, string password)
    {
        _registrationService.Register(nickname, email, password);
    }
}
```

Ejemplo incorrecto (el host consulta la base de datos)

```c#
public partial class ServiceImplementation : IUsersManager
{
    public void RegisterPlayerAccount(string nickname, string email, string password)
    {
        using (var context = new TLMGContext())
        {
        }
    }
}
```

2. ## Separación entre el motor de reglas y la infraestructura {#separación-entre-el-motor-de-reglas-y-la-infraestructura}

   El motor de reglas de juego reside en TLMG.Domain y es una biblioteca de clases pura: no referencia WPF, WCF, Entity Framework ni ningún otro paquete de infraestructura. El servidor decide el resultado de cada jugada con este motor, y esa separación permite probar las reglas en memoria (mínimo 300 pruebas, ASR-04) sin red ni base de datos.

   Para conservar esa pureza se cumple lo siguiente:

* TLMG.Domain no usa los DTO de TLMG.Contracts.DataContracts, porque llevan atributos de WCF, ni las entidades de Entity Framework. La conversión entre estos tipos y los del dominio la hace TLMG.Services.  
* El dominio no invoca callbacks, repositorios ni registro de eventos. Devuelve un resultado y quien lo llamó (Services) decide qué notificar y qué guardar.  
* El dominio define las interfaces de los repositorios (ej. IPlayerRepository), pero desconoce cómo se implementan (Arquitectura Hexagonal). 

Correcto (el dominio solo calcula; el servicio notifica y guarda)

```c#
namespace TLMG.Domain
{
    public class RoundResolver
    {
        public RoundResult Resolve(RoundState state)
        {
            RoundResult result = new RoundResult();
            return result;
        }
    }
}
```

Incorrecto (el dominio depende de WCF y de acceso a datos)

```c#
using System.ServiceModel;
using TLMG.DataAccess;

namespace TLMG.Domain
{
    public class RoundResolver
    {
        public void Resolve(IMatchManagerCallback callback, PlayerRepository repository)
        {
        }
    }
}
```

Respaldo: SAD S4.3 (regla prohibida de ADR-02), C-04, Drivers ADR-02. Los nombres del ejemplo son ilustrativos.

3. ## Estructura de carpetas y namespaces {#estructura-de-carpetas-y-namespaces}

**Organización.** Un tipo superior por archivo y correspondencia entre carpeta y namespace. Durante una reorganización física provisional, conservar namespaces y contratos externos hasta que exista una separación real y se evalúe su compatibilidad.

El repositorio se organiza en carpetas de primer nivel por función: Client/ (solución del cliente), Server/ (solución del servidor), database/ (script de la base de datos y, en database/seed/, scripts de datos de demostración) , docs/ (documentación) y gov/ (gobierno del proyecto: bitácoras y backlog).

En cada proyecto, el namespace raíz es el nombre del proyecto y cada carpeta agrega un nivel con su mismo nombre (S3.13). El nombre de cada archivo coincide con el del tipo que contiene, y cada archivo contiene un solo tipo de nivel superior. Las clases parciales se nombran Tipo.Aspecto.cs (por ejemplo, ServiceImplementation.UsersManager.cs). Las carpetas llevan nombres en inglés y PascalCase.

**Cliente (TLMG.Client):**

| Carpeta | Contenido | Namespace |
| :---- | :---- | :---- |
| raíz | App, MainWindow, LanguageManager, ValidationHelper | TLMG.Client |
| Views | Screen y su clase base | TLMG.Client.Views |
| ViewModels | ViewModel de cada pantalla MVVM | TLMG.Client.ViewModels |
| Communication | ServerConnectionManager (sesión, canales, login y registro), las interfaces por área (…Connection), la implementación de los callbacks y el apoyo al proxy  | TLMG.Client.Communication |
| Languages | Diccionarios Strings.\<cultura\>.xaml | XAML (sin namespace de código) |
| Styles | Estilos globales XAML | XAML (sin namespace de código) |
| Images | Imágenes (ver S4.4) | Sin namespace (recurso) |
| Connected Services | Proxy generado por svcutil.exe | Generado; no se edita a mano |
| Models | Modelos de presentación del cliente (por ejemplo, PlayerRankEntry) | TLMG.Client.Models |
| Navigation | Administrador de navegación, fábrica de pantallas, ScreenId y parámetros de navegación  | TLMG.Client.Navigation  |
| Behaviors | Comportamientos adjuntos de WPF  | TLMG.Client.Behaviors |

**Servidor (cada proyecto con prefijo TLMG.):**

| Proyecto | Carpetas | Namespaces |
| :---- | :---- | :---- |
| TLMG.Domain | Constants (ErrorKeys, ValidationRules y catálogos), Entities (datos puros del dominio, no las entidades de EF), Validators (validaciones del dominio), Repositories (interfaces y resultados de repositorio), Engine (motor de reglas)  | TLMG.Domain.Constants, TLMG.Domain.Entities, TLMG.Domain.Validators, TLMG.Domain.Repositories, TLMG.Domain.Engine  |
| TLMG.Contracts | raíz (interfaces WCF), DataContracts (DTO) | TLMG.Contracts, TLMG.Contracts.DataContracts |
| TLMG.Services | raíz (ServiceImplementation parcial), Accounts, Chat, Friends, Leaderboard, Lobby, Sessions, Helpers, Utilities, Factories  | LMG.Services, TLMG.Services.Accounts, TLMG.Services.Chat, TLMG.Services.Friends, TLMG.Services.Leaderboard, TLMG.Services.Lobby, TLMG.Services.Sessions, TLMG.Services.Helpers, TLMG.Services.Utilities, TLMG.Services.Factories  |
| TLMG.DataAccess | DatabaseModel (EF), Repositories | TLMG.DataAccess.DatabaseModel, TLMG.DataAccess.Repositories |
| TLMG.Server | raíz (arranque y configuración) | TLMG.Server |

Correcto: Views/LoginScreen.xaml.cs

```c#
namespace TLMG.Client.Views
{
    public partial class LoginScreen : LocalizableScreen
    {
    }
}
```

Incorrecto: el namespace no sigue a la carpeta

```c#
namespace TLMG
{
}
```

Incorrecto: carpeta en español

```c#
namespace TLMG.Client.Pantallas
{
}
```

4. ## Gestión de recursos gráficos (nomenclatura y ubicación) {#gestión-de-recursos-gráficos-(nomenclatura-y-ubicación)}

   Las imágenes del cliente viven en la carpeta Images/ del proyecto TLMG.Client. Los archivos llevan nombre en inglés, PascalCase, descriptivo del contenido (por ejemplo, DefaultAvatar.png, CardBack.png). Las imágenes no deben contener texto traducible: el texto se muestra desde los diccionarios de idioma (S7.3).

   Los recursos se compilan en el ensamblado con la acción de compilación indicada en la tabla y se referencian con un URI de paquete (pack URI). Se aceptan dos formas: la de ensamblado local (pack://application:,,,/Images/DefaultAvatar.png) y la que incluye el nombre del ensamblado (pack://application:,,,/TLMG.Client;component/Images/DefaultAvatar.png), Nunca usan rutas absolutas del disco.

| Tipo de recurso | Carpeta | Acción de compilación | Ejemplo |
| :---- | :---- | :---- | :---- |
| Imagen (PNG para elementos con transparencia, JPG para fondos fotográficos) | Images | Resource | Images/DefaultAvatar.png |
| Estilos | Styles | Page | Styles/GlobalStyles.xaml |
| Diccionario de idioma | Languages | Page | Languages/Strings.es-MX.xaml |

Correcto

```xml
<Image Source="pack://application:,,,/Images/DefaultAvatar.png" />
```

Incorrecto: ruta absoluta del equipo de un desarrollador

```xml
<Image Source="C:\Users\Martin\Desktop\default.jpg" />
```

5. # Capa de datos y persistencia {#capa-de-datos-y-persistencia}

   1. ## Acceso a datos con Entity Framework y repositorios {#acceso-a-datos-con-entity-framework-y-repositorios}

      Todo acceso a SQL Server se realiza con Entity Framework 6 desde el proyecto TLMG.DataAccess, mediante clases Repository con su interfaz I…Repository (S3.14). Ninguna otra capa referencia Entity Framework ni el contexto (S4.1). Se cumplen las siguientes reglas:  
* El contexto es de vida corta: se crea al iniciar cada operación y se libera al terminar con using. No se guarda en campos estáticos ni se comparte entre operaciones.  
* El repositorio devuelve resultados ya materializados (valores, objetos o listas), nunca IQueryable ni objetos que dependan de un contexto abierto.  
* Las consultas se escriben con LINQ to Entities o con procedimientos almacenados con parámetros. Nunca se concatena texto proporcionado por el jugador dentro de una instrucción SQL.  
* Las operaciones que deben ser atómicas (por ejemplo, las de códigos de verificación y registros pendientes) se resuelven en la base de datos mediante procedimientos almacenados.  
* Las excepciones de Entity Framework y de SQL Server (DbUpdateException, EntityException, SqlException) se capturan dentro de TLMG.DataAccess. El detalle técnico se registra en el log y hacia arriba solo sale un resultado controlado (S10.2). Las capas superiores no conocen esos tipos.  
* La concurrencia sobre un mismo registro se resuelve en la base de datos, no con construcciones de hilos (S11.1).

Ejemplo correcto

```c#
public bool PlayerExistsByNickname(string nickname)
{
    bool playerExists;
    using (var context = new TLMGContext())
    {
        playerExists = context.Players.Any(player => player.Nickname == nickname);
    }
    return playerExists;
}
```

Ejemplo incorrecto (contexto sin liberar, SQL concatenado e IQueryable expuesto)

```c#
public IQueryable<Player> GetPlayers()
{
    var context = new TLMGContext();
    return context.Players;
}

public bool PlayerExistsByNickname(string nickname)
{
    var context = new TLMGContext();
    string sql = "SELECT COUNT(*) FROM Player WHERE nickname = '" + nickname + "'";
    return context.Database.SqlQuery<int>(sql).First() > 0;
}
```

Que el repositorio devuelva datos ya materializados y no IQueryable es una regla del equipo.

2. ## Configuración externa (archivos de settings y credenciales) {#configuración-externa-(archivos-de-settings-y-credenciales)}

   Ningún secreto (contraseñas, cadenas de conexión con credenciales, claves) se escribe en el código ni en archivos rastreados por Git. La configuración que no es secreta sí se versiona.

   El cliente no contiene cadena de conexión a la base de datos ni credenciales: solo conoce la dirección del servidor. El servidor se conecta a SQL Server con una cuenta de base de datos de mínimo privilegio, con permisos solo sobre lo que utiliza (D-53; CONS-05).

| Dato | Dónde vive | ¿En Git? |
| :---- | :---- | :---- |
| Direcciones de endpoints WCF y tiempos límite del binding | App.config de cada proyecto | Sí |
| Configuración de log4net | log4net.config | Sí |
| Cadena de conexión a SQL Server | Solo en el servidor, en ConnectionStrings.config (referenciado con configSource) | No; en Git solo ConnectionStrings.config.example con marcadores, sin valores reales |
| Credenciales del correo (SMTP) | Archivo ignorado por Git y cifrado con DPAPI en el servidor (D-43) | No |
| Tiempos y reglas del juego | Constantes en el código (S10.4) | Sí |

La dirección que se versiona en el cliente es la del equipo de desarrollo (localhost); la IP de un entorno concreto se ajusta de forma local y no se sube a Git. 

El archivo .gitignore debe tener reglas explícitas para los archivos de secretos. Como DPAPI cifra por equipo, cada servidor genera su propio archivo cifrado, que no es portable.

Correcto: App.config solo apunta al archivo externo, ignorado por Git

```xml
<connectionStrings configSource="ConnectionStrings.config" />
```

Incorrecto: credencial en un App.config rastreado por Git

```xml
<add name="TLMG_DBEntities" connectionString="...User Id=tlmg;Password=[contraseña en texto plano];..." />
```

**Respaldo:** Microsoft (s.f.-i), *Connection strings and configuration files* (pie: 2022-07-23). La página documenta connectionStrings, el archivo externo con configSource y la protección con DPAPI o RSA, y confirma que con DPAPI la cadena solo se descifra en el equipo que la cifró. No dice literalmente “no pongas credenciales en texto plano”, así que esa regla es del curso (Zero-Trust y secretos fuera de Git), no de Microsoft.

3. ## Versionado del script de base de datos {#versionado-del-script-de-base-de-datos}

   La carpeta database/ contiene un único script vigente, con el nombre TLMG\_DB\_v\<mayor\>.sql. La versión mayor cambia cuando el esquema deja de ser compatible con la anterior, y la menor cuando solo se agregan elementos. Al cambiar la versión se renombra el archivo (git mv) y el historial anterior queda en Git. El encabezado del script lista cada versión con su fecha y una descripción breve del cambio.

   El script y el modelo de Entity Framework se actualizan en el mismo commit, y la versión vigente se menciona en la bitácora de decisiones.

> Los scripts de datos de demostración viven en database/seed/. No forman parte del esquema, no cambian su versión, no contienen contraseñas ni datos personales reales y los ejecuta una cuenta administradora, no la de la aplicación (S5.2; D-59).

6. # Capa de lógica de negocio y de juego {#capa-de-lógica-de-negocio-y-de-juego}

   1. ## Responsabilidades de los servicios y del dominio {#responsabilidades-de-los-servicios-y-del-dominio}

**Arquitectura hexagonal.** Domain conserva las reglas sin dependencias de EF, WCF o WPF. DataAccess encapsula persistencia. Services coordina casos de uso. El servidor valida y arbitra las acciones y el cliente presenta el resultado. Los ViewModels dependen de interfaces. Resolver dependencias mediante composición y fábricas, limitar el code-behind a interacción visual y vincular suscripciones y desuscripciones al ciclo de vida (§14.2).

**Concurrencia.** Preservar atomicidad, invariantes y orden de adquisición de locks. Mantener SQL y callbacks fuera de _sessionsLock. Al dividir un método, verificar qué estado debe observarse o actualizarse bajo el mismo bloqueo.

El servidor es la autoridad: el cliente se considera no confiable y toda operación se valida otra vez en el servidor (Zero-Trust). Cada capa tiene una responsabilidad única:

| Capa | Hace | No hace |
| :---- | :---- | :---- |
| ServiceImplementation | Recibe la llamada WCF, identifica la sesión y delega | Reglas ni acceso a datos |
| …Service | Orquesta la operación: valida, autoriza, invoca el dominio y los repositorios, y arma la respuesta | Reglas del juego (van en TLMG.Domain) ni consultas SQL |
| TLMG.Domain (cuando exista) | Decide si una jugada es legal y calcula su resultado | Red, datos ni notificaciones |
| …Repository | Guarda y consulta datos | Reglas ni validación de entrada |

Toda operación del servidor sigue este orden: (1) validar el formato de la entrada y responder con un ErrorKey si falla (S16.1); (2) comprobar que la sesión es válida y que el jugador tiene permiso para esa acción; (3) aplicar la regla; (4) persistir si corresponde; (5) responder o notificar con el resultado, sin revelar información ajena al destinatario.

Correcto: valida primero y solo después persiste

```c#
public string Register(string nickname, string email, string password)
{
    string errorKey = PlayerAccountRegistrationValidationHelper.Validate(nickname, email, password);
    if (errorKey == null)
    {
        string passwordHash = HashPassword(password);
        _playerRepository.CreatePlayer(nickname, email, passwordHash);
    }
    return errorKey;
}
```

Incorrecto: guarda antes de validar

```c#
public void Register(string nickname, string email, string password)
{
    string passwordHash = HashPassword(password);
    _playerRepository.CreatePlayer(nickname, email, passwordHash);
    string errorKey = PlayerAccountRegistrationValidationHelper.Validate(nickname, email, password);
}
```

**Respaldo:** ADR-01 (Zero-Trust), ADR-02, S4.1 y S4.2, D-49. El orden de cinco pasos es una convención del equipo. 

2. ## Patrones de diseño utilizados y su justificación {#patrones-de-diseño-utilizados-y-su-justificación}

La siguiente tabla recoge solo los patrones que el proyecto usa o que ya tienen una decisión (ADR o bitácora). Un patrón nuevo se agrega aquí cuando se implementa o se decide, con su ventaja, su desventaja y la alternativa descartada. Los patrones se eligen por necesidad; no se agregan por tenerlos.

| Patrón | Dónde se aplica | Estado y respaldo | Ventaja | Desventaja | Alternativa descartada |
| :---- | :---- | :---- | :---- | :---- | :---- |
| Repository | TLMG.DataAccess (S5.1) | En uso. SAD C-08 | Aísla Entity Framework y permite sustituirlo en pruebas | Más interfaces y clases | Usar el contexto de EF desde la lógica |
| Callback dúplex de WCF | Contratos I…ManagerCallback (S11.1) | En uso. ADR-03 | El servidor notifica sin que el cliente pregunte | El cliente recibe avisos fuera del hilo de la interfaz y debe pasarlos al Dispatcher | Consulta periódica del cliente |
| Diccionarios de recursos intercambiables | LanguageManager (S7.3) | En uso. ADR-06 | Cambio de idioma en caliente, sin reiniciar | Hay que mantener la paridad de claves en dos archivos | Recargar ventanas o usar archivos .resx |
| MVVM con Data Binding | Cliente (S7.1) | Decidido; migración por pantallas. ADR-03, D-39, D-50 | Separa la lógica de la vista y la hace comprobable | Más archivos y más código | Lógica en el code-behind |
| Servidor autoritativo con DTO asimétrico | Contratos y servicios | Decidido. ADR-01 | El cliente nunca recibe cartas ajenas ocultas | El servidor arma una proyección por jugador | Sincronización entre pares |
| Puertos y adaptadores | TLMG.Domain (S4.2) | PENDIENTE | Reglas comprobables en memoria | Más indirección al inicio | Reglas dentro de los servicios |
| Máquina de estados | Ciclo de vida de sala, partida y sesión | Decidido; por implementar. ADR-04 | Impide jugadas en fases indebidas | Más clases por estado | Banderas booleanas sueltas |
| Instantánea (Memento) | Reconexión de un jugador | Decidido; por implementar. ADR-04 | Restaura la mesa completa tras reconectar | Costo de serializar al reconectar | Reenviar solo cambios sueltos |
| Registro de sesiones (SessionRegistry) | Servidor | Decidido; por implementar. D-41, D-58 | Un solo lugar conoce a los jugadores conectados y une los canales de un mismo jugador por su sessionToken  | Estado compartido entre sesiones; se protege con un único lock (S11.1)  | Cada contrato con su lista; ConcurrentDictionary; InstanceContextMode.Single  |
| Limitador de tasa (Token Bucket) | Envío del código 2FA | Decidido, función apagada por ahora. ADR-05, D-42 | Protege la cuenta de correo | Un reenvío rápido puede rechazarse | Cola con hilo propio (choca con S11.1) |
| Inyección de dependencias | Raíz de composición de cada solución | Decidido con condiciones; implementación diferida. D-51 | Dependencias por interfaz y sustituibles en pruebas | Configuración adicional y WCF necesita un proveedor propio de instancias | Crear las dependencias a mano |
| Observador (eventos .NET)  | Capa Comunicación del cliente (ServerConnectionManager)  | Decidido; por implementar. D-60, ADR-08  | Las pantallas reaccionan a los callbacks del servidor sin conocer WCF  | Hay que desuscribir en OnScreenUnloaded (S14.2)  | Proxy y callback propios en cada pantalla |

**Respaldo:** SAD S3 (ADR-01 a ADR-06) y S4.1, Drivers (definiciones de patrones) y bitácoras (D-41, D-42, D-50, D-51). Las ventajas y desventajas son las de los Drivers, resumidas.

7. # Capa de presentación/UI {#capa-de-presentación/ui}

   1. ## Separación entre lógica y vista {#separación-entre-lógica-y-vista}

Cada pantalla tiene un ViewModel en la carpeta ViewModels, nombrado con el nombre de la pantalla y el sufijo ViewModel (por ejemplo, LoginScreen usa LoginViewModel). La vista (XAML y code-behind) solo contiene InitializeComponent, la asignación del DataContext y lógica puramente visual, como animaciones. Las validaciones y las llamadas al servidor van en el ViewModel (D-39).

Los ViewModels heredan de ObservableObject, del paquete CommunityToolkit.Mvvm, que ya implementa INotifyPropertyChanged; cada propiedad que cambia notifica a la vista con SetProperty. Las acciones se exponen como propiedades de tipo ICommand con el sufijo Command, creadas con RelayCommand (o RelayCommand\<T\> si reciben un parámetro) y un método con nombre, sin expresiones lambda. Del paquete solo se usan estas clases: no se emplean sus generadores de código (\[ObservableProperty\] y \[RelayCommand\]) ni AsyncRelayCommand (S11.1).

Correcto

```c#
private string _email;

public string Email
{
    get { return _email; }
    set { SetProperty(ref _email, value); }
}
```

Incorrecto: asigna el campo sin notificar a la vista

```c#
public string Email
{
    get { return _email; }
    set { _email = value; }
}
```

Correcto (comando)

```c#
public class LoginViewModel : ObservableObject
{
    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(ExecuteLogin);
    }

    public ICommand LoginCommand { get; }

    private void ExecuteLogin()
    {
    }
}
```

Incorrecto (comando con lambda)

```c#
LoginCommand = new RelayCommand(() => ExecuteLogin());
```

**Respaldo:** Microsoft (s.f.-w) documenta ObservableObject y SetProperty, y Microsoft (s.f.-}) documenta RelayCommand. Exponer los comandos con un método nombrado y sin lambdas es una regla del equipo(S9.8; D-50).

El ViewModel no referencia controles de WPF ni muestra ventanas. Para los errores expone la clave del recurso (ErrorKey, S10.2), nunca el texto traducido; la vista la resuelve con el diccionario activo.

Excepción: la propiedad Password del PasswordBox no admite enlace de datos. La vista copia su valor al ViewModel desde el manejador PasswordChanged; el ViewModel recibe una cadena y no conoce el control.

Ejemplo correcto (el manejador copia la contraseña al ViewModel)

```c#
private void PwdPassword_PasswordChanged(object sender, RoutedEventArgs e)
{
    if (sender is PasswordBox passwordBox && DataContext is LoginViewModel viewModel)
    {
        viewModel.Password = passwordBox.Password;
    }
}
```

2. ## Nombrado de elementos de interfaz {#nombrado-de-elementos-de-interfaz}

Los métodos manejadores de eventos (event handlers) en la capa de UI deberán nombrarse siguiendo el patrón NombreDelControl\_Evento, utilizando PascalCase tanto para el nombre del control (incluyendo su prefijo de tipo) como para el nombre del evento (por ejemplo, BtnLogIn\_Click). Para métodos que gestionan eventos del ciclo de vida de la ventana o vista, se utilizarán los nombres OnScreenLoaded y OnScreenUnloaded. Todos los manejadores de eventos se declararán con visibilidad privada en el code-behind y mantendrán la firma estándar de .NET (object sender, RoutedEventArgs e o el argumento de evento correspondiente). sender es el objeto que originó el evento (por ejemplo, el control) y e contiene los datos del evento. de .NET (object sender, RoutedEventArgs e). La regla de nombres descriptivos de S9.8 aplica a los parámetros de expresiones lambda, no a la firma de los manejadores.

Ejemplo correcto

```c#
private const double HoverOpacity = 0.8;

private void ImgCardBack_MouseEnter(object sender, MouseEventArgs e)
{
    imgCardBack.Opacity = HoverOpacity;
}

private void OnScreenLoaded(object sender, RoutedEventArgs e)
{
    LanguageManager.LanguageChanged += OnLanguageChanged;
}
```

Ejemplo incorrecto (acción de negocio en un manejador Click)

```c#
private void BtnLogIn_Click(object sender, RoutedEventArgs e)
{
    string email = txtEmail.Text;
    string password = pwdPassword.Password;
}
```

Todo elemento XAML al que se le asigne un x:Name para ser referenciado desde el code-behind deberá nombrarse utilizando camelCase prefijado obligatoriamente con las 3-4 letras correspondientes al tipo de control (por ejemplo, btn, txt, pwd, lbl, cmb, chk, img, grd, lst, rect, brd, ell, sld, thm), seguido de una descripción breve de su propósito en el dominio.

En las pantallas con MVVM, las acciones de los botones se enlazan a comandos (Command="{Binding LoginCommand}") y no llevan manejador Click. El patrón NombreDelControl\_Evento queda para eventos que no tienen comando, como los de ciclo de vida (OnScreenLoaded) o las animaciones.

Esta regla constituye una **excepción documentada a la sección 3.2** (campos privados e internos con prefijo de guión bajo). El motivo es técnico: cuando un elemento XAML tiene x:Name, el compilador de marcado genera un campo en la clase parcial que, por defecto, es internal, no private, por lo que aplicar literalmente el prefijo \_ generaría inconsistencia visual con el resto del código sin aportar claridad real. En su lugar, el prefijo por tipo de control cumple el mismo propósito que persigue la sección 3.2 (distinguir visualmente la categoría de un identificador): permite diferenciar de un vistazo un campo generado por XAML tanto de una variable local (camelCase sin prefijo, sección 3.1) como de un campo privado ordinario (camelCase con \_, sección 3.2).

Prefijos adoptados por el equipo (D-22, D-25):

| Prefijo | Tipo de control |
| :---- | :---- |
| btn | Button |
| txt | TextBox |
| pwd | PasswordBox |
| lbl | Label / TextBlock usado como etiqueta |
| cmb | ComboBox |
| chk | CheckBox |
| img | Image |
| grd | Grid (solo cuando se referencia desde code-behind) |
| lst | ListBox / ListView / ItemsControl |
| rect | Rectangle |
| brd | Border |
| ell | Ellipse |
| sld | Slider |
| thm | Thumb |
| pop | Popup |
| scr | ScrollViewer |
| pnl | StackPanel / WrapPanel / DockPanel / Canvas |

Los elementos declarados dentro de un ControlTemplate o Style (por ejemplo PART\_Track, PART\_ContentHost o partes referenciadas por triggers) quedan exentos de esta tabla, porque su nombre lo exige WPF o la propia plantilla.

Ejemplo correcto

```xml
<PasswordBox x:Name="pwdPassword" PasswordChanged="PwdPassword_PasswordChanged" Width="250" Height="30"/>

<Image x:Name="imgCardBack" Source="pack://application:,,,/Images/CardBack.png" MouseEnter="ImgCardBack_MouseEnter"/>

<Button Content="{DynamicResource Login_LogInButton}" Command="{Binding LoginCommand}"/>
```

Ejemplo incorrecto

```xml
<TextBox x:Name="Email" Width="250" Height="30"/>

<PasswordBox x:Name="_password" Width="250" Height="30"/>

<Button x:Name="button1" Content="{DynamicResource Login_LogInButton}" Click="Button1_Click"/>

<TextBlock x:Name="ErrorMessageShownWhenTheLoginFails" Foreground="Red" Visibility="Collapsed"/>

<Button x:Name="btnLogIn" Content="{DynamicResource Login_LogInButton}" Click="BtnLogIn_Click"/>
```

3. ## Localización de textos y alcance de claves {#localización-de-textos-y-alcance-de-claves}

Ningún texto visible para el jugador (títulos, botones, etiquetas, mensajes del sistema, mensajes de error) deberá escribirse como cadena fija dentro del XAML. Todo texto localizable deberá declararse como un recurso con clave en un diccionario de recursos por cultura, y consumirse en el XAML mediante {DynamicResource}, nunca mediante {StaticResource}, ya que este último no permite refrescar el valor cuando el usuario cambia de idioma sin reiniciar la ventana.

Convenciones:

* Cada cultura soportada tendrá su propio archivo ResourceDictionary, ubicado en la carpeta Languages/, nombrado Strings.{cultura}.xaml (ej. Strings.es-MX.xaml, Strings.en-US.xaml).

  * La clave de cada recurso sigue el formato NombrePantalla\_NombreControl\[Rol\], en PascalCase y en inglés (D-21). NombreControl nombra el propósito del texto; si el control tiene x:Name, normalmente coincide con él sin su prefijo de tipo; cuando un mismo control muestra varios textos asignados desde código (mensajes de error, estados), cada clave nombra su mensaje o estado (por ejemplo, Login\_InvalidCredentialsError). La relación entre cada clave y su x:Name queda registrada en el diccionario de internacionalización del proyecto. Rol es un sufijo opcional que describe para qué se usa el texto y se elige de esta lista cerrada: Button, Label, Title, Subtitle, Hint, Placeholder, Error, Message, Tab, Format, Status, Description, Instructions, Header. Excepción: cuando varias claves forman una lista o un conjunto (por ejemplo, las pistas de contraseña), el rol puede ir antes del detalle (CreateAccount\_HintLength). El sufijo nunca es la clase del control (TextBlock, Border, etc.), ya que la clave identifica el texto, no el control.

  * Excepción: pueden escribirse directamente en el XAML, sin clave de recurso, (1) los glifos e íconos decorativos (emojis, símbolos) que no transmiten texto y no cambian entre culturas, y (2) los nombres de los idiomas en el selector, escritos en su propia lengua ("Español", "English"), porque deben reconocerse sin importar el idioma activo. Si un glifo comunica algo que el usuario necesita entender y no lleva texto al lado, la excepción no aplica: debe acompañarse de un texto localizado.

  * El cambio de idioma en tiempo de ejecución se centraliza en una única clase (LanguageManager), responsable de reemplazar el diccionario de idioma en Application.Current.Resources.MergedDictionaries en su misma posición (preservando el orden de otros recursos como estilos globales). Ninguna otra clase deberá manipular MergedDictionaries directamente.

  * Alcance de las claves: Las claves de recursos generales o compartidos entre múltiples vistas deben prefijarse con Common\_ (por ejemplo, Common\_AcceptButton, Common\_CancelButton o Common\_EgyptianCatLabel). Por otro lado, una clave usada en una sola pantalla lleva como prefijo el nombre de esa pantalla (por ejemplo, Login\_LogInButton). Si un mismo texto lo usan varios controles de la misma pantalla, la clave se nombra por la acción y no por un control específico (por ejemplo, Lobby\_CancelKickButton). Cuando un recurso se use exclusivamente dentro de un ControlTemplate o Style redefinido (como los estilos globales definidos en App.xaml), la clave no llevará prefijo de pantalla y se nombrará directamente con la categoría o nombre del control y su rol (por ejemplo, CustomTextBox\_WatermarkHint o PrimaryButton\_ContentText). Los textos de un catálogo del dominio (como los nombres de los seis gatos) se consideran generales y llevan Common\_ aunque hoy se muestren en una sola pantalla. Un mismo texto puede repetirse en claves de pantallas distintas; Common\_ se usa cuando una misma clave se consume desde más de una pantalla. (D-30, D-37)

**Ejemplo correcto**

Mapeo de elementos a claves de recurso:

* Botón de iniciar sesión (Login) → clave: Login\_LogInButton  
* Texto de ayuda del apodo (CreateAccount) → clave: CreateAccount\_NicknameHint  
* Título del menú principal → clave: MainMenu\_Title  
* Etiqueta del gato egipcio → clave: Common\_EgyptianCatLabel

Languages/Strings.es-MX.xaml

```xml
<sys:String x:Key="Login_LogInButton">Iniciar Sesión</sys:String>
```

LoginScreen.xaml

```xml
<Button x:Name="btnLogIn" Content="{DynamicResource Login_LogInButton}"/>
```

LanguageManager.cs

```c#
using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;

namespace TLMG.Client
{
    /// <summary>
    /// Manages the culture and the language resource dictionaries of the application. 
    /// </summary>
    public static class LanguageManager
    {
        private const int NotFoundIndex = -1;
        private const int FirstIndex = 0;

        private static readonly Regex _languageDictionaryRegex = new Regex(
            @"Languages[/\\]Strings\.[a-zA-Z]{2}-[a-zA-Z]{2}\.xaml",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Raised after the application language changes. 
        /// </summary>
        public static event EventHandler LanguageChanged;

        /// <summary>
        /// Applies the requested language to localized resources when the application is available.
        /// </summary>
        /// <param name="cultureCode">Culture code to apply (for example, "es-MX" or "en-US").</param> 
        public static void ChangeLanguage(string cultureCode)
        {
            if (Application.Current != null)
            {
                ApplyLanguage(cultureCode);
            }
        }

        private static void ApplyLanguage(string cultureCode)
        {
            ResourceDictionary newDictionary = new ResourceDictionary()
            {
                Source = new Uri($"pack://application:,,,/Languages/Strings.{cultureCode}.xaml")
            };

            Collection<ResourceDictionary> mergedDictionaries =
                Application.Current.Resources.MergedDictionaries;
            int targetIndex = NotFoundIndex;

            for (int dictionaryIndex = FirstIndex;
                 dictionaryIndex < mergedDictionaries.Count && targetIndex == NotFoundIndex;
                 dictionaryIndex++)
            {
                ResourceDictionary currentDictionary = mergedDictionaries[dictionaryIndex];
                if (currentDictionary.Source != null &&
                    _languageDictionaryRegex.IsMatch(currentDictionary.Source.OriginalString))
                {
                    targetIndex = dictionaryIndex;
                }
            }

            if (targetIndex != NotFoundIndex)
            {
                mergedDictionaries[targetIndex] = newDictionary;
            }
            else
            {
                mergedDictionaries.Add(newDictionary);
            }

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
```

Ejemplo incorrecto

```xml
<Button x:Name="btnLogIn" Content="Iniciar Sesión"/>

<Button x:Name="btnLogIn" Content="{StaticResource Login_LogInButton}"/>
```

**Respaldo:** Microsoft (s.f.-z) documenta la forma pack://application:,,,/Ruta de los URI de recursos del ensamblado local.

La forma con el nombre del ensamblado y ;component es la que Microsoft documenta para ensamblados referenciados; para el ensamblado local, el equipo acepta ambas formas.

8. # Control de versiones {#control-de-versiones}

   1. ## Herramienta y repositorio remoto {#herramienta-y-repositorio-remoto}

El control de versiones es Git y el repositorio remoto (origin) está alojado en GitHub. El repositorio contiene las carpetas Client/, Server/, database/, docs/ y gov/ descritas en S4.3. Los secretos no se versionan (S5.2).

2. ## Política de commits {#política-de-commits}

Los commits los hace el desarrollador, no la herramienta de IA, y no llevan líneas de atribución automática (D-47). El mensaje se escribe en español, en una sola frase que dice qué cambió, y puede terminar con una referencia entre paréntesis a la decisión o al pendiente que atiende. Los archivos que deben cambiar juntos (por ejemplo, el script de la base de datos y el modelo de Entity Framework, S5.3) van en el mismo commit.

Ejemplo correcto

```
Renombrar las clases de lógica con el sufijo Service (D-49)
Agregar la validación del nickname en el servidor (avance de BL-026)
```

Ejemplo incorrecto

```
fix
Update login screen
```

3. ## Archivos ignorados (.gitignore) {#archivos-ignorados-(.gitignore)}

El repositorio parte de la plantilla de Visual Studio para .gitignore. Los archivos de secretos (la cadena de conexión y las credenciales del correo, S5.2) deben tener reglas explícitas en este archivo. 

9. # Estilo de código {#estilo-de-código}

   1. ## Indentación {#indentación}

El código deberá indentarse utilizando el espaciado predeterminado configurado en el IDE del equipo (cuatro espacios por nivel), sin usar caracteres de tabulación explícitos. No se exige que cada integrante modifique manualmente la configuración de su editor; basta con que el IDE esté ajustado para traducir la tecla Tab en el número de espacios acordado, garantizando una indentación visualmente consistente entre distintos entornos.

Ejemplo correcto (4 espacios por nivel)

```c#
public class CardDealer
{
    public void DealCard()
    {
        if (_activeCardCount < MaxCardsInHand)
        {
            CreateCard();
        }
    }
}
```

Ejemplo incorrecto (2 espacios por nivel)

```c#
public class CardDealer
{
  public void DealCard()
  {
    if (_activeCardCount < MaxCardsInHand)
    {
      CreateCard();
    }
  }
}
```

Ejemplo incorrecto (tabulaciones)

```c#
public class CardDealer
{
	public void DealCard()
	{
		if (_activeCardCount < MaxCardsInHand)
		{
			CreateCard();
		}
	}
}
```

2. ## Límite de columna {#límite-de-columna}

Las sentencias largas deberán dividirse en varias líneas para mejorar la claridad y la experiencia de lectura. El equipo adopta un límite de 120 caracteres por línea, que aplica también a los comentarios (D-35), definido formalmente con base en la resolución de pantalla de trabajo de ambos integrantes 1920×1080 y la guía vertical de columna configurada en Visual Studio. Este valor se aplicará de forma consistente en todo el proyecto y solo podrá modificarse por acuerdo explícito del equipo.

Ejemplo correcto

```c#
int totalRoundScore = CalculateTotalRoundScore(
    currentPlayerBidAmount,
    accumulatedBonusMultiplier,
    penaltyCardsCount,
    currentRoundNumber);
```

Ejemplo incorrecto (línea de más de 120 columnas sin dividir)

```c#
int totalRoundScore = CalculateTotalRoundScore(currentPlayerBidAmount, accumulatedBonusMultiplier, penaltyCardsCount, currentRoundNumber);
```

Al dividir una sentencia, la continuación lleva una sangría adicional de un nivel (cuatro espacios). El corte se hace después del operador de asignación (=), después de un operador lógico (&& o \|\|), con un argumento por línea dentro de los paréntesis de una llamada, o antes del punto en una cadena de llamadas encadenadas. El límite aplica también a los comentarios (// y ///): un texto que no cabe se acorta o continúa en otra línea del mismo tipo.

Ejemplo correcto

```c#
List<string> activePlayerNames = players
    .Where(IsActive)
    .Select(GetPlayerName)
    .ToList();
```

Ejemplo incorrecto (línea de 128 columnas sin cortar)

```c#
List<string> activePlayerNames = players.Where(IsActive).Select(GetPlayerName).OrderBy(GetSortKey).ThenBy(GetJoinDate).ToList();
```

3. ## Uso de espacios en blanco (Vertical y horizontal) {#uso-de-espacios-en-blanco-(vertical-y-horizontal)}

Cada línea deberá contener una única sentencia y una única declaración de variable.

Ejemplo correcto

```c#
int playerScore = 0;
int cardCount = 0;
```

Ejemplo incorrecto

```c#
int playerScore = 0; int cardCount = 0;
```

Deberá dejarse al menos una línea en blanco entre definiciones de métodos y de propiedades.

Ejemplo correcto (una línea en blanco entre miembros)

```c#
public int Score { get; private set; }

public void AddScore(int amount)
{
}
```

Ejemplo incorrecto (sin línea en blanco entre miembros)

```c#
public int Score { get; private set; }
public void AddScore(int amount)
{
}
```

No deberá insertarse un espacio en blanco inmediatamente después de un paréntesis de apertura ni justo antes de uno de cierre.

Ejemplo correcto

```c#
if (isGameOver)
{
    ShowGameOverScreen();
}
```

Ejemplo incorrecto

```c#
if ( isGameOver )
{
    ShowGameOverScreen();
}
```

4. ## Organización del archivo (Usings, namespace, clase) {#organización-del-archivo-(usings,-namespace,-clase)}

Las directivas using deberán colocarse fuera de la declaración del namespace, al inicio del archivo, conforme al valor predeterminado de la regla de estilo oficial de .NET. Las directivas using se agrupan en tres bloques separados por una línea en blanco, cada bloque en orden alfabético: (1) System.\*, (2) librerías de terceros (por ejemplo log4net) y (3) namespaces propios (TLMG.\*).

Ejemplo correcto

```c#
using System;
using System.Collections.Generic;

using log4net;

using TLMG.DataAccess.Repositories;
using TLMG.Services;

namespace TLMG.Services.Accounts
{
    public class PlayerAccountRegistrationService
    {
    }
}
```

Ejemplo incorrecto (sin separación entre usings nativos y propios, ni orden alfabético)

```c#
using System;
using TLMG.Services;
using System.Collections.Generic;
using TLMG.DataAccess.Repositories;
```

5. ## Orden de miembros dentro de la clase {#orden-de-miembros-dentro-de-la-clase}

Los miembros de una clase se ordenarán de la siguiente manera: constantes, campos estáticos, campos de instancia, constructores, eventos, propiedades, métodos y tipos anidados privados; dentro de cada categoría, de mayor a menor accesibilidad (público, interno, protegido, privado). Los manejadores de eventos de la interfaz son métodos privados y se agrupan juntos entre los métodos privados.

Ejemplo correcto

```c#
public class Player
{
    private const int MaxScore = 100;

    private int _score;

    public Player(int initialScore)
    {
        _score = initialScore;
    }

    public int Score => _score;

    public void AddScore(int amount)
    {
        _score = Math.Min(_score + amount, MaxScore);
    }
}
```

Ejemplo incorrecto (orden invertido: métodos y propiedades antes que los campos)

```c#
public class Player
{
    public void AddScore(int amount)
    {
        _score = Math.Min(_score + amount, MaxScore);
    }

    public int Score => _score;

    public Player(int initialScore)
    {
        _score = initialScore;
    }

    private int _score;

    private const int MaxScore = 100;
}
```

6. ## Uso de llaves {#uso-de-llaves}

Se establece que debe usarse el estilo "Allman" para las llaves: la llave de apertura y la de cierre en su propia línea nueva, alineadas con el nivel de indentación actual.

Ejemplo correcto

```c#
if (isGameOver)
{
    ShowGameOverScreen();
}
```

Ejemplo incorrecto

```c#
if (isGameOver) {
    ShowGameOverScreen();
}
```

Todo bloque de código controlado por una estructura condicional o un bucle deberá usar llaves, incluso si contiene una única sentencia. La documentación oficial de reglas de estilo de .NET incluye esta opción (csharp\_prefer\_braces \= true) como configuración disponible y verificable por el compilador

Ejemplo correcto

```c#
if (isGameOver)
{
    ShowGameOverScreen();
}
```

Ejemplo incorrecto

```c#
if (isGameOver)
    ShowGameOverScreen();
```

7. ## Uso de var vs tipos explícitos {#uso-de-var-vs-tipos-explícitos}

Deberá usarse tipado implícito (var) únicamente cuando el tipo sea evidente a partir del lado derecho de la asignación, lo cual incluye el uso de un operador new, una conversión explícita o un literal. Asimismo, deberá usarse var en las variables de consulta LINQ escritas en sintaxis de consulta (from ... select) y cuando el tipo producido sea anónimo. En cualquier otro caso deberá usarse el tipo explícito; esto incluye asignaciones desde una llamada a método o propiedad, variables de iteración de foreach, y cadenas de métodos LINQ que terminan en .ToList(), .ToArray(), .First() o similares.

Ejemplo correcto

```c#
var playerHand = new PlayerHand();
var roundNumber = 3;
```

Ejemplo incorrecto

```c#
var roundScore = ResolveRound();
```

Se indica que no debe usarse var cuando el tipo no es evidente por el lado derecho de la asignación, y que no debe asumirse que el tipo es evidente a partir del nombre de un método; se aclara que un tipo se considera evidente si proviene de un operador new, una conversión explícita o la asignación de un valor literal.

Ejemplo correcto

```c#
int roundScore = ResolveRound();
```

Ejemplo incorrecto

```c#
var roundScore = ResolveRound();
```

En la declaración de variables de consulta LINQ y variables de rango, deberá preferirse el tipado implícito, incluso cuando esta guía sea distinta a la regla general de variables locales, debido a que las consultas LINQ suelen producir tipos anónimos o genéricos anidados.

Ejemplo correcto (var en consulta LINQ)

```c#
var highScorePlayers = from player in players
                       where player.Score > HighScoreThreshold
                       select player.Name;
```

Ejemplo incorrecto (tipo explícito en una consulta LINQ)

```c#
IEnumerable<string> highScorePlayers = from player in players
                                       where player.Score > HighScoreThreshold
                                       select player.Name;
```

8. ## Expresiones lambda y LINQ (reglas de nombres y sintaxis) {#expresiones-lambda-y-linq-(reglas-de-nombres-y-sintaxis)}

Las variables de rango en consultas LINQ y los parámetros en expresiones lambda deberán utilizar nombres descriptivos que identifiquen claramente el elemento procesado; se prohíbe estrictamente el uso de identificadores de una sola letra (como x o e). Todos los parámetros de lambda deberán ser descriptivos (por ejemplo, eventSender, eventArguments, player, Card). En consultas LINQ con sintaxis de consulta, las cláusulas deberán alinearse verticalmente bajo la cláusula inicial from, y la cláusula where deberá anteceder a las operaciones de ordenamiento o proyección.

Ejemplo correcto

```c#
var eliminatedPlayers = from player in players
                        where player.Score <= ZeroScore
                        select player;
```

Ejemplo incorrecto (variable de rango de una sola letra)

```c#
var eliminatedPlayers = from p in players
                        where p.Score <= ZeroScore
                        select p;
```

En consultas LINQ con sintaxis de consulta, las cláusulas deberán alinearse verticalmente bajo la cláusula inicial from. Asimismo, la cláusula where deberá anteceder a las operaciones de ordenamiento o proyección para asegurar la filtración previa de los datos.

Ejemplo correcto

```c#
var topScorers = from player in players
                 where player.Score > MinimumScoreThreshold
                 orderby player.Score descending
                 select player.Name;
```

Ejemplo incorrecto (orderby antes de where y sin alinear)

```c#
var topScorers = from player in players
    orderby player.Score descending
    where player.Score > MinimumScoreThreshold
    select player.Name;
```

Al escribir una condición dentro de una cláusula where o un predicado de Where, no deberán usarse literales numéricos sin nombre; el valor deberá declararse previamente como constante con nombre descriptivo, conforme a la regla general de la sección 10.4.

Ejemplo correcto

```c#
private const int ZeroScore = 0;

var eliminatedPlayers = from player in players
                        where player.Score <= ZeroScore
                        select player;
```

Ejemplo incorrecto (literal sin nombre)

```c#
var eliminatedPlayers = from player in players
                        where player.Score <= 0
                        select player;
```

Se prohíbe el uso de expresiones lambda con cuerpo de bloque ({ }) que contengan múltiples sentencias, estructuras de control o efectos secundarios (como modificación de estado o llamadas a métodos ajenos al filtro/proyección). En su lugar, dicha lógica deberá extraerse a un método privado con nombre descriptivo.

Ejemplo correcto

```c#
List<Player> eliminatedPlayers = players.Where(IsEliminated).ToList();

private bool IsEliminated(Player player)
{
    bool isEliminated = false;
    if (player.Score <= ZeroScore)
    {
        isEliminated = true;
    }
    return isEliminated;
}
```

Ejemplo incorrecto (lambda con bloque, lógica condicional y efecto secundario oculto dentro de un LINQ)

```c#
List<Player> eliminatedPlayers = players.Where(player =>
{
    bool isEliminated = false;
    if (player.Score <= ZeroScore)
    {
        isEliminated = true;
    }
    return isEliminated;
}).ToList();
```

Uso en retrollamadas (callbacks): Se permite el uso de expresiones lambda que consistan en una sola sentencia como retrollamadas directas en delegados. Si una retrollamada requiere más de una sentencia, deberá extraerse obligatoriamente a un método privado con nombre.

Restricción en suscripción a eventos: Queda estrictamente prohibido suscribir expresiones lambda anónimas a eventos mediante el operador \+= cuando posteriormente se requiera desuscripción, ya que al carecer de una referencia nombrada se imposibilita su cancelación mediante \-= y se generan fugas de memoria (memory leaks). Para estos casos, se deberá utilizar obligatoriamente un método manejador privado (event handler).

Ejemplo correcto (retrollamada de una sola sentencia)

```c#
Dispatcher.BeginInvoke(new Action(() => lblStatusMessage.Visibility = Visibility.Collapsed));
```

Ejemplo incorrecto (retrollamada con varias sentencias en una lambda)

```c#
Dispatcher.BeginInvoke(new Action(() => { lblStatusMessage.Visibility = Visibility.Collapsed; _log.Info("Mensaje ocultado."); }));
```

Ejemplo correcto (método nombrado que permite desuscribirse)

```c#
LanguageManager.LanguageChanged += OnLanguageChanged;

LanguageManager.LanguageChanged -= OnLanguageChanged;
```

Ejemplo incorrecto (lambda anónima imposible de desuscribir)

```c#
LanguageManager.LanguageChanged += (eventSender, eventArguments) => ApplyTexts();
```

10. # Prácticas de programación {#prácticas-de-programación}

    1. ## Uso de override y anotaciones de atributos {#uso-de-override-y-anotaciones-de-atributos}

Cuando un método proporcione una nueva implementación de un método heredado de una clase base, deberá declararse con el modificador override; el método sobreescrito en la clase base deberá ser virtual, abstract u override.

Ejemplo correcto

```c#
public abstract class Card
{
    public abstract int CalculateScore();
}

public class BonusCard : Card
{
    public override int CalculateScore()
    {
        int calculatedScore = BaseScore;
        return calculatedScore;
    }
}
```

Ejemplo incorrecto (no usa override)

```c#
public class BonusCard : Card
{
    public int CalculateScore()
    {
        return 100;
    }
}
```

Toda clase que defina un atributo personalizado deberá nombrarse con el sufijo Attribute, conforme a la convención del lenguaje.

Ejemplo correcto

```c#
public class GameTagAttribute : Attribute
{
}
```

Ejemplo incorrecto

```c#
public class GameTag : Attribute
{
}
```

2. ## Manejo de excepciones (jerarquía, bloques catch) {#manejo-de-excepciones-(jerarquía,-bloques-catch)}

Las capturas deberán realizarse en el límite que pueda tratar el fallo. Se evitarán los bloques catch vacíos, artificiales y genéricos indiscriminados.

**Limpieza.** Evitar catch when y try-finally sin catch. Preservar liberación de recursos, por ejemplo con using para IDisposable. Usar try-catch-finally cuando exista una excepción que realmente se trate y una limpieza necesaria. Evitar introducir un catch artificial.

**Comunicación y registro.** En SafeNotify para callbacks y en Comunicación del cliente, tratar CommunicationException, TimeoutException y ObjectDisposedException según proceda. Gestionar cierre, aborto de canales defectuosos y recuperación. Registrar una vez en el límite responsable y pasar el objeto excepción al logger cuando corresponda. Usar DEBUG para timeout recuperable de heartbeat, WARN para pérdida del canal y ERROR para fallos inesperados, preservando los casos FATAL de §10.3.

Errores entre servidor y cliente (D-45). Del servidor al cliente, una validación o una regla de juego que falla no lanza excepción: se responde con un ErrorKey, que el cliente traduce a su idioma con el diccionario activo. FaultException se usa solo para fallos inesperados y lleva un mensaje genérico, sin detalle técnico. El detalle (excepción y traza de pila) va solo al log del servidor. Del cliente al servidor, el servidor valida de nuevo todo lo que recibe y responde de forma controlada. Al jugador nunca se le muestra qué falló por dentro.

Las respuestas públicas deberán ajustarse al contrato de OperationResult de §17.1 (D-66). Una respuesta nula o una clave de fallo ausente o desconocida deberá tratarse como fallo controlado.

Las convenciones privadas dependen del contrato de cada capa: (1) los validadores de TLMG.Domain.Validators que devuelven ErrorKey usan string.Empty como ausencia de error; (2) los métodos Get…ErrorKey de ValidationHelper e InputValidator usan null cuando el dato es válido, y otros métodos privados pueden conservar null como ausencia de error; (3) la ausencia de datos opcionales, como sala o partida, puede representarse con null sin significar por sí sola un fallo de infraestructura; (4) los protocolos internos de notificación, como el eco del chat y MatchDelivery, conservan sus propias convenciones. Toda conversión de un resultado privado a uno público debe aplicar el contrato de §17.1.

Los repositorios capturarán SqlException, DataException y los tipos específicos de EF aplicables, incluida EntityException, y devolverán un estado explícito de fallo de acceso. Los servicios dejarán propagar las excepciones inesperadas. En operaciones request-reply, ServiceImplementation capturará únicamente OutOfMemoryException y la convertirá en `FaultException<SystemFault>`. Una operación one-way carece de canal de respuesta para devolver ese fault. Las excepciones de comunicación se tratarán en los límites indicados en esta sección.

Las excepciones deben reservarse exclusivamente para fallos inesperados en tiempo de ejecución o condiciones excepcionales del sistema. Para la validación de formularios de usuario y entradas de datos comunes, no se deben lanzar excepciones; en su lugar, se deben utilizar valores de retorno controlados (como claves de error localizadas, objetos de resultado) para comunicar errores de entrada sin afectar el flujo de ejecución. Por su parte, los errores de programación (como violaciones de precondiciones o argumentos inválidos en métodos internos) deben señalarse mediante excepciones de la familia ArgumentException. Para consultar el modelo de centralización de validaciones de entrada, véase la sección 16 (Validación de entrada y seguridad).

Ejemplo correcto (validación de nickname sin lanzar excepción)

```c#
public string ValidateNickname(string nickname)
{
    string errorKey = null;
    if (string.IsNullOrWhiteSpace(nickname))
    {
        errorKey = "CreateAccount_NicknameEmptyError";
    }
    return errorKey;
}
```

Este ejemplo ilustra un método de validación local que utiliza null como «sin error». No describe el contrato público de OperationResult ni el convenio de los validadores de TLMG.Domain.Validators que devuelven ErrorKey, donde el éxito se representa con string.Empty (S10.2 y S16.1).

Ejemplo incorrecto (lanzar excepción ante una entrada inválida de usuario)

```c#
public void ValidateNickname(string nickname)
{
    if (string.IsNullOrWhiteSpace(nickname))
    {
        throw new ArgumentException("El nickname no puede estar vacío.", nameof(nickname));
    }
}
```

Los bloques catch deberán capturar el tipo de excepción más específico disponible o, para embolsar las excepciones en una categoría, se pondrá su excepción padre. No deberá usarse Exception o SystemException como captura general ni como única captura. 

Ejemplo correcto (sin catch específico, solo el padre)

```c#
try
{
    notificationOfResult();
}
catch (CommunicationException communicationException)
{
    _log.Warn("Error de comunicación al notificar al cliente; verifique la conexión por parte del cliente.",
        communicationException);
}
```

Ejemplo incorrecto (captura genérica dentro de la lógica, sin cascada: registra, pero ni relanza ni devuelve un resultado controlado)

```c#
try
{
    _emailSender.Send(playerEmail, verificationCode);
}
catch (Exception ex)
{
    _log.Error("No se pudo enviar el código.", ex);
}
```

Los bloques catch no deberán estar vacíos; un bloque catch vacío elimina la posibilidad de que la aplicación maneje la excepción o de que cualquier otra parte del sistema la detecte, lo que equivale a ignorar silenciosamente un error.

Ejemplo correcto

```c#
try
{
    _emailSender.Send(playerEmail, verificationCode);
}
catch (SmtpException ex)
{
    _log.Error(string.Format("No se pudo enviar el código al jugador {0}.", playerId), ex);
}
```

Ejemplo incorrecto (catch vacío)

```c#
try
{
    _emailSender.Send(playerEmail, verificationCode);
}
catch (SmtpException)
{
}
```

Queda prohibido capturar una excepción y retornar un valor vacío, nulo o por defecto sin haber registrado (logueado) o propagado el incidente. Silenciar el error de esta forma oculta el fallo real y obliga a depurar a ciegas en producción.

Ejemplo correcto 

```c#
public string SendVerificationCode(int playerId, string playerEmail, string verificationCode)
{
    string errorKey = null;
    try
    {
        _emailSender.Send(playerEmail, verificationCode);
    }
    catch (SmtpException ex)
    {
        _log.Error(string.Format("No se pudo enviar el código al jugador {0}.", playerId), ex);
        errorKey = "Common_EmailSendError";
    }
    return errorKey;
}
```

Ejemplo incorrecto (retorna un valor por defecto sin registrar el incidente)

```c#
public string SendVerificationCode(int playerId, string playerEmail, string verificationCode)
{
    string errorKey = null;
    try
    {
        _emailSender.Send(playerEmail, verificationCode);
    }
    catch (SmtpException)
    {
        errorKey = "Common_EmailSendError";
    }
    return errorKey;
}
```

Cuando sea necesario relanzar una excepción capturada, deberá usarse throw sin especificar la excepción (no throw ex), para preservar la traza de pila original y no perder la información del punto donde ocurrió el error.

Ejemplo correcto

```c#
try
{
    _emailSender.Send(playerEmail, verificationCode);
}
catch (SmtpException)
{
    LogSendError(playerId);
    throw;
}
```

Ejemplo incorrecto

```c#
try
{
    _emailSender.Send(playerEmail, verificationCode);
}
catch (SmtpException ex)
{
    LogSendError(playerId);
    throw ex;
}
```

La excepción que se lance deberá ser el tipo más específico (más derivado) disponible que se ajuste a la condición de error; no deberán lanzarse intencionalmente System.Exception, System.SystemException o System.NullReferenceException desde el código propio.

Ejemplo correcto

```c#
private const int ZeroAmount = 0;

public void AddScore(int amount)
{
    if (amount < ZeroAmount)
    {
        throw new ArgumentOutOfRangeException(nameof(amount), "El puntaje no puede ser negativo.");
    }
}
```

Ejemplo incorrecto

```c#
private const int ZeroAmount = 0;

public void AddScore(int amount)
{
    if (amount < ZeroAmount)
    {
        throw new Exception("Valor inválido.");
    }
}
```

3. ## Manejo de logs y categorización de errores {#manejo-de-logs-y-categorización-de-errores}

### *Mecanismo de logging* {#mecanismo-de-logging}

Uso de log4net (ILog) en lugar de logging ad-hoc: todo mensaje de log deberá emitirse a través de la interfaz ILog (declarada como campo privado \_log e inicializada mediante LogManager.GetLogger) y no mediante Console.WriteLine. Esto permite centralizar el formato de salida, filtrar por nivel según el entorno y mantener desacoplada la lógica del juego.

Ejemplo correcto

```c#
public class RoundResolver
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(RoundResolver));

    public void ResolveRound()
    {
        _log.InfoFormat("Ronda resuelta. Jugadores activos: {0}", _activePlayerCount);
    }
}
```

Ejemplo incorrecto (logging directo, no desacoplado)

```c#
public class RoundResolver
{
    public void ResolveRound()
    {
        Console.WriteLine("Ronda resuelta. Jugadores activos: " + _activePlayerCount);
    }
}
```

Los valores variables dentro de un mensaje de log deberán insertarse utilizando marcadores posicionales numéricos ({0}, {1}) mediante los métodos Format de ILog (DebugFormat, InfoFormat, WarnFormat, ErrorFormat, FatalFormat), evitando la concatenación directa de cadenas (+) cuando esta afecte la legibilidad o rendimiento. Cuando el mensaje deba incluir una excepción, log4net no ofrece sobrecarga ...Format con excepción; en ese caso se usará \_log.Error(string.Format("…{0}…", valor), ex). Fuera de ese caso, string.Format y la interpolación no se usan en logs.

Ejemplo correcto

```c#
_log.WarnFormat("La sala {0} no existe. Se ignora el mensaje.", roomCode);
```

Ejemplo incorrecto

```c#
_log.Warn("La sala " + roomCode + " no existe. Se ignora el mensaje.");
```

Los mensajes del log se escriben en español y no se traducen (S10.2). Las siguientes tablas fijan qué registra hoy cada módulo y a qué nivel; un módulo nuevo se agrega a la tabla cuando empieza a registrar eventos.

| Módulo | Qué registra hoy | Niveles |
| :---- | :---- | :---- |
| Servidor · Program | Arranque, apertura y cierre del host, errores de arranque | Info, Warn, Fatal |
| Servidor · ServiceImplementation | Operaciones rechazadas y fallos inesperados de una petición  | Warn, Error |
| Servidor · PlayerAccountRegistrationService | Fallos del registro | Error |
| Cliente · LoginScreen, CreateAccountScreen, LeaderboardScreen, LobbyScreen, FriendsScreen, ServerConnectionManager  | Fallos de comunicación y de pantalla | Error, Warn, Info |
| Servidor · SessionRegistry  | Alta, baja y sustitución de sesiones por inicio de sesión repetido (sin el token)  | Info |
| Servidor · PlayerRepository, FriendshipRepository | Fallos de acceso a datos (excepciones de EF y SQL) | Error |
| Servidor · LobbyService, FriendsService  | Operaciones rechazadas (sesión no válida, invitación no válida, sala llena) y fallos al notificar a un cliente  | Info, Warn, Error  |

Configuración: el cliente escribe en logs\\aplicacion.log (10 MB, 5 respaldos) y el servidor en consola y en logs\\servidor.log. En las compilaciones de entrega no se habilitan los niveles Trace ni Debug. 

Ningún mensaje de log ni traza de pila deberá incluir información confidencial (contraseñas, tokens de sesión, datos personales); esa información deberá omitirse o enmascararse antes de registrarse.

Ejemplo correcto

```c#
_log.Error(string.Format("Fallo de autenticación para el jugador {0}.", playerId), ex);
```

Ejemplo incorrecto (expone datos sensibles en el log)

```c#
_log.Error(string.Format("Fallo de autenticación. Password: {0}.", password), ex);
```

**Niveles de log (log4net)**

| Nivel Estándar | Método log4net (ILog) | Observaciones |
| :---- | :---- | :---- |
| Trace | \_log.Debug(...) | Mapeado a Debug por falta de método Trace nativo. |
| Debug | \_log.Debug(...) | Detalles técnicos durante desarrollo. |
| Information | \_log.Info(...) | Eventos normales del flujo. |
| Warning | \_log.Warn(...) | Condiciones anómalas recuperables. |
| Error | \_log.Error(...) | Fallos que impiden completar la operación actual. |
| Critical / Fatal | \_log.Fatal(...) | Fallos graves que comprometen la estabilidad general. |

### *Trace* {#trace}

Nivel más detallado. Dado que log4net no incluye un nivel nativo Trace en su interfaz ILog por defecto, este nivel se mapea directamente al método \_log.Debug. Se usa para trazas internas de muy bajo nivel que solo aportan valor durante depuración profunda de flujos poco frecuentes. No deberá estar habilitado en builds de entrega.

Ejemplo correcto

```c#
public void ResolveRound(int roundNumber)
{
    _log.DebugFormat("Iniciando resolución de la ronda {0}.", roundNumber);
    _log.DebugFormat("Resolución de la ronda {0} finalizada. Puntajes calculados: {1}.", roundNumber, scoreCount);
}
```

Ejemplo incorrecto (Trace dentro de un temporizador, en cada tick)

```c#
private void OnTimerTick(object sender, EventArgs e)
{
    _log.DebugFormat("Tick ejecutado: {0}.", _tickCount);
    ProcessTick();
}
```

### *Debug* {#debug}

Información técnica útil durante el desarrollo, pero que no debe aparecer en el build de producción por defecto. Sirve para inspeccionar valores intermedios de una operación cuando no es evidente por qué falla.

Ejemplo correcto

```c#
public void AddScore(int amount)
{
    _log.DebugFormat("AddScore llamado con amount={0}, scoreActual={1}.", amount, _score);
    _score = Math.Min(_score + amount, MaxScore);
}
```

Ejemplo incorrecto (un evento normal del flujo va en Information)

```c#
public void StartMatch()
{
    _log.Debug("El jugador inició una nueva partida.");
}
```

### *Information* {#information}

Eventos normales del funcionamiento del juego, útiles para confirmar que un flujo se completó como se esperaba. No deberá emitirse dentro de bucles de alta frecuencia, ya que satura el log sin aportar valor adicional.

Ejemplo correcto

```c#
public void RegisterPlayer(string nickname, string email, string password)
{
    _log.InfoFormat("Jugador {0} registrado correctamente.", nickname);
}
```

Ejemplo incorrecto (Information dentro de un bucle de alta frecuencia)

```c#
foreach (Player player in _activePlayers)
{
    _log.InfoFormat("Actualizando el estado del jugador {0}.", player.Id);
    player.UpdateState();
}
```

### *Warning* {#warning}

Situaciones anómalas o condiciones inesperadas pero recuperables, para las cuales existe un valor o camino alternativo definido y la operación logra completarse con éxito (por ejemplo, cuando el mazo está agotado y se reconstruye con las cartas descartadas).

Ejemplo correcto

```c#
public Card GetNextCard()
{
    Card nextCard;
    if (_deck.Count == ZeroCardCount)
    {
        _log.Warn("El mazo está agotado. Se reconstruye con las cartas descartadas.");
        nextCard = RebuildDeck();
    }
    else
    {
        nextCard = _deck.Pop();
    }
    return nextCard;
}
```

Ejemplo incorrecto (la operación se cancela: es un Error, no un Warning)

```c#
public Room JoinRoom(string roomCode)
{
    Room room = FindRoom(roomCode);
    if (room == null)
    {
        _log.Warn("La sala no existe. Se cancela el ingreso.");
        throw new RoomNotFoundException(roomCode);
    }
    return room;
}
```

### *Error* {#error}

Un fallo concreto o excepción que impide completar la operación actual, aunque la aplicación pueda continuar ejecutándose. Debe incluir siempre la excepción como segundo parámetro para preservar la traza de pila (stack trace) en el registro.

Ejemplo correcto

```c#
try
{
    _emailSender.Send(playerEmail, verificationCode);
}
catch (SmtpException ex)
{
    _log.Error(string.Format("No se pudo enviar el código al jugador {0}.", playerId), ex);
    errorKey = "Common_EmailSendError";
}
```

Ejemplo incorrecto (registra el error sin la excepción, perdiendo la traza de pila)

```c#
try
{
    _emailSender.Send(playerEmail, verificationCode);
}
catch (SmtpException)
{
    _log.Error("No se pudo enviar el código.");
    errorKey = "Common_EmailSendError";
}
```

### *Critical (Fatal)* {#critical-(fatal)}

Un fallo grave que compromete la estabilidad general de la aplicación o impide continuar operando con seguridad (por ejemplo, el servidor no puede abrir el ServiceHost). Debe usarse con moderación: un uso excesivo satura el espacio en disco reservado a los logs del servidor.

Ejemplo correcto

```c#
public void StartHost()
{
    try
    {
        _serviceHost.Open();
    }
    catch (CommunicationException ex)
    {
        _log.Fatal("No se pudo abrir el ServiceHost. El servidor no puede iniciar.", ex);
        throw;
    }
}
```

Ejemplo incorrecto (Fatal usado para un evento recuperable)

```c#
_log.Fatal("El mazo está vacío.");
```

4. ## Prohibición de números mágicos {#prohibición-de-números-mágicos}

**Alcance de las constantes.** Solo deberá compartirse cuando represente la misma regla. Dos límites numéricamente iguales pueden cambiar por motivos distintos y requieren nombres independientes. Evitar constantes artificiales para literales evidentes sin semántica de negocio, respetando las reglas específicas de consultas (§9.8) y pruebas (§15.3). Reutilizar constantes, políticas, validadores y recursos localizados existentes.

Los literales numéricos que representen un valor de negocio o de configuración del juego no deberán usarse directamente en expresiones; deberán declararse como const o static readonly siguiendo los criterios de elección y convención de nombrado definidos en la sección 3.4.

Ejemplo correcto

```c#
private const int HeartbeatIntervalSeconds = 3;
private const int MaxLoginAttempts = 3;

public void StartSession()
{
    _remainingAttempts = MaxLoginAttempts;
    _heartbeatTimer.Interval = TimeSpan.FromSeconds(HeartbeatIntervalSeconds);
}
```

Ejemplo incorrecto (números sin nombre ni contexto)

```c#
public void StartSession()
{
    _remainingAttempts = 3;
    _heartbeatTimer.Interval = TimeSpan.FromSeconds(3);
}
```

### ***Temporizadores del sistema*** {#temporizadores-del-sistema}

Los temporizadores del sistema deberán declararse como constantes siguiendo las convenciones de nombrado del proyecto (incorporando la unidad de tiempo en el propio identificador):

| Temporizador | Valor | Constante | Quién lo mide |
| :---- | :---- | :---- | :---- |
| Cuenta regresiva de inicio | 3 s | LobbyStartCountdownSeconds | Cliente (DispatcherTimer); el servidor valida la hora límite |
| Latido (heartbeat) | 3 s | HeartbeatIntervalSeconds | El cliente lo envía; el servidor revisa los vencimientos al recibirlo |
| El Gatito Fugitivo | 15 s | FugitiveKittenWindowSeconds | Cliente (DispatcherTimer); el servidor rechaza las respuestas tardías |
| Ventana de gracia por desconexión | 15 s | PlayerReconnectGraceSeconds | Servidor, por hora límite en el registro de sesiones |
| Inactividad de sala | 10 min | MatchInactivityTimeoutMinutes | Servidor, por hora límite en el registro de sesiones |

La columna "Quién lo mide" corresponde a D-54: el servidor no usa temporizadores; guarda cada plazo como hora límite en UTC y lo revisa con su propio reloj (S11.1). Los valores y las constantes provienen de D-01.

Cabe señalar que dos de estos temporizadores miden 15 segundos con alcance distinto (la ventana del minijuego del Gatito Fugitivo a nivel de partida frente a la ventana de gracia de reconexión para un jugador individual); los nombres elegidos deben reflejar con precisión ese alcance para que la distinción sobreviva con total claridad en la fase de código.

Excepción: los valores de diseño visual escritos directamente en el código de la interfaz (márgenes, rellenos, tamaños; por ejemplo new Thickness(0, 0, 0, 16)) no se consideran números mágicos. La excepción no aplica a valores de negocio, tiempos ni límites, que siempre van como constantes con nombre.

Alcance de las constantes. Una constante que usa una sola clase se declara en esa clase, lo más cerca posible de su uso. Una constante de negocio que usan varias clases o proyectos se centraliza en TLMG.Domain.Constants: ValidationRules para las reglas de validación y ErrorKeys para las claves de error, agrupadas en clases anidadas por pantalla o área. Cada valor de ErrorKeys debe coincidir con una clave de ambos diccionarios de idioma (S7.3). 

5. ## Límite de operadores por expresión {#límite-de-operadores-por-expresión}

Una expresión condicional no deberá contener más de tres operadores lógicos o de comparación en la misma condición; cuando se supere ese límite, la condición deberá extraerse a una variable booleana con nombre descriptivo.

Ejemplo correcto

```c#
bool canJoinRoom = isRoomOpen && hasFreeSeat && !isMatchStarted;
if (canJoinRoom)
{
    JoinRoom();
}
```

Ejemplo incorrecto

```c#
if (isRoomOpen && hasFreeSeat && !isMatchStarted && playerRole == PlayerRole.Registered)
{
    JoinRoom();
}
```

6. ## Complejidad ciclomática máxima {#complejidad-ciclomática-máxima}

**Complejidad.** Máximo 10 por método. Evaluar también el flujo de un accesor con lógica propia.

**Tamaño.** Máximo tres parámetros por método y nueve métodos declarados por clase o interfaz. Excluir constructores de ambos límites. Sumar las declaraciones propias de todas las partes de una clase parcial como un solo tipo. En ServiceImplementation, solo se excluyen del límite de nueve métodos las implementaciones de operaciones exigidas por los contratos WCF de §3.10. Contar cada sobrecarga declarada. Las propiedades y sus accesores se revisan como propiedades, sin convertir get y set en métodos ordinarios para el recuento. Excluir miembros generados según §1.3. Mantener las excepciones previamente acordadas para out dentro de su alcance original. Un exceso de parámetros no autoriza una excepción nueva.

**Diseño.** Aplicar SRP, ISP, DRY y KISS. Extraer una responsabilidad identificable o reutilizar una existente. Evitar clases artificiales, abstracciones sin consumidor, dependencias innecesarias y traslados arbitrarios a Helpers para ajustar una cifra.

Cuando un método supere el umbral de complejidad, deberá refactorizar extrayendo lógica a métodos privados con nombres descriptivos.

Ejemplo correcto

```c#
public void ResolveRound(Round round)
{
    if (CanResolve(round))
    {
        int score = CalculateScore(round);
        ApplyScore(round, score);
    }
}

private bool CanResolve(Round round)
{
    bool canResolve = round.IsOpen && round.HasAllBids;
    return canResolve;
}

private int CalculateScore(Round round)
{
    int totalScore = round.BaseScore * round.Multiplier;
    return totalScore;
}

private void ApplyScore(Round round, int score)
{
    round.Winner.AddScore(score);
}
```

Ejemplo incorrecto (complejidad 11: supera el umbral de 10\)

```c#
public int ProcessEvaluation(int[] values, int processType, bool isBonusRound)
{
    int totalScore = 0;
    if (processType == PrimaryType)
    {
        totalScore += BaseBonus;
    }
    else if (processType == SecondaryType)
    {
        totalScore += ExtendedBonus;
    }
    else if (processType == TertiaryType)
    {
        totalScore += SpecialBonus;
    }
    else
    {
        totalScore += DefaultBonus;
    }
    foreach (int value in values)
    {
        if (value > UpperThreshold && isBonusRound)
        {
            totalScore += AdjustmentUnit;
        }
        else if (value < LowerLimit)
        {
            totalScore -= AdjustmentUnit;
        }
        switch (value)
        {
            case HalfScale:
                totalScore += SmallReward;
                break;
            case HighScale:
                totalScore += LargeReward;
                break;
            case QuarterScale:
                totalScore += TinyReward;
                break;
            default:
                break;
        }
    }
    return totalScore;
}
```

7. ## Uso de nullable reference types / manejo de nulos {#uso-de-nullable-reference-types-/-manejo-de-nulos}

Dado que el proyecto utiliza la versión de C\# 7.3, la característica de tipado de referencias nulas (Nullable Reference Types) no se encuentra disponible. Por lo tanto, el manejo de nulos se realizará mediante verificaciones tradicionales (por ejemplo, \!= null). Los métodos que puedan devolver null deberán documentarlo explícitamente en su comentario XML mediante la etiqueta \<returns\>.

8. ## Uso de propiedades vs campos públicos {#uso-de-propiedades-vs-campos-públicos}

Los campos de instancia no deberán ser públicos ni protegidos; deberán ser privados o internos y exponerse mediante propiedades con visibilidad externa.

Ejemplo correcto

```c#
public class Player
{
    private int _score;

    public int Score => _score;
}
```

Ejemplo incorrecto (campo público)

```c#
public class Player
{
    public int score;
}
```

Las propiedades no deberán ser de solo escritura (únicamente set sin get); tampoco deberán tener el setter con mayor accesibilidad que el getter; si el getter de una propiedad no puede proveerse, la funcionalidad debe implementarse como método en su lugar.

Ejemplo correcto

```c#
public int Score { get; private set; }
```

Ejemplo incorrecto (el getter es menos accesible que el setter)

```c#
public int Score { protected get; set; }
```

9. ## Principios de Código y Diseño {#principios-de-código-y-diseño}

Principio de responsabilidad única (SRP): cada método y cada clase deberán tener una única responsabilidad claramente definida. Un método se considera de tamaño adecuado cuando no supera las 40 líneas de código (sin contar llaves ni líneas en blanco); al superarlas, deberá evaluarse dividirlo en métodos privados con nombres descriptivos.

Ejemplo correcto (responsabilidad única, método corto)

```c#
public void RegisterPlayer(string nickname, string email, string password)
{
    string passwordHash = HashPassword(password);
    _playerRepository.CreatePlayer(nickname, email, passwordHash);
    _log.InfoFormat("Jugador {0} registrado correctamente.", nickname);
}
```

Ejemplo incorrecto (una clase con varias responsabilidades)

```c#
public class PlayerManager
{
    private int _score;
    private string _connectionString = "server=db;database=game;";

    public void AddScore(int amount)
    {
        _score += amount;
    }

    public void SaveToDatabase()
    {
    }

    public void DrawScoreBar()
    {
    }
}
```

Lo que solo usa su propia clase (campos, propiedades y métodos) se declara private. Los tipos (clases, interfaces, enumeraciones) y los miembros que usan otras clases pueden ser public o internal: se prefiere internal cuando solo se usan dentro de su proyecto, y public cuando los consume otro proyecto, el framework (WCF, XAML, Entity Framework) o un proyecto de pruebas.

Casos particulares de accesibilidad (D-32): 

* Clases parciales XAML: Las clases parciales de la interfaz de usuario generadas por WPF se mantienen public, que es el modificador con el que WPF genera su parte de marcado.

* Miembros de clases privadas anidadas: Los miembros de una clase anidada con visibilidad privada pueden declararse public o internal dentro de esa clase anidada, dado que el alcance de acceso ya está restringido externamente por el nivel privado de la clase contenedora.

Ejemplo correcto

```c#
private void ValidateInput(string input)
{
}

internal class InvitationCodeParser
{
}
```

Ejemplo incorrecto

```c#
public void ValidateInput(string input)
{
}
```

Principio de simplicidad (KISS; D-31): se debe favorecer la solución más simple y directa que resuelva el problema sin introducir abstracciones innecesarias, patrones complejos o sobreingeniería antes de que la necesidad real exista.

Ejemplo correcto (solución directa y legible)

```c#
public class RoundScoreCalculator
{
    public int CalculateScore(int cardsWon, int multiplier)
    {
        int totalScore = cardsWon * multiplier;
        return totalScore;
    }
}
```

Ejemplo incorrecto (sobreingeniería e interfaces innecesarias para una operación simple)

```c#
public interface IScoreCalculationStrategy
{
    int Execute(int value, int factor);
}

public class MultiplicationScoreStrategy : IScoreCalculationStrategy
{
    public int Execute(int value, int factor) => value * factor;
}

public class ScoreCalculatorFactory
{
    public static IScoreCalculationStrategy CreateDefaultStrategy() => new MultiplicationScoreStrategy();
}
```

11. # Comunicación en tiempo real y rendimiento {#comunicación-en-tiempo-real-y-rendimiento}

    1. ## Comunicación en tiempo real con WCF y DispatcherTimer {#comunicación-en-tiempo-real-con-wcf-y-dispatchertimer}

El objetivo principal de esta sección es garantizar que la comunicación en tiempo real mediante el canal dúplex de WCF con netTcpBinding (D-28) y la temporización en WPF mediante DispatcherTimer se mantengan fluidas y libres de bloqueos visuales, asegurando la capacidad de respuesta de la interfaz de usuario durante la partida.

Para la comunicación cliente-servidor mediante WCF, se deben preferir operaciones síncronas o de solicitud-respuesta (request-response) únicamente cuando se requiera un valor de retorno o confirmación inmediata. En contratos dúplex para notificaciones del servidor hacia los clientes, las operaciones de callback deben declararse obligatoriamente con IsOneWay \= true para evitar bloqueos en el hilo de ejecución del servidor y garantizar la fluidez de la partida. Del mismo modo, toda operación del contrato de servicio que entregue su resultado por callback se declara con IsOneWay \= true.

Ejemplo correcto de contrato de callback dúplex

```c#
[OperationContract(IsOneWay = true)]
void SendMessage(string message);
```

Ejemplo incorrecto (operación de un contrato dúplex que espera respuesta en lugar de recibirla por callback)

```c#
[OperationContract(IsOneWay = false)]
string SendMessageAndBlock(string message);
```

Toda recepción de eventos o notificaciones del servidor enviadas a través del canal de callback de WCF (netTcpBinding dúplex) deberá procesarse en la vista o cliente mediante delegados e invocación explícita al hilo de la interfaz de usuario cuando se requiera actualizar elementos XAML, evitando acceder a controles visuales directamente desde el hilo del callback.

Ejemplo correcto

```c#
public void OnMessageReceived(string message)
{
    Dispatcher.BeginInvoke(new Action(() => AppendChatMessage(message)));
}
```

Ejemplo incorrecto

```c#
public void OnMessageReceived(string message)
{
    txtChatHistory.Text += message + "\n";
}
```

Los temporizadores periódicos utilizados para actualizar la interfaz gráfica o controlar la lógica de tiempo del juego deberán implementarse utilizando la clase DispatcherTimer de WPF, configurando su intervalo mediante la propiedad Interval y suscribiendo manejadores al evento Tick.

Ejemplo correcto

```c#
private DispatcherTimer _gameTimer;

public void InitializeTimer()
{
    _gameTimer = new DispatcherTimer();
    _gameTimer.Interval = TimeSpan.FromSeconds(TimerIntervalSeconds);
    _gameTimer.Tick += OnGameTimerTick;
    _gameTimer.Start();
}
```

Ejemplo incorrecto

```c#
private System.Threading.Timer _threadTimer;

public void InitializeTimer()
{
    _threadTimer = new System.Threading.Timer(OnTimerCallback, null, 0, 1000);
}
```

En TLMG no se crean ni se administran hilos propios, ni en el cliente ni en el servidor. La concurrencia la aporta el propio WCF, que atiende cada sesión en hilos de su runtime, y la interfaz se mantiene fluida con callbacks IsOneWay, tiempos límite del binding y DispatcherTimer. Esta restricción proviene de la indicación del curso y se refleja en las decisiones de arquitectura del proyecto (D-27, ADR-03 y ADR-05). Por ello queda prohibido el uso de los elementos de la siguiente tabla.

| Elemento prohibido | Motivo | Alternativa permitida |
| :---- | :---- | :---- |
| Thread, ThreadPool (incluidos Thread.CurrentThread y Thread.Sleep) | WCF ya administra los hilos; un hilo propio introduce condiciones de carrera | Operaciones del contrato y callbacks IsOneWay. Para la cultura: CultureInfo.CurrentCulture y CurrentUICulture |
| Task, Task.Run, Task.Delay | Esperar su resultado (.Result, .Wait()) bloquea el hilo de la interfaz y puede causar interbloqueos | Resultado por callback; pausas con DispatcherTimer de un solo disparo |
| async y await (incluidos async void y AsyncRelayCommand) | Igual que Task | RelayCommand síncrono con método con nombre |
| BackgroundWorker | Ejecuta el trabajo en un hilo de fondo | Callback IsOneWay |
| System.Threading.Timer y System.Timers.Timer | Disparan en hilos del ThreadPool, no en el de la interfaz | DispatcherTimer |
| lock, SemaphoreSlim, Interlocked | Solo se justifican con estado compartido entre sesiones en el servidor | Requieren aprobación previa y justificación técnica documentada |

El servidor no usa temporizadores propios: guarda cada plazo como hora límite en UTC y lo revisa con su propio reloj cuando llega un latido (S10.4; D-54). El cliente muestra la cuenta regresiva con DispatcherTimer. El registro de sesiones (SessionRegistry) es estado compartido entre las sesiones y su acceso se protege con un único lock, autorizado por el encargado (D-58). Cualquier otro uso de lock, SemaphoreSlim o Interlocked sigue requiriendo autorización. 

El lock de SessionRegistry se usa con tres reglas: 

(1) un único objeto de candado, privado y de solo lectura, para que no pueda haber interbloqueo entre candados.

 (2) dentro del lock no se accede a datos, no se invoca ningún callback, no se escribe en el log ni se espera nada: solo se lee o se modifica el estado en memoria.

 (3) la sección protegida es corta: dentro se decide y se copia lo necesario (por ejemplo, la lista de callbacks a notificar) y la notificación se hace fuera. Una operación compuesta sobre el registro, como comprobar el cupo de una sala y ocuparlo, se hace dentro de una misma sección protegida. 

Ejemplo correcto (Nombres ilustrativos):

```c#
private static readonly object _sessionRegistryLock = new object();

List<ILobbyManagerCallback> roomCallbacks;
lock (_sessionRegistryLock)
{
    roomCallbacks = new List<ILobbyManagerCallback>(room.PlayerCallbacks);
}

foreach (ILobbyManagerCallback roomCallback in roomCallbacks)
{
    roomCallback.OnLobbyPlayersUpdated(lobbyPlayers);
}
```

Ejemplo incorrecto (Nombres ilustrativos):

```c#
private static readonly object _sessionRegistryLock = new object();

List<ILobbyManagerCallback> roomCallbacks;
lock (_sessionRegistryLock)
{
    roomCallbacks = new List<ILobbyManagerCallback>(room.PlayerCallbacks);
}

foreach (ILobbyManagerCallback roomCallback in roomCallbacks)
{
    roomCallback.OnLobbyPlayersUpdated(lobbyPlayers);
}
```

Ejemplo Ejemplo incorrecto (invoca los callbacks dentro del lock: un cliente lento detiene a todas las sesiones):

```c#
lock (_sessionRegistryLock)
{
    foreach (ILobbyManagerCallback roomCallback in room.PlayerCallbacks)
    {
        roomCallback.OnLobbyPlayersUpdated(lobbyPlayers);
    }
}
```

Configuración actual del servicio y del canal:

* ServiceImplementation usa InstanceContextMode.PerSession y ConcurrencyMode.Single (D-41).  
* El cliente fija los tiempos del binding: apertura 10 s, envío 15 s y cierre 5 s (D-46).   
* Los callbacks del cliente se declaran con UseSynchronizationContext \= false, por lo que llegan en un hilo del runtime de WCF y deben pasar por el Dispatcher antes de tocar la interfaz.  
* El servidor tiene includeExceptionDetailInFaults \= "false": no envía detalle técnico al cliente (S10.2).  
* El cierre del canal (Close, o Abort cuando falla) corresponde a la capa Comunicación (S4.1).

Ejemplo correcto (pausa con DispatcherTimer de un solo disparo)

```c#
private void ShowStatusMessage()
{
    lblStatusMessage.Visibility = Visibility.Visible;
    _statusMessageTimer.Start();
}

private void OnStatusMessageTimerTick(object sender, EventArgs e)
{
    _statusMessageTimer.Stop();
    lblStatusMessage.Visibility = Visibility.Collapsed;
}
```

Ejemplo incorrecto (async/await y Task en código de la interfaz)

```c#
private async void ShowStatusMessage()
{
    lblStatusMessage.Visibility = Visibility.Visible;
    await Task.Delay(StatusMessageDelayMilliseconds);
    lblStatusMessage.Visibility = Visibility.Collapsed;
}
```

Para temporizadores de un solo uso (single-shot), se deberá detener el DispatcherTimer dentro de su propio manejador del evento Tick llamando únicamente a Stop(), deteniendo así la ejecución del temporizador sin necesidad de realizar una desuscripción explícita mediante \-=.

Ejemplo correcto

```c#
private void OnSingleShotTimerTick(object sender, EventArgs e)
{
    _singleShotTimer.Stop();
    ExecuteDelayAction();
}
```

12. # Comentarios {#comentarios}

    1. ## Comentarios de implementación (bloque, línea, finales) {#comentarios-de-implementación-(bloque,-línea,-finales)}

El código de autoría propia deberá expresar su intención mediante los nombres y la estructura. No se añadirán comentarios privados o de implementación.

2. ## Comentarios de documentación (XML, doc comments) {#comentarios-de-documentación-(xml,-doc-comments)}

Los contratos públicos entre componentes se documentarán en inglés mediante XML bien formado, inmediatamente antes del tipo o miembro. La documentación deberá explicar el propósito, las garantías y las restricciones que necesita conocer quien consume el contrato. No se duplicará en implementaciones concretas ni se utilizará `<inheritdoc/>`. Los constructores XAML triviales quedan exentos (D-36).

Cada comentario comenzará con `///` e incluirá `<summary>`. Cada parámetro tendrá una etiqueta `<param>` cuyo atributo `name` coincida con el identificador de la firma. Para los métodos que devuelven un valor se incluirá `<returns>`.

Ejemplo de documentación de un contrato:

```c#
public interface IPlayerLookup
{
    /// <summary>Determines whether a player is present in the current lookup.</summary>
    /// <param name="playerId">Identifier of the player to locate.</param>
    /// <returns>True if the lookup contains the specified player.</returns>
    bool ContainsPlayer(int playerId);
}
```

3. ## Comentarios especiales (TODO, FIXME, HACK, UNDONE) {#comentarios-especiales-(todo,-fixme,-hack,-undone)}

Los pendientes y defectos se registrarán en el backlog. No se utilizarán comentarios TODO ni FIXME en el código.

13. # Estructuras de control {#estructuras-de-control}

    1. ## Sentencias condicionales {#sentencias-condicionales}

Toda sentencia condicional (if/else) deberá utilizar siempre bloques delimitados por llaves {}, incluso si el cuerpo contiene una sola línea de código, garantizando consistencia visual y evitando errores de mantenimiento.

Ejemplo correcto

```c#
if (isGameOver)
{
    ShowGameOverScreen();
}
```

Ejemplo incorrecto

```c#
if (isGameOver)
    ShowGameOverScreen();
```

2. ## Uso de return {#uso-de-return}

**Elección del flujo.** Preferir métodos nombrados a lambdas, if/else al ternario y una condición de continuación clara al break de un bucle. Aplicar las restricciones formales de §§9.8 y 13.5 para las expresiones admitidas. Conservar las terminaciones necesarias del switch. Preferir sintaxis de consulta LINQ cuando sea adecuada y tipos explícitos donde §9.7 permita elegir. Usar var en consultas y tipos anónimos según §9.7. Justificar en la revisión las expresiones necesarias para traducción EF u otras restricciones técnicas. Verificar que cualquier sustitución preserve evaluación, traducción y efectos observables.

Los métodos no void deberán contener una única sentencia return al final (D-29) del cuerpo del método, asignando el valor a devolver a una variable local declarada al inicio o durante el cálculo, evitando salidas prematuras múltiples que compliquen el flujo de control. Quedan exentos los miembros con cuerpo de expresión (=\>), los accesores de propiedades y las lambdas. Los métodos void de autoría propia no admitirán retornos anticipados.

Ejemplo correcto

```c#
public int CalculateFinalScore(int baseScore, int bonus, bool hasBonus)
{
    int finalScore = baseScore;
    if (hasBonus)
    {
        finalScore += bonus;
    }
    return finalScore;
}
```

Ejemplo incorrecto (varias salidas)

```c#
public int CalculateFinalScore(int baseScore, int bonus, bool hasBonus)
{
    if (hasBonus)
    {
        return baseScore + bonus;
    }
    return baseScore;
}
```

3. ## Bucles {#bucles}

Se prefiere el uso de bucles foreach sobre bucles for o while cuando se iteren colecciones de solo lectura o secuencias legibles, reservando el bucle for únicamente para escenarios donde se requiera modificar la colección o acceder mediante un índice numérico explícito.

Ejemplo correcto

```c#
foreach (Player player in activePlayers)
{
    player.ResetVote();
}
```

Ejemplo incorrecto (se recorre con índice sin necesitarlo)

```c#
for (int playerIndex = 0; playerIndex < activePlayers.Count; playerIndex++)
{
    activePlayers[playerIndex].ResetVote();
}
```

Queda prohibido modificar el tamaño de una colección (agregar o remover elementos) durante su iteración mediante un bucle foreach, ya que esto provoca una InvalidOperationException  en tiempo de ejecución.

Ejemplo correcto (for hacia atrás con RemoveAt)

```c#
for (int playerIndex = activePlayers.Count - 1; playerIndex >= 0; playerIndex--)
{
    if (!activePlayers[playerIndex].IsConnected)
    {
        activePlayers.RemoveAt(playerIndex);
    }
}
```

Ejemplo incorrecto (foreach con Remove)

```c#
foreach (Player player in activePlayers)
{
    if (!player.IsConnected)
    {
        activePlayers.Remove(player);
    }
}
```

4. ## Sentencias switch {#sentencias-switch}

Todas las sentencias switch deberán incluir siempre una sección default explícita para capturar valores no contemplados, garantizando la robustez ante futuros cambios o nuevos valores agregados a enumeraciones.

Ejemplo correcto

```c#
switch (roomState)
{
    case RoomState.Waiting:
        ShowWaitingRoom();
        break;
    case RoomState.InMatch:
        ShowMatchBoard();
        break;
    default:
        _log.Warn("Estado de sala no reconocido.");
        break;
}
```

Ejemplo incorrecto (sin sección default)

```c#
switch (roomState)
{
    case RoomState.Waiting:
        ShowWaitingRoom();
        break;
    case RoomState.InMatch:
        ShowMatchBoard();
        break;
}
```

Se deben utilizar sentencias switch tradicionales de C\# 7.3 para mantener compatibilidad, asegurando que cada caso asigne el valor a una variable local y se utilice un único return al final del cuerpo del método.

Ejemplo correcto

```c#
private const string WaitingKey = "Lobby_WaitingMessage";
private const string InMatchKey = "Lobby_InMatchMessage";
private const string UnknownKey = "Common_UnknownStateError";

public string GetStateMessageKey(RoomState roomState)
{
    string messageKey;
    switch (roomState)
    {
        case RoomState.Waiting:
            messageKey = WaitingKey;
            break;
        case RoomState.InMatch:
            messageKey = InMatchKey;
            break;
        default:
            messageKey = UnknownKey;
            break;
    }
    return messageKey;
}
```

Ejemplo incorrecto

```c#
public string GetStateMessageKey(RoomState roomState)
{
    if (roomState == RoomState.Waiting) return "Lobby_WaitingMessage";
    if (roomState == RoomState.InMatch) return "Lobby_InMatchMessage";
    return "Common_UnknownStateError";
}
```

5. ## Uso del operador ternario {#uso-del-operador-ternario}

El operador ternario (? :) solo se utilizará para asignaciones condicionales simples y cortas de una sola línea; queda estrictamente prohibido anidar operadores ternarios. Si la expresión no cabe en el límite de 120 columnas (sección 9.2), se usa if/else.

Ejemplo correcto

```c#
int displayScore = playerScore > ZeroScore ? playerScore : ZeroScore;
```

Ejemplo incorrecto (ternarios anidados)

```c#
int displayScore = playerScore > MaxScore ? MaxScore : playerScore > ZeroScore ? playerScore : ZeroScore;
```

Queda prohibido invocar métodos que tengan efectos secundarios dentro de las expresiones del operador ternario; la evaluación de ambas ramas solo debe retornar valores o variables puras.

Ejemplo correcto

```c#
int bonusScore = isDoubleRound ? DoubleRoundBonus : BaseBonus;
if (isDoubleRound)
{
    ShowDoubleRoundBanner();
}
```

Ejemplo incorrecto (método con efectos secundarios dentro del ternario)

```c#
int bonusScore = isDoubleRound ? StartDoubleRound() : BaseBonus;
```

6. ## Pattern matching {#pattern-matching}

Se debe preferir el uso de pattern matching (is tipo variable) para la comprobación y conversión de tipos en un solo paso, evitando casteos explícitos e identificadores sin verificación previa.

Ejemplo correcto

```c#
if (participant is RegisteredPlayer registeredPlayer)
{
    registeredPlayer.AddScore(RoundWinBonus);
}
```

Ejemplo incorrecto

```c#
if (participant is RegisteredPlayer)
{
    RegisteredPlayer registeredPlayer = (RegisteredPlayer)participant;
    registeredPlayer.AddScore(RoundWinBonus);
}
```

Se deberá utilizar la verificación \!= null para validar que una referencia no sea nula, manteniendo el estándar de C\# 7.3.

Ejemplo correcto

```c#
if (selectedCard != null)
{
    selectedCard.Reveal();
}
```

Ejemplo incorrecto (sintaxis de C\# 9, no disponible en C\# 7.3)

```c#
if (selectedCard is not null)
{
    selectedCard.Reveal();
}
```

14. # Manejo de eventos y arquitectura reactiva {#manejo-de-eventos-y-arquitectura-reactiva}

    1. ## Convenciones para eventos y delegados {#convenciones-para-eventos-y-delegados}

El nombrado y el tipo de los eventos se definen en S3.8; esta sección regula su invocación. Todo evento público deberá invocarse utilizando el operador de navegación segura ?.Invoke(this, e).

Ejemplo correcto

```c#
PlayerDisconnected?.Invoke(this, new PlayerEventArgs(playerId));
```

Ejemplo incorrecto

```c#
PlayerDisconnected(this, new PlayerEventArgs(playerId));
```

Excepción: Un evento estático, como LanguageManager.LanguageChanged, no tiene this y se invoca con null como emisor.

```c#
LanguageChanged?.Invoke(null, EventArgs.Empty);
```

2. ## Suscripción y desuscripción (prevención de memory leaks) {#suscripción-y-desuscripción-(prevención-de-memory-leaks)}

Toda suscripción a eventos (incluyendo eventos estáticos globales de clases como LanguageManager o de pantallas UI) de objetos con un ciclo de vida más largo que el suscriptor deberá desuscribirse explícitamente en el método OnScreenUnloaded o IDisposable para evitar fugas de memoria (memory leaks). Asimismo, cualquier DispatcherTimer activo en la vista deberá detenerse explícitamente mediante Stop(). Queda estrictamente prohibido suscribir expresiones lambda anónimas cuando se requiera desuscripción posterior, debiendo usarse métodos manejadores nombrados.

La desuscripción con \-= solo es obligatoria cuando el publicador vive más que el suscriptor (por ejemplo LanguageManager o un servicio). Los eventos de los controles de la propia pantalla no requieren desuscripción. Todo DispatcherTimer activo deberá detenerse con Stop() en OnScreenUnloaded, porque el Dispatcher lo mantiene vivo mientras esté habilitado.

Ejemplo correcto

```c#
private void OnScreenLoaded(object sender, RoutedEventArgs e)
{
    LanguageManager.LanguageChanged += OnLanguageChanged;
}

private void OnScreenUnloaded(object sender, RoutedEventArgs e)
{
    LanguageManager.LanguageChanged -= OnLanguageChanged;
    _updateTimer?.Stop();
}
```

Ejemplo incorrecto (suscripción con lambda anónima, imposible de desuscribir)

```c#
private void OnScreenLoaded(object sender, RoutedEventArgs e)
{
    LanguageManager.LanguageChanged += (eventSender, eventArguments) => ApplyTexts();
}
```

15. # Pruebas {#pruebas}

    1. ## Framework de pruebas {#framework-de-pruebas}

Las pruebas unitarias aislarán SQL y WCF mediante dobles de interfaces. Las pruebas de integración utilizarán entornos y datos controlados.

**Integridad de las pruebas.** Verificar estados y datos del contrato. Conservar los asserts que detectan defectos. No eliminar, omitir ni deshabilitar pruebas para obtener una ejecución sin fallos. Evitar operaciones destructivas sobre bases compartidas.

El curso exige un mínimo de 300 pruebas, y cada caso de uso cubre un flujo normal, tres alternos y uno de excepción.

El proyecto utiliza xUnit.net como único framework de pruebas para todos sus casos de prueba (D-61). Es compatible con .NET CLI y Visual Studio, y sus pruebas parametrizadas (\[Theory\] con \[InlineData\]) son las que esta sección exige para evitar lógica de control dentro de la prueba. 

El nombre de cada método de prueba deberá estar compuesto por tres partes que expresen, en ese orden, el nombre del método que se prueba, el escenario bajo el cual se prueba y el comportamiento esperado ante ese escenario.

Ejemplo correcto

```c#
private const string ExpectedErrorKey = "CreateAccount_NicknameEmptyError";

[Theory]
[InlineData("")]
[InlineData("   ")]
[InlineData(null)]
public void ValidateNickname_WithBlankNickname_ReturnsEmptyError(string blankNickname)
{
    string nickname = blankNickname;

    string errorKey = ValidationHelper.ValidateNickname(nickname);

    Assert.Equal(ExpectedErrorKey, errorKey);
}
```

Ejemplo incorrecto (nombre sin estructura ni propósito claro)

```c#
[Fact]
public void TestScore()
{
}
```

No se deberá incluir lógica de control de flujo (condicionales, bucles, estructuras switch) dentro del cuerpo de una prueba, ya que ello incrementa la probabilidad de introducir errores en el propio código de prueba y reduce la confianza en los resultados.

Ejemplo correcto (sin lógica condicional; se usan datos parametrizados)

```c#
private const int InitialScore = 0;

[Theory]
[InlineData(10, 10)]
[InlineData(50, 50)]
[InlineData(100, 100)]
public void AddScore_VariousAmounts_IncreasesScoreByAmount(int amount, int expectedScore)
{
    Player player = new Player(InitialScore);

    player.AddScore(amount);

    Assert.Equal(expectedScore, player.Score);
}
```

Ejemplo incorrecto (lógica de control dentro de la prueba)

```c#
[Fact]
public void AddScore_VariousAmounts_IncreasesScoreByAmount()
{
    Player player = new Player(0);
    foreach (int amount in new[] { 10, 50, 100 })
    {
        player.AddScore(amount);
    }
}
```

2. ## Estructura de proyectos de prueba y nomenclatura de clases {#estructura-de-proyectos-de-prueba-y-nomenclatura-de-clases}

El código de producción reside en sus propios proyectos y las pruebas unitarias deben ubicarse en proyectos separados organizados en una carpeta de solución llamada test/. Cada proyecto de pruebas unitarias debe llevar el sufijo .Tests correspondiendo exactamente al nombre del proyecto de producción que prueba (por ejemplo, /Server/TLMG.Domain \-\> /test/TLMG.Domain.Tests, /Server/TLMG.Services \-\> /test/TLMG.Services.Tests, /Client/TLMG.Client \-\> /test/TLMG.Client.Tests). Las pruebas de integración de la solución del servidor (repositorios con la base de datos y host WCF) van juntas en un único proyecto, /test/TLMG.Server.Tests, nombrado con el host que las ejecuta.

Ejemplo correcto de estructura de proyectos

```
/Server
|__/TLMG.Domain
|__/TLMG.Services
|__/TLMG.DataAccess
|__/TLMG.Server
|__/test
   |__/TLMG.Services.Tests   <- unitarias de lógica (stubs y mocks)
   |__/TLMG.Server.Tests     <- integración (host WCF, base de datos)
/Client
|__/TLMG.Client
|__/test
   |__/TLMG.Client.Tests     <- ViewModels
```

Ejemplo incorrecto (un solo proyecto monolítico de pruebas, fuera de la carpeta test)

```
/TLMG
|__/TLMG.Tests
```

El nombre de la clase de prueba deberá corresponder al nombre de la clase bajo prueba seguido del sufijo Tests, para que sea evidente qué clase de producción verifica cada archivo.

Adicionalmente, el conjunto de pruebas de una clase deberá diseñarse de forma que, con solo leer los nombres de sus métodos, sea posible inferir el comportamiento esperado de la clase bajo prueba sin necesidad de consultar su implementación. Esto es especialmente valioso cuando una prueba falla: el nombre por sí solo debe permitir identificar qué escenario dejó de cumplirse, sin tener que depurar el código para entenderlo.

Ejemplo correcto (archivo RoundResolverTests.cs)

```c#
public class RoundResolverTests
{
}
```

Ejemplo incorrecto (no refleja la clase bajo prueba)

```c#
public class GameTests
{
}
```

Se exige no introducir dependencias de infraestructura al escribir pruebas unitarias, ya que esas dependencias hacen las pruebas lentas y frágiles, y deben reservarse para las pruebas de integración; se exige mantener las pruebas unitarias en un proyecto separado del de las pruebas de integración. Para consultar los criterios de separación de proyectos de pruebas de lógica frente a infraestructura, véase la sección 15.4 (Pruebas unitarias de lógica pura vs pruebas de integración con infraestructura).

3. ## Estructura de un test (Arrange-Act-Assert) {#estructura-de-un-test-(arrange-act-assert)}

El cuerpo de cada prueba deberá organizarse en tres secciones claramente diferenciadas, en el orden siguiente: preparación del estado y los objetos necesarios (Arrange), ejecución de la acción que se desea probar (Act) y verificación del resultado esperado (Assert).

Ejemplo correcto

```c#
private const int InitialScore = 0;
private const int ScoreToAdd = 30;
private const int ExpectedScore = 30;

[Fact]
public void AddScore_WithValidAmount_IncreasesPlayerScore()
{
    Player player = new Player(InitialScore);

    player.AddScore(ScoreToAdd);

    Assert.Equal(ExpectedScore, player.Score);
}
```

Ejemplo incorrecto (sin las tres secciones diferenciadas)

```c#
[Fact]
public void AddScore_WithValidAmount_IncreasesPlayerScore()
{
    Player player = new Player(0);
    player.AddScore(30);
    Assert.Equal(30, player.Score);
    player.AddScore(10);
    Assert.Equal(40, player.Score);
}
```

Los valores usados en la etapa Arrange deberán ser los más simples posibles que permitan verificar el comportamiento bajo prueba, evitando propiedades innecesarias o valores distintos de cero cuando no aporten información relevante al escenario.

Ejemplo correcto (valor mínimo que verifica el comportamiento)

```c#
private const int OneCard = 1;

[Fact]
public void AddCard_WhenCalled_IncreasesCardCount()
{
    PlayerHand hand = new PlayerHand();

    hand.AddCard(new Card());

    Assert.Equal(OneCard, hand.CardCount);
}
```

Ejemplo incorrecto (valores arbitrarios que no aportan al escenario)

```c#
[Fact]
public void AddCard_WhenCalled_IncreasesCardCount()
{
    PlayerHand hand = new PlayerHand(
        maxCards: 999,
        dealDelaySeconds: 9.7f,
        difficulty: DifficultyLevel.Hard);

    hand.AddCard(new Card());

    Assert.Equal(1, hand.CardCount);
}
```

Los valores literales usados en la etapa Arrange deberán asignarse a constantes con nombre descriptivo en lugar de usarse directamente como valores sin contexto, con el fin de que el propósito de cada valor sea evidente.

Ejemplo correcto

```c#
private const int InitialScore = 0;
private const int NegativeScoreAmount = -1;

[Fact]
public void AddScore_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
{
    Player player = new Player(InitialScore);

    Action act = () => player.AddScore(NegativeScoreAmount);

    Assert.Throws<ArgumentOutOfRangeException>(act);
}
```

Ejemplo incorrecto (valor sin nombre ni contexto)

```c#
[Fact]
public void AddScore_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
{
    Player player = new Player(0);

    Action act = () => player.AddScore(-1);

    Assert.Throws<ArgumentOutOfRangeException>(act);
}
```

4. ## Pruebas unitarias de lógica pura vs pruebas de integración con infraestructura {#pruebas-unitarias-de-lógica-pura-vs-pruebas-de-integración-con-infraestructura}

Las pruebas unitarias de lógica de juego y las pruebas de integración con infraestructura externa (UI, persistencia, servicios WCF) deberán residir en proyectos separados dentro de /test. El proyecto de pruebas unitarias de dominio (ej. TLMG.Domain.Tests) no debe contener referencias a librerías de infraestructura como Entity Framework, WCF ni WPF.

Además, cuando un mismo comportamiento pueda verificarse tanto con una prueba unitaria como con una de integración, deberá preferirse siempre la prueba unitaria, ya que se ejecuta más rápido y no depende de una conexión real ni del hilo de UI.

Ejemplo correcto de organización

```
/test
|__/TLMG.Services.Tests   <- pruebas unitarias de lógica (sin infraestructura)
|__/TLMG.Server.Tests     <- pruebas que requieren infraestructura real (persistencia, WCF)
```

Ejemplo incorrecto

```
/test
|__/TLMG.Tests
   |__RoundResolverTests.cs            <- prueba unitaria de lógica pura
   |__PlayerRepositoryTests.cs         <- prueba de integración, requiere base de datos real
```

La lógica de juego que no dependa de tipos de infraestructura (como PlayerRepository, DbContext, o clases de WPF como Window o DependencyObject) deberá diseñarse de forma que pueda instanciarse y verificarse en un proyecto de pruebas convencional de .NET, usando objetos de prueba simulados (fakes, mocks o stubs) en lugar de componentes reales de infraestructura. Esta separación es una consecuencia directa del principio de ausencia de dependencias de infraestructura en pruebas unitarias: el uso de objetos falsos o simulados produce una ejecución más rápida de las pruebas.

Ejemplo correcto (la clase en TLMG.Domain y su prueba en TLMG.Domain.Tests)

```c#
public class ScoreCalculator
{
    public int Calculate(int baseScore, float multiplier)
    {
        int calculatedScore = (int)(baseScore * multiplier);
        return calculatedScore;
    }
}

private const int BaseScore = 100;
private const float HalfMultiplier = 0.5f;
private const int ExpectedHalfScore = 50;

[Fact]
public void Calculate_WithHalfMultiplier_ReturnsHalfScore()
{
    ScoreCalculator calculator = new ScoreCalculator();

    int result = calculator.Calculate(BaseScore, HalfMultiplier);

    Assert.Equal(ExpectedHalfScore, result);
}
```

Las pruebas que requieran el ciclo de vida de la interfaz (por ejemplo, el hilo de despacho de WPF, eventos Loaded) o acceso a la base de datos real deberán escribirse como pruebas de integración, en el proyecto separado ya establecido en la sección 15.4, y no en el proyecto de pruebas unitarias puras.

Ejemplo correcto de prueba de integración (en TLMG.Server.Tests)

```c#
public class PlayerRepositoryTests
{
    private const string Nickname = "TestPlayer";
    private const string Email = "test@example.com";
    private const string PasswordHash = "hash";

    [Fact]
    public void CreatePlayer_WithValidData_PlayerExistsByNickname()
    {
        PlayerRepository repository = new PlayerRepository();

        repository.CreatePlayer(Nickname, Email, PasswordHash);

        Assert.True(repository.PlayerExistsByNickname(Nickname));
    }
}
```

Ejemplo incorrecto (dependencia de base de datos real, pero tratada como prueba unitaria)

```c#
public class PlayerRepositoryTests
{
    [Fact]
    public void CreatePlayer_WithValidData_PlayerExistsByNickname()
    {
        PlayerRepository repository = new PlayerRepository();

        repository.CreatePlayer("TestPlayer", "test@example.com", "hash"); // Accesses the real database. 

        Assert.True(repository.PlayerExistsByNickname("TestPlayer"));
    }
}
```

En .NET no se requiere un atributo especial para las pruebas de integración: se usan los mismos atributos del framework elegido (sección 15.1), y la separación la da el proyecto en el que reside la prueba.

Ejemplo correcto (prueba unitaria de lógica pura, sin infraestructura)

```c#
private const int OneCard = 1;

[Fact]
public void AddCard_WhenCalled_IncreasesCardCount()
{
    PlayerHand hand = new PlayerHand();

    hand.AddCard(new Card());

    Assert.Equal(OneCard, hand.CardCount);
}
```

Ejemplo incorrecto (prueba de lógica pura con una espera innecesaria)

```c#
[Fact]
public void AddCard_WhenCalled_IncreasesCardCount()
{
    System.Threading.Thread.Sleep(SleepMilliseconds); // Unnecessary wait: PlayerHand does not depend on anything external. 
    PlayerHand hand = new PlayerHand();

    hand.AddCard(new Card());

    Assert.Equal(1, hand.CardCount);
}
```

5. ## Uso de mocks/simulación de dependencias de infraestructura {#uso-de-mocks/simulación-de-dependencias-de-infraestructura}

Se deberá usar los términos fake, mock y stub de forma precisa y no como sinónimos intercambiables, para evitar malentendidos sobre la naturaleza e intención de cada objeto de prueba.

Llamar "mock" a lo que en realidad es un stub lleva a otros integrantes del equipo a asumir, de forma incorrecta, que ese objeto se está usando para verificar una interacción (por ejemplo, que un método fue llamado), cuando en realidad solo está proporcionando datos de prueba. Esta ambigüedad dificulta entender qué es lo que la prueba realmente está verificando.

Ejemplo correcto (Stub: reemplaza una dependencia para proporcionar datos controlados)

```c#
public class StubPlayerRepository : IPlayerRepository
{
    public bool PlayerExistsByEmail(string email) => true;

    public bool PlayerExistsByNickname(string nickname) => true;

    public void CreatePlayer(string nickname, string email, string passwordHash)
    {
    }
}
```

Ejemplo correcto (Mock: además de reemplazar, verifica que se produjo una interacción)

```c#
public class MockEmailSender : IEmailSender
{
    public bool SendWasCalled { get; private set; }

    public void Send(string recipient, string verificationCode) => SendWasCalled = true;
}
```

Ejemplo incorrecto (es un stub, pero está nombrado como si fuera un mock)

```c#
public class MockPlayerRepository : IPlayerRepository
{
    public bool PlayerExistsByEmail(string email) => true;

    public bool PlayerExistsByNickname(string nickname) => true;

    public void CreatePlayer(string nickname, string email, string passwordHash)
    {
    }
}
```

Ejemplo incorrecto (es un mock, pero está nombrado como si fuera un stub)

```c#
public class StubEmailSender : IEmailSender
{
    public bool SendWasCalled { get; private set; }

    public void Send(string recipient, string verificationCode) => SendWasCalled = true;
}
```

Para que una dependencia pueda ser reemplazada por un objeto de prueba, la clase bajo prueba deberá recibir sus dependencias a través de interfaces, no como instancias concretas

Ejemplo correcto (la dependencia se recibe como interfaz)

```c#
public class AccountVerificationService
{
    private readonly IEmailSender _emailSender;

    public AccountVerificationService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public void SendVerificationCode(string playerEmail, string verificationCode)
    {
        _emailSender.Send(playerEmail, verificationCode);
    }
}
```

Ejemplo incorrecto (la dependencia es concreta e insustituible en pruebas)

```c#
public class AccountVerificationService
{
    public void SendVerificationCode(string playerEmail, string verificationCode)
    {
        new SmtpEmailSender().Send(playerEmail, verificationCode);
    }
}
```

En las pruebas unitarias, las dependencias externas (correo, persistencia, red) deberán sustituirse por stubs o mocks, nunca por sus implementaciones reales, ya que el uso de componentes reales de infraestructura hace las pruebas lentas y frágiles.

Ejemplo de prueba con un mock

```c#
private const string PlayerEmail = "test@example.com";
private const string VerificationCode = "123456";

[Fact]
public void SendVerificationCode_WithValidEmail_CallsEmailSender()
{
    MockEmailSender mockEmailSender = new MockEmailSender();
    AccountVerificationService service = new AccountVerificationService(mockEmailSender);

    service.SendVerificationCode(PlayerEmail, VerificationCode);

    Assert.True(mockEmailSender.SendWasCalled);
}
```

Los objetos de simulación deberán crearse únicamente sobre interfaces o tipos que el equipo comprende en su totalidad.

Ejemplo correcto (interfaz acotada y comprendida por el equipo)

```c#
public interface IEmailSender
{
    void Send(string recipient, string verificationCode);
}
```

Ejemplo incorrecto (se simula SmtpClient completo, un tipo con decenas de miembros que el equipo no comprende por completo)

```c#
public class MockSmtpClient : SmtpClient
{
}
```

16. # Validación de entrada y seguridad {#validación-de-entrada-y-seguridad}

    1. ## Centralización de validaciones {#centralización-de-validaciones}

**Garantía por capa.** Centralizar cada validación según su responsabilidad. El cliente orienta al usuario, el servidor autoriza por token y rol y aplica reglas de negocio, y persistencia asegura integridad y consistencia. Un botón deshabilitado solo controla la interacción visual. Evitar repetir una garantía contractual dentro del mismo límite. Conservar defensas e invariantes internas hasta comprobar quién garantiza sus precondiciones. Comunicar alternativas previsibles mediante resultados explícitos.

La validación del formato de los datos vive en clases Validator del dominio, en TLMG.Domain.Validators: PlayerAccountRegistrationValidator para el registro y PlayerLogInValidator para el inicio de sesión. Las reglas de campo que comparten varios flujos (formato de correo, rango de longitud y normalización del correo) viven en AccountFieldValidator, que devuelve bool o el valor normalizado; cada validador traduce el fallo a la ErrorKey de su propio flujo. Un validador acepta una entrada null y la rechaza con la ErrorKey de campos incompletos, sin depender de que el servicio la haya comprobado antes. Los métodos de TLMG.Domain.Validators que devuelven una ErrorKey utilizan string.Empty cuando el dato es válido o la ErrorKey que describe el fallo (S10.2); los métodos Get…ErrorKey de los validadores locales del cliente (ValidationHelper e InputValidator) devuelven null cuando el dato es válido. Las entradas inválidas habituales se comunican mediante resultados controlados, sin lanzar excepciones. En el cliente, la validación inmediata vive en ValidationHelper e InputValidator. El correo se normaliza (recorte y minúsculas) una sola vez, al convertir el DTO a la entidad del dominio, con AccountFieldRules.NormalizeEmail.

El cliente valida para dar una respuesta inmediata al jugador, y el servidor valida de nuevo todo lo que recibe, porque el cliente no es confiable (ADR-01). Las reglas de formato (longitudes, caracteres permitidos y su ErrorKey) están en el diccionario de reglas de validación del proyecto y no se repiten en este documento. El nickname admite letras, números y guion bajo (D-23).

2. ## Manejo de datos sensibles {#manejo-de-datos-sensibles}

**Seguridad.** Parametrizar SQL, usar UTC donde corresponda y mantener secretos fuera de Git y logs. Proteger contraseñas, tokens y códigos de verificación. Excluir también las direcciones de correo de los logs. Redactar los mensajes de diagnóstico en español. Mostrar errores mediante recursos localizados. Reservar detalles SQL y stack traces para el diagnóstico interno y evitar exponer códigos internos o credenciales al jugador.

La regeneración de referencias WCF deberá coordinarse previamente y preservar la compatibilidad de los contratos (§3 y §17). La protección de archivos generados se rige por §1.3. Las configuraciones reservadas del servidor y del cliente, incluidos sus App.config, deberán mantenerse fuera de las modificaciones de código.

* La contraseña se captura con PasswordBox (S7.1) y viaja al servidor. Solo el servidor calcula su hash, con SHA-256 y una sal aleatoria por contraseña (D-44); la contraseña en texto plano no se guarda ni se registra.

* Ningún log ni traza incluye contraseñas, códigos de verificación, tokens de sesión ni datos personales (S10.3).

* La cadena de conexión y las credenciales del correo viven fuera de Git (S5.2).

* El servidor se conecta a SQL Server con una cuenta de mínimo privilegio (S5.2; D-53).

* El token de sesión (sessionToken) lo genera solo el servidor con un generador criptográfico, no es adivinable y se valida en cada operación que lo recibe (S6.1; D-41, D-58).

  3. ## Prevención de trampas y manipulación de datos del cliente {#prevención-de-trampas-y-manipulación-de-datos-del-cliente}

El servidor es la autoridad de la partida (ADR-01; Zero-Trust, S6.1). De ello se derivan tres reglas:

1. El servidor valida cada jugada con las reglas del juego antes de aplicarla; lo que el cliente envía es una petición, no un resultado.

2. El cliente solo recibe la información que le corresponde: nunca las cartas ocultas de otros jugadores (ADR-01). El servidor arma una proyección por jugador.

3. Los plazos se miden con el reloj del servidor; el cliente solo muestra la cuenta regresiva (S10.4).

17. # DTO y contratos de datos {#dto-y-contratos-de-datos}

    1. ## Estructura de los DTO {#estructura-de-los-dto}

**Contrato de resultado (D-66).** Expresar el éxito mediante IsSuccess, un booleano de significado explícito o un estado definido. Para OperationResult y sus derivados se aplican estas combinaciones:

| IsSuccess | ErrorKey | Interpretación |
|---|---|---|
| true | string.Empty | Éxito si están presentes los datos que exige la operación. |
| false | Clave reconocida en es-MX y en-US | Fallo explícito. El cliente localiza la clave. |
| true | null | Resultado inconsistente. El límite receptor debe convertirlo en fallo controlado con IsSuccess = false y una ErrorKey válida. |
| Otras combinaciones | | Resultado inconsistente. Tratarlo en el límite responsable y garantizar un contrato válido antes de consumirlo. |

**Consumo.** Es incorrecto deducir éxito de `result.ErrorKey == null`, `string.IsNullOrEmpty(result.ErrorKey)` o `string.IsNullOrWhiteSpace(result.ErrorKey)`. Estas comprobaciones sirven para validar una clave. Leer IsSuccess y validar el contenido necesario, por ejemplo la identidad que requiere una sesión exitosa. Una vez garantizado el contrato, los consumidores pueden confiar en él. Los validadores históricos que devuelven solo ErrorKey conservan la convención de su firma.

**Significado del resultado.** El envío de una solicitud WCF acredita envío. La ejecución efectiva requiere evidencia del servidor. Una consulta exitosa sin filas entrega una colección vacía. Un fallo de infraestructura exige estado explícito. Admitir null para ausencia legítima, datos opcionales o comprobación defensiva justificada. Una dependencia obligatoria sin configurar y un fallo conocido requieren tratamiento explícito.

Los DTO viven en el proyecto TLMG.Contracts, en el namespace TLMG.Contracts.DataContracts (S4.3). El namespace los distingue; por eso su nombre no lleva sufijo genérico como Data o Info: se nombran por lo que contienen (por ejemplo, un jugador del ranking). El sufijo Request se reserva para el DTO de entrada de una operación y el sufijo Result para el de salida (como en el contrato existente OperationRegisterPlayerAccountResult). (D-56).

Un DTO solo transporta datos: no contiene reglas de juego, acceso a datos ni referencias a tipos de Entity Framework. TLMG.Domain no usa los DTO (S4.2); la conversión entre DTO y tipos del dominio la hace TLMG.Services.

2. ## Serialización {#serialización}

Los DTO se serializan con el serializador de contratos de datos de WCF, por lo que llevan los atributos de contrato de datos de WCF (S4.2). No se usa BinaryFormatter; en el código solo aparece dentro de un archivo de recursos generado automáticamente, que queda fuera de este Estándar (S1.3).

18. # Navegación entre pantallas y retroalimentación {#navegación-entre-pantallas-y-retroalimentación}

    1. ## Navegación entre pantallas {#navegación-entre-pantallas}

La aplicación cliente tiene una ventana principal, MainWindow, que muestra una pantalla a la vez. Toda pantalla hereda de LocalizableScreen (S4.3) y se muestra con MainWindow.ShowScreen(UserControl newScreen, bool saveInHistory \= true): reemplaza la pantalla visible y, cuando saveInHistory es true, la guarda en el historial para poder volver a ella.

Los ViewModels solicitan la navegación mediante INavigationManager, indicando un ScreenId y, si hace falta, un parámetro de navegación (…NavigationParameter). ScreenFactory crea cada vista con su ViewModel y sus dependencias, y NavigationManager administra el historial y libera los ViewModels descartados. Ningún ViewModel referencia una vista. 

2. ## Mensajes y retroalimentación al jugador {#mensajes-y-retroalimentación-al-jugador}

Todo texto que ve el jugador sale de una clave de recurso (S7.3). Los errores que decide el servidor llegan como ErrorKey y la vista los resuelve con el diccionario del idioma activo (S7.1 y S10.2). Nunca se muestra al jugador el texto de una excepción, una traza de pila ni el nombre de un tipo técnico.

19. # Dependencias externas y seguridad {#dependencias-externas-y-seguridad}

    1. ## Librerías y paquetes utilizados {#librerías-y-paquetes-utilizados}

| Paquete | Versión | Dónde | Para qué | Estado |
| :---- | :---- | :---- | :---- | :---- |
| Entity Framework | 6.5.2 | Servidor (TLMG.DataAccess) | Acceso a SQL Server mediante repositorios (S5.1) | En uso |
| log4net | 3.5.0 | Cliente y servidor | Registro de eventos (S10.3) | En uso  |
| CommunityToolkit.Mvvm | 8.4.2 | Cliente | ObservableObject y RelayCommand (S7.1) | Por instalar |
| Framework de pruebas | \<versión al instalarlo\>  | Proyectos de prueba | Pruebas unitarias y de integración (S15.1) | Por instalar |

Respaldo: D-50 para CommunityToolkit.Mvvm;  la versión de Entity Framework y de log4net es la que usa el código al 21 de octubre de 2026 y D-61 para el framework de pruebas” 

20. # Anexos {#anexos}

    1. ## Glosario de términos {#glosario-de-términos}

| Término | Significado en TLMG |
| :---- | :---- |
| Callback | Operación del contrato dúplex con la que el servidor avisa al cliente sin que este pregunte (I…ManagerCallback). |
| DTO | Objeto que solo transporta datos entre el cliente y el servidor (S17.1). |
| ErrorKey | Clave de recurso que el servidor devuelve cuando una validación o regla falla; el cliente la traduce a su idioma (S10.2). |
| Hora límite | Momento en UTC en que vence un plazo; el servidor la guarda y la compara con su propio reloj (S10.4). |
| Latido (heartbeat) | Mensaje periódico del cliente (cada 3 s) con el que el servidor sabe que sigue conectado. |
| Mock | Objeto de prueba que además de reemplazar una dependencia verifica que se produjo una interacción (S15.5). |
| Servidor autoritativo | El servidor decide y valida todo; el cliente solo muestra y pide (ADR-01). |
| Stub | Objeto de prueba que reemplaza una dependencia para dar datos controlados (S15.5). |
| Zero-Trust | El cliente se considera no confiable y toda operación se valida otra vez en el servidor (S6.1). |

    2. ## Documentación de decisiones de arquitectura {#documentación-de-decisiones-de-arquitectura}

Este Estándar recoge lo que está decidido (decisiones D-xx de las bitácoras y ADR del SAD). La siguiente tabla lista las decisiones en que se apoya y la sección donde se citan. Si una decisión resumida aquí contradice a su bitácora o al SAD, gana la bitácora o el SAD.e

| Decisión | Qué fija | Se cita en |
| :---- | :---- | :---- |
| D-01 | Cinco temporizadores del sistema, cada uno con constante y unidad | S10.4 |
| D-21 | Claves de recurso NombrePantalla\_NombreControl\[Rol\] con sufijo de rol opcional | S7.3 |
| D-22 | Ajustes del Estándar tras la auditoría (prefijos rect, brd, ell; literales a constantes) | S7.2 |
| D-23 | El nickname admite letras, números y guion bajo | S16.1 |
| D-25 | Prefijos sld y thm; partes de plantilla exentas | S7.2 |
| D-27 | Sin async/await ni hilos propios | S11.1 |
| D-28 | WCF con netTcpBinding | S11.1 |
| D-29 | Un único return al final en métodos no void | S13.2 |
| D-30 | Claves de idioma con prefijo de pantalla o Common\_ | S7.3 |
| D-31 | Principio de simplicidad y reutilización | S10.9 |
| D-32 | Excepciones de accesibilidad (clases parciales XAML y clases anidadas privadas) | S10.9 |
| D-34 | Contadores de bucle con nombre descriptivo | S3.1 |
| D-35 | Límite de 120 columnas, también en comentarios | S9.2 |
| D-36 | Documentación de contratos y excepción para constructores XAML triviales | S12.2 |
| D-37 | Criterio de Common\_ para claves compartidas | S7.3 |
| D-38 | Ejemplo WCF con IChatManager e IChatManagerCallback | S3.10 |
| D-39 | Cliente con MVVM | S6.2, S7.1 |
| D-40 | Proxy generado con svcutil.exe; TLMG.Contracts solo en el servidor | S4.1 |
| D-41 | Siete contratos por canal, con PerSession | S6.2, S11.1 |
| D-42 | 2FA sin hilos propios (Token Bucket) | S6.2 |
| D-43 | Remitente, secretos e idioma del correo | S5.2 |
| D-44 | Hash SHA-256 con sal | S16.2 |
| D-45 | Errores por origen y destino (ErrorKey) | S10.2 |
| D-46 | Tiempos límite del binding | S11.1 |
| D-47 | Flujo de trabajo y uso de IA (commits) | S8.2 |
| D-49 | Sufijo Service y proyecto TLMG.Services | S6.1, S8.2 |
| D-50 | CommunityToolkit.Mvvm 8.4.2 sin generadores  | S6.2, S7.1, S19.1 |
| D-51 | Contenedor de inyección de dependencias  | S6.2 |
| D-53 | Login de SQL Server de mínimo privilegio | S16.2, S5.2  |
| D-54 | Temporizadores por hora límite, sin hilos en el servidor | S10.4, S11.1 |
| D-55 | ADR-07 corregido: comunicación por el canal dúplex  | S11.1  |
| D-56  | DTO sin sufijo genérico Data ni Info; Request y Result reservados a la entrada y la salida de una operación. | S17.1  |
| D-57 | Accesibilidad: private para lo que solo usa su clase; los tipos y los miembros compartidos pueden ser public o internal  | S10.9 |
| D-58  | SessionRegistry: único lock autorizado, sessionToken generado por el servidor y el último inicio de sesión reemplaza al anterior  | S6.2, S11.1, S16.2  |
| D-59  | Scripts de datos de demostración en database/seed/  | S4.3, S5.3  |
| D-60  | Capa de comunicación del cliente (ServerConnectionManager): conserva el token y levanta eventos hacia las pantallas  | S4.1, S6.2  |
| D-61  | Marco de pruebas: xUnit.net  | S15.1, S19.1  |
| D-62 | Los avisos de los callbacks WCF llevan el prefijo "On"; los resultados de operación se nombran con el resultado | S3.7, S3.10  |
| D-63 | El chat acepta mensajes de 1 a 160 caracteres, medidos tras recortar espacios y normalizar. Se elimina el mínimo de 2 (RC-M-02) por quedar cubierto por la regla de campo obligatorio (RC-G-01). Afecta al diccionario de validación y a las pruebas del chat.  |  |
| D-64  | Navegación y diálogos por interfaz (`INavigationManager`, `IDialogManager`); los diálogos reciben `ErrorKey`  | §18.1  |
| D-65  | Domain amplio (entidades, validadores, repositorios, motor de reglas), sin “Managers”  | §3.14, §4.3  |
| D-66  | Clase base `OperationResult` para `IsSuccess` y `ErrorKey` en los `…Result`  | §17.1  |
| D-67  | Chat por cuenta, 24 h como máximo y solo como evidencia, con purga por lotes  | §5.1, §10.4  |
| D-68  | `Game_Player` conserva su fila al salir (`joined_at`, `left_at`)  | §5.1  |
| D-69  | Script v4.0, procedimientos por Function Import y rol de mínimo privilegio  | §5.1, §5.2, §5.3  |
| ADR-01 | Servidor autoritativo y Zero-Trust | S6.1, S6.2, S16.1, S16.3, S20.1 |
| ADR-02 | Aislamiento del motor de reglas | S4.2, S6.1, S6.2 |
| ADR-03 | WCF dúplex con callbacks | S6.2, S11.1 |
| ADR-04 | Latidos, ventana de gracia y snapshot | S6.2 |
| ADR-05 | Token Bucket para el 2FA | S6.2, S11.1 |
| ADR-06 | Internacionalización dinámica | S6.2 |
| ADR-08  | Identidad y estado de sesión del jugador entre canales  | S6.2, S11.1, S16.2  |

3. ## Referencias externas consultadas  {#referencias-externas-consultadas}

Cwalina, K., & Abrams, B. (2008a). *Design Guidelines for Exceptions*. En *Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries* (2.ª ed.). Microsoft Learn. [https\://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-3.5/ms229014(v=vs.90)](https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-3.5/ms229014\(v=vs.90\))

Cwalina, K., & Abrams, B. (2008b). *Enum Design*. En *Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries* (2.ª ed.). Microsoft Learn. [https\://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/enum](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/enum)

Cwalina, K., & Abrams, B. (2008c). *Field Design*. En *Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries* (2.ª ed.). Microsoft Learn. [https\://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/field](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/field)

Cwalina, K., & Abrams, B. (2008d). *Names of Classes, Structs, and Interfaces*. En *Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries* (2.ª ed.). Microsoft Learn. [https\://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces)

Cwalina, K., & Abrams, B. (2008e). *Names of Namespaces*. En *Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries* (2.ª ed.). Microsoft Learn. [https\://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces)

Cwalina, K., & Abrams, B. (2008f). *Names of Type Members*. En *Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries* (2.ª ed.). Microsoft Learn. [https\://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members)

Cwalina, K., & Abrams, B. (2008g). *Property Design*. En *Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries* (2.ª ed.). Microsoft Learn. [https\://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/property](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/property)

Microsoft. (2008). *Patterns in practice: Design for testability*. Microsoft Learn (archivo). [https\://learn.microsoft.com/en-us/archive/msdn-magazine/2008/december/patterns-in-practice-design-for-testability](https://learn.microsoft.com/en-us/archive/msdn-magazine/2008/december/patterns-in-practice-design-for-testability)

Microsoft. (2014). *Visual Studio 2013: TODO comments and custom tokens in the Task List*. Microsoft Learn (archivo). [https\://learn.microsoft.com/en-us/archive/blogs/zainnab/visual-studio-2013-todo-comments-and-custom-tokens-in-the-task-list](https://learn.microsoft.com/en-us/archive/blogs/zainnab/visual-studio-2013-todo-comments-and-custom-tokens-in-the-task-list)

Microsoft. (2023). *Implement the infrastructure persistence layer with Entity Framework Core*. En *.NET Microservices Architecture for Containerized .NET Applications*. Microsoft Learn. [https\://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core)

Microsoft. (s.f.-a). *Attributes – C\# language specification*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/attributes](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/attributes)

Microsoft. (s.f.-b). *CA1031: Do not catch general exception types – .NET*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1031](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1031)

Microsoft. (s.f.-c). *CA1051: Do not declare visible instance fields – .NET*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1051](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1051)

Microsoft. (s.f.-d). *CA1502: Avoid excessive complexity – .NET*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1502](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1502)

Microsoft. (s.f.-e). *CA1711: Identifiers should not have incorrect suffix (code analysis) – .NET*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1711](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1711)

Microsoft. (s.f.-f). *CA1717: Only FlagsAttribute enums should have plural names*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-gb/dotnet/fundamentals/code-analysis/quality-rules/ca1717](https://learn.microsoft.com/en-gb/dotnet/fundamentals/code-analysis/quality-rules/ca1717)

Microsoft. (s.f.-g). *CA2200: Rethrow to preserve stack details – .NET*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-ie/dotnet/fundamentals/code-analysis/quality-rules/ca2200](https://learn.microsoft.com/en-ie/dotnet/fundamentals/code-analysis/quality-rules/ca2200)

Microsoft. (s.f.-h). *Code-style naming rules – .NET*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/naming-rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/naming-rules)

Microsoft. (s.f.-i). *Connection strings and configuration files*. Microsoft Learn. Recuperado el 22 de octubre de 2026, de [https\://learn.microsoft.com/en-us/dotnet/framework/data/adonet/connection-strings-and-configuration-files](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/connection-strings-and-configuration-files)

Microsoft. (s.f.-j). *Creating and throwing exceptions – C\# programming guide*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2010/ms173163(v=vs.100)](https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2010/ms173163\(v=vs.100\))

Microsoft. (s.f.-k). *DispatcherTimer class (System.Windows.Threading)*. Microsoft Learn. Recuperado el 21 de octubre de 2026, de [https\://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatchertimer](https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatchertimer)

Microsoft. (s.f.-l). *Documentation comments – C\# language specification*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments)

Microsoft. (s.f.-m). *Get started with unit testing – Visual Studio (Windows)*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/visualstudio/test/getting-started-with-unit-testing?view=visualstudio](https://learn.microsoft.com/en-us/visualstudio/test/getting-started-with-unit-testing?view=visualstudio)

Microsoft. (s.f.-n). *IDE0011: Add braces – .NET*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0011](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0011)

Microsoft. (s.f.-o). *IDE0065: 'using' directive placement – .NET*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-ie/dotnet/fundamentals/code-analysis/style-rules/ide0065](https://learn.microsoft.com/en-ie/dotnet/fundamentals/code-analysis/style-rules/ide0065)

Microsoft. (s.f.-p). *Identifier names – rules and conventions – C\#*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)

Microsoft. (s.f.-q). *Implicitly typed local variables – C\#*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/implicitly-typed-local-variables](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/implicitly-typed-local-variables)

Microsoft. (s.f.-r). *Integration tests in ASP.NET Core*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)

Microsoft. (s.f.-s). *Names of Classes, Structs, and Interfaces (previous version)*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/zh-tw/previous-versions/dotnet/netframework-3.0/ms229040(v=vs.85)](https://learn.microsoft.com/zh-tw/previous-versions/dotnet/netframework-3.0/ms229040\(v=vs.85\))

Microsoft. (s.f.-t). *Namespaces and using directives – C\#*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/namespaces](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/namespaces)

Microsoft. (s.f.-u). *.NET coding conventions – C\#*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

Microsoft. (s.f.-v). *Nullable reference type warnings – C\# reference*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings)

Microsoft. (s.f.-w). *ObservableObject*. Microsoft Learn. Recuperado el 22 de octubre de 2026, de [https\://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observableobject](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observableobject)

Microsoft. (s.f.-x). *Organizing and testing projects with the .NET CLI*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/core/tutorials/testing-with-cli](https://learn.microsoft.com/en-us/dotnet/core/tutorials/testing-with-cli)

Microsoft. (s.f.-y). *Override modifier – C\# reference*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/override](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/override)

Microsoft. (s.f.-z). *Pack URIs in WPF*. Microsoft Learn. Recuperado el 22 de octubre de 2026, de [https\://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf)

Microsoft. (s.f.-{). *Raw SQL Queries (EF6)*. Microsoft Learn. Recuperado el 22 de octubre de 2026, de [https\://learn.microsoft.com/en-us/ef/ef6/querying/raw-sql](https://learn.microsoft.com/en-us/ef/ef6/querying/raw-sql)

Microsoft. (s.f.-\|). *Recommended XML documentation tags – C\# reference*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags)

Microsoft. (s.f.-}). *RelayCommand*. Microsoft Learn. Recuperado el 22 de octubre de 2026, de [https\://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/relaycommand](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/relaycommand)

Microsoft. (s.f.-\~). *Structure types – C\# reference*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/lt-lt/dotnet/csharp/language-reference/builtin-types/struct](https://learn.microsoft.com/lt-lt/dotnet/csharp/language-reference/builtin-types/struct)

Microsoft. (s.f.-). *Testing in .NET*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/core/testing/](https://learn.microsoft.com/en-us/dotnet/core/testing/)

Microsoft. (s.f.-). *Threading model*. Microsoft Learn. Recuperado el 21 de octubre de 2026, de [https\://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model)

Microsoft. (s.f.-). *Unit testing best practices for .NET*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

Microsoft. (s.f.-). *Use stubs to isolate parts of your app for testing – Visual Studio (Windows)*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/visualstudio/test/using-stubs-to-isolate-parts-of-your-application-from-each-other-for-unit-testing?view=vs-2022](https://learn.microsoft.com/en-us/visualstudio/test/using-stubs-to-isolate-parts-of-your-application-from-each-other-for-unit-testing?view=vs-2022)

Microsoft. (s.f.-). *Use Task List to track and use code comments – Visual Studio (Windows)*. Microsoft Learn. Recuperado el 20 de agosto de 2026, de [https\://learn.microsoft.com/en-us/visualstudio/ide/using-the-task-list?view=visualstudio](https://learn.microsoft.com/en-us/visualstudio/ide/using-the-task-list?view=visualstudio)

Microsoft. (s.f.-). *Working with DbContext*. Microsoft Learn. Recuperado el 22 de octubre de 2026, de [https\://learn.microsoft.com/en-us/ef/ef6/fundamentals/working-with-dbcontext](https://learn.microsoft.com/en-us/ef/ef6/fundamentals/working-with-dbcontext)

21. # Declaración de uso de IA {#declaración-de-uso-de-ia}

En la preparación del documento se emplearon herramientas de inteligencia artificial para investigar convenciones y referencias, contrastar nomenclaturas, elaborar ejemplos y revisar la redacción. Las propuestas se sometieron a revisión humana para determinar su incorporación al estándar.
