# LearningAgent.Api

## Contexto del proyecto

Este proyecto es un agente de Inteligencia Artificial educativo desarrollado en **ASP.NET Core 8 Web API**, cuyo objetivo es aprender e implementar progresivamente una arquitectura de agente utilizando **.NET + Ollama + Llama 3.2**.

El proyecto no busca únicamente consumir un LLM, sino construir progresivamente un agente propio que pueda:

* Mantener conversaciones.
* Administrar memoria conversacional.
* Construir prompts.
* Utilizar Tools.
* Ejecutar Tools desde .NET.
* Devolver resultados de Tools al modelo.
* Realizar múltiples llamadas al modelo.
* Incorporar RAG.
* Posteriormente combinar memoria, Tools y RAG dentro del agente.

El proyecto tiene un enfoque educativo: cada etapa debe entenderse y construirse antes de pasar a la siguiente.

---

# Estado actual del proyecto

## Stack

* .NET 8
* ASP.NET Core Web API
* C#
* Swagger
* Ollama
* Llama 3.2
* HTTP Client
* `System.Text.Json`
* LocalDB para persistencia cuando corresponde
* Arquitectura basada en servicios e interfaces

Ollama actualmente se ejecuta localmente:

```text
http://127.0.0.1:11434
```

Modelo utilizado:

```text
llama3.2
```

La API ejecuta actualmente en:

```text
https://localhost:7125
http://localhost:5072
```

---

# Objetivo arquitectónico

La arquitectura actual busca mantener separadas las responsabilidades:

```text
Controller
    ↓
AgentService
    ↓
PromptBuilder
    ↓
ChatService / OllamaService
    ↓
Ollama
```

Cuando el modelo solicita una Tool:

```text
Ollama
    ↓
ToolCall
    ↓
AgentService
    ↓
ToolRegistry
    ↓
ITool
    ↓
Tool.ExecuteAsync()
    ↓
resultado
    ↓
ConversationContext
    ↓
PromptBuilder
    ↓
Ollama
    ↓
respuesta final
```

La idea es que `AgentService` no conozca implementaciones concretas de Tools.

---

# Etapas completadas

## 1. Chat básico

Se implementó la comunicación entre la API y Ollama mediante un servicio de chat.

La interfaz principal es:

```csharp
public interface IChatService
{
    Task<ChatResult> GetResponseAsync(
        IEnumerable<ConversationMessage> messages);
}
```

Actualmente existe una implementación para Ollama:

```text
OllamaService
```

y también una implementación para OpenAI:

```text
OpenAIService
```

La implementación utilizada actualmente para las pruebas del agente es `OllamaService`.

---

## 2. Memoria conversacional

Se implementó un `ConversationContext` que mantiene los mensajes de la conversación.

El flujo general es:

```text
conversationId
    ↓
MemoryService
    ↓
ConversationContext
    ↓
Messages
```

`AgentService` obtiene o crea el contexto:

```csharp
var context = _memoryService.GetOrCreate(conversationId);
```

y agrega el mensaje del usuario:

```csharp
context.Messages.Add(new ConversationMessage
{
    Role = "user",
    Content = message
});
```

La conversación posteriormente se guarda mediante:

```csharp
_memoryService.Save(context);
```

---

## 3. PromptBuilder

Existe:

```text
IPromptBuilder
PromptBuilder
```

La responsabilidad de `PromptBuilder` es construir la colección de mensajes que se enviará al LLM.

Actualmente agrega primero el `system prompt`:

```csharp
new ConversationMessage
{
    Role = "system",
    Content = contex.SystemPrompt
}
```

y posteriormente los mensajes de la conversación:

```csharp
messages.AddRange(contex.Messages);
```

Por lo tanto, el modelo recibe conceptualmente:

```text
system
user
assistant
tool
assistant
...
```

según el estado de la conversación.

---

## 4. Tools

Esta etapa ya está terminada y probada.

Se creó:

```text
ITool
ToolDefinition
ToolCall
ToolRegistry
CalculatorTool
```

### ITool

```csharp
public interface ITool
{
    ToolDefinition Definition { get; }

    Task<string> ExecuteAsync(string arguments);
}
```

Cada Tool debe proporcionar:

* definición para el LLM
* ejecución

Esto permite agregar nuevas Tools sin modificar `AgentService`.

---

## 5. ToolDefinition

Actualmente una Tool expone:

```csharp
public class ToolDefinition
{
    public string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    public JsonElement Parameters { get; set; }
}
```

