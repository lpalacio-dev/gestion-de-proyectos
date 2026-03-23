namespace gestion_de_proyectos.DTOs.AI
{
    /// <summary>
    /// DTO interno que estandariza la solicitud hacia cualquier proveedor LLM.
    /// FallbackLLMService usa este DTO para llamar a Groq, Cerebras, Gemini u OpenRouter
    /// con la misma interfaz, sin importar qué proveedor esté activo.
    /// </summary>
    public class LLMRequestDto
    {
        /// <summary>
        /// Instrucción de sistema (system prompt).
        /// Define el rol, comportamiento y formato de respuesta esperado del LLM.
        /// Este prompt es constante por tipo de operación (generación de proyecto, sugerencia de tareas, etc.).
        /// </summary>
        public string SystemPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje del usuario que se enviará al LLM.
        /// Contiene la descripción del proyecto u otras instrucciones de contexto.
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// Temperatura del LLM (creatividad vs. determinismo).
        /// 0.0 = muy determinista, 1.0 = muy creativo.
        /// Para generación de proyectos, se recomienda un valor entre 0.4 y 0.7.
        /// </summary>
        public float Temperature { get; set; } = 0.5f;

        /// <summary>
        /// Número máximo de tokens en la respuesta del LLM.
        /// Ajustar según la longitud esperada del JSON de salida.
        /// </summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// Identificador del tipo de operación para logging.
        /// Ejemplo: "GenerateProject", "SuggestTasks"
        /// </summary>
        public string OperationType { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO interno que encapsula la respuesta de cualquier proveedor LLM.
    /// El FallbackLLMService retorna siempre este DTO independientemente del proveedor usado.
    /// </summary>
    public class LLMResponseDto
    {
        /// <summary>
        /// Texto generado por el LLM.
        /// Se espera que sea un JSON válido según el system prompt configurado.
        /// </summary>
        public string GeneratedText { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del proveedor que respondió exitosamente.
        /// Ejemplo: "Groq", "Cerebras", "Gemini", "OpenRouter"
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del modelo específico utilizado por el proveedor.
        /// Ejemplo: "llama-3.3-70b-versatile", "gemini-2.5-flash"
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Total de tokens consumidos en esta solicitud (prompt + completion).
        /// Útil para monitorear costos y consumo de cuotas gratuitas.
        /// </summary>
        public int TokensUsed { get; set; }

        /// <summary>
        /// Indica si se activó el mecanismo de fallback.
        /// True si el proveedor de mayor prioridad falló y se usó uno alternativo.
        /// </summary>
        public bool UsedFallback { get; set; }

        /// <summary>
        /// Lista de proveedores que fallaron antes de obtener una respuesta exitosa.
        /// Útil para logging y diagnóstico.
        /// Ejemplo: ["Groq", "Cerebras"] significa que Gemini fue quien respondió.
        /// </summary>
        public List<string> FailedProviders { get; set; } = new();

        /// <summary>
        /// Tiempo en milisegundos que tardó el proveedor en responder.
        /// </summary>
        public long ResponseTimeMs { get; set; }
    }
}
