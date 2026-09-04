# PROJECT.md


# LearningAgent.Api

> Documento de contexto, documentación técnica y prompt de continuidad para futuras sesiones de desarrollo.

---

# 1. Descripción del proyecto

`LearningAgent.Api` es un proyecto educativo desarrollado para aprender cómo construir un agente conversacional utilizando:

- C#
- .NET 8
- ASP.NET Core Web API
- Ollama
- Llama 3.2
- SQL Server / LocalDB
- Inyección de dependencias
- Manejo de memoria conversacional
- Persistencia de conversaciones
- Control de concurrencia

El objetivo no es únicamente crear un chatbot, sino entender progresivamente la arquitectura interna de un agente de IA.

La aplicación recibe mensajes mediante una API HTTP, construye un contexto conversacional, lo envía a un modelo de lenguaje y devuelve la respuesta generada.

---

# 2. Objetivo principal

Construir un agente educativo especializado en:

- Desarrollo de software
- C#
- .NET
- APIs
- Arquitectura de software
- Inteligencia Artificial

El agente debe poder:

1. Recibir mensajes.
2. Mantener conversaciones independientes.
3. Recordar mensajes anteriores.
4. Evitar mezclar conversaciones.
5. Persistir conversaciones.
6. Manejar solicitudes concurrentes.
7. Comunicarse con un modelo LLM.
8. Evolucionar posteriormente hacia herramientas, RAG y comportamiento más autónomo.

---

# 3. Stack tecnológico

## Backend

- C#
- .NET 8
- ASP.NET Core Web API

## Base de datos

- SQL Server LocalDB

Cadena de conexión actual:

```json
"ConnectionStrings": {
  "LearningAgentDb": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=LearningAgentDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=LearningAgent.Api"
}
````

## Modelo de IA

Actualmente:

* Ollama
* Modelo: `llama3.2`

Configuración:

```json
"Ollama": {
  "BaseUrl": "http://127.0.0.1:11434",
  "Model": "llama3.2"
}
```

También existe configuración previa para OpenAI, pero actualmente no se está utilizando debido a problemas de cuota.

---

# 4. Estado general del proyecto

## Avance estimado: 45%

El porcentaje es aproximado.

### Completado

* [x] API básica
* [x] Endpoint de chat
* [x] Comunicación con un LLM
* [x] Integración inicial con OpenAI
* [x] Cambio a Ollama local
* [x] Prompt del sistema
* [x] Contexto conversacional
* [x] Memoria en RAM
* [x] Aislamiento entre conversaciones
* [x] Persistencia básica en SQL Server
* [x] Sincronización de solicitudes por conversación
* [x] Pruebas de concurrencia
* [x] Diagnóstico de timeout del cliente `.http`
* [x] Pruebas de rendimiento contra Ollama
* [x] Medición del tiempo de respuesta

### Pendiente

* [ ] Restaurar `OllamaService` después de las pruebas temporales
* [ ] Optimizar el historial conversacional
* [ ] Definir una estrategia de límite de contexto
* [ ] Mejorar el manejo de errores y timeouts
* [ ] Persistencia completa y recuperación de conversaciones
* [ ] Resumen automático de conversaciones largas
* [ ] Gestión de contexto por tokens
* [ ] Herramientas para el agente
* [ ] RAG
* [ ] Embeddings
* [ ] Base vectorial
* [ ] Streaming de respuestas
* [ ] Pruebas unitarias
* [ ] Pruebas de integración
* [ ] Pruebas de carga
* [ ] Observabilidad y métricas

---

# 5. Arquitectura actual

La aplicación sigue una separación por responsabilidades:

```text
Cliente
   │
   │ HTTP POST
   ▼
ChatController
   │
   ▼
IAgentService
   │
   ▼
AgentService
   │
   ├── IMemoryService
   │
   ├── IPromptBuilder
   │
   └── IChatService
           │
           ▼
       OllamaService
           │
           ▼
     Ollama API Local
           │
           ▼
       llama3.2
```

Para la persistencia:

```text
AgentService
      │
      ▼
IMemoryService
      │
      ▼
IConversationStore
      │
      ▼
