# PROJECT.md — LearningAgent.Api

## 1. Propósito del proyecto

**LearningAgent.Api** es un proyecto educativo para aprender a construir un agente de Inteligencia Artificial utilizando C# y ASP.NET Core.

El proyecto comenzó como una API sencilla para comunicarse con un Large Language Model (LLM) y ha evolucionado progresivamente hacia una arquitectura modular de agente.

El objetivo final es construir un agente capaz de incorporar progresivamente:

* conversación contextual;
* memoria;
* Tools;
* acceso a SQL Server;
* RAG;
* integración con documentos;
* persistencia de conversaciones;
* posibilidad de cambiar de proveedor de LLM sin modificar la lógica principal del agente.

El proyecto prioriza el aprendizaje de arquitectura, separación de responsabilidades y comprensión de las decisiones de diseño antes que la incorporación rápida de funcionalidades.

---

# 2. Notas

Este proyecto tiene un enfoque completamente educativo.

Cada fase busca comprender los conceptos y decisiones de diseño antes de incorporar nuevas funcionalidades.

El objetivo final no es únicamente obtener un chatbot funcional, sino construir una arquitectura profesional para aplicaciones de Inteligencia Artificial utilizando .NET.

La evolución del proyecto debe realizarse de forma incremental:

```text
Entender
   ↓
Diseñar
   ↓
Implementar
   ↓
Probar
   ↓
Documentar
   ↓
Continuar
```

No introducir abstracciones únicamente porque podrían ser útiles en el futuro.

Cada componente debe tener una responsabilidad clara y una razón concreta para existir.

---

# 3. Tecnologías

| Tecnología        | Uso                     |
| ----------------- | ----------------------- |
| ASP.NET Core 8    | API REST                |
| C#                | Lenguaje principal      |
| Ollama            | Ejecución local del LLM |
| Llama 3.2         | Modelo de lenguaje      |
| Swagger           | Pruebas de la API       |
| HttpClientFactory | Consumo de APIs         |
| System.Text.Json  | Serialización JSON      |

---

# 4. Estado actual del proyecto

## Fase 1 — Consumir un LLM

**Estado: completada**

Se creó una API ASP.NET Core 8 y se consiguió comunicación funcional con Ollama utilizando el modelo local `llama3.2`.

La API expone Swagger para realizar pruebas.

La integración utiliza:

```text
POST http://localhost:11434/api/chat
```

`OllamaService` transforma los mensajes internos `ConversationMessage` a los contratos específicos de Ollama, serializa el request, realiza la petición HTTP y deserializa la respuesta.

---

## Fase 2 — Construcción de la arquitectura del agente

**Estado: completada**

Se separó la responsabilidad del agente de la comunicación con el LLM.

El controlador depende de `IAgentService` y no directamente de `IChatService`.

La arquitectura base quedó dividida en:

* Controller;
* Agent;
* Conversation;
* Prompt;
* Chat/LLM.

---

## Fase 3 — Memoria conversacional en RAM

**Estado: completada**

Se implementó memoria conversacional utilizando:

```text
ConversationId
      ↓
MemoryService
      ↓
ConversationContext
      ↓
Messages
```

La memoria permite mantener el historial de una conversación entre diferentes requests HTTP.

También se verificó que diferentes conversaciones permanezcan aisladas.

### Pruebas realizadas

#### Prueba A — Persistencia entre requests

Primer request:

```json
{
  "conversationId": "11111111-1111-1111-1111-111111111111",
  "message": "Me llamo Juan"
}
```

Segundo request:

```json
{
  "conversationId": "11111111-1111-1111-1111-111111111111",
  "message": "¿Cómo me llamo?"
}
```

Resultado:

```text
Llama 3.2 recordó correctamente que el nombre era Juan.
```

#### Prueba B — Aislamiento entre conversaciones

Se utilizaron dos `ConversationId` diferentes.

```text
Conversation A → Juan
Conversation B → Pedro
```

