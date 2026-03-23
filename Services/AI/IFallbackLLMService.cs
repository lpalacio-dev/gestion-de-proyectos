using gestion_de_proyectos.DTOs.AI;

namespace gestion_de_proyectos.Services.AI
{
    /// <summary>
    /// Servicio orquestador de la cascada de proveedores LLM.
    /// Recibe una solicitud, intenta los proveedores en orden de prioridad
    /// y retorna la primera respuesta exitosa.
    ///
    /// Orden de fallback: Groq (1) → Cerebras (2) → Gemini (3) → OpenRouter (4)
    ///
    /// Este servicio NO tiene conocimiento del dominio de la aplicación,
    /// solo gestiona la comunicación con APIs externas de LLM.
    /// </summary>
    public interface IFallbackLLMService
    {
        /// <summary>
        /// Envía una solicitud LLM iterando los proveedores disponibles en orden de prioridad.
        /// Si el proveedor activo retorna 429 o un error de red, se pasa automáticamente al siguiente.
        /// </summary>
        /// <param name="request">DTO con el prompt de sistema, mensaje del usuario y parámetros.</param>
        /// <param name="cancellationToken">Token para cancelar toda la cadena de llamadas.</param>
        /// <returns>Respuesta del primer proveedor que responda exitosamente.</returns>
        /// <exception cref="LLMUnavailableException">
        /// Si TODOS los proveedores fallan. Incluye el detalle de cada fallo.
        /// </exception>
        Task<LLMResponseDto> CompleteWithFallbackAsync(LLMRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retorna la lista de proveedores registrados, ordenados por prioridad.
        /// Útil para diagnóstico y health checks.
        /// </summary>
        IReadOnlyList<ILLMProvider> GetProviders();
    }
}