SQL Server / LocalDB
```

---

# 6. Estructura conceptual del proyecto

Las partes principales incluyen:

```text
LearningAgent.Api
│
├── Controllers
│   └── ChatController.cs
│
├── Contracts
│   └── Ollama
│
├── Dtos
│   ├── ChatRequest.cs
│   └── ChatResponse.cs
│
├── Models
│   ├── Chat
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
│   │   ├── IConversationStore.cs
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
├── appsettings.json
├── launchSettings.json
├── LearningAgent.Api.http
└── Program.cs
```

---

# 7. Endpoint principal

## POST

```text
/api/Chat
```

El controlador actual recibe un `ChatRequest`.

Conceptualmente:

```csharp
[HttpPost]
public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request)
{
    string response = await _agentService.ProcessAsync(
        request.ConversationId,
        request.Message);

    return Ok(new ChatResponse
    {
        Response = response
    });
}
```

---

# 8. Flujo de procesamiento de un mensaje

Cuando llega una solicitud:

```text
POST /api/Chat
```

ocurre el siguiente flujo:

```text
1. ChatController recibe la solicitud
        │
        ▼
2. AgentService.ProcessAsync()
        │
        ▼
3. MemoryService obtiene el lock de la conversación
        │
        ▼
4. Se obtiene o crea ConversationContext
        │
        ▼
5. Se agrega el mensaje del usuario
        │
        ▼
6. PromptBuilder construye el contexto
        │
        ▼
7. OllamaService llama a Ollama
        │
        ▼
8. Ollama / llama3.2 genera la respuesta
        │
        ▼
9. Se agrega la respuesta del asistente
        │
        ▼
10. Se guarda el contexto
        │
        ▼
11. Se libera el lock
        │
        ▼
12. Se devuelve la respuesta HTTP
```

---

# 9. AgentService

El servicio principal del agente es responsable de coordinar:

* Memoria
* Contexto
* Prompt
* LLM
* Respuesta

La lógica conceptual actual es:

```csharp
public async Task<string> ProcessAsync(
    Guid conversationId,
    string message)
{
    return await _memoryService.ExecuteAsync(
        conversationId,
        async () =>
        {
            var context =
                _memoryService.GetOrCreate(conversationId);

            context.Messages.Add(
                new ConversationMessage
                {
                    Role = "user",
                    Content = message
                });

            var messages =
                _promptBuilder.Build(context);

            var response =
                await _chatService.GetResponseAsync(messages);

            context.Messages.Add(
                new ConversationMessage
                {
                    Role = "assistant",
                    Content = response
                });

            _memoryService.Save(context);

            return response;
        });
}
```

---

# 10. Memoria conversacional

La memoria se identifica mediante:

```text
ConversationId
```

Cada conversación tiene su propio contexto.

Ejemplo:

```text
Conversation A
ID: 1111
Usuario: Me llamo Juan
```

Después:

```text
Conversation A
Usuario: ¿Cómo me llamo?
```

El agente puede recordar:

```text
Juan
```

Pero una conversación diferente:

```text
Conversation B
ID: 2222
Usuario: Me llamo Pedro
```

mantiene un contexto independiente.

---

# 11. Pruebas realizadas sobre memoria

## Prueba A — Persistencia dentro de una conversación

Primera solicitud:

```text
Me llamo Juan
```

Segunda solicitud con el mismo `ConversationId`:

```text
¿Cómo me llamo?
```

Resultado:

```text
Juan
```

Resultado esperado confirmado.

---

## Prueba B — Aislamiento entre conversaciones

Conversación A:

```text
Me llamo Juan
```

Conversación B:

```text
Me llamo Pedro
```

Cada conversación mantuvo su propio contexto.

No se mezclaron los mensajes.

Resultado esperado confirmado.

---

# 12. Concurrencia

## Problema

Dos solicitudes HTTP pueden llegar al mismo tiempo.

Si ambas pertenecen a la misma conversación:

```text
ConversationId = AAA
```

podrían modificar simultáneamente:

```text
context.Messages
```

Esto podría provocar condiciones de carrera.

Ejemplo:

```text
Request 1
    │
    ├── Obtiene contexto
    │
    ├── Agrega mensaje
    │
    └── Espera Ollama


