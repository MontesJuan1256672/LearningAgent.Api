# PROJECT.md — LearningAgent.Api

## 1. Propósito del proyecto

`LearningAgent.Api` es un proyecto educativo para aprender a construir un agente de Inteligencia Artificial utilizando C# y ASP.NET Core.

El objetivo no es solamente obtener un chatbot funcional, sino comprender progresivamente las decisiones de diseño y arquitectura necesarias para construir un agente mantenible.

Tecnologías actuales:

- ASP.NET Core 8
- C#
- Ollama
- Llama 3.2
- Microsoft.Data.SqlClient 7.0.2
- SQL Server LocalDB
- Swagger
- HttpClientFactory
- System.Text.Json

El proyecto evolucionará progresivamente hacia:

- conversación contextual;
- memoria;
- persistencia;
- concurrencia;
- Tools;
- RAG;
- documentos;
- cambio de proveedor LLM.

---

# 2. Metodología de aprendizaje

La metodología utilizada durante el proyecto es:

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

Reglas de trabajo:

1. No introducir abstracciones solamente porque podrían ser útiles en el futuro.
2. Implementar una etapa a la vez.
3. Probar cada cambio antes de continuar.
4. Utilizar el debugger cuando sea útil para comprender el flujo.
5. Explicar la responsabilidad de cada componente nuevo.
6. Antes de introducir un concepto importante, hacer una pregunta breve para comprobar la comprensión cuando sea conveniente.
7. Si el usuario no sabe responder, explicar la respuesta y continuar.
8. No convertir cada paso en un examen.
9. No adelantarse varias capas de arquitectura.
10. Preferir comprender el problema antes de aplicar una solución.

El usuario quiere aprender razonando y no únicamente recibir código terminado.

---

# 3. Estado general y porcentaje de avance

## Avance estimado: 55%

Este porcentaje es una estimación respecto al objetivo global del proyecto, no una métrica automática.

```text
[███████████░░░░░░░░░] ~55%
```

### Completado

- Consumo de un LLM.
- Arquitectura base del agente.
- Memoria conversacional en RAM.
- Separación entre memoria y persistencia.
- Abstracción `IConversationStore`.
- Persistencia real en SQL Server.
- Recuperación de conversaciones después de reiniciar la API.

### Siguiente

- Concurrencia por `ConversationId`.
- Robustecimiento de persistencia.
- Tools.
- RAG.
- Documentos.
- Evolución del proveedor LLM.
- Mejoras de arquitectura y observabilidad.

El porcentaje debe actualizarse aproximadamente conforme se completen las etapas.

---

# 4. Arquitectura actual

```text
                         ChatController
                              ↓
                        IAgentService
                              ↓
                         AgentService
                              ↓
                        IMemoryService
                              ↓
                     IConversationStore
                              ↓
                    SqlConversationStore
                              ↓
                         SQL Server
```

Flujo conversacional:

```text
HTTP Request
     ↓
ChatController
     ↓
AgentService
     ↓
MemoryService.GetOrCreate()
     ↓
IConversationStore.Get()
     ↓
ConversationContext
     ↓
agregar mensaje user
     ↓
PromptBuilder
     ↓
IChatService
     ↓
OllamaService
     ↓
Ollama / Llama 3.2
     ↓
agregar mensaje assistant
     ↓
MemoryService.Save()
     ↓
IConversationStore.Save()
     ↓
SQL Server
```

---

# 5. Fase 1 — Consumir un LLM

Completada.

La API se comunica correctamente con Ollama.

Configuración actual:

```text
BaseUrl = http://localhost:11434
Model = llama3.2
```

Endpoint utilizado por `OllamaService`:

```text
http://localhost:11434/api/chat
```

La arquitectura permite cambiar posteriormente el proveedor mediante `IChatService`.

---

# 6. Fase 2 — Arquitectura base del agente

Completada.

El controlador no depende directamente del proveedor LLM.