Al preguntar posteriormente el nombre en cada conversación:

```text
A → Juan
B → Pedro
```

Las conversaciones no se mezclaron.

### Resultado

La memoria conversacional en RAM funciona correctamente.

---

# 5. Arquitectura actual

```text
                         +----------------+
                         |     Cliente    |
                         +-------+--------+
                                 |
                                 | HTTP POST
                                 | ConversationId + Message
                                 v
                         +----------------+
                         | ChatController |
                         +-------+--------+
                                 |
                                 | IAgentService
                                 v
                         +----------------+
                         |  AgentService  |
                         +-------+--------+
                                 |
                                 v
                         +----------------+
                         | MemoryService  |
                         +-------+--------+
                                 |
                         GetOrCreate(id)
                                 |
                                 v
                    +------------------------+
                    | ConversationContext    |
                    |                        |
                    | ConversationId         |
                    | SystemPrompt           |
                    | Messages               |
                    +-----------+------------+
                                |
                                v
                         +--------------+
                         | PromptBuilder|
                         +------+-------+
                                |
                                v
                         +--------------+
                         | IChatService |
                         +------+-------+
                                |
                                v
                         +--------------+
                         |OllamaService |
                         +------+-------+
                                |
                                v
                         +--------------+
                         |    Ollama    |
                         +------+-------+
                                |
                                v
                         +--------------+
                         |   Llama 3.2  |
                         +--------------+
```

---

# 6. Flujo de una conversación

Una petición sigue este flujo:

```text
ChatController
      |
      | ConversationId + Message
      v
AgentService
      |
      v
MemoryService.GetOrCreate()
      |
      +---- existe ----> recuperar ConversationContext
      |
      +---- no existe -> ConversationContextFactory
                              |
                              v
                       SystemPromptProvider
      |
      v
ConversationContext
      |
      +--> historial existente
      +--> nuevo mensaje
      |
      v
PromptBuilder
      |
      v
IChatService
      |
      v
OllamaService
      |
      v
Llama 3.2
      |
      v
respuesta
      |
      v
ConversationContext
      |
      +--> agrega respuesta assistant
      |
      v
MemoryService.Save()
```

---

# 7. Estructura actual

```text
LearningAgent.Api
│
├── Controllers
│   └── ChatController.cs
│
├── Contracts
│   └── Ollama
│       ├── OllamaChatRequest.cs
│       ├── OllamaChatResponse.cs
│       └── OllamaMessage.cs
│
├── Dtos
│   ├── ChatRequest.cs
│   └── ChatResponse.cs
│
├── Models
│   ├── Chat
│   │   └── ConversationMessage.cs
│   └── Conversation
│       └── ConversationContext.cs
│
├── Options
│   ├── OllamaOptions.cs
│   └── OpenAIOptions.cs
│
├── Services
│   ├── Agent
│   │   ├── AgentService.cs
│   │   └── IAgentService.cs
│   │
│   ├── Chat
│   │   ├── IChatService.cs
│   │   ├── OllamaService.cs
│   │   └── OpenAIService.cs
│   │
│   ├── Conversation
│   │   ├── ConversationContextFactory.cs
│   │   └── IConversationContextFactory.cs
│   │
│   ├── Memory
│   │   ├── IMemoryService.cs
│   │   └── MemoryService.cs
│   │
│   └── Prompts
│       ├── IPromptBuilder.cs
│       ├── PromptBuilder.cs
│       ├── ISystemPromptProvider.cs
│       └── SystemPromptProvider.cs
│
├── Program.cs
├── PROJECT.md
├── CHANGELOG.md
├── LearningAgent.Api.http
└── appsettings.json
```

> `ConversationMemory.cs` e `IConversationMemory.cs` fueron eliminados al no tener una responsabilidad diferente al sistema de memoria basado en `ConversationContext` + `MemoryService`.

---

# 8. Componentes principales

## 8.1 ChatController