Request 2
    │
    ├── Obtiene el mismo contexto
    │
    ├── Agrega mensaje
    │
    └── Modifica el estado antes de que termine Request 1
```

---

# 13. Solución de concurrencia

`MemoryService` utiliza un mecanismo de bloqueo por conversación.

Conceptualmente:

```text
ConcurrentDictionary<Guid, SemaphoreSlim>
```

Cada `ConversationId` tiene su propio `SemaphoreSlim`.

Esto permite:

```text
Conversation A
     │
     └── Lock A
```

y:

```text
Conversation B
     │
     └── Lock B
```

Por lo tanto, dos conversaciones diferentes no tienen que bloquearse mutuamente.

---

# 14. Comportamiento esperado del lock

## Mismo ConversationId

```text
Request 1
ConversationId = AAA
        │
        ▼
Acquired lock
        │
        ▼
Procesando
        │
        ▼
Ollama
        │
        ▼
Released lock
```

Mientras tanto:

```text
Request 2
ConversationId = AAA
        │
        ▼
Waiting for lock
        │
        ▼
        espera
        │
        ▼
Acquired lock
```

Resultado:

Las solicitudes de la misma conversación se procesan secuencialmente.

---

## Diferente ConversationId

```text
Request 1
ConversationId = AAA
        │
        ▼
Lock A
```

y simultáneamente:

```text
Request 2
ConversationId = BBB
        │
        ▼
Lock B
```

Ambas pueden avanzar independientemente.

---

# 15. Prueba de concurrencia realizada

Se realizaron solicitudes desde:

1. Archivo `LearningAgent.Api.http`
2. Swagger

Se utilizó el mismo:

```text
ConversationId
```

El log mostró:

```text
[519f...] Waiting for lock
[519f...] Acquired lock

[519f...] Waiting for lock
```

La segunda solicitud llegó mientras la primera todavía estaba ejecutándose.

Después:

```text
[519f...] Released lock
```

Entonces:

```text
[519f...] Acquired lock
```

Esto confirmó que:

> El mecanismo de concurrencia por conversación está funcionando correctamente.

---

# 16. LearningAgent.Api.http

Configuración utilizada:

```http
@LearningAgent.Api_HostAddress = http://localhost:5072

### Conversation
POST {{LearningAgent.Api_HostAddress}}/api/Chat
Content-Type: application/json

{
  "conversationId": "GUID-AQUI",
  "message": "Hola"
}

###
```

La API escucha actualmente en:

```text
https://localhost:7125
http://localhost:5072
```

---

# 17. Problema de timeout del archivo .http

Inicialmente el archivo `.http` mostraba:

```text
The request was cancelled due to the configured timeout of 20 second(s) elapsing.
```

Se investigaron inicialmente:

* Puertos
* `launchSettings.json`
* `appsettings.json`
* HTTPS
* Swagger
* Ollama
* `HttpClient`
* Locks

Finalmente se identificó que Visual Studio tenía configurado un timeout de:

```text
20 segundos
```

El problema era que algunas respuestas de Ollama tardaban más que ese tiempo.

---

# 18. Solución del timeout en Visual Studio 2026

La configuración correcta se encuentra en:

```text
Herramientas
└── Opciones
    └── Editor de texto / Idiomas
        └── REST
            └── Avanzado
                └── Respuesta
                    └── Tiempo de espera de solicitud
```

Se modificó temporalmente a:

```text
120 segundos
```

Después de este cambio, solicitudes que tardaban aproximadamente 100 segundos pudieron completarse correctamente.

---

# 19. Diagnóstico de Ollama

Se realizaron pruebas directas desde PowerShell.

Consulta de modelos:

```powershell
curl.exe http://localhost:11434/api/tags
```

Resultado:

```text
llama3.2:latest
```

También se realizó una petición directa:

```powershell
Invoke-RestMethod `
    -Uri "http://localhost:11434/api/chat" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

Ollama respondió correctamente.

Esto confirmó que:

```text
Ollama API local: funcionando
Modelo llama3.2: disponible
Puerto 11434: funcionando
```

---

# 20. Prueba controlada de OllamaService

Para eliminar temporalmente la influencia de:

* Historial
* Memoria
* System Prompt
* PromptBuilder

se modificó temporalmente `OllamaService`.

En lugar de utilizar los mensajes reales, se envió siempre:

```json
{
  "model": "llama3.2",
  "stream": false,
  "messages": [
    {
      "role": "user",
      "content": "Hola, responde brevemente"
    }
  ]
}
```

Esto permitió medir directamente el comportamiento de Ollama.

---

# 21. Medición de rendimiento

Se agregó temporalmente:

```csharp
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

