# PROJECT.md — LearningAgent.Api

## 1. Propósito del proyecto

`LearningAgent.Api` es un proyecto educativo para aprender, desde cero y de forma incremental, cómo construir un agente de IA en C#/.NET.

La prioridad del proyecto no es solamente conseguir que "funcione", sino comprender:

- qué responsabilidad tiene cada componente;
- cómo se comunican entre sí;
- por qué se utiliza cada abstracción;
- cómo evolucionar de un chatbot sencillo hacia un agente;
- qué problemas aparecen al introducir memoria, persistencia, Tools y RAG;
- cómo mantener una arquitectura mantenible y extensible.

La evolución general del proyecto es:

```text
Chatbot
   ↓
Memory
   ↓
Persistence
   ↓
Tools
   ↓
RAG
   ↓
Agente más autónomo
```

---

# 2. Objetivo tecnológico

Tecnologías actuales:

- C#
- .NET 8
- ASP.NET Core Web API
- Swagger
- SQL Server LocalDB
- Ollama
- Modelo `llama3.2`
- `HttpClientFactory`
- `System.Text.Json`
- `Microsoft.Data.SqlClient`

Ollama:

```text
Base URL: http://127.0.0.1:11434
Modelo: llama3.2
Endpoint utilizado: /api/chat
```

OpenAI existe todavía en el proyecto como implementación alternativa (`OpenAIService` y `OpenAIOptions`), pero para el aprendizaje actual el proveedor activo es Ollama.

La razón principal es mantener el proyecto educativo sin depender de una API de pago.

---

# 3. Arquitectura actual

El flujo principal de conversación es:

```text
ChatController
      ↓
AgentService
      ↓
MemoryService
      ↓
PromptBuilder
      ↓
IChatService
      ↓
OllamaService
      ↓
Ollama
      ↓
llama3.2
```

La persistencia sigue esta ruta:

```text
AgentService
      ↓
IMemoryService
      ↓
IConversationStore
      ↓
SqlConversationStore
      ↓
SQL Server / LocalDB
```

La arquitectura utiliza DI de ASP.NET Core para desacoplar las responsabilidades.

---

# 4. Estructura actual del proyecto

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
│       ├── OllamaMessage.cs
│       ├── OllamaTool.cs
│       ├── OllamaToolCall.cs
│       ├── OllamaToolCallFunction.cs
│       └── OllamaToolFunction.cs
│
├── Dtos
│   ├── ChatRequest.cs
│   └── ChatResponse.cs
│
├── Models
│   ├── Chat
│   │   ├── ChatResult.cs
│   │   └── ConversationMessage.cs
│   │
│   └── Conversation
│       └── ConversationContext.cs
│
├── Options
│   ├── OllamaOptions.cs
│   └── OpenAIOptions.cs
│
├── Services
│   │
│   ├── Agent
│   │   ├── IAgentService.cs
│   │   └── AgentService.cs
│   │
│   ├── Chat
│   │   ├── IChatService.cs
│   │   ├── OllamaService.cs
│   │   └── OpenAIService.cs
│   │
│   ├── Conversation
│   │   ├── ConversationContextFactory.cs
│   │   ├── IConversationContextFactory.cs
│   │   ├── IConversationStore.cs
│   │   ├── InMemoryConversationStore.cs
│   │   └── SqlConversationStore.cs
│   │
│   ├── Memory
│   │   ├── IMemoryService.cs
│   │   └── MemoryService.cs
│   │
│   ├── Prompts
│   │   ├── IPromptBuilder.cs
│   │   ├── PromptBuilder.cs
│   │   ├── ISystemPromptProvider.cs
│   │   └── SystemPromptProvider.cs
│   │
│   └── Tools
│       ├── CalculatorTool.cs
│       ├── ITool.cs
│       ├── IToolRegistry.cs
│       ├── ToolCall.cs
│       ├── ToolDefinition.cs
│       └── ToolRegistry.cs
│
├── appsettings.json
├── launchSettings.json
├── LearningAgent.Api.http
└── Program.cs
```

---

# 5. Etapas ya completadas

## 5.1 Chat básico

Se implementó el flujo inicial:

```text
HTTP Request
   ↓
ChatController
   ↓
