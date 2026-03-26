# Módulo de IA — Sistema de Gestión de Proyectos

**Versión:** 1.0.0  
**Fecha:** Marzo 2026  
**Stack:** ASP.NET Core 8 · PostgreSQL · JWT  
**Estrategia LLM:** Fallback en cascada (Groq → Cerebras → Gemini → OpenRouter)

---

## Tabla de contenidos

1. [Qué hace el módulo](#1-qué-hace-el-módulo)
2. [Arquitectura](#2-arquitectura)
3. [Estructura de archivos](#3-estructura-de-archivos)
4. [Flujos principales](#4-flujos-principales)
5. [Proveedores LLM y fallback](#5-proveedores-llm-y-fallback)
6. [Referencia de archivos](#6-referencia-de-archivos)
7. [API endpoints](#7-api-endpoints)
8. [Configuración](#8-configuración)
9. [Variables de entorno](#9-variables-de-entorno)
10. [Rate limiting](#10-rate-limiting)
11. [Logging y observabilidad](#11-logging-y-observabilidad)
12. [Pruebas manuales](#12-pruebas-manuales)
13. [Checklist de deploy](#13-checklist-de-deploy)

---

## 1. Qué hace el módulo

El módulo de IA permite a cualquier usuario autenticado **generar un proyecto completo con tareas a partir de una descripción en lenguaje natural**. El flujo tiene tres pasos:

1. El usuario describe lo que quiere construir en texto libre.
2. La IA genera una sugerencia estructurada: nombre del proyecto, descripción, estado y lista de tareas ordenadas por dependencia lógica.
3. El usuario revisa, edita si quiere, y confirma. Solo entonces se persiste en la base de datos.

Adicionalmente, el módulo puede **sugerir tareas faltantes para un proyecto existente**, analizando las tareas actuales e identificando huecos lógicos sin duplicar las existentes.

El módulo utiliza exclusivamente proveedores LLM con **capa gratuita**, con una estrategia de fallback automático en cascada. Si el proveedor principal está saturado o caído, el sistema pasa al siguiente sin que el usuario lo note. Costo operativo: **$0**.

---

## 2. Arquitectura

```
Angular (Frontend)
        │
        ▼
┌─────────────────────────────────────────┐
│         AIController  /api/ai/*         │
│  · Validación de entrada                │
│  · Manejo de errores HTTP               │
│  · Rate limiting (middleware)           │
├─────────────────────────────────────────┤
│              AIService                  │
│  · Construcción de prompts              │
│  · Sanitización y parseo de JSON        │
│  · Delegación a servicios existentes    │
│    (ProjectService + TaskService)       │
├─────────────────────────────────────────┤
│         FallbackLLMService              │
│  · Groq       (prioridad 1)             │
│  · Cerebras   (prioridad 2)             │
│  · Gemini     (prioridad 3)             │
│  · OpenRouter (prioridad 4)             │
├─────────────────────────────────────────┤
│    Servicios existentes (sin cambios)   │
│    ProjectService · TaskService         │
│    ProjectAuthorizationService          │
└─────────────────────────────────────────┘
        │
        ▼
   PostgreSQL (sin cambios de esquema)
```

### Principios de diseño

- **El módulo no modifica ningún servicio existente.** `ProjectService` y `TaskService` se reutilizan sin cambios.
- **Separación de responsabilidades estricta.** `FallbackLLMService` solo conoce de HTTP. `AIService` solo conoce de dominio. `AIController` solo maneja HTTP.
- **Generación y persistencia son dos pasos distintos.** `generate-project` nunca escribe en base de datos. Solo `confirm-project` lo hace.
- **Los providers son intercambiables.** Todos implementan `ILLMProvider`. Agregar un quinto provider es añadir una clase y registrarla en `Program.cs`.

---

## 3. Estructura de archivos

Los archivos marcados con `←` son nuevos. Los marcados con `(mod)` son existentes modificados.

```
gestion-de-proyectos/
│
├── Controllers/
│   ├── AuthController.cs
│   ├── ProjectController.cs
│   ├── ProjectMemberController.cs
│   ├── TaskController.cs
│   ├── UserController.cs
│   └── AIController.cs                          ← Fase 4
│
├── Services/
│   ├── ProjectService.cs
│   ├── ProjectMemberService.cs
│   ├── ProjectAuthorizationService.cs
│   ├── TaskService.cs
│   ├── UserService.cs
│   ├── S3Service.cs
│   ├── UserContextAccessor.cs
│   │
│   └── AI/                                      ← Fases 1-3/6
│       ├── AIService.cs                          ← Fase 3 + patch Fase 6
│       ├── FallbackLLMService.cs                 ← Fase 2 + patch Fase 6
│       ├── IAIService.cs                         ← Fase 1
│       ├── IFallbackLLMService.cs                ← Fase 1
│       ├── ILLMProvider.cs                       ← Fase 1
│       ├── AIExceptions.cs                       ← Fase 1
│       ├── AILoggerExtensions.cs                 ← Fase 6
│       │
│       └── Providers/                            ← Fase 2
│           ├── OpenAICompatibleProvider.cs
│           ├── GroqProvider.cs
│           ├── CerebrasProvider.cs
│           ├── GeminiProvider.cs
│           └── OpenRouterProvider.cs
│
├── Repositories/
│   ├── IProjectRepository.cs
│   ├── ITaskRepository.cs
│   ├── ProjectRepository.cs
│   └── TaskRepository.cs
│
├── Models/
│   ├── ApplicationUser.cs
│   ├── Project.cs
│   ├── ProjectMember.cs
│   └── Task.cs
│
├── DTOs/
│   ├── (DTOs existentes...)
│   │
│   └── AI/                                      ← Fase 1
│       ├── GenerateProjectRequestDto.cs
│       ├── AIGeneratedProjectDto.cs
│       ├── AIGeneratedTaskDto.cs
│       ├── AIConfirmProjectDto.cs
│       └── LLMDtos.cs
│
├── Middleware/
│   └── AIRateLimitMiddleware.cs                  ← Fase 5
│
├── Configuration/
│   └── AIOptions.cs                              ← Fase 1
│
├── Prompts/                                      ← Fase 1
│   ├── ProjectGenerationPrompt.txt
│   └── TaskSuggestionPrompt.txt
│
├── Mappers/
│   └── MappingProfile.cs
│
├── ApplicationDbContext.cs
├── Program.cs                                    ← (mod) Fase 5
├── appsettings.json                              ← (mod) Fase 5
├── .env                                          ← (mod) Fase 5
├── .env.example                                  ← Fase 5
└── gestion-de-proyectos.csproj                   ← (mod) Fase 5
```

---

## 4. Flujos principales

### 4.1 Generar y confirmar un proyecto

```
Usuario                  AIController            AIService              FallbackLLMService
   │                          │                      │                        │
   │  POST /api/ai/           │                      │                        │
   │  generate-project        │                      │                        │
   │─────────────────────────►│                      │                        │
   │                          │ GenerateProjectAsync │                        │
   │                          │─────────────────────►│                        │
   │                          │                      │ BuildProjectGeneration │
   │                          │                      │ Message()              │
   │                          │                      │                        │
   │                          │                      │ CompleteWithFallback   │
   │                          │                      │───────────────────────►│
   │                          │                      │                        │ intenta Groq
   │                          │                      │                        │──────────►
   │                          │                      │                        │◄── JSON
   │                          │                      │◄───────────────────────│
   │                          │                      │                        │
   │                          │                      │ ParseProjectJson()     │
   │                          │                      │ SanitizeJson()         │
   │                          │                      │                        │
   │                          │◄─────────────────────│                        │
   │◄─────────────────────────│                      │                        │
   │  200 AIGeneratedProjectDto                      │                        │
   │  (NO persistido aún)     │                      │                        │
   │                          │                      │                        │
   │  [usuario revisa/edita]  │                      │                        │
   │                          │                      │                        │
   │  POST /api/ai/           │                      │                        │
   │  confirm-project         │                      │                        │
   │─────────────────────────►│                      │                        │
   │                          │ ConfirmAndPersist    │                        │
   │                          │─────────────────────►│                        │
   │                          │                      │ ProjectService         │
   │                          │                      │ .CreateProjectAsync()  │
   │                          │                      │──────────────────────► DB
   │                          │                      │                        │
   │                          │                      │ TaskService            │
   │                          │                      │ .CreateTaskAsync() x N │
   │                          │                      │──────────────────────► DB
   │                          │                      │                        │
   │◄─────────────────────────│◄─────────────────────│                        │
   │  201 Created             │                      │                        │
   │  Location: /api/projects/{id}                   │                        │
```

### 4.2 Fallback en cascada cuando Groq está saturado

```
FallbackLLMService
        │
        ├─► GroqProvider.CompleteAsync()
        │       └─► HTTP 429 → LLMRateLimitException
        │               │
        │       LogProviderRateLimit("Groq")
        │
        ├─► CerebrasProvider.CompleteAsync()
        │       └─► Timeout → TimeoutException
        │               │
        │       LogProviderTimeout("Cerebras")
        │
        ├─► GeminiProvider.CompleteAsync()
        │       └─► 200 OK ✓
        │               │
        │       response.UsedFallback = true
        │       response.FailedProviders = ["Groq(RateLimit)", "Cerebras(Timeout)"]
        │
        └─► Retorna LLMResponseDto al AIService
```

### 4.3 Sugerir tareas para proyecto existente

```
GET /api/ai/suggest-tasks/{projectId}
        │
        ├── Validar acceso con ProjectAuthorizationService
        │       └── NotFoundException / UnauthorizedAccessException si falla
        │
        ├── Cargar proyecto con tareas existentes (ProjectRepository)
        │
        ├── BuildTaskSuggestionMessage()
        │       └── Incluye lista de tareas existentes para evitar duplicados
        │
        ├── FallbackLLMService.CompleteWithFallbackAsync()
        │
        ├── ParseTaskListJson()
        │       └── Maneja respuesta como array [] o como objeto {"tasks": [...]}
        │
        └── Ajustar orderIndex para continuar desde tareas existentes
                └── Retorna IEnumerable<AIGeneratedTaskDto> (NO persiste)
```

---

## 5. Proveedores LLM y fallback

### Tabla de proveedores

| Prioridad | Proveedor | Modelo | Límite gratuito | Obtener API key |
|:---------:|-----------|--------|-----------------|-----------------|
| 1 | **Groq** | llama-3.3-70b-versatile | ~14,400 req/día | [console.groq.com/keys](https://console.groq.com/keys) |
| 2 | **Cerebras** | llama-3.3-70b | Alto volumen diario | [cloud.cerebras.ai](https://cloud.cerebras.ai) |
| 3 | **Gemini** | gemini-2.5-flash | Mayor volumen en tokens | [aistudio.google.com/apikey](https://aistudio.google.com/apikey) |
| 4 | **OpenRouter** | llama-3.3-70b-instruct:free | 50 req/día | [openrouter.ai/settings/keys](https://openrouter.ai/settings/keys) |

### Regla de fallback

El `FallbackLLMService` pasa al siguiente proveedor cuando recibe cualquiera de:

- `LLMRateLimitException` — el proveedor retornó `429 Too Many Requests`
- `TimeoutException` — el proveedor no respondió en el tiempo configurado
- `HttpRequestException` — error de red o respuesta `5xx`

Si todos los proveedores fallan, lanza `LLMUnavailableException`, que el controlador mapea a `503 Service Unavailable`.

### Base class compartida

Groq, Cerebras, Gemini y OpenRouter son todos compatibles con la API de OpenAI (endpoint `chat/completions`). La clase abstracta `OpenAICompatibleProvider` implementa toda la lógica HTTP común. Cada provider concreto solo declara su nombre, prioridad, `HttpClient` nombrado y API key. `OpenRouterProvider` sobreescribe adicionalmente `ConfigureAdditionalHeaders()` para agregar `HTTP-Referer` y `X-Title`, requeridos por OpenRouter.

---

## 6. Referencia de archivos

### DTOs (`DTOs/AI/`)

| Archivo | Dirección | Descripción |
|---------|-----------|-------------|
| `GenerateProjectRequestDto.cs` | Entrada | Descripción del usuario (20-2000 chars), idioma, maxTasks (1-20), detailLevel |
| `AIGeneratedTaskDto.cs` | Salida | Tarea sugerida por IA: título, descripción, prioridad, dueDateOffsetDays, orderIndex |
| `AIGeneratedProjectDto.cs` | Salida | Proyecto sugerido completo + metadata: generatedByProvider, usedFallback, generatedAt |
| `AIConfirmProjectDto.cs` | Entrada | Proyecto confirmado por el usuario. Contiene `List<AIConfirmTaskDto>` para persistir |
| `LLMDtos.cs` | Interno | `LLMRequestDto` (systemPrompt, userMessage, temperature, maxTokens) y `LLMResponseDto` (texto, proveedor, tokens, latencia, fallbackInfo) |

### Interfaces (`Services/AI/`)

| Archivo | Descripción |
|---------|-------------|
| `ILLMProvider.cs` | Contrato de cada provider. Define `ProviderName`, `Priority`, `IsEnabled`, `CompleteAsync()` |
| `IFallbackLLMService.cs` | Contrato del orquestador. Define `CompleteWithFallbackAsync()` y `GetProviders()` |
| `IAIService.cs` | Contrato del servicio de dominio. Define los tres métodos de negocio |

### Excepciones (`Services/AI/AIExceptions.cs`)

| Excepción | Cuándo se lanza | Mapeo HTTP |
|-----------|-----------------|------------|
| `LLMUnavailableException` | Todos los providers fallaron | `503 Service Unavailable` |
| `LLMParseException` | El JSON del LLM no tiene el formato esperado | `502 Bad Gateway` |
| `LLMRateLimitException` | Un provider retornó 429 (uso interno, no llega al controller) | — |

### Providers (`Services/AI/Providers/`)

| Archivo | Descripción |
|---------|-------------|
| `OpenAICompatibleProvider.cs` | Clase base abstracta. Implementa la llamada HTTP, manejo de 429, timeout, y deserialización de la respuesta |
| `GroqProvider.cs` | Prioridad 1. `api.groq.com/openai/v1/` |
| `CerebrasProvider.cs` | Prioridad 2. `api.cerebras.ai/v1/` |
| `GeminiProvider.cs` | Prioridad 3. `generativelanguage.googleapis.com/v1beta/openai/` |
| `OpenRouterProvider.cs` | Prioridad 4. `openrouter.ai/api/v1/`. Agrega headers `HTTP-Referer` y `X-Title` |

### Servicios (`Services/AI/`)

**`FallbackLLMService.cs`**  
Recibe `IEnumerable<ILLMProvider>` por inyección de dependencias, los ordena por `Priority`, y los itera en cascada. Registra cada intento con `AILoggerExtensions`. No tiene conocimiento del dominio de la aplicación.

**`AIService.cs`**  
Servicio de dominio. Sus tres métodos públicos corresponden a los tres casos de uso del módulo:

- `GenerateProjectAsync` — construye el mensaje del usuario, llama al `FallbackLLMService`, sanitiza el JSON con `SanitizeJson()`, lo parsea con `ParseProjectJson()`, y aplica el límite `MaxTasksPerProject`. No persiste nada.
- `ConfirmAndPersistProjectAsync` — delega a `ProjectService.CreateProjectAsync()` y luego itera las tareas llamando a `TaskService.CreateTaskAsync()`. Un fallo individual en una tarea se loggea pero no revierte el proyecto.
- `SuggestTasksForProjectAsync` — valida acceso con `ProjectAuthorizationService`, construye el contexto con las tareas existentes, llama al LLM, y ajusta el `orderIndex` para continuar desde las tareas ya existentes.

**`AILoggerExtensions.cs`**  
Static class con extension methods sobre `ILogger`. Centraliza todos los mensajes de log con propiedades nombradas consistentes para CloudWatch Logs Insights: `AIOperation`, `AIProvider`, `AITokens`, `AIResponseMs`, `AIFallback`, `AIProjectId`, `AIUserId`.

### Prompts (`Prompts/`)

Los archivos `.txt` se cargan en runtime con `File.ReadAllText` desde `AppContext.BaseDirectory/Prompts/`. La carga es lazy, thread-safe y cacheada en memoria (se leen una sola vez al arrancar).

**`ProjectGenerationPrompt.txt`** — instruye al LLM a responder exclusivamente con JSON válido siguiendo el schema exacto de `AIGeneratedProjectDto`. Prohíbe explícitamente markdown, backticks, y texto fuera del JSON.

**`TaskSuggestionPrompt.txt`** — variante que recibe el contexto de un proyecto existente con sus tareas, e instruye al LLM a sugerir únicamente tareas que no existan ya. Retorna un array JSON directamente.

> **Importante para el build:** el `.csproj` debe incluir el siguiente bloque para que los `.txt` se copien al directorio de salida:
> ```xml
> <ItemGroup>
>   <Content Include="Prompts\**\*.txt">
>     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
>   </Content>
> </ItemGroup>
> ```

### Middleware (`Middleware/AIRateLimitMiddleware.cs`)

Intercepta todas las peticiones a `/api/ai/*`. Implementa una ventana deslizante en memoria por usuario (`ConcurrentDictionary<userId, Queue<DateTime>>`). Por defecto: 10 solicitudes por 60 minutos. Retorna `429` con los headers estándar `Retry-After`, `X-RateLimit-Limit`, `X-RateLimit-Remaining` y `X-RateLimit-Reset`.

> **Nota multi-instancia:** el estado vive en memoria del proceso. En un entorno ECS con múltiples tasks, cada instancia tiene su propia ventana. Para un límite estrictamente compartido entre instancias se necesitaría Redis.

### Configuración (`Configuration/AIOptions.cs`)

Options pattern fuertemente tipado. Se registra en `Program.cs` con:
```csharp
builder.Services.Configure<AIOptions>(
    builder.Configuration.GetSection(AIOptions.SectionName));
```

Propiedades principales:

| Propiedad | Default | Descripción |
|-----------|---------|-------------|
| `Enabled` | `true` | Si `false`, todos los endpoints retornan 503 sin llamar al LLM |
| `MaxTasksPerProject` | `20` | Límite global de tareas que la IA puede sugerir |
| `Providers.{Name}.Enabled` | `true` | Deshabilitar un provider sin tocar código |
| `Providers.{Name}.Priority` | 1-4 | Orden en la cascada |
| `Providers.{Name}.TimeoutSeconds` | 30-60 | Timeout del HttpClient |
| `RateLimit.MaxRequestsPerWindow` | `10` | Solicitudes permitidas por usuario en la ventana |
| `RateLimit.WindowMinutes` | `60` | Tamaño de la ventana de rate limit |

---

## 7. API endpoints

Todos los endpoints requieren `Authorization: Bearer <JWT>` con el rol `User`.

### `POST /api/ai/generate-project`

Genera una sugerencia de proyecto. **No persiste nada.**

**Request body:**
```json
{
  "description": "Quiero crear una tienda online con carrito y pagos con Stripe.",
  "language": "es",
  "maxTasks": 10,
  "detailLevel": "brief"
}
```

**Respuesta exitosa `200 OK`:**
```json
{
  "name": "Tienda Online con Stripe",
  "description": "E-commerce para venta de productos...",
  "status": "OnHold",
  "tasks": [
    {
      "title": "Diseñar esquema de base de datos",
      "description": "Definir tablas para productos, usuarios y pedidos.",
      "priority": "High",
      "dueDateOffsetDays": 5,
      "orderIndex": 1
    }
  ],
  "generatedByProvider": "Groq",
  "usedFallback": false,
  "generatedAt": "2026-03-23T18:00:00Z"
}
```

| Código | Causa |
|--------|-------|
| `200` | Sugerencia generada correctamente |
| `400` | Descripción vacía, muy corta (<20), muy larga (>2000), o maxTasks fuera de rango |
| `401` | Sin token JWT |
| `429` | Rate limit del usuario superado |
| `502` | El LLM respondió con formato JSON inválido |
| `503` | Todos los providers LLM fallaron |

---

### `POST /api/ai/confirm-project`

Persiste el proyecto y las tareas confirmadas. El usuario actual se convierte en Owner.

**Request body:**
```json
{
  "name": "Tienda Online con Stripe",
  "description": "E-commerce de ropa con carrito y pagos.",
  "status": "InProgress",
  "selectedTasks": [
    {
      "title": "Diseñar esquema de base de datos",
      "description": "Tablas para productos, usuarios y pedidos.",
      "priority": "High",
      "dueDateOffsetDays": 5
    }
  ]
}
```

**Respuesta exitosa `201 Created`:**  
`Location: /api/projects/{id}`  
Body: `ProjectDto` completo del proyecto recién creado.

| Código | Causa |
|--------|-------|
| `201` | Proyecto creado correctamente |
| `400` | Nombre vacío u otros datos inválidos |
| `401` | Sin token JWT |

---

### `GET /api/ai/suggest-tasks/{projectId}`

Sugiere tareas faltantes para un proyecto existente. **No persiste nada.**

**Respuesta exitosa `200 OK`:**
```json
[
  {
    "title": "Escribir tests de integración",
    "description": "Cubrir los endpoints principales con tests automatizados.",
    "priority": "Medium",
    "dueDateOffsetDays": 21,
    "orderIndex": 6
  }
]
```

| Código | Causa |
|--------|-------|
| `200` | Lista de sugerencias (puede ser array vacío `[]` si el proyecto ya está completo) |
| `401` | Sin token JWT |
| `403` | El usuario no tiene acceso al proyecto |
| `404` | El proyecto no existe |
| `429` | Rate limit del usuario superado |
| `502` | El LLM respondió con formato JSON inválido |
| `503` | Todos los providers LLM fallaron |

---

## 8. Configuración

Agregar la sección `AI` en `appsettings.json`:

```json
{
  "AI": {
    "Enabled": true,
    "MaxTasksPerProject": 20,
    "RateLimit": {
      "MaxRequestsPerWindow": 10,
      "WindowMinutes": 60
    },
    "Providers": {
      "Groq": {
        "Enabled": true,
        "Priority": 1,
        "BaseUrl": "https://api.groq.com/openai/v1/",
        "Model": "llama-3.3-70b-versatile",
        "TimeoutSeconds": 30,
        "ApiKey": ""
      },
      "Cerebras": {
        "Enabled": true,
        "Priority": 2,
        "BaseUrl": "https://api.cerebras.ai/v1/",
        "Model": "llama-3.3-70b",
        "TimeoutSeconds": 30,
        "ApiKey": ""
      },
      "Gemini": {
        "Enabled": true,
        "Priority": 3,
        "BaseUrl": "https://generativelanguage.googleapis.com/v1beta/openai/",
        "Model": "gemini-2.5-flash",
        "TimeoutSeconds": 45,
        "ApiKey": ""
      },
      "OpenRouter": {
        "Enabled": true,
        "Priority": 4,
        "BaseUrl": "https://openrouter.ai/api/v1/",
        "Model": "meta-llama/llama-3.3-70b-instruct:free",
        "TimeoutSeconds": 60,
        "ApiKey": "",
        "AppName": "Gestion de Proyectos"
      }
    }
  }
}
```

Las `ApiKey` siempre deben estar vacías en `appsettings.json`. Se sobreescriben desde variables de entorno.

Agregar en `Program.cs` dentro del bloque de servicios:

```csharp
// Configuración
builder.Services.Configure<AIOptions>(
    builder.Configuration.GetSection(AIOptions.SectionName));
builder.Services.Configure<AIRateLimitOptions>(
    builder.Configuration.GetSection(AIRateLimitOptions.SectionName));

// HttpClients
builder.Services.AddHttpClient("Groq", client => { ... });
builder.Services.AddHttpClient("Cerebras", client => { ... });
builder.Services.AddHttpClient("Gemini", client => { ... });
builder.Services.AddHttpClient("OpenRouter", client => { ... });

// Providers y servicios
builder.Services.AddScoped<ILLMProvider, GroqProvider>();
builder.Services.AddScoped<ILLMProvider, CerebrasProvider>();
builder.Services.AddScoped<ILLMProvider, GeminiProvider>();
builder.Services.AddScoped<ILLMProvider, OpenRouterProvider>();
builder.Services.AddScoped<IFallbackLLMService, FallbackLLMService>();
builder.Services.AddScoped<IAIService, AIService>();
```

Y en el pipeline HTTP, antes de `UseAuthorization`:

```csharp
app.UseAIRateLimit();
```

---

## 9. Variables de entorno

Las API keys se configuran como variables de entorno usando la convención de .NET con doble guion bajo para secciones anidadas.

**Desarrollo local (`.env`):**

```bash
AI__Providers__Groq__ApiKey=gsk_xxxxxxxxxxxxxxxxxxxx
AI__Providers__Cerebras__ApiKey=csk-xxxxxxxxxxxxxxxxxxxx
AI__Providers__Gemini__ApiKey=AIzaxxxxxxxxxxxxxxxxxxxx
AI__Providers__OpenRouter__ApiKey=sk-or-v1-xxxxxxxxxxxx
```

**Producción (ECS Task Definition — via AWS Secrets Manager):**

```json
[
  {
    "name": "AI__Providers__Groq__ApiKey",
    "valueFrom": "arn:aws:secretsmanager:us-east-2:ACCOUNT:secret:gestion-proyectos/ai-keys:groq_api_key::"
  },
  {
    "name": "AI__Providers__Cerebras__ApiKey",
    "valueFrom": "arn:aws:secretsmanager:us-east-2:ACCOUNT:secret:gestion-proyectos/ai-keys:cerebras_api_key::"
  },
  {
    "name": "AI__Providers__Gemini__ApiKey",
    "valueFrom": "arn:aws:secretsmanager:us-east-2:ACCOUNT:secret:gestion-proyectos/ai-keys:gemini_api_key::"
  },
  {
    "name": "AI__Providers__OpenRouter__ApiKey",
    "valueFrom": "arn:aws:secretsmanager:us-east-2:ACCOUNT:secret:gestion-proyectos/ai-keys:openrouter_api_key::"
  }
]
```

**GitHub Actions Secrets** (Settings → Secrets and variables → Actions):

| Secret | Descripción |
|--------|-------------|
| `AI_GROQ_API_KEY` | API key de Groq |
| `AI_CEREBRAS_API_KEY` | API key de Cerebras |
| `AI_GEMINI_API_KEY` | API key de Google AI Studio |
| `AI_OPENROUTER_API_KEY` | API key de OpenRouter |

---

## 10. Rate limiting

El `AIRateLimitMiddleware` aplica una ventana deslizante por usuario autenticado sobre todas las rutas `/api/ai/*`.

**Valores por defecto:** 10 solicitudes cada 60 minutos.  
**Configuración:** sección `AI:RateLimit` en `appsettings.json`.

Cuando el límite se supera, el middleware responde con:

```
HTTP 429 Too Many Requests
Retry-After: 3542
X-RateLimit-Limit: 10
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 2026-03-23T19:00:00Z

{
  "Message": "Has alcanzado el límite de 10 solicitudes al módulo de IA por 60 minutos. Intenta de nuevo en 3542 segundos."
}
```

Las respuestas exitosas incluyen `X-RateLimit-Limit` y `X-RateLimit-Remaining` como información para el frontend.

---

## 11. Logging y observabilidad

El módulo utiliza `AILoggerExtensions` — una clase de extension methods sobre `ILogger` que garantiza nombres de propiedades consistentes en todos los eventos. Esto permite consultarlos como campos estructurados en CloudWatch Logs Insights.

### Propiedades estructuradas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `AIOperation` | string | `GenerateProject`, `ConfirmProject`, `SuggestTasks` |
| `AIProvider` | string | Nombre del proveedor que respondió |
| `AITokens` | int | Tokens consumidos en la llamada |
| `AIResponseMs` | long | Latencia del proveedor en ms |
| `AIFallback` | bool | Si se activó el fallback |
| `AIProjectId` | Guid | ID del proyecto involucrado |
| `AIUserId` | string | ID del usuario que hizo la solicitud |

### Prefijos de log por categoría

| Prefijo | Categoría |
|---------|-----------|
| `[AI:Cascade]` | Eventos del orquestador FallbackLLMService |
| `[AI:Provider]` | Respuesta exitosa de un provider |
| `[AI:RateLimit]` | Rate limit de un provider (429) o del middleware |
| `[AI:Timeout]` | Timeout de un provider |
| `[AI:HttpError]` | Error HTTP de un provider |
| `[AI:AllFailed]` | Todos los providers fallaron |
| `[AI:Generate]` | Eventos de generación de proyecto |
| `[AI:Confirm]` | Eventos de confirmación y persistencia |
| `[AI:Suggest]` | Eventos de sugerencia de tareas |
| `[AI:Parse]` | Errores de parseo del JSON del LLM |

### Queries de CloudWatch Logs Insights destacadas

**Tasa de éxito por proveedor:**
```
fields AIProvider, AITokens, AIResponseMs, AIFallback
| filter @message like /\[AI:Provider\]/
| stats count() as Total, avg(AIResponseMs) as TiempoPromedioMs by AIProvider
```

**Latencia P95 por proveedor:**
```
fields AIProvider, AIResponseMs
| filter @message like /\[AI:Provider\]/
| stats pct(AIResponseMs, 95) as P95ms by AIProvider
```

**Usuarios bloqueados por rate limit:**
```
fields AIUserId
| filter @message like /\[AI:RateLimit\]/ and @message like /LimiteAlcanzado=true/
| stats count() as VecesBlockeado by AIUserId
| sort VecesBlockeado desc
```

El archivo `cloudwatch-queries.logs` contiene las 10 queries completas documentadas.

---

## 12. Pruebas manuales

El archivo `pruebas-manuales.sh` contiene 24 pruebas organizadas en 7 bloques. No está diseñado para ejecutarse de un tirón — se usa como referencia de comandos `curl` listos para copiar y pegar.

### Prerrequisitos

```bash
# 1. Instalar jq
brew install jq          # macOS
sudo apt install jq      # Ubuntu

# 2. Obtener el JWT
TOKEN=$(curl -s -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "Admin123!"}' | jq -r '.token')

# 3. Usar -k si el certificado local es autofirmado
curl -sk ...
```

### Resumen de bloques

| Bloque | Tests | Qué verifican |
|--------|-------|---------------|
| 1 | T01–T05 | Validaciones de entrada (no llegan al LLM) |
| 2 | T06–T10 | Generación en español, inglés, dominios técnicos y no técnicos |
| 3 | T11–T14 | Confirmación, persistencia y ownership automático |
| 4 | T15–T18 | Sugerencia de tareas, acceso denegado, proyecto inexistente |
| 5 | T19–T20 | Fallback con Groq caído; 503 con todos caídos |
| 6 | T21–T22 | Rate limit en solicitud 11; headers de rate limit presentes |
| 7 | T23–T24 | Logs estructurados visibles en CloudWatch con propiedades nombradas |

> **Advertencia T19-T20:** estas pruebas requieren modificar las API keys en `.env` y reiniciar el servidor. Restaurar las keys reales después de ejecutarlas.

> **Advertencia T21:** el loop consume 10 solicitudes reales al LLM, gastando cuota gratuita. Ejecutarlo solo cuando se necesite verificar el rate limiting.

---

## 13. Checklist de deploy

Completar antes de cualquier despliegue a producción.

### Seguridad
- [ ] Las cuatro API keys están en variables de entorno, nunca en código ni en `appsettings.json`
- [ ] Los secrets de GitHub Actions (`AI_GROQ_API_KEY`, etc.) están configurados
- [ ] Las variables de entorno están en la ECS Task Definition o en AWS Secrets Manager
- [ ] El `.env` está en `.gitignore`

### Funcionalidad
- [ ] Los endpoints de IA requieren autenticación JWT (`[Authorize(Roles = "User")]`)
- [ ] El rate limiting por usuario está activo y configurado
- [ ] La validación de 20-2000 chars en la descripción funciona
- [ ] El fallback en cascada está verificado con T19

### Build
- [ ] El `.csproj` incluye el bloque `<Content Include="Prompts\**\*.txt">` con `CopyToOutputDirectory`
- [ ] Los archivos `Prompts/*.txt` existen en el directorio de salida (`bin/`)
- [ ] El módulo compila sin warnings

### Observabilidad
- [ ] Los logs estructurados aparecen en CloudWatch con las propiedades `AI*`
- [ ] Los errores `503` y `502` llegan con mensajes legibles al frontend
- [ ] La query de resumen ejecutivo (query 10) retorna datos

---

## Funcionalidades futuras (post v1.0)

**Resumen inteligente de proyecto** — dado el estado actual (tareas completadas, pendientes, miembros activos), la IA genera un párrafo de resumen ejecutivo. Solo lectura, sin persistencia.

**Detección de riesgos** — la IA analiza el proyecto y emite advertencias: tareas de alta prioridad sin asignar, proyectos con deadline sin tareas de testing, miembros con sobrecarga.

**Chat contextual** — endpoint de chat donde el usuario hace preguntas en lenguaje natural y la IA responde con información real extraída de la base de datos.

**Auto-descripción de tareas** — el usuario escribe solo el título de una tarea y la IA sugiere la descripción con criterios de aceptación.

---

*Sistema de Gestión de Proyectos v1.0 — Módulo IA v1.0*  
*Desarrollado con ASP.NET Core 8 · Desplegado en AWS ECS Fargate*