HttpResponseMessage response =
    await _httpClient.PostAsync("/api/chat", content);

stopwatch.Stop();

Console.WriteLine(
    $"Tiempo HTTP: {stopwatch.ElapsedMilliseconds} ms");
```

Esto permitió comparar:

```text
Tiempo medido por .NET
```

contra:

```text
total_duration
load_duration
prompt_eval_duration
eval_duration
```

devueltos por Ollama.

---

# 22. Resultado de la primera solicitud

La primera solicitud tardó aproximadamente:

```text
Tiempo HTTP: ~11.2 segundos
```

Ollama reportó aproximadamente:

```text
total_duration:       ~11.0 segundos
load_duration:         ~8.4 segundos
prompt_eval_duration:  ~1.35 segundos
eval_duration:         ~1.29 segundos
```

Conclusión:

La mayor parte del tiempo correspondió a:

```text
load_duration
```

Es decir, la preparación/carga del modelo.

---

# 23. Resultado de la segunda solicitud

La segunda solicitud se realizó prácticamente inmediatamente después.

Tiempo aproximado:

```text
Tiempo HTTP: ~1.8 segundos
```

Ollama reportó aproximadamente:

```text
total_duration: ~1.7 segundos
load_duration:  ~0.49 segundos
```

Conclusión:

El modelo ya estaba disponible/preparado y la segunda solicitud fue significativamente más rápida.

---

# 24. Conclusión sobre el rendimiento

Se confirmó que el tiempo de respuesta puede variar considerablemente dependiendo del estado del modelo.

Conceptualmente:

```text
Primera solicitud
       │
       ▼
Modelo necesita cargarse/prepararse
       │
       ▼
Respuesta más lenta
```

Después:

```text
Segunda solicitud
       │
       ▼
Modelo ya disponible
       │
       ▼
Respuesta más rápida
```

Esto explica parte de la diferencia observada entre solicitudes.

---

# 25. Conclusiones descartadas

Las pruebas realizadas permiten concluir que los siguientes elementos no eran la causa principal del timeout inicial:

* Puerto `5072`
* Puerto `7125`
* Swagger
* HTTPS
* `launchSettings.json`
* `appsettings.json`
* Endpoint `/api/Chat`
* Comunicación básica de `HttpClient`
* API local de Ollama
* `MemoryService`
* `SemaphoreSlim`

El timeout inicial del archivo `.http` estaba relacionado con el límite de espera de 20 segundos configurado en Visual Studio.

---

# 26. Estado actual temporal de OllamaService

IMPORTANTE:

`OllamaService` fue modificado temporalmente para realizar pruebas.

Actualmente ignora el parámetro:

```csharp
IEnumerable<ConversationMessage> messages
```

y siempre envía un mensaje fijo:

```text
Hola, responde brevemente
```

Esto significa que temporalmente el agente NO está utilizando:

* El mensaje real enviado por el usuario
* El System Prompt
* El historial conversacional

La siguiente etapa debe comenzar restaurando el comportamiento real.

---

# 27. Código esperado después de restaurar OllamaService

La implementación debe volver a utilizar los mensajes recibidos:

```csharp
public async Task<string> GetResponseAsync(
    IEnumerable<ConversationMessage> messages)
{
    var request = new OllamaChatRequest
    {
        Model = _options.Model,
        Stream = false,
        Messages = messages
            .Select(m => new OllamaMessage
            {
                Role = m.Role,
                Content = m.Content
            })
            .ToList()
    };

    string json = JsonSerializer.Serialize(request);

    using var content = new StringContent(
        json,
        Encoding.UTF8,
        "application/json");

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    HttpResponseMessage response =
        await _httpClient.PostAsync("/api/chat", content);

    stopwatch.Stop();

    Console.WriteLine(
        $"Tiempo HTTP: {stopwatch.ElapsedMilliseconds} ms");

    response.EnsureSuccessStatusCode();

    string responseJson =
        await response.Content.ReadAsStringAsync();

    var ollamaResponse =
        JsonSerializer.Deserialize<OllamaChatResponse>(
            responseJson,
            JsonOptions);

    return ollamaResponse?.Message.Content
        ?? "No se recibió respuesta.";
}
```

El `Stopwatch` puede mantenerse temporalmente para continuar analizando el rendimiento.

---

# 28. Problema pendiente: crecimiento del contexto

Aunque no fue la causa del timeout del archivo `.http`, existe un problema potencial.

Actualmente la conversación puede crecer así:

```text
System Prompt