Ollama recibe estas definiciones mediante:

```json
{
  "type": "function",
  "function": {
    "name": "...",
    "description": "...",
    "parameters": {}
  }
}
```

---

## 6. CalculatorTool

La primera Tool implementada fue:

```text
CalculatorTool
```

Su nombre para Ollama es:

```text
calculator
```

Acepta:

```json
{
  "expression": "..."
}
```

La Tool utiliza un parser matemático propio.

Actualmente soporta:

```text
+
-
*
/
()
```

y respeta la precedencia matemática.

También soporta números negativos.

Ejemplos probados:

```text
20 + 30 * 2
→ 80
```

```text
(20 + 30) * 2
→ 100
```

```text
100 / 5 + 3
→ 23
```

```text
-10 + 5
→ -5
```

División entre cero:

```text
10 / 0
→ No se puede dividir entre cero.
```

El parser fue validado independientemente del LLM.

---

## 7. ToolRegistry

El agente obtiene las Tools mediante:

```csharp
_toolRegistry.GetTool(toolCall.Name);
```

Por lo tanto `AgentService` no depende directamente de:

```text
CalculatorTool
```

Esto permite posteriormente registrar nuevas Tools.

La arquitectura esperada es:

```text
ToolRegistry
    ├── CalculatorTool
    ├── ...
    ├── ...
    └── ...
```

---

## 8. Tool Calling con Ollama

Se implementó el flujo completo de Tool Calling.

Ollama puede devolver:

```json
{
  "message": {
    "role": "assistant",
    "content": "",
    "tool_calls": [
      {
        "id": "call_xxx",
        "function": {
          "name": "calculator",
          "arguments": {
            "expression": "(20 + 30) * 2"
          }
        }
      }
    ]
  }
}
```

La aplicación convierte esa respuesta a:

```csharp
ToolCall
```

que contiene:

```csharp
public string Id { get; set; }

public string Name { get; set; }

public string Arguments { get; set; }
```

---

## 9. Conservación del ToolCallId

Se agregó soporte para conservar el identificador de la llamada.

`ConversationMessage` contiene:

```csharp
public string ToolCallId { get; set; } = string.Empty;
```

Esto permite relacionar:

```text
assistant
    ↓
call_1x3yp5pc
    ↓
calculator
    ↓
tool result
```

El mensaje de Tool se construye en `AgentService` conceptualmente como:

```csharp
context.Messages.Add(new ConversationMessage
{
    Role = "tool",
    ToolName = toolCall.Name,
    ToolCallId = toolCall.Id,
    Content = toolResult
});
```

---

## 10. AgentService

El flujo principal del agente está implementado en:

```text
AgentService
```

El algoritmo actual es:

```text
1. Obtener contexto
2. Agregar mensaje del usuario
3. Construir mensajes
4. Llamar a Ollama
5. Revisar ToolCalls
6. Ejecutar cada Tool
7. Agregar resultados al contexto
8. Construir nuevamente los mensajes
9. Volver a llamar a Ollama
10. Obtener respuesta final
11. Guardar contexto
```

La lógica principal es:

```csharp
var chatResult = await _chatService.GetResponseAsync(messages);

if (chatResult.ToolCalls.Any())
{
    context.Messages.Add(new ConversationMessage
    {
        Role = "assistant",
        Content = chatResult.Content,
        ToolCalls = chatResult.ToolCalls
    });

    foreach (var toolCall in chatResult.ToolCalls)
    {
        var tool = _toolRegistry.GetTool(toolCall.Name);

        if (tool is null)
        {
            context.Messages.Add(new ConversationMessage
            {
                Role = "tool",
                ToolName = toolCall.Name,
                Content = $"Tool '{toolCall.Name}' not found."
            });

            continue;
        }

        var toolResult = await tool.ExecuteAsync(toolCall.Arguments);

        context.Messages.Add(new ConversationMessage
        {
            Role = "tool",
            ToolName = toolCall.Name,
            ToolCallId = toolCall.Id,
            Content = toolResult
        });
    }

    messages = _promptBuilder.Build(context);

    chatResult = await _chatService.GetResponseAsync(messages);
}
```

El flujo de Tools ya fue probado correctamente.

---

## 11. Prueba final de Tools

Se probó:

```text
Calcula 20 + 30 y después multiplica el resultado por 2.
```

Ollama generó correctamente:

```json
{
  "expression": "(20 + 30) * 2"
}
```

La CalculatorTool devolvió:

```text
100
```

La segunda llamada a Ollama produjo una respuesta final coherente:

```text
El resultado de la expresión es 100.
```

Esto confirmó que funcionan correctamente:

```text
Tool Definition
        ↓
Tool Call
        ↓
ToolRegistry
        ↓
CalculatorTool
        ↓
resultado
        ↓
ConversationContext
        ↓
segunda llamada a Ollama
        ↓
respuesta final
```

---

# Estado de Tools

```text
Tools
├── Definición                         ✓
├── ToolDefinition                     ✓
├── ToolRegistry                       ✓
├── ToolCall                           ✓
├── Recepción de tool_calls            ✓
├── Ejecución desde AgentService       ✓
├── Resultado de Tool                  ✓
├── Regreso del resultado al LLM       ✓
├── Segunda llamada al LLM             ✓
├── ToolCallId                          ✓
└── Respuesta final                    ✓
```

La etapa de Tools puede considerarse completada para los objetivos actuales.

---

# Problemas que se encontraron y resolvieron

## Problema 1 — argumentos no JSON

Inicialmente se ejecutaba:

```csharp
tool.ExecuteAsync("10 + 20");
```

pero `CalculatorTool` esperaba JSON.

Se corrigió utilizando:

```json
{
  "expression": "10 + 20"
}
```

---

## Problema 2 — parser matemático

Se implementó un parser matemático propio para evitar depender de evaluación insegura de expresiones.

Se validó:

```text
20 + 30 * 2 → 80
(20 + 30) * 2 → 100
100 / 5 + 3 → 23
-10 + 5 → -5
10 / 0 → error controlado
```

---

## Problema 3 — LLM construía expresiones incorrectas

Ante:

```text
Calcula 20 + 30 y después multiplica el resultado por 2.
```

Llama inicialmente generó:

```text
20 + 30 * 2
```

que produce:

```text
80
```

Posteriormente, mediante una mejor descripción de la Tool, Llama generó:

```text
(20 + 30) * 2
```

produciendo:

```text
100
```

Esto confirmó la importancia de las descripciones de las Tools para guiar al modelo.

---

# Siguiente etapa: RAG

La siguiente etapa del proyecto es:

```text
RAG
```

El objetivo es incorporar conocimiento externo al contexto del agente para que el LLM pueda responder utilizando información almacenada en documentos o fuentes propias.

No comenzar directamente escribiendo código.

Primero analizar la arquitectura necesaria.

---

# Objetivos de la etapa RAG

La implementación debe permitir entender claramente el flujo:

```text
Usuario
   ↓
AgentService
   ↓
¿Necesita conocimiento externo?
   ↓
RAG
   ↓
Documentos
   ↓
Chunks
   ↓
Embeddings
   ↓
Vector Store
   ↓
Similarity Search
   ↓
Contexto relevante
   ↓
Prompt
   ↓
LLM
   ↓
Respuesta
```

La implementación debe mantenerse alineada con el objetivo educativo del proyecto.

---

# Lo que se debe estudiar primero

Antes de implementar RAG, analizar y explicar:

1. ¿Qué problema resuelve RAG?
2. Diferencia entre:

   * LLM
   * contexto
   * embeddings
   * vector database
   * semantic search
   * RAG
3. Flujo completo de ingestión.
4. Flujo completo de consulta.
5. Chunking.
6. Embeddings.
7. Almacenamiento vectorial.
8. Similarity search.
9. Construcción del contexto.
10. Integración con `AgentService`.

---

# Arquitectura RAG esperada

La implementación debe procurar mantener responsabilidades separadas.

Una arquitectura posible:

```text
Services
│
├── Agent
│   └── AgentService
│
├── Chat
│   └── OllamaService
│
├── Conversation
│
├── Memory
│
├── Prompts
│
├── Tools
│   ├── ITool
│   ├── ToolRegistry
│   └── CalculatorTool
│
└── RAG
    ├── IRagService
    ├── DocumentLoader
    ├── ChunkingService
    ├── EmbeddingService
    ├── VectorStore
    └── RetrievalService
```

La estructura final debe decidirse después de analizar el proyecto actual.

No crear interfaces o servicios innecesarios antes de conocer el flujo que vamos a implementar.

---

# Ollama y RAG

Actualmente Ollama se utiliza como LLM.

Para RAG se deberá analizar si utilizaremos Ollama también para generar embeddings.