Responsabilidad:

* recibir la petición HTTP;
* obtener `ConversationId` y `Message`;
* delegar el procesamiento al agente;
* devolver `ChatResponse`.

El controlador no contiene lógica de conversación ni de memoria.

---

## 8.2 AgentService

`AgentService` es el orquestador principal.

Depende de:

```csharp
IChatService
IPromptBuilder
IMemoryService
```

No depende directamente de:

```csharp
OllamaService
ConversationContextFactory
```

El flujo de `ProcessAsync` es:

```text
1. Obtener o crear ConversationContext.
2. Agregar mensaje del usuario.
3. Construir mensajes mediante PromptBuilder.
4. Enviar mensajes a IChatService.
5. Agregar respuesta del asistente.
6. Guardar el contexto.
7. Devolver la respuesta.
```

La firma actual es:

```csharp
Task<string> ProcessAsync(
    Guid conversationId,
    string message);
```

---

# 9. MemoryService

`MemoryService` administra las conversaciones activas en memoria.

La interfaz es:

```csharp
public interface IMemoryService
{
    ConversationContext GetOrCreate(Guid conversationId);

    void Save(ConversationContext context);
}
```

La implementación utiliza:

```csharp
Dictionary<Guid, ConversationContext>
```

El método:

```csharp
GetOrCreate(Guid conversationId)
```

busca una conversación existente.

Si existe:

```text
ConversationId
      ↓
Dictionary
      ↓
ConversationContext existente
```

Si no existe:

```text
ConversationId
      ↓
ConversationContextFactory
      ↓
ConversationContext nuevo
      ↓
Dictionary
```

---

# 10. Lifetime de MemoryService

`MemoryService` está registrado como:

```csharp
builder.Services.AddSingleton<IMemoryService, MemoryService>();
```

Esto es intencional.

El diccionario debe sobrevivir entre diferentes requests HTTP.

Si `MemoryService` fuera `Scoped`, cada request tendría una instancia diferente y el historial se perdería al terminar cada petición.

Por lo tanto:

```text
Request 1
    ↓
MemoryService Singleton
    ↓
Conversation A

Request 2
    ↓
MemoryService Singleton
    ↓
Conversation A existente
```

---

# 11. ConversationContext

`ConversationContext` representa el estado de una conversación.

Contiene:

```csharp
Guid ConversationId
List<ConversationMessage> Messages
string SystemPrompt
```

El contexto es el centro de la arquitectura conversacional.

En futuras fases podrá incorporar, cuando sea necesario:

* usuario;
* metadata;
* variables;
* herramientas;
* resultados de herramientas;
* información recuperada mediante RAG;
* otros datos necesarios para decidir la respuesta.

No agregar propiedades prematuramente.

---

# 12. ConversationContextFactory

La fábrica es responsable de crear un `ConversationContext` correctamente inicializado.

Recibe:

```csharp
ISystemPromptProvider
```

y expone:

```csharp
ConversationContext Create(Guid conversationId);
```

La creación establece:

```text
ConversationId
SystemPrompt
```

El flujo es:

```text
ConversationContextFactory
          |
          v
SystemPromptProvider
          |
          v
ConversationContext
```

`MemoryService` utiliza la fábrica cuando necesita crear una nueva conversación.

De esta manera `MemoryService` no necesita conocer los detalles de construcción del contexto.

---

# 13. PromptBuilder

`PromptBuilder` transforma un `ConversationContext` en una colección de `ConversationMessage`.

Construye:

```text
System message
+
Messages del contexto
```

Conceptualmente:

```text
ConversationContext
       |
       +--> SystemPrompt
       |
       +--> Messages
       |
       v
PromptBuilder
       |
       v
IEnumerable<ConversationMessage>
```

`PromptBuilder` no debe depender de un proveedor específico de LLM.

El `SystemPrompt` ya forma parte de `ConversationContext`.

---

# 14. SystemPromptProvider