Usuario 1
Asistente 1

Usuario 2
Asistente 2

Usuario 3
Asistente 3

Usuario 4
Asistente 4

...
```

Si cada respuesta del asistente es larga, cada nueva solicitud enviará más información a Ollama.

Con el tiempo:

```text
Más mensajes
      +
Más tokens
      +
Más procesamiento
      +
Mayor uso de memoria
      +
Mayor latencia
```

Esto debe solucionarse en una etapa posterior.

---

# 29. Posibles estrategias para limitar el contexto

## Opción A — Últimos N mensajes

Ejemplo:

```text
System Prompt
+
Últimos 10 mensajes
```

Ventaja:

* Fácil de implementar
* Predecible
* Reduce rápidamente el tamaño del contexto

Desventaja:

* Se pierde información antigua

---

## Opción B — Últimos N pares de conversación

Ejemplo:

```text
System Prompt

Usuario 8
Asistente 8

Usuario 9
Asistente 9

Usuario 10
Asistente 10
```

Ventaja:

* Mantiene interacciones completas

Desventaja:

* Sigue existiendo pérdida de contexto antiguo

---

## Opción C — Resumen de conversaciones antiguas

Ejemplo:

```text
System Prompt

Resumen de la conversación anterior:
El usuario está desarrollando una API en .NET.
Está trabajando con Ollama.
Está implementando memoria y concurrencia.

Mensajes recientes:
...
```

Ventaja:

* Conserva información importante
* Reduce tokens

Desventaja:

* Requiere lógica adicional
* El resumen también debe administrarse

Esta estrategia probablemente será más adecuada para una fase posterior.

---

# 30. Configuración actual de dependencias

Configuración conceptual actual:

```csharp
builder.Services.AddHttpClient();

builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<IChatService, OllamaService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();

builder.Services.AddSingleton<ISystemPromptProvider, SystemPromptProvider>();
builder.Services.AddSingleton<IConversationContextFactory, ConversationContextFactory>();
builder.Services.AddSingleton<IMemoryService, MemoryService>();
builder.Services.AddSingleton<IConversationStore, SqlConversationStore>();
```

---

# 31. Decisiones sobre ciclos de vida

## Singleton

Se utilizan como Singleton componentes que deben mantenerse durante la vida de la aplicación:

```text
ISystemPromptProvider
IConversationContextFactory
IMemoryService
IConversationStore
```

En particular:

```text
IMemoryService
```

debe mantener estado entre diferentes solicitudes HTTP.

Si fuera Scoped:

```text
Request 1
    └── MemoryService A

Request 2
    └── MemoryService B