El modelo de embeddings debe seleccionarse explícitamente antes de implementar esa parte.

No asumir automáticamente que:

```text
llama3.2
```

es el modelo adecuado para embeddings.

Primero evaluar las opciones disponibles en el entorno local de Ollama.

---

# Vector Store

Antes de elegir una base vectorial, evaluar alternativas compatibles con el objetivo educativo del proyecto.

Considerar inicialmente opciones como:

```text
SQLite + almacenamiento vectorial
Chroma
Qdrant
PostgreSQL + pgvector
```

La elección debe considerar:

* facilidad de instalación
* compatibilidad con .NET
* funcionamiento local
* facilidad para aprender
* posibilidad de ejecutar el proyecto sin servicios de pago
* complejidad de integración

No elegir automáticamente una opción sin comparar brevemente las alternativas.

---

# Restricciones importantes

Este proyecto es educativo.

Priorizar:

```text
claridad
```

sobre:

```text
complejidad
```

No introducir frameworks de alto nivel de RAG si ocultan completamente el funcionamiento interno que queremos aprender.

La intención es entender primero:

```text
documento
    ↓
chunk
    ↓
embedding
    ↓
vector
    ↓
similarity search
    ↓
contexto
    ↓
LLM
```

antes de abstraerlo mediante frameworks.

---

# Relación entre Tools y RAG

No confundir ambas capacidades.

## Tools

Permiten que el agente:

```text
ACTÚE
```

Ejemplo:

```text
calculator
database query
API externa
```

## RAG

Permite que el agente:

```text
CONSULTE CONOCIMIENTO
```

Ejemplo:

```text
documentación .NET
manual interno
PDF
base de conocimiento
```

La arquitectura futura deberá poder combinar ambas:

```text
                    ┌── Tool
                    │
Usuario → Agent ────┤
                    │
                    └── RAG
```

---

# Objetivo final del proyecto

La arquitectura completa que se busca construir progresivamente es:

```text
                    ┌──────────────┐
                    │     User     │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │ AgentService │
                    └──────┬───────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
          Memory        Tools          RAG
              │            │            │
              │            ▼            ▼
              │       ToolRegistry  Retrieval
              │            │            │
              │            ▼            ▼
              │           Tool       Context
              │
              └────────────┬────────────┘
                           │
                           ▼
                    PromptBuilder
                           │
                           ▼
                     Ollama / LLM
                           │
                           ▼
                     Final Response
```

---

# Instrucciones para el siguiente chat

El siguiente chat debe comenzar trabajando exclusivamente en la etapa:

```text
RAG
```

No volver a implementar Tools desde cero.

Asumir que la etapa de Tools está funcionando.

Antes de modificar código:

1. Revisar la arquitectura actual.
2. Explicar el flujo RAG que construiremos.
3. Determinar cómo se obtendrán los documentos.
4. Determinar cómo se dividirán en chunks.
5. Determinar qué modelo de embeddings utilizaremos con Ollama.
6. Determinar qué almacenamiento/vector store utilizaremos.
7. Diseñar las interfaces mínimas necesarias.
8. Implementar primero la ingestión.
9. Implementar posteriormente retrieval.
10. Integrar retrieval con `AgentService`.
11. Probar el flujo completo con un documento pequeño.

No avanzar varias etapas simultáneamente.

Después de cada etapa realizar una prueba funcional antes de continuar.

---

# Estado aproximado

El proyecto ha completado:

```text
Chat básico                 ✓
Memoria                      ✓
PromptBuilder                ✓
Tools                        ✓
Tool Calling                 ✓
Tool execution               ✓
Tool result → LLM            ✓
Segunda llamada al LLM       ✓
RAG                          → SIGUIENTE ETAPA
```

La siguiente conversación debe comenzar desde:

```text
RAG
```

y conservar todo el contexto arquitectónico descrito en este documento.


# Estructura actual del proyecto

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


# Estado actual de DI

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

# Principios arquitectónicos que deben mantenerse

Durante las siguientes etapas:

## No reemplazar la arquitectura actual sin necesidad

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

## Mantener interfaces donde aporten desacoplamiento

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

## Evitar lógica innecesariamente compleja

El proyecto es educativo.

La solución debe ser suficientemente profesional para enseñar buenas prácticas, pero no debe introducir patrones o abstracciones que todavía no aporten valor.

---

## Seguridad primero para Tools

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

# Fin del PROJECT.md
