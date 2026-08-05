# LearningAgent.Api

## Proyecto

LearningAgent.Api es un proyecto de aprendizaje cuyo objetivo es construir un agente de Inteligencia Artificial desde cero 
utilizando .NET.

El propósito no es únicamente consumir un Modelo de Lenguaje (LLM), sino comprender la arquitectura completa necesaria para 
desarrollar un agente capaz de:

- Mantener conversaciones.
- Recordar contexto.
- Utilizar herramientas (Tools).
- Consultar bases de datos.
- Leer documentos.
- Implementar RAG (Retrieval-Augmented Generation).
- Integrarse con diferentes proveedores de IA.

El proyecto se desarrollará por fases para comprender cada componente antes de avanzar al siguiente.

---

# Objetivos

## Objetivo general

Construir un agente de IA modular, desacoplado y escalable utilizando ASP.NET Core.

## Objetivos específicos

- Aprender cómo consumir un LLM.
- Diseñar una API REST.
- Aplicar principios SOLID.
- Utilizar Dependency Injection.
- Comprender la arquitectura de un agente moderno.
- Preparar el proyecto para integrar memoria, herramientas y RAG.

---

# Tecnologías

| Tecnología        | Uso                     |
|-------------------|-------------------------|
| ASP.NET Core 8    | API REST                |
| C#                | Lenguaje principal      |
| Ollama            | Ejecución local del LLM |
| Llama 3.2         | Modelo de lenguaje      |
| Swagger           | Pruebas de la API       |
| HttpClientFactory | Consumo de APIs         |
| System.Text.Json  | Serialización JSON      |

---

# Arquitectura

Actualmente la arquitectura sigue el siguiente flujo:

```
Cliente
    │
    ▼
ChatController
    │
    ▼
IChatService
    │
    ▼
OllamaService
    │
    ▼
HttpClient
    │
    ▼
Ollama
    │
    ▼
Llama 3.2
```

---

# Arquitectura del proyecto

```
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
├── Options
│   ├── OllamaOptions.cs
│   └── OpenAIOptions.cs
│
├── Services
│   ├── IChatService.cs
│   ├── OllamaService.cs
│   └── OpenAIService.cs
│
├── Program.cs
│
└── appsettings.json
```

---

# Responsabilidad de cada carpeta

## Controllers

Expone los endpoints HTTP de la API.

Actualmente contiene:

- ChatController

Responsabilidad:

- Recibir peticiones.
- Validar DTOs.
- Invocar la lógica de negocio.
- Regresar respuestas HTTP.

---

## Services

Contiene la lógica de negocio.

Actualmente:

- IChatService
- OllamaService
- OpenAIService (referencia)

Responsabilidades:

- Comunicarse con proveedores de IA.
- Implementar la lógica de conversación.
- Ocultar detalles de implementación al controlador.

---

## Contracts

Modela el contrato JSON utilizado por APIs externas.

Actualmente:

- OllamaChatRequest
- OllamaChatResponse
- OllamaMessage

Estos objetos representan exactamente el JSON esperado por Ollama.

No pertenecen al dominio de la aplicación.

---

## DTOs

Representan los datos intercambiados con los clientes de nuestra API.

Actualmente:

- ChatRequest
- ChatResponse

Estos DTOs son independientes de Ollama.

---

## Options

Representa la configuración obtenida desde appsettings.json.

Actualmente:

- OllamaOptions
- OpenAIOptions

Se utiliza el patrón Options de ASP.NET Core.

---

# Flujo de una petición

1. El cliente realiza un POST a:

```
POST /api/chat
```

2. ChatController recibe el ChatRequest.

3. ChatController invoca:

```
IChatService.GetResponseAsync()
```

4. La implementación registrada es:

```
OllamaService
```

5. OllamaService:

- Construye un OllamaChatRequest.
- Serializa el objeto a JSON.
- Envía un POST hacia Ollama.
- Recibe la respuesta.
- Deserializa el JSON.
- Devuelve únicamente el contenido del mensaje.

6. ChatController construye un ChatResponse.

7. La respuesta se devuelve al cliente.

---

# Principios utilizados

Actualmente el proyecto aplica los siguientes principios:

## Dependency Injection

El controlador no conoce la implementación concreta del servicio.

Depende únicamente de:

```
IChatService
```

Esto permite reemplazar Ollama por cualquier otro proveedor.

---

## Separación de responsabilidades

Cada componente tiene una única responsabilidad.

Controller

↓

Service

↓

Proveedor de IA

---

## Programación contra interfaces

Se utiliza:

```
IChatService
```

En lugar de depender directamente de:

```
OllamaService
```

Esto facilita:

- pruebas
- mantenimiento
- escalabilidad

---

## HttpClientFactory

El proyecto utiliza IHttpClientFactory para administrar HttpClient.

Beneficios:

- reutilización de conexiones
- mejor rendimiento
- configuración centralizada

---

## Options Pattern

Toda la configuración se obtiene mediante:

```
IOptions<T>
```

Esto evita valores hardcodeados.

---

# Estado actual

## Fase 1 — Consumir un LLM

Estado:

✔ Completada

Características implementadas:

- API REST.
- Swagger.
- Integración con Ollama.
- Comunicación con Llama 3.2.
- Dependency Injection.
- Options Pattern.
- HttpClientFactory.
- Arquitectura desacoplada.
- Contratos JSON separados.
- DTOs independientes.

Actualmente el proyecto ya puede responder preguntas desde un LLM ejecutándose localmente.

---

# Próximas fases

## Fase 2

System Prompt

Objetivo:

Controlar el comportamiento del modelo mediante instrucciones del sistema.

---

## Fase 3

Historial de conversación

Objetivo:

Enviar múltiples mensajes al modelo para mantener contexto.

---

## Fase 4

Memoria

Objetivo:

Recordar información del usuario entre conversaciones.

---

## Fase 5

Herramientas (Tools)

Objetivo:

Permitir que el modelo invoque funciones de C#.

Ejemplos:

- Obtener fecha.
- Consultar SQL Server.
- Leer archivos.
- Ejecutar cálculos.

---

## Fase 6

Agente

Objetivo:

Construir un sistema capaz de decidir cuándo utilizar herramientas y cuándo responder directamente.

---

## Fase 7

RAG

Objetivo:

Responder utilizando documentos propios.

Tecnologías previstas:

- Embeddings
- Base de datos vectorial
- Recuperación semántica

---

## Fase 8

Integraciones

Objetivo:

Agregar acceso a:

- SQL Server
- Archivos PDF
- Word
- Excel
- APIs externas

---

# Visión final

La arquitectura objetivo será:

```
Usuario
    │
    ▼
API
    │
    ▼
Agente
    │
    ├── Memoria
    ├── Tools
    ├── SQL Server
    ├── PDFs
    ├── APIs
    ├── RAG
    └── LLM
            │
            ▼
        Ollama
```

El LLM será únicamente uno de los componentes del agente, no el agente completo.

---

# Notas

Este proyecto tiene un enfoque completamente educativo.

Cada fase busca comprender los conceptos y decisiones de diseño antes de incorporar nuevas funcionalidades.

El objetivo final no es únicamente obtener un chatbot funcional, sino construir una arquitectura profesional para aplicaciones de 
Inteligencia Artificial utilizando .NET.