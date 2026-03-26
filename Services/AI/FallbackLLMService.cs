using gestion_de_proyectos.Configuration;
using gestion_de_proyectos.DTOs.AI;
using Microsoft.Extensions.Options;

namespace gestion_de_proyectos.Services.AI
{
    /// <summary>
    /// Versión actualizada de FallbackLLMService usando AILoggerExtensions
    /// para logging estructurado consistente. Reemplaza la versión de la Fase 2.
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
            _providers = providers
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.Priority)
                .ToList()
                .AsReadOnly();

            _logger  = logger;
            _options = aiOptions.Value;
        }

        public IReadOnlyList<ILLMProvider> GetProviders() => _providers;

        public async Task<LLMResponseDto> CompleteWithFallbackAsync(
            LLMRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                throw new LLMUnavailableException(
                    "El módulo de IA está desactivado en la configuración actual.",
                    Array.Empty<string>());

            if (_providers.Count == 0)
                throw new LLMUnavailableException(
                    "No hay proveedores LLM habilitados. Verifica la configuración y las API keys.",
                    Array.Empty<string>());

            var firstProvider  = _providers[0].ProviderName;
            var failedProviders = new List<string>();

            _logger.LogCascadeStarted(request.OperationType, _providers.Select(p => p.ProviderName));

            for (int i = 0; i < _providers.Count; i++)
            {
                var provider = _providers[i];
                _logger.LogProviderAttempt(request.OperationType, provider.ProviderName, i + 1);

                try
                {
                    var response = await provider.CompleteAsync(request, cancellationToken);

                    response.UsedFallback    = provider.ProviderName != firstProvider;
                    response.FailedProviders = failedProviders;

                    _logger.LogProviderSuccess(
                        request.OperationType,
                        provider.ProviderName,
                        response.TokensUsed,
                        response.ResponseTimeMs,
                        response.UsedFallback);

                    return response;
                }
                catch (LLMRateLimitException ex)
                {
                    _logger.LogProviderRateLimit(
                        request.OperationType, provider.ProviderName, ex.RetryAfterSeconds);
                    failedProviders.Add($"{provider.ProviderName}(RateLimit)");
                }
                catch (TimeoutException)
                {
                    _logger.LogProviderTimeout(request.OperationType, provider.ProviderName);
                    failedProviders.Add($"{provider.ProviderName}(Timeout)");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogProviderHttpError(
                        request.OperationType, provider.ProviderName, ex.Message);
                    failedProviders.Add($"{provider.ProviderName}(HTTP_{(int?)ex.StatusCode})");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "[AI:Cascade] Operación={AIOperation} CanceladaPorCliente=true",
                        request.OperationType);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[AI:Cascade] Operación={AIOperation} Provider={AIProvider} ErrorInesperado=true",
                        request.OperationType, provider.ProviderName);
                    failedProviders.Add($"{provider.ProviderName}(Unknown)");
                }
            }

            _logger.LogAllProvidersFailed(request.OperationType, failedProviders);
            throw new LLMUnavailableException(failedProviders);
        }
    }
}