`SystemPromptProvider` proporciona las instrucciones base del agente.

Actualmente define el comportamiento general de `LearningAgent`.

Su responsabilidad es proporcionar el prompt base, mientras que `ConversationContext` conserva el prompt correspondiente a la conversación.

---

# 15. IChatService

La interfaz es:

```csharp
Task<string> GetResponseAsync(
    IEnumerable<ConversationMessage> messages);
```

Esto permite enviar al LLM:

* system message;
* historial;
* mensajes del usuario;
* respuestas anteriores;
* futuros resultados de herramientas.

El agente no depende de un proveedor concreto.

---

# 16. OllamaService

`OllamaService` implementa `IChatService`.

Su única responsabilidad es comunicarse con Ollama.

Proceso:

```text
ConversationMessage[]
        |
        v
OllamaChatRequest
        |
        v
JSON
        |
        v
HTTP POST /api/chat
        |
        v
Ollama
        |
        v
OllamaChatResponse
        |
        v
string response
```

No contiene lógica de memoria ni lógica del agente.

---

# 17. OpenAIService

Existe una implementación `OpenAIService` como parte del aprendizaje inicial.

La integración con OpenAI produjo:

```text
HTTP 429 insufficient_quota
```

Debido al objetivo educativo y a la intención de evitar costos de API, se decidió utilizar Ollama localmente.

Actualmente:

* `OllamaService` es el proveedor activo.
* `OpenAIService` permanece como referencia.
* `IChatService` permite cambiar de proveedor sin modificar `AgentService`.

---

# 18. Dependency Injection

La configuración actual relevante es:

```csharp
builder.Services.AddHttpClient();

builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<IChatService, OllamaService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();

builder.Services.AddSingleton<ISystemPromptProvider, SystemPromptProvider>();
builder.Services.AddSingleton<IConversationContextFactory, ConversationContextFactory>();
builder.Services.AddSingleton<IMemoryService, MemoryService>();
```

### Decisión importante sobre lifetimes

`MemoryService` es `Singleton` porque mantiene estado entre requests.

`ConversationContextFactory` y `SystemPromptProvider` también son `Singleton` porque son dependencias de `MemoryService` y actualmente no mantienen estado específico de un request.

Los servicios relacionados con la ejecución de cada request permanecen `Scoped`.

---

# 19. ChatRequest

Actualmente:

```csharp
public class ChatRequest
{
    public Guid ConversationId { get; set; }

    public string Message { get; set; } = string.Empty;
}
```

`ConversationId` identifica la conversación a la que pertenece el mensaje.

El mismo identificador permite recuperar el historial correspondiente.

---

# 20. ChatResponse

Actualmente:

```csharp
public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
}
```

No es necesario devolver el `ConversationId` porque actualmente el cliente lo proporciona en cada request.

En una fase futura puede evaluarse un flujo donde la API cree conversaciones y devuelva el identificador.

---

# 21. Limitaciones actuales de la memoria

La memoria actual es deliberadamente simple y educativa.

Utiliza:

```csharp
Dictionary<Guid, ConversationContext>
```

en memoria RAM.

Por lo tanto:

### Reinicio de aplicación

Si la API se detiene y vuelve a iniciar:

```text
Memoria → perdida
```

Esto es esperado.

### Múltiples instancias

La memoria no está compartida entre diferentes instancias de la API.

### Persistencia

No existe persistencia permanente.

### Concurrencia

La implementación actual utiliza un `Dictionary` estándar y no está diseñada todavía como una solución de almacenamiento concurrente de producción.

Estas limitaciones son intencionales en esta etapa.

---

# 22. Configuración

Las opciones actuales incluyen:

```text
OpenAI
Ollama
```

Configuración relevante:

```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "llama3.2"
}
```

Ollama se ejecuta localmente.

El objetivo educativo es evitar costos de API mientras se aprende la arquitectura.

---

# 23. Pruebas completadas

## Prueba A — misma conversación

