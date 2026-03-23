using gestion_de_proyectos.DTOs.AI;
using Microsoft.Extensions.Options;
using gestion_de_proyectos.Configuration;

namespace gestion_de_proyectos.Services.AI
{
    /// <summary>
    /// Implementación del servicio de fallback en cascada para proveedores LLM.
    ///
    /// Flujo de ejecución:
    ///   1. Filtra los providers habilitados y los ordena por prioridad.
    ///   2. Intenta el primer provider disponible.
    ///   3. Si recibe LLMRateLimitException, TimeoutException o HttpRequestException,
    ///      registra el fallo y pasa al siguiente provider.
    ///   4. Si algún provider responde exitosamente, retorna su respuesta
    ///      marcando UsedFallback=true si no fue el primero de la lista.
    ///   5. Si TODOS fallan, lanza LLMUnavailableException con el detalle de los fallos.
    ///
    /// Este servicio NO tiene conocimiento del dominio de la aplicación.
    /// </summary>
    public class FallbackLLMService : IFallbackLLMService
    {
        private readonly IReadOnlyList<ILLMProvider> _providers;
        private readonly ILogger<FallbackLLMService> _logger;
        private readonly AIOptions _options;

        public FallbackLLMService(
            IEnumerable<ILLMProvider> providers,
            ILogger<FallbackLLMService> logger,
            IOptions<AIOptions> aiOptions)
        {
            // Filtrar solo los habilitados y ordenar por prioridad ascendente
            _providers = providers
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.Priority)
                .ToList()
                .AsReadOnly();

            _logger = logger;
            _options = aiOptions.Value;
        }

        public IReadOnlyList<ILLMProvider> GetProviders() => _providers;

        public async Task<LLMResponseDto> CompleteWithFallbackAsync(
            LLMRequestDto request,
            CancellationToken cancellationToken = default)
        {
            // Verificar que el módulo de IA esté habilitado
            if (!_options.Enabled)
            {
                throw new LLMUnavailableException(
                    "El módulo de IA está desactivado en la configuración actual.",
                    Array.Empty<string>());
            }

            if (_providers.Count == 0)
            {
                throw new LLMUnavailableException(
                    "No hay proveedores LLM habilitados. Verifica la configuración y las API keys.",
                    Array.Empty<string>());
            }

            var failedProviders = new List<string>();
            var firstProvider = _providers[0].ProviderName;

            _logger.LogInformation(
                "[FallbackLLM] Iniciando cascada para operación '{Op}'. " +
                "Providers disponibles: {Providers}",
                request.OperationType,
                string.Join(" → ", _providers.Select(p => p.ProviderName)));

            foreach (var provider in _providers)
            {
                try
                {
                    _logger.LogInformation("[FallbackLLM] Intentando provider: {Provider}", provider.ProviderName);

                    var response = await provider.CompleteAsync(request, cancellationToken);

                    // Éxito — enriquecer la respuesta con metadata del fallback
                    response.UsedFallback = provider.ProviderName != firstProvider;
                    response.FailedProviders = failedProviders;

                    if (response.UsedFallback)
                    {
                        _logger.LogWarning(
                            "[FallbackLLM] Respuesta obtenida de provider de respaldo '{Provider}'. " +
                            "Fallidos antes: {Failed}",
                            provider.ProviderName,
                            string.Join(", ", failedProviders));
                    }
                    else
                    {
                        _logger.LogInformation(
                            "[FallbackLLM] Respuesta exitosa del provider primario '{Provider}'. " +
                            "Tokens: {Tokens}. Tiempo: {Ms}ms",
                            provider.ProviderName,
                            response.TokensUsed,
                            response.ResponseTimeMs);
                    }

                    return response;
                }
                catch (LLMRateLimitException ex)
                {
                    _logger.LogWarning(
                        "[FallbackLLM] Rate limit en '{Provider}'. {Message}. Pasando al siguiente.",
                        provider.ProviderName, ex.Message);
                    failedProviders.Add($"{provider.ProviderName}(RateLimit)");
                }
                catch (TimeoutException ex)
                {
                    _logger.LogWarning(
                        "[FallbackLLM] Timeout en '{Provider}': {Message}. Pasando al siguiente.",
                        provider.ProviderName, ex.Message);
                    failedProviders.Add($"{provider.ProviderName}(Timeout)");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(
                        "[FallbackLLM] Error HTTP en '{Provider}': {Message}. Pasando al siguiente.",
                        provider.ProviderName, ex.Message);
                    failedProviders.Add($"{provider.ProviderName}(HTTP_{(int?)ex.StatusCode})");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // El cliente canceló la solicitud — no seguir intentando
                    _logger.LogInformation("[FallbackLLM] Operación cancelada por el cliente.");
                    throw;
                }
                catch (Exception ex)
                {
                    // Error inesperado — loggear pero continuar al siguiente provider
                    _logger.LogError(ex,
                        "[FallbackLLM] Error inesperado en '{Provider}'. Pasando al siguiente.",
                        provider.ProviderName);
                    failedProviders.Add($"{provider.ProviderName}(Unknown)");
                }
            }

            // Todos los providers fallaron
            _logger.LogError(
                "[FallbackLLM] TODOS los providers fallaron para la operación '{Op}'. Fallidos: {Failed}",
                request.OperationType,
                string.Join(", ", failedProviders));

            throw new LLMUnavailableException(failedProviders);
        }
    }
}
