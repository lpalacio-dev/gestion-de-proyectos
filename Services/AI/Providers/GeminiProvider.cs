using gestion_de_proyectos.Configuration;
using Microsoft.Extensions.Options;

namespace gestion_de_proyectos.Services.AI.Providers
{
    /// <summary>
    /// Proveedor LLM usando Google Gemini vía Google AI Studio (https://aistudio.google.com).
    /// Prioridad 3 — ventana de contexto de 1M tokens, mayor volumen de tokens en capa gratuita.
    /// Modelo: Gemini 2.5 Flash.
    /// Google AI Studio expone un endpoint compatible con OpenAI desde 2024,
    /// por lo que reutilizamos la clase base OpenAICompatibleProvider sin modificaciones.
    /// </summary>
    public class GeminiProvider : OpenAICompatibleProvider
    {
        private readonly ProviderOptions _options;

        public GeminiProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIOptions> aiOptions,
            ILogger<GeminiProvider> logger)
            : base(httpClientFactory, logger)
        {
            _options = aiOptions.Value.Providers.Gemini;
        }

        public override string ProviderName => "Gemini";
        public override int Priority => _options.Priority > 0 ? _options.Priority : 3;
        public override bool IsEnabled => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

        protected override string HttpClientName => "Gemini";
        protected override string ApiKey => _options.ApiKey;
        protected override string Model => string.IsNullOrWhiteSpace(_options.Model)
            ? "gemini-2.5-flash"
            : _options.Model;
    }
}