IAgentService
   ↓
IChatService
   ↓
Ollama
```

Se creó una abstracción `IChatService` para evitar que `AgentService` dependa directamente del proveedor de IA.

---

## 5.2 System Prompt

Se separó el System Prompt mediante:

```text
ISystemPromptProvider
SystemPromptProvider
```

El objetivo es evitar que las instrucciones del agente estén mezcladas con la lógica de conversación.

System Prompt actual:

```text
LearningAgent.

Tu propósito es ayudar a los usuarios a aprender desarrollo de software e Inteligencia Artificial utilizando .NET.

Siempre responde de forma clara, técnica y estructurada.

Si existen varias alternativas, explica las ventajas y desventajas de cada una antes de recomendar una solución.

Cuando expliques código, prioriza el aprendizaje antes que simplemente proporcionar la respuesta.
```

---

## 5.3 Memoria conversacional

Se implementó memoria mediante:

```text
ConversationContext
ConversationMessage
MemoryService
```

Una conversación tiene un `ConversationId` y una colección de mensajes.

Conceptualmente:

```text
ConversationContext
 ├── ConversationId
 ├── SystemPrompt
 └── Messages
      ├── user
      ├── assistant
      ├── user
      └── assistant
```

---

## 5.4 Persistencia

Se agregó persistencia mediante:

```text
IConversationStore
InMemoryConversationStore
SqlConversationStore
```

La aplicación puede conservar las conversaciones en SQL Server/LocalDB.

La memoria y la persistencia se mantienen separadas para evitar que `AgentService` conozca detalles específicos de SQL Server.

---

## 5.5 Concurrencia

`MemoryService` utiliza:

```csharp
ConcurrentDictionary<Guid, SemaphoreSlim>
```

para obtener un lock por `ConversationId`.

Conceptualmente:

```text
Conversation A → Lock A
Conversation B → Lock B
Conversation C → Lock C
```

Esto permite evitar que dos solicitudes concurrentes modifiquen simultáneamente el mismo contexto de conversación.

Las pruebas de aislamiento y concurrencia fueron realizadas correctamente.

---

# 6. Estado actual de Tools

La etapa de Tools ya tiene implementado y probado el ciclo principal de Tool Calling.

## 6.1 Concepto

Una Tool es una capacidad que el modelo puede solicitar, pero que realmente ejecuta nuestra aplicación.

El modelo no ejecuta C# directamente.

El flujo es:

```text
Usuario
   ↓
LLM
   ↓
Tool Call
   ↓
LearningAgent.Api
   ↓
Tool en C#
   ↓
Tool Result
   ↓
LLM
   ↓
Respuesta final
```

Esta separación es importante:

```text
LLM = decide qué necesita hacer
Aplicación = decide cómo y si puede ejecutarlo
Tool = ejecuta la operación permitida
```

---

# 7. Infraestructura de Tools implementada

## 7.1 ITool

Actualmente:

```csharp
public interface ITool
{
    ToolDefinition Definition { get; }
    Task<string> ExecuteAsync(string arguments);
}
```

La interfaz define dos responsabilidades:

1. describir la Tool;
2. ejecutar la Tool.

---

## 7.2 ToolDefinition

Actualmente:

```csharp
public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonElement Parameters { get; set; }
}
```

`Parameters` contiene un JSON Schema que describe los argumentos que la Tool acepta.

Esto permite que Ollama conozca:

```text
nombre
descripción
parámetros
```

sin conocer la implementación C#.

---

## 7.3 CalculatorTool

Existe actualmente:

```text
CalculatorTool
```

y su definición declara:

```text
Name:
calculator