```text
ChatController
      ↓
IAgentService
      ↓
AgentService
      ↓
IChatService
      ↓
OllamaService
      ↓
Ollama / Llama 3.2
```

`AgentService` es el orquestador principal.

Responsabilidades actuales:

1. Obtener o crear la conversación.
2. Agregar el mensaje del usuario.
3. Construir el contexto para el LLM.
4. Solicitar la respuesta al LLM.
5. Agregar la respuesta del assistant.
6. Guardar el contexto.

---

# 7. Fase 3 — Memoria conversacional en RAM

Completada.

Inicialmente la memoria se implementó mediante:

```text
Dictionary<Guid, ConversationContext>
```

Se realizaron pruebas de:

### Misma conversación

```text
Conversation A
"Me llamo Juan"
"¿Cómo me llamo?"
→ recordó Juan
```

### Aislamiento

```text
Conversation A → Juan
Conversation B → Pedro
```

Resultado:

```text
A → Juan
B → Pedro
```

Se confirmó que las conversaciones no se mezclaban.

Limitaciones identificadas:

- La memoria RAM se pierde al reiniciar la API.
- No funciona como almacenamiento persistente.
- El `Dictionary` no es suficiente para resolver concurrencia.
- Una instancia de la aplicación no comparte automáticamente la memoria con otra instancia.

---

# 8. Fase 4 — Separación entre memoria y persistencia

Completada.

Se introdujo:

```csharp
IConversationStore
```

Contrato mínimo:

```csharp
ConversationContext? Get(Guid conversationId);

void Save(ConversationContext context);
```

Arquitectura:

```text
MemoryService
      ↓
IConversationStore
      ↓
implementación de almacenamiento
```

Decisión arquitectónica:

> `MemoryService` administra la memoria del agente y delega la persistencia a un Store.

`MemoryService` no conoce SQL Server.

---

# 9. InMemoryConversationStore

Se creó `InMemoryConversationStore` como implementación de `IConversationStore` para validar la separación de responsabilidades sin introducir SQL inmediatamente.

Actualmente existe en el proyecto, pero no es la implementación activa.

---

# 10. Fase 5 — Persistencia SQL Server

Completada.

Se eligió:

```text
Microsoft.Data.SqlClient
```

Paquete:

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="7.0.2" />
```

Base de datos:

```text
LearningAgentDb
```

Esquema:

```text
Agent
```

Tablas:

```text
Agent.Conversations
Agent.ConversationMessages
```

---

# 11. Esquema SQL actual

```sql
CREATE TABLE Agent.Conversations
(
    ConversationId UNIQUEIDENTIFIER NOT NULL,
    SystemPrompt NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
        CONSTRAINT DF_Conversations_CreatedAt
        DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL
        CONSTRAINT DF_Conversations_UpdatedAt
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Conversations
        PRIMARY KEY (ConversationId)
);

CREATE TABLE Agent.ConversationMessages
(
    MessageId BIGINT IDENTITY(1,1) NOT NULL,
    ConversationId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
        CONSTRAINT DF_ConversationMessages_CreatedAt
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ConversationMessages
        PRIMARY KEY (MessageId),

    CONSTRAINT FK_ConversationMessages_Conversations
        FOREIGN KEY (ConversationId)
        REFERENCES Agent.Conversations(ConversationId)
);

CREATE INDEX IX_ConversationMessages_ConversationId
ON Agent.ConversationMessages (ConversationId);
```

---

# 12. SqlConversationStore

Implementación activa:

```text
IConversationStore
        ↑
        │
SqlConversationStore
        ↓
Microsoft.Data.SqlClient
        ↓
