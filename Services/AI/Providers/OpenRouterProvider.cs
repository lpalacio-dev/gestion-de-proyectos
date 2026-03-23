using gestion_de_proyectos.Configuration;
using Microsoft.Extensions.Options;

namespace gestion_de_proyectos.Services.AI.Providers
{
    /// <summary>
    /// Proveedor LLM usando OpenRouter (https://openrouter.ai).
    /// Prioridad 4 — proveedor de emergencia que agrega múltiples modelos bajo una sola API.
    /// Capa gratuita: ~50 req/día. Modelo: meta-llama/llama-3.3-70b-instruct:free.
    ///
    /// OpenRouter requiere dos headers adicionales para identificar la aplicación:
    ///   HTTP-Referer: URL o nombre de la app
    ///   X-Title: Nombre visible de la app en el dashboard de OpenRouter
    /// </summary>
    public class OpenRouterProvider : OpenAICompatibleProvider
    {
        private readonly OpenRouterProviderOptions _options;

        public OpenRouterProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIOptions> aiOptions,
            ILogger<OpenRouterProvider> logger)
            : base(httpClientFactory, logger)
        {
            _options = aiOptions.Value.Providers.OpenRouter;
        }

        public override string ProviderName => "OpenRouter";
        public override int Priority => _options.Priority > 0 ? _options.Priority : 4;
        public override bool IsEnabled => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

        protected override string HttpClientName => "OpenRouter";
        protected override string ApiKey => _options.ApiKey;
        protected override string Model => string.IsNullOrWhiteSpace(_options.Model)
            ? "meta-llama/llama-3.3-70b-instruct:free"
            : _options.Model;

        /// <summary>
        /// OpenRouter requiere estos headers para identificar la aplicación.
        /// Sin ellos, la solicitud puede ser rechazada o limitar el acceso a modelos gratuitos.
        /// </summary>
        protected override void ConfigureAdditionalHeaders(HttpClient client)
        {
            var appName = string.IsNullOrWhiteSpace(_options.AppName)
                ? "Gestion de Proyectos"
                : _options.AppName;

            // Remover headers previos para evitar duplicados en clientes reutilizados
            client.DefaultRequestHeaders.Remove("HTTP-Referer");
            client.DefaultRequestHeaders.Remove("X-Title");

            client.DefaultRequestHeaders.Add("HTTP-Referer", $"https://github.com/{appName.Replace(" ", "-").ToLower()}");
            client.DefaultRequestHeaders.Add("X-Title", appName);
        }
    }
}
