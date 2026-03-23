using gestion_de_proyectos.DTOs.AI;

namespace gestion_de_proyectos.Services.AI
{
    /// <summary>
    /// Contrato que deben implementar todos los proveedores LLM individuales.
    /// Cada proveedor (Groq, Cerebras, Gemini, OpenRouter) implementa esta interfaz,
    /// lo que permite al FallbackLLMService tratarlos de forma polimórfica.
    /// </summary>
    public interface ILLMProvider
    {
        /// <summary>
        /// Nombre identificador del proveedor.
        /// Usado en logging y en el DTO de respuesta.
        /// Ejemplo: "Groq", "Cerebras", "Gemini", "OpenRouter"
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Número de prioridad en la cascada de fallback.
        /// El proveedor con número más bajo se intenta primero.
        /// 1 = Groq (mayor prioridad), 4 = OpenRouter (emergencia).
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Indica si este proveedor está habilitado en la configuración actual.
        /// Permite desactivar proveedores sin eliminar su código.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Envía una solicitud al proveedor LLM y retorna la respuesta.
        /// </summary>
        /// <param name="request">DTO con el prompt de sistema, mensaje del usuario y parámetros.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>DTO con el texto generado y metadata de la llamada.</returns>
        /// <exception cref="HttpRequestException">Si hay un error de red o el proveedor retorna 5xx.</exception>
        /// <exception cref="LLMRateLimitException">Si el proveedor retorna 429 Too Many Requests.</exception>
        /// <exception cref="TaskCanceledException">Si se supera el timeout configurado.</exception>
        Task<LLMResponseDto> CompleteAsync(LLMRequestDto request, CancellationToken cancellationToken = default);
    }
}