SQL Server
```

Responsabilidades:

- abrir conexiones;
- ejecutar consultas SQL;
- reconstruir `ConversationContext`;
- guardar conversaciones;
- guardar mensajes;
- utilizar transacciones.

Registro actual:

```csharp
builder.Services.AddSingleton<IConversationStore, SqlConversationStore>();
```

---

# 13. Get()

`Get()` realiza dos consultas:

```sql
SELECT ConversationId, SystemPrompt
FROM Agent.Conversations
WHERE ConversationId = @ConversationId;
```

y:

```sql
SELECT Role, Content
FROM Agent.ConversationMessages
WHERE ConversationId = @ConversationId
ORDER BY MessageId;
```

Los resultados se reconstruyen en:

```text
ConversationContext
    ├── ConversationId
    ├── SystemPrompt
    └── Messages
         ├── ConversationMessage
         ├── ConversationMessage
         └── ...
```

Se verificó mediante debugger que los mensajes recuperados desde SQL llegan correctamente al `ConversationContext`.

---

# 14. Save()

La implementación actual utiliza una transacción.

Flujo:

```text
Save(context)
    ↓
BEGIN TRANSACTION
    ↓
UPDATE Conversation
    ↓
si no existe → INSERT Conversation
    ↓
COUNT mensajes existentes
    ↓
INSERT solamente mensajes nuevos
    ↓
COMMIT
```

Si ocurre una excepción:

```text
ROLLBACK
```

La estrategia actual asume que:

1. `Get()` devuelve mensajes ordenados por `MessageId`.
2. Los mensajes nuevos se agregan al final de `context.Messages`.
3. `AgentService` es quien modifica la conversación.
4. Todavía no existe procesamiento concurrente controlado para la misma conversación.

Esta estrategia funcionó correctamente en las pruebas realizadas.

---

# 15. Pruebas de persistencia realizadas

## Recuperar conversación existente

Se utilizó un `ConversationId` existente en SQL Server y se comprobó que el historial completo fue recuperado.

## Agregar mensajes sin eliminar los anteriores

Se comprobó que los mensajes anteriores permanecieron y solamente se agregaron los nuevos.

## Crear conversación nueva

Cuando el `ConversationId` no existía:

```text
Get()
 ↓
SQL → null
 ↓
ConversationContextFactory.Create()
 ↓
Save()
 ↓
INSERT Conversation
 ↓
INSERT Messages
```

Funcionó.

## Reiniciar la API

Se creó una conversación, se detuvo la API, se inició nuevamente y se utilizó el mismo `ConversationId` para realizar una pregunta dependiente del historial.

La conversación fue recuperada desde SQL Server después del reinicio y el LLM pudo utilizar el historial.

Conclusión:

> La memoria conversacional ya no depende exclusivamente de RAM y sobrevive al reinicio de la aplicación.

---

# 16. MemoryService actual

```csharp
public ConversationContext GetOrCreate(Guid conversationId)
{
    var context = _conversationStore.Get(conversationId);

    if (context is not null)
    {
        return context;
    }

    context = _contextFactory.Create(conversationId);

    _conversationStore.Save(context);

    return context;
}

public void Save(ConversationContext context)
{
    _conversationStore.Save(context);
}
```

Responsabilidades actuales:

- obtener una conversación mediante el Store;
- crear una conversación mediante la Factory cuando no existe;
- delegar el guardado al Store.

Importante: aunque inicialmente se había contemplado una caché RAM dentro de `MemoryService`, la implementación actual ya no mantiene un `Dictionary` propio. Actualmente delega directamente en `IConversationStore`.

---

# 17. Modelos actuales

## ConversationContext

```csharp
public class ConversationContext
{
    public Guid ConversationId { get; init; } = Guid.NewGuid();

    public List<ConversationMessage> Messages { get; } = [];

    public string SystemPrompt { get; set; } = string.Empty;
}
```

## ConversationMessage

```csharp
public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}
```

`ConversationMessage` actualmente no contiene `MessageId`. `MessageId` pertenece a la persistencia SQL y no se ha incorporado al modelo conversacional.

---

# 18. DI actual

```csharp
builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<IChatService, OllamaService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();