```

no existiría memoria compartida entre solicitudes.

Con Singleton:

```text
Request 1 ──┐
Request 2 ──┼── MemoryService único
Request 3 ──┘
```

---

# 32. Estado de persistencia

El proyecto ya cuenta con:

```text
IConversationStore
SqlConversationStore
```

La persistencia en SQL Server forma parte de la arquitectura actual.

Debe revisarse en futuras etapas:

* Qué se persiste exactamente
* Cuándo se carga una conversación
* Cuándo se guarda
* Cómo se reconstruye el contexto
* Cómo se eliminan conversaciones antiguas
* Cómo se evita que el historial persistido crezca indefinidamente

---

# 33. Próxima etapa inmediata

## ETAPA 1 — Restaurar OllamaService

Restaurar:

```text
Mensajes reales
```

en lugar de:

```text
Mensaje fijo de prueba
```

Mantener temporalmente:

```text
Stopwatch
```

y posiblemente registrar:

```text
Cantidad de mensajes
Tamaño del JSON
Tiempo total
```

---

# 34. ETAPA 2 — Medir el impacto del contexto

Realizar tres pruebas.

## Prueba A

```text
Usuario solamente
```

Ejemplo:

```text
Hola, responde brevemente
```

Objetivo:

Establecer la latencia base.

---

## Prueba B

```text
System Prompt
+
Usuario
```

Objetivo:

Medir el costo del System Prompt.

---

## Prueba C

```text
System Prompt
+
Historial
+
Usuario
```

Objetivo:

Medir el impacto real del crecimiento de la conversación.

---

# 35. ETAPA 3 — Optimizar el contexto

Una vez obtenidas las mediciones, implementar inicialmente una estrategia simple.

Recomendación inicial:

```text
System Prompt
+
Últimos N mensajes
```

Más adelante evolucionar hacia:

```text
System Prompt
+
Resumen histórico
+
Mensajes recientes
```

---

# 36. ETAPA 4 — Mejorar manejo de errores

Implementar manejo explícito para:

```text
Ollama no disponible
Modelo no encontrado
Timeout
HTTP no exitoso
JSON inválido
Cancelación
```

Ejemplo conceptual:

```csharp
try
{
    // llamada a Ollama
}
catch (TaskCanceledException)
{
    // timeout o cancelación
}
catch (HttpRequestException)
{
    // error HTTP
}
```

---

# 37. ETAPA 5 — Timeout configurable

Actualmente el `HttpClient` utiliza el comportamiento/configuración existente.

Debe considerarse mover el timeout a configuración:

```json
"Ollama": {
  "BaseUrl": "http://127.0.0.1:11434",
  "Model": "llama3.2",
  "TimeoutSeconds": 120
}
```

Después configurar el cliente de forma explícita.

El timeout del cliente API y el timeout de Ollama deben considerarse como conceptos diferentes.

```text
Cliente .http
      │
      │ Timeout del cliente
      ▼
LearningAgent.Api
      │
      │ Timeout de HttpClient
      ▼
Ollama
```

---

# 38. ETAPA 6 — Pruebas de concurrencia avanzadas

Realizar pruebas con:

## Caso A

```text
Mismo ConversationId
2 solicitudes simultáneas
```

Resultado esperado:

```text
Secuenciales
```

---

## Caso B

```text
ConversationId A
ConversationId B
```

Resultado esperado:

```text
Procesamiento independiente
```

---

## Caso C

```text
5 o más solicitudes
```

Objetivo:

Analizar:

* Locks
* Orden
* Rendimiento
* Posibles condiciones de carrera

---

# 39. ETAPA 7 — Streaming

Actualmente:

```text
Stream = false
```

La API espera a que Ollama termine completamente.

En una etapa futura se puede implementar:

```text
Stream = true
```

para enviar tokens progresivamente.

Flujo futuro:

```text
Ollama
  │
  ├── token 1
  ├── token 2
  ├── token 3
  └── token N
       │
       ▼
LearningAgent.Api
       │
       ▼
Cliente
```

Esto puede mejorar significativamente la percepción de velocidad.

---

# 40. ETAPA 8 — Herramientas del agente

El agente podrá evolucionar para utilizar herramientas.

Ejemplos:

```text
Agente
   │
   ├── Consultar API
   ├── Consultar base de datos
   ├── Leer documentos
   ├── Buscar información
   └── Ejecutar acciones controladas
```

---

# 41. ETAPA 9 — RAG

Posteriormente implementar:

```text
Documentos
    │
    ▼
Embeddings
    │
    ▼
Base vectorial
    │
    ▼
Búsqueda semántica
    │
    ▼
Contexto relevante
    │
    ▼
LLM
```

El objetivo será que el agente pueda responder utilizando información externa específica.

---

# 42. ETAPA 10 — Pruebas

Pendiente implementar:

## Pruebas unitarias

Para:

```text
AgentService
MemoryService
PromptBuilder
OllamaService
ConversationStore
```

## Pruebas de integración

Para:

```text
POST /api/Chat
Persistencia
Memoria
Ollama
```

## Pruebas de carga

Por ejemplo con:

```text
JMeter
```

Objetivo:

* Solicitudes simultáneas
* Conversaciones múltiples
* Misma conversación
* Latencia
* Errores
* Uso de recursos

---

# 43. Estado actual antes de continuar

El proyecto se encuentra en este punto:

```text
Cliente
   │
   ▼