Description:
Realiza operaciones matemáticas simples.
```

El parámetro definido es:

```text
expression
```

Actualmente la Tool todavía no realiza el cálculo matemático real.

La implementación temporal devuelve el argumento recibido, por ejemplo:

```text
CalculatorTool recibió: {"expression":"10+20"}
```

Esto fue intencional para validar primero la arquitectura de Tool Calling.

---

## 7.4 ToolRegistry

Existe:

```text
IToolRegistry
ToolRegistry
```

El Registry recibe:

```csharp
IEnumerable<ITool>
```

mediante Dependency Injection.

Permite:

```csharp
GetTools()
```

para obtener todas las Tools disponibles.

También:

```csharp
GetTool(string name)
```

para localizar una Tool por nombre, ignorando mayúsculas/minúsculas.

Esto permite agregar nuevas Tools sin modificar `AgentService` para cada Tool.

---

# 8. Integración con Ollama

Se agregaron contratos específicos para Tool Calling:

```text
OllamaTool.cs
OllamaToolFunction.cs
OllamaToolCall.cs
OllamaToolCallFunction.cs
```

`OllamaChatRequest` ahora incluye:

```csharp
public List<OllamaTool> Tools { get; set; } = [];
```

`OllamaMessage` ahora soporta:

```csharp
Role
Content
ToolCalls
ToolName
```

Esto permite representar tanto mensajes normales como mensajes relacionados con Tools.

---

# 9. Resultado de la prueba real de Tool Calling

Se probó la solicitud:

```text
¿Cuánto es 10 + 20?
```

La primera llamada a Ollama devolvió un `tool_call` equivalente a:

```json
{
  "name": "calculator",
  "arguments": {
    "expression": "10+20"
  }
}
```

El modelo no produjo contenido final en esa primera respuesta:

```text
content = ""
```

Esto es correcto.

Significa que el modelo decidió solicitar una Tool.

---

# 10. Ejecución del Tool

`AgentService` recibió el `ToolCall`.

Después utilizó:

```csharp
_toolRegistry.GetTool(toolCall.Name)
```

para localizar:

```text
calculator
```

Posteriormente ejecutó:

```csharp
tool.ExecuteAsync(toolCall.Arguments)
```

El resultado fue agregado al contexto como mensaje de Tool.

---

# 11. Segunda llamada a Ollama

Después de ejecutar la Tool, `AgentService` volvió a construir el contexto y llamó nuevamente a:

```csharp
_chatService.GetResponseAsync(messages)
```

La segunda llamada recibió 4 mensajes.

El flujo conceptual fue:

```text
system
user
assistant + tool_call
tool + tool_result
```

Ollama entonces generó la respuesta final:

```text
Según el resultado del calculador, la respuesta es:

10 + 20 = 30
```

Por lo tanto, el ciclo completo de Tool Calling ya fue validado.

---

# 12. Estado de los seis objetivos principales de Tools

```text
1. Definición de Tools para Ollama
   ✅ COMPLETADO

2. Recepción de tool_calls
   ✅ COMPLETADO

3. Ejecución desde AgentService
   ✅ COMPLETADO

4. Devolución del resultado al modelo
   ✅ COMPLETADO

5. Segunda llamada al modelo
   ✅ COMPLETADO

6. Respuesta final
   ✅ COMPLETADO
```

La infraestructura básica funciona.

---

# 13. Siguiente objetivo inmediato

El siguiente trabajo es terminar la primera Tool real:

```text
CalculatorTool
```

Actualmente solamente recibe y devuelve los argumentos.

Debe evolucionar para que realmente pueda procesar una expresión matemática.

Ejemplo conceptual:

```text
Usuario:
¿Cuánto es 10 + 20?

LLM:
calculator
{
    "expression": "10+20"
}

CalculatorTool:
30