builder.Services.AddSingleton<ISystemPromptProvider, SystemPromptProvider>();
builder.Services.AddSingleton<IConversationContextFactory, ConversationContextFactory>();
builder.Services.AddSingleton<IMemoryService, MemoryService>();
builder.Services.AddSingleton<IConversationStore, SqlConversationStore>();
```

Actualmente:

- `AgentService` → Scoped
- `IChatService` / `OllamaService` → Scoped
- `IPromptBuilder` → Scoped
- `MemoryService` → Singleton
- `IConversationStore` → Singleton
- `ConversationContextFactory` → Singleton
- `SystemPromptProvider` → Singleton

---

# 19. Próxima etapa — Concurrencia

## Objetivo

Resolver correctamente qué ocurre cuando dos requests trabajan simultáneamente con la misma conversación.

Ejemplo:

```text
Request A ──────┐
                ├── Conversation X
Request B ──────┘
```

Existen dos problemas diferentes:

### Thread safety

Acceso concurrente a estructuras compartidas.

### Concurrencia lógica

Dos requests pueden leer y modificar el mismo estado conversacional al mismo tiempo.

Ejemplo:

```text
Request A
    ↓
Get Conversation X
    ↓
procesa LLM

Request B
    ↓
Get Conversation X
    ↓
procesa LLM
```

Ambos pueden terminar trabajando sobre versiones diferentes del historial.

---

# 20. Decisión conceptual sobre concurrencia

El control de concurrencia pertenece conceptualmente a `MemoryService`.

No debe introducirse directamente en `AgentService`.

Tampoco debe delegarse completamente al `IConversationStore`, porque el problema abarca el estado vivo y el procesamiento completo de la conversación.

La unidad natural de sincronización es:

```text
ConversationId
```

Conceptualmente:

```text
Conversation A → lock A
Conversation B → lock B
Conversation C → lock C
```

Esto permite que una conversación esté siendo procesada sin bloquear las demás.

---

# 21. Advertencia para la siguiente etapa

NO implementar simplemente:

```csharp
lock (...)
{
    GetOrCreate();
}
```

porque el procesamiento completo incluye una operación asíncrona con el LLM:

```text
Get context
    ↓
add user message
    ↓
build prompt
    ↓
await LLM
    ↓
add assistant message
    ↓
Save
```

Un bloqueo que solamente cubra `GetOrCreate()` no protege la operación completa.

Además, `lock` tradicional no es la solución adecuada para mantener un bloqueo a través de un `await`.

La siguiente etapa debe estudiar y elegir un mecanismo de sincronización asíncrono por `ConversationId`.

---

# 22. Objetivo inmediato del próximo chat

El próximo chat debe comenzar con:

> Diseñar e implementar concurrencia por `ConversationId`.

No comenzar todavía con:

- Tools;
- RAG;
- embeddings;
- documentos;
- nuevos proveedores LLM.

Primero resolver la consistencia de las conversaciones.

---

# 23. Estrategia recomendada para la próxima etapa

### Paso 1 — Comprender el problema

Crear un escenario controlado donde dos requests utilicen la misma conversación.

### Paso 2 — Reproducir el problema

Demostrar qué puede ocurrir sin sincronización.

### Paso 3 — Elegir mecanismo

Evaluar una solución basada en sincronización asíncrona por `ConversationId`.

Debe:

- permitir concurrencia entre conversaciones diferentes;
- serializar operaciones de una misma conversación;
- funcionar correctamente con `async/await`;
- evitar bloquear innecesariamente todo el servicio.

### Paso 4 — Implementar

Modificar la capa responsable de administrar la memoria/conversación.

### Paso 5 — Probar

Escenarios mínimos:

```text
Conversation A + Request A
Conversation A + Request B
```

El primer caso debe serializarse.

Y:

```text
Conversation A + Request A
Conversation B + Request B
```

El segundo debe poder ejecutarse independientemente.

### Paso 6 — Revisar persistencia

Verificar que `SqlConversationStore.Save()` sigue siendo consistente bajo el nuevo modelo.

### Paso 7 — Documentar

Actualizar `PROJECT.md` y `CHANGELOG.md`.

---

# 24. Arquitectura objetivo inmediata

```text
                         ChatController
                              ↓
                        IAgentService
                              ↓
                         AgentService
                              ↓
                        IMemoryService
                              ↓
                 ┌────────────────────────┐
                 │ sincronización por     │
                 │ ConversationId         │
                 └───────────┬────────────┘
                             ↓
                    IConversationStore
                             ↓
                   SqlConversationStore
                             ↓
                        SQL Server
