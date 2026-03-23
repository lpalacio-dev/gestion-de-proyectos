// ============================================================================
// SECCIÓN A AGREGAR EN appsettings.json
// ============================================================================
// Las API keys NUNCA deben ir aquí en texto plano.
// En desarrollo: usar .env (ya configurado en Program.cs con DotNetEnv).
// En producción: usar AWS Secrets Manager inyectado en la ECS Task Definition.
// ============================================================================

/*
"AI": {
  "Enabled": true,
  "MaxTasksPerProject": 20,
  "Providers": {
    "Groq": {
      "Enabled": true,
      "Priority": 1,
      "BaseUrl": "https://api.groq.com/openai/v1",
      "Model": "llama-3.3-70b-versatile",
      "TimeoutSeconds": 30,
      "ApiKey": ""  // Leer de variable de entorno AI__Providers__Groq__ApiKey
    },
    "Cerebras": {
      "Enabled": true,
      "Priority": 2,
      "BaseUrl": "https://api.cerebras.ai/v1",
      "Model": "llama-3.3-70b",
      "TimeoutSeconds": 30,
      "ApiKey": ""  // Leer de variable de entorno AI__Providers__Cerebras__ApiKey
    },
    "Gemini": {
      "Enabled": true,
      "Priority": 3,
      "BaseUrl": "https://generativelanguage.googleapis.com/v1beta/openai",
      "Model": "gemini-2.5-flash",
      "TimeoutSeconds": 45,
      "ApiKey": ""  // Leer de variable de entorno AI__Providers__Gemini__ApiKey
    },
    "OpenRouter": {
      "Enabled": true,
      "Priority": 4,
      "BaseUrl": "https://openrouter.ai/api/v1",
      "Model": "meta-llama/llama-3.3-70b-instruct:free",
      "TimeoutSeconds": 60,
      "ApiKey": "",  // Leer de variable de entorno AI__Providers__OpenRouter__ApiKey
      "AppName": "Gestion de Proyectos"  // Requerido por OpenRouter en header HTTP-Referer
    }
  }
}
*/

// ============================================================================
// CLASE DE CONFIGURACIÓN FUERTEMENTE TIPADA (options pattern)
// ============================================================================

namespace gestion_de_proyectos.Configuration
{
    /// <summary>
    /// Configuración global del módulo de IA.
    /// Se registra en Program.cs con: builder.Services.Configure<AIOptions>(builder.Configuration.GetSection("AI"));
    /// </summary>
    public class AIOptions
    {
        public const string SectionName = "AI";

        /// <summary>
        /// Si false, todos los endpoints de IA retornan 503 inmediatamente sin llamar a ningún LLM.
        /// Útil para desactivar el módulo en emergencias sin redesplegar.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Número máximo de tareas que la IA puede sugerir por proyecto.
        /// Limita los tokens consumidos y controla la complejidad de la respuesta.
        /// </summary>
        public int MaxTasksPerProject { get; set; } = 20;

        /// <summary>
        /// Configuración de cada proveedor LLM.
        /// </summary>
        public ProvidersOptions Providers { get; set; } = new();
    }

    public class ProvidersOptions
    {
        public ProviderOptions Groq { get; set; } = new();
        public ProviderOptions Cerebras { get; set; } = new();
        public ProviderOptions Gemini { get; set; } = new();
        public OpenRouterProviderOptions OpenRouter { get; set; } = new();
    }

    public class ProviderOptions
    {
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// API key del proveedor. Se lee desde variables de entorno en producción.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
    }

    public class OpenRouterProviderOptions : ProviderOptions
    {
        /// <summary>
        /// Nombre de la aplicación que se envía en el header HTTP-Referer.
        /// Requerido por OpenRouter para identificar el origen de las solicitudes.
        /// </summary>
        public string AppName { get; set; } = "Gestion de Proyectos";
    }
}