LLM:
10 + 20 = 30
```

La implementación debe mantenerse sencilla y segura.

No se debe permitir que la calculadora se convierta en una vía para ejecutar código arbitrario.

No utilizar:

```text
eval
compilación dinámica
ejecución de C#
procesos del sistema
```

para resolver las expresiones.

La implementación puede comenzar soportando un conjunto limitado y explícito de operaciones matemáticas.

---

# 14. Después de CalculatorTool

Una vez que `CalculatorTool` sea realmente funcional, se deben revisar y fortalecer las siguientes partes de Tools:

### A. Validación de argumentos

Validar que los argumentos recibidos tengan el formato esperado.

Por ejemplo:

```json
{
    "expression": "10+20"
}
```

y manejar correctamente argumentos inválidos.

---

### B. Manejo de errores

Una Tool no debe provocar que toda la conversación falle ante una entrada inválida.

Ejemplo:

```text
calculator
→ expresión inválida
→ Tool devuelve error controlado
→ LLM recibe el resultado
→ LLM informa al usuario
```

---

### C. Seguridad

El agente nunca debe ejecutar automáticamente operaciones peligrosas.

No permitir como consecuencia de un Tool:

```text
ejecución arbitraria de comandos
ejecución arbitraria de C#
eliminación arbitraria de archivos
SQL arbitrario
acciones destructivas sin autorización
```

Las Tools deben representar capacidades explícitas y controladas.

---

### D. Registro de Tool Calls

Posteriormente se puede mejorar el logging para conocer:

```text
Tool solicitada
Argumentos
Resultado
Duración
Error, si ocurrió
```

Esto será útil para depuración y observabilidad.

---

### E. Persistencia de Tool Calls

Actualmente `ConversationMessage` ya contempla:

```csharp
ToolName
ToolCalls
```

pero se debe revisar `SqlConversationStore`.

Es posible que la persistencia SQL actual solamente almacene:

```text
Role
Content
```

Si es así, habrá que decidir cómo persistir correctamente:

```text
assistant + tool_calls
tool + tool_name + result
```

Esto debe hacerse después de que el ciclo básico de Tools esté completamente estable.

No debe introducirse complejidad innecesaria antes de terminar la calculadora.

---

# 15. Rendimiento de Ollama

Existe un problema conocido de rendimiento.

Las pruebas han mostrado tiempos elevados en Ollama, especialmente en:

```text
load_duration
prompt_eval_duration
eval_duration
```

En una prueba reciente:

```text
Primera llamada:
≈ 18.6 segundos

Segunda llamada:
≈ 4.6 segundos
```

La primera llamada también tuvo un costo importante de carga del modelo y evaluación del prompt.

Esto está documentado pero:

**NO debe bloquear el desarrollo de Tools.**

La optimización de rendimiento queda pendiente para una etapa posterior.

También existe el antecedente de un timeout de `HttpClient` de 100 segundos durante pruebas anteriores.

No rediseñar la arquitectura por este problema.

---

# 16. Estado actual de DI

La infraestructura de Tools se registra mediante DI:

```csharp
builder.Services.AddScoped<ITool, CalculatorTool>();
builder.Services.AddScoped<IToolRegistry, ToolRegistry>();
```

La intención es que futuras Tools puedan registrarse de la misma manera:

```text
ITool
 ├── CalculatorTool
 ├── CurrentTimeTool
 └── WeatherTool
```

sin modificar el mecanismo central de descubrimiento.

---

# 17. Principios arquitectónicos que deben mantenerse

Durante las siguientes etapas:

### No reemplazar la arquitectura actual sin necesidad

El proyecto ya tiene una separación funcional entre:

```text
Controller
Agent
Memory
Conversation
Prompt
Chat Provider
Tools
```

Las nuevas capacidades deben integrarse sobre esta arquitectura.

---

### Mantener interfaces donde aporten desacoplamiento

Por ejemplo:

```text
IAgentService
IChatService
IMemoryService
IConversationStore
ITool
IToolRegistry
IPromptBuilder
ISystemPromptProvider
```

No crear interfaces únicamente por crear interfaces.

---

### Evitar lógica innecesariamente compleja

El proyecto es educativo.

La solución debe ser suficientemente profesional para enseñar buenas prácticas, pero no debe introducir patrones o abstracciones que todavía no aporten valor.

---

### Seguridad primero para Tools

El LLM puede solicitar una Tool, pero eso no significa que la aplicación deba ejecutar cualquier cosa.

La aplicación controla:

```text
qué Tools existen
qué argumentos aceptan
cómo se ejecutan
qué errores producen
qué acciones están permitidas
```

---

# 18. Qué NO hacer en el siguiente chat

No:

- reiniciar el proyecto;
- cambiar Ollama por otro proveedor;
- introducir RAG todavía;
- introducir embeddings todavía;
- introducir una vector database todavía;
- rediseñar `AgentService` sin una razón concreta;
- reemplazar `IToolRegistry`;
- implementar Tools peligrosas;
- solucionar el rendimiento de Ollama antes de terminar la etapa actual;
- agregar complejidad innecesaria.

La prioridad es terminar correctamente la primera Tool real y consolidar el ciclo de Tool Calling.

---

# 19. Instrucciones para el siguiente chat

## CONTEXTO

Este documento es el estado actual de `LearningAgent.Api`.

El proyecto es educativo y el objetivo es aprender cómo construir un agente de IA en .NET mediante una evolución incremental.

La infraestructura básica de Tools ya funciona.

## PUNTO EXACTO DONDE CONTINUAR

Continuar desde:

```text
CalculatorTool
```

Actualmente recibe los argumentos pero todavía no calcula realmente.

El objetivo inmediato es convertirla en una calculadora funcional y segura.

## Forma de trabajo

Trabajar de manera práctica y explicar simultáneamente.

No es necesario realizar otra ronda extensa de preguntas conceptuales antes de programar.

Para cada cambio:

1. explicar brevemente qué problema resuelve;
2. indicar qué archivo se modifica;
3. mostrar el código necesario;
4. explicar las partes importantes;
5. probar;
6. revisar el resultado;
7. continuar con el siguiente paso.

No entregar todo el proyecto de golpe.

Avanzar incrementalmente.

## Orden sugerido

```text
1. Revisar la implementación actual de CalculatorTool.
2. Crear la representación necesaria para los argumentos, si realmente aporta valor.
3. Implementar cálculo seguro de expresiones soportadas.
4. Validar argumentos.
5. Manejar errores de forma controlada.
6. Probar desde Swagger.
7. Probar mediante conversación real con Ollama.
8. Revisar el flujo completo:
   usuario → LLM → tool_call → Tool → resultado → LLM → respuesta.
