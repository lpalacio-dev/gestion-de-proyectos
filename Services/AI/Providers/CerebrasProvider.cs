using gestion_de_proyectos.Configuration;
using Microsoft.Extensions.Options;

namespace gestion_de_proyectos.Services.AI.Providers
{
    /// <summary>
    /// Proveedor LLM usando Cerebras (https://cloud.cerebras.ai).
    /// Prioridad 2 — velocidad comparable a Groq, gran límite diario en capa gratuita.
    /// Modelo: Llama 3.3 70B. Compatible con la API de OpenAI (chat/completions).
    /// </summary>
    public class CerebrasProvider : OpenAICompatibleProvider
    {
        private readonly ProviderOptions _options;

        public CerebrasProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIOptions> aiOptions,
            ILogger<CerebrasProvider> logger)
            : base(httpClientFactory, logger)
        {
            _options = aiOptions.Value.Providers.Cerebras;
        }

        public override string ProviderName => "Cerebras";
        public override int Priority => _options.Priority > 0 ? _options.Priority : 2;
        public override bool IsEnabled => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

        protected override string HttpClientName => "Cerebras";
        protected override string ApiKey => _options.ApiKey;
        protected override string Model => string.IsNullOrWhiteSpace(_options.Model)
            ? "llama-3.3-70b"
            : _options.Model;
    }
}