```text
ConversationId = A

Usuario:
Me llamo Juan

Usuario:
¿Cómo me llamo?
```

Resultado:

```text
El agente recordó correctamente "Juan".
```

## Prueba B — conversaciones independientes

```text
ConversationId A → Juan
ConversationId B → Pedro
```

Resultado:

```text
A → Juan
B → Pedro
```

Las conversaciones no se mezclaron.

## Prueba C — reinicio de aplicación

Comportamiento esperado:

```text
Detener API
    ↓
Reiniciar API
    ↓
Memoria perdida
```

Esto es correcto porque la implementación actual utiliza memoria RAM.

---

# 24. Decisiones arquitectónicas importantes

## El LLM no es el agente

`OllamaService` únicamente comunica con el modelo.

El agente coordina:

* contexto;
* prompts;
* memoria;
* herramientas futuras;
* conocimiento futuro;
* LLM.

---

## No acoplar el agente a Ollama

El agente depende de:

```csharp
IChatService
```

y no directamente de:

```csharp
OllamaService
```

Esto permitirá cambiar posteriormente de proveedor sin modificar la lógica principal del agente.

---

## El contexto es el centro

`ConversationContext` representa el estado de una conversación.

La memoria se encarga de almacenarlo y recuperarlo.

El agente lo utiliza.

`PromptBuilder` lo transforma en mensajes para el LLM.

---

## La memoria no pertenece al Controller

`ChatController` únicamente recibe la petición y delega.

No debe conocer:

* `Dictionary`;
* historial;
* `ConversationContext`;
* almacenamiento;
* proveedor de LLM.

---

# 25. Lo que NO se debe implementar todavía

La siguiente fase debe comenzar únicamente después de confirmar que la fase actual está correctamente documentada.

No introducir todavía:

* SQL Server;
* Entity Framework;
* Redis;
* RAG;
* embeddings;
* vector databases;
* Tools;
* agentes autónomos complejos;
* persistencia permanente.

La memoria RAM debe considerarse la implementación educativa base sobre la cual se podrá evolucionar posteriormente.

---

# 26. Fases posteriores

## Fase siguiente — Memoria persistente

Evaluar persistencia mediante SQL Server.

Objetivo conceptual:

```text
Conversation
    |
    +--> ConversationId
    +--> User
    +--> Messages
    +--> Timestamp
```

La persistencia permitirá conservar conversaciones después de reiniciar la aplicación.

---

## Tools

Permitir que el agente pueda ejecutar funciones de C#.

Ejemplos futuros:

```text
GetCurrentDate()
ConsultarEmpleado()
ConsultarSQL()
LeerArchivo()
```

---

## RAG

Agregar conocimiento externo al contexto:

```text
Pregunta
   |
   v
Retriever
   |
   v
Documentos relevantes
   |
   v
ConversationContext
   |
   v
LLM
```

---

# 27. Estado exacto para continuar

El proyecto actualmente tiene funcionando:

* ASP.NET Core 8;
* API REST;
* Swagger;
* Ollama local;
* Llama 3.2;
* `IChatService`;
* `OllamaService`;
* `IAgentService`;
* `AgentService`;
* `ConversationContext`;
* `IConversationContextFactory`;
* `ConversationContextFactory`;
* `IPromptBuilder`;
* `PromptBuilder`;
* `ISystemPromptProvider`;
* `SystemPromptProvider`;
* `IMemoryService`;
* `MemoryService`;
* `ConversationId` en `ChatRequest`.

La memoria conversacional en RAM está implementada y probada.

Las pruebas realizadas confirmaron:

```text
Mismo ConversationId
        ↓
mismo historial
        ↓
memoria funcional
```

y:

```text
ConversationId A
        ≠
ConversationId B
        ↓
historiales independientes
```

La siguiente fase lógica es evaluar **memoria persistente**, pero antes de implementarla se debe analizar qué responsabilidades deben permanecer en `MemoryService` y qué responsabilidades deben pasar a una capa de persistencia.