9. Revisar qué falta para considerar Tools terminadas.
10. Revisar persistencia de ToolCalls/ToolName.
11. Documentar la etapa Tools.
12. Preparar el siguiente PROJECT.md para comenzar RAG en un chat separado.
```

## Regla importante

Si una decisión técnica requiere cambiar una parte existente, primero explicar:

```text
qué problema existe
por qué el cambio es necesario
qué alternativa se descartó
```

No realizar cambios arquitectónicos solamente por preferencia de estilo.

---

# 20. Próxima gran etapa: RAG

Después de terminar Tools, la siguiente etapa será:

```text
RAG
```

y se realizará en otro chat.

Objetivo conceptual:

```text
Usuario
   ↓
Agent
   ↓
RAG
   ↓
Documentos / conocimiento
   ↓
Contexto relevante
   ↓
LLM
   ↓
Respuesta
```

RAG debe estudiarse después de comprender correctamente Tools.

Posibles temas posteriores:

```text
Documentos
   ↓
Chunking
   ↓
Embeddings
   ↓
Vector Store
   ↓
Similarity Search
   ↓
Contexto
   ↓
LLM
```

La tecnología concreta para embeddings/vector store se decidirá cuando llegue esa etapa.

---

# 21. Visión final del proyecto

La meta educativa es llegar progresivamente a una arquitectura parecida a:

```text
                         ┌──────────────┐
                         │     User     │
                         └──────┬───────┘
                                │
                                ▼
                         ┌──────────────┐
                         │    Agent     │
                         └──────┬───────┘
                                │
                 ┌──────────────┼──────────────┐
                 │              │              │
                 ▼              ▼              ▼
              Memory          Tools           RAG
                 │              │              │
                 │              ▼              ▼
                 │         External APIs    Documents
                 │
                 ▼
             Persistence
                 │
                 ▼
             SQL Server
                                │
                                ▼
                              LLM
                                │
                                ▼
                           Final Answer
```

La arquitectura final no está cerrada. Se debe evolucionar conforme se aprenden los conceptos y aparecen necesidades reales.

---

# 22. Porcentaje aproximado de avance

El proyecto ha completado aproximadamente:

```text
Chat básico             ████████████████████ 100%
System Prompt           ████████████████████ 100%
Memory                  ████████████████████ 100%
Persistence             ████████████████████ 100%
Concurrency             ████████████████████ 100%
Tools - infraestructura ████████████████████ 100%
Tools - consolidación   ████████████░░░░░░░░  ~60%
RAG                     ░░░░░░░░░░░░░░░░░░░░   0%
```

El porcentaje es educativo/aproximado, no una métrica de código terminado.

El siguiente objetivo es llevar Tools a una primera versión estable antes de comenzar RAG.