```

---

# 25. Restricciones para el próximo chat

- No cambiar el proveedor LLM.
- Mantener Ollama + Llama 3.2.
- No agregar EF Core.
- Continuar utilizando `Microsoft.Data.SqlClient`.
- No introducir una arquitectura innecesariamente compleja.
- No agregar propiedades de persistencia al dominio sin una razón clara.
- No resolver concurrencia mediante un lock global que bloquee todas las conversaciones.
- No implementar concurrencia avanzada antes de reproducir y comprender el problema.
- Mantener `AgentService` como orquestador y evitar cargarlo con detalles de sincronización si pueden permanecer en `MemoryService`.
- Probar cada cambio antes de avanzar.
- Hacer preguntas breves de comprobación cuando aparezca un concepto importante.

---

# 26. Estructura actual del proyecto

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
│   │   ├── IConversationContextFactory.cs
│   │   ├── IConversationStore.cs
│   │   ├── InMemoryConversationStore.cs
│   │   └── SqlConversationStore.cs
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

---

# 27. Estado funcional actual

```text
✓ Recibir un ConversationId
✓ Recibir un mensaje
✓ Crear una conversación
✓ Recuperar una conversación existente
✓ Mantener historial conversacional
✓ Construir contexto para el LLM
✓ Consultar Ollama
✓ Guardar respuestas
✓ Persistir conversaciones en SQL Server
✓ Recuperar conversaciones después de reiniciar la API
✓ Mantener conversaciones independientes
✓ Agregar solamente mensajes nuevos durante Save()
```

Todavía no resuelve completamente:

```text
○ Concurrencia de múltiples requests sobre la misma conversación
○ Caché/memoria RAM como capa separada de persistencia
○ Tools
○ RAG
○ Documentos
○ Observabilidad avanzada
○ Escalamiento a múltiples instancias
```

---

# 28. Prompt de continuidad para el próximo chat

Estamos continuando el proyecto `LearningAgent.Api`.

Utiliza este `PROJECT.md` como documentación del estado actual y como contexto de continuidad.

El proyecto ya tiene:

- `AgentService`.
- `IAgentService`.
- `IChatService`.
- `OllamaService`.
- `ConversationContext`.
- `ConversationMessage`.
- `ConversationContextFactory`.
- `PromptBuilder`.
- `MemoryService`.
- `IConversationStore`.
- `InMemoryConversationStore`.
- `SqlConversationStore`.
- SQL Server con `Agent.Conversations` y `Agent.ConversationMessages`.

La persistencia SQL ya fue implementada y probada correctamente, incluyendo recuperación de conversaciones después de reiniciar la API.

El siguiente objetivo es resolver la **concurrencia por `ConversationId`**.

No comiences escribiendo código inmediatamente.

Primero analiza el estado actual y explica brevemente dónde está el problema de concurrencia.

Después diseña una prueba que permita reproducir el problema.

Luego propone el mecanismo de sincronización apropiado para operaciones asíncronas con `await`.

La sincronización debe ser por `ConversationId`, no un bloqueo global.

Mantén `AgentService` como orquestador y evita introducir detalles de sincronización en él si pueden permanecer en `MemoryService`.

No introducir EF Core ni cambiar `Microsoft.Data.SqlClient`.

No avanzar a Tools, RAG o documentos hasta que la concurrencia de conversaciones esté correctamente implementada y probada.

Mantener la metodología educativa:

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
```

Cuando aparezca un concepto importante, hacer una pregunta breve para comprobar mi comprensión antes de avanzar, siempre que no bloquee innecesariamente el progreso.