ChatController
   │
   ▼
AgentService
   │
   ├── MemoryService
   │       │
   │       ├── Contexto en memoria
   │       ├── Lock por ConversationId
   │       └── Persistencia
   │
   ├── PromptBuilder
   │
   ▼
OllamaService
   │
   ▼
Ollama
   │
   ▼
llama3.2
```

La arquitectura básica funciona.

La memoria funciona.

El aislamiento entre conversaciones funciona.

La sincronización por conversación funciona.

El archivo `.http` funciona después de aumentar su timeout.

Ollama funciona.

Se confirmó que la carga del modelo puede afectar considerablemente el tiempo de respuesta.

---

# 44. Instrucciones para el próximo chat

El siguiente chat debe tomar este documento como contexto del proyecto.

NO reiniciar el proyecto desde cero.

El punto exacto de continuación es:

> Restaurar `OllamaService` para que vuelva a utilizar los mensajes reales recibidos desde `AgentService`, manteniendo temporalmente la medición con `Stopwatch`.

Después:

1. Medir latencia base.
2. Medir System Prompt.
3. Medir historial.
4. Comparar resultados.
5. Implementar una estrategia inicial para limitar el contexto.
6. Mejorar manejo de errores y timeout.
7. Continuar con las siguientes etapas del agente.

---

# 45. Prompt de continuidad para un nuevo chat

```text
Estoy desarrollando LearningAgent.Api como un proyecto educativo para aprender a construir un agente de IA con C#, .NET 8, ASP.NET Core Web API y Ollama.

Lee PROJECT.md como fuente principal de contexto antes de proponer cambios.

No reinicies el proyecto ni propongas una arquitectura completamente diferente sin analizar primero la arquitectura existente.

El proyecto ya tiene:

- ChatController
- AgentService
- MemoryService
- PromptBuilder
- SystemPromptProvider
- ConversationContextFactory
- OllamaService
- SqlConversationStore
- memoria conversacional
- persistencia
- control de concurrencia por ConversationId

El modelo actual es llama3.2 ejecutándose localmente mediante Ollama.

El flujo principal es:

ChatController
→ AgentService
→ MemoryService
→ PromptBuilder
→ IChatService / OllamaService
→ Ollama

MemoryService utiliza un SemaphoreSlim por ConversationId para evitar condiciones de carrera.

Ya se probó que:

- La memoria funciona.
- Las conversaciones no se mezclan.
- Dos solicitudes con el mismo ConversationId se serializan correctamente.
- Swagger y LearningAgent.Api.http pueden utilizarse para realizar pruebas.
- El timeout original del archivo .http era de 20 segundos y se cambió a 120 segundos en Visual Studio 2026.
- Ollama funciona correctamente en localhost:11434.
- El tiempo de respuesta puede variar considerablemente por load_duration del modelo.
- Una primera solicitud puede ser lenta por carga/preparación del modelo y una segunda inmediata puede ser mucho más rápida.

IMPORTANTE:
Actualmente OllamaService está modificado temporalmente para enviar un mensaje fijo:

"Hola, responde brevemente"

Ignora temporalmente los mensajes reales recibidos en el parámetro GetResponseAsync.

La siguiente tarea inmediata es restaurar OllamaService para utilizar los mensajes reales construidos por PromptBuilder, manteniendo Stopwatch y logs de rendimiento temporalmente.

Después debemos medir:

A. Solo mensaje del usuario.
B. System Prompt + usuario.
C. System Prompt + historial + usuario.

El objetivo es medir el impacto real del crecimiento del contexto antes de implementar una estrategia de limitación de historial.

Explica los cambios paso a paso y prioriza el aprendizaje. No cambies varias partes de la arquitectura simultáneamente sin justificarlo.
```

---

**Fin de PROJECT.md**

```

El siguiente paso sería guardar este contenido reemplazando el `PROJECT.md` actual y abrir el nuevo chat utilizando ese archivo como contexto. 
```