---

# 28. Prompt de continuidad para el próximo chat

Utiliza el siguiente texto como contexto inicial del próximo chat:

> Estoy desarrollando un proyecto educativo llamado `LearningAgent.Api` utilizando ASP.NET Core 8, C# y Ollama con el modelo local `llama3.2`.
>
> El objetivo final es construir un agente de IA modular que pueda incorporar memoria, Tools, RAG, SQL Server, documentos y persistencia de conversaciones.
>
> La API funciona correctamente desde Swagger.
>
> La arquitectura actual es:
>
> ```text
> ChatController
>       |
>       v
> IAgentService
>       |
>       v
> AgentService
>       |
>       v
> IMemoryService
>       |
>       v
> MemoryService
>       |
>       v
> ConversationContext
>       |
>       v
> PromptBuilder
>       |
>       v
> IChatService
>       |
>       v
> OllamaService
>       |
>       v
> Ollama / Llama 3.2
> ```
>
> `ConversationContext` contiene:
>
> ```csharp
> Guid ConversationId
> List<ConversationMessage> Messages
> string SystemPrompt
> ```
>
> `ConversationContextFactory` crea nuevos contextos y obtiene el `SystemPrompt` mediante `ISystemPromptProvider`.
>
> `MemoryService` utiliza:
>
> ```csharp
> Dictionary<Guid, ConversationContext>
> ```
>
> y está registrado como:
>
> ```csharp
> builder.Services.AddSingleton<IMemoryService, MemoryService>();
> ```
>
> También están registrados como Singleton:
>
> ```csharp
> ISystemPromptProvider
> IConversationContextFactory
> IMemoryService
> ```
>
> `AgentService` recibe:
>
> ```csharp
> IChatService
> IPromptBuilder
> IMemoryService
> ```
>
> y su flujo es:
>
> ```text
> GetOrCreate(conversationId)
>       ↓
> agregar mensaje user
>       ↓
> PromptBuilder
>       ↓
> IChatService
>       ↓
> agregar respuesta assistant
>       ↓
> MemoryService.Save()
> ```
>
> La memoria conversacional en RAM ya fue probada correctamente.
>
> Prueba 1:
>
> ```text
> ConversationId A
> "Me llamo Juan"
> "¿Cómo me llamo?"
> → recordó Juan
> ```
>
> Prueba 2:
>
> ```text
> ConversationId A → Juan
> ConversationId B → Pedro
> → las conversaciones no se mezclaron
> ```
>
> La memoria se pierde al reiniciar la aplicación. Esto es esperado porque todavía no existe persistencia.
>
> El proyecto tiene un enfoque completamente educativo. Cada fase debe comprenderse, implementarse, probarse y documentarse antes de avanzar.
>
> La siguiente fase propuesta es estudiar memoria persistente, posiblemente mediante SQL Server, pero **no comenzar a implementar código inmediatamente**.
>
> Primero analizar:
>
> 1. qué responsabilidades debe conservar `MemoryService`;
> 2. qué responsabilidades debe asumir una futura capa de persistencia;
> 3. qué interfaz debería abstraer el almacenamiento;
> 4. cómo evolucionar de memoria RAM a persistencia sin romper `AgentService`;
> 5. qué diseño permite mantener la arquitectura limpia.
>
> Guíame paso a paso y explica primero las decisiones arquitectónicas antes de escribir código.

---

# 29. Regla de trabajo para futuras fases

El proyecto debe evolucionar incrementalmente.

La prioridad es:

```text
Entender
   ↓
Diseñar
   ↓
Implementar
   ↓
Probar
   ↓
Documentar
   ↓
Continuar
```

No avanzar a una nueva funcionalidad hasta que la anterior esté funcionando y probada.

No introducir abstracciones únicamente porque podrían ser útiles en el futuro.

Cada componente debe tener una responsabilidad clara y una razón concreta para existir.
