namespace gestion_de_proyectos.Services.AI
{
    /// <summary>
    /// Se lanza cuando TODOS los proveedores LLM de la cascada han fallado.
    /// El controlador mapea esta excepción a un HTTP 503 Service Unavailable.
    /// </summary>
    public class LLMUnavailableException : Exception
    {
        /// <summary>
        /// Lista de proveedores que se intentaron y fallaron antes de lanzar esta excepción.
        /// </summary>
        public IReadOnlyList<string> FailedProviders { get; }

        public LLMUnavailableException(IEnumerable<string> failedProviders)
            : base($"Todos los proveedores LLM han fallado. Intentados: {string.Join(", ", failedProviders)}. Por favor intenta de nuevo en unos minutos.")
        {
            FailedProviders = failedProviders.ToList().AsReadOnly();
        }

        public LLMUnavailableException(string message, IEnumerable<string> failedProviders)
            : base(message)
        {
            FailedProviders = failedProviders.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Se lanza cuando el LLM responde exitosamente pero con un formato JSON inesperado
    /// que no puede parsearse al DTO esperado.
    /// El controlador mapea esta excepción a un HTTP 502 Bad Gateway.
    /// </summary>
    public class LLMParseException : Exception
    {
        /// <summary>
        /// Nombre del proveedor que generó la respuesta que falló al parsear.
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// El texto raw que el LLM generó y no pudo ser parseado.
        /// Útil para debugging.
        /// </summary>
        public string RawResponse { get; }

        public LLMParseException(string providerName, string rawResponse, Exception innerException)
            : base($"El proveedor '{providerName}' generó una respuesta que no pudo ser interpretada. " +
                   "El modelo no respondió en el formato JSON esperado.", innerException)
        {
            ProviderName = providerName;
            RawResponse = rawResponse;
        }
    }

    /// <summary>
    /// Se lanza cuando un proveedor LLM individual retorna 429 Too Many Requests.
    /// El FallbackLLMService captura esta excepción para pasar al siguiente proveedor.
    /// NO debería propagarse hasta el controlador.
    /// </summary>
    public class LLMRateLimitException : Exception
    {
        /// <summary>
        /// Nombre del proveedor que retornó el rate limit.
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// Segundos sugeridos de espera antes de reintentar (si el proveedor lo indica).
        /// Null si el proveedor no especificó tiempo de espera.
        /// </summary>
        public int? RetryAfterSeconds { get; }

        public LLMRateLimitException(string providerName, int? retryAfterSeconds = null)
            : base($"El proveedor '{providerName}' ha alcanzado su límite de solicitudes (429). " +
                   (retryAfterSeconds.HasValue ? $"Reintentar en {retryAfterSeconds} segundos." : ""))
        {
            ProviderName = providerName;
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}
