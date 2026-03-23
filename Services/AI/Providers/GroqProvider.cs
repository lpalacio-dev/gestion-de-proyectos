using gestion_de_proyectos.Configuration;
using Microsoft.Extensions.Options;

namespace gestion_de_proyectos.Services.AI.Providers
{
    /// <summary>
    /// Proveedor LLM usando Groq (https://console.groq.com).
    /// Prioridad 1 — el más rápido gracias a sus LPUs (Language Processing Units).
    /// Modelo: Llama 3.3 70B. Límite free: ~14,400 req/día.
    /// Compatible con la API de OpenAI (chat/completions).
    /// </summary>
    public class GroqProvider : OpenAICompatibleProvider
    {
        private readonly ProviderOptions _options;

        public GroqProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIOptions> aiOptions,
            ILogger<GroqProvider> logger)
            : base(httpClientFactory, logger)
        {
            _options = aiOptions.Value.Providers.Groq;
        }

        public override string ProviderName => "Groq";
        public override int Priority => _options.Priority > 0 ? _options.Priority : 1;
        public override bool IsEnabled => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

        protected override string HttpClientName => "Groq";
        protected override string ApiKey => _options.ApiKey;
        protected override string Model => string.IsNullOrWhiteSpace(_options.Model)
            ? "llama-3.3-70b-versatile"
            : _options.Model;
    }
}
