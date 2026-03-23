using gestion_de_proyectos.Configuration;
using gestion_de_proyectos.DTOs.AI;
using gestion_de_proyectos.Services.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gestion_de_proyectos.Services.AI.Providers
{
    /// <summary>
    /// Clase base para todos los proveedores compatibles con la API de OpenAI.
    /// Groq, Cerebras y OpenRouter comparten el mismo protocolo HTTP,
    /// solo difieren en BaseUrl, ApiKey y nombre de modelo.
    /// Gemini también es compatible desde 2024 pero tiene su propia subclase por el header de auth.
    /// </summary>
    public abstract class OpenAICompatibleProvider : ILLMProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;

        protected abstract string HttpClientName { get; }
        protected abstract string ApiKey { get; }
        protected abstract string Model { get; }

        public abstract string ProviderName { get; }
        public abstract int Priority { get; }
        public abstract bool IsEnabled { get; }

        protected OpenAICompatibleProvider(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<LLMResponseDto> CompleteAsync(LLMRequestDto request, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var client = _httpClientFactory.CreateClient(HttpClientName);

            // Configurar autorización
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", ApiKey);

            // Agregar headers adicionales que el proveedor pueda requerir
            ConfigureAdditionalHeaders(client);

            // Construir el body según el formato OpenAI Chat Completions
            var requestBody = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user",   content = request.UserMessage  }
                },
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                // Algunos proveedores soportan response_format para forzar JSON
                response_format = new { type = "text" }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("[{Provider}] Enviando solicitud. Operación: {Operation}. Modelo: {Model}",
                ProviderName, request.OperationType, Model);

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("chat/completions", content, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning("[{Provider}] Timeout después de {Ms}ms.", ProviderName, stopwatch.ElapsedMilliseconds);
                throw new TimeoutException($"El proveedor '{ProviderName}' no respondió a tiempo.", ex);
            }

            stopwatch.Stop();

            // Manejar rate limit
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                int? retryAfter = null;
                if (response.Headers.RetryAfter?.Delta.HasValue == true)
                    retryAfter = (int)response.Headers.RetryAfter.Delta!.Value.TotalSeconds;

                _logger.LogWarning("[{Provider}] Rate limit alcanzado (429). RetryAfter: {Retry}s.",
                    ProviderName, retryAfter);

                throw new LLMRateLimitException(ProviderName, retryAfter);
            }

            // Manejar otros errores HTTP
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("[{Provider}] Error HTTP {Status}: {Body}",
                    ProviderName, (int)response.StatusCode, errorBody);
                throw new HttpRequestException(
                    $"[{ProviderName}] HTTP {(int)response.StatusCode}: {errorBody}");
            }

            // Parsear respuesta
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonSerializer.Deserialize<OpenAIChatResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var generatedText = parsed?.Choices?.FirstOrDefault()?.Message?.Content
                ?? throw new HttpRequestException($"[{ProviderName}] La respuesta no contiene contenido generado.");

            var tokensUsed = parsed?.Usage?.TotalTokens ?? 0;

            _logger.LogInformation("[{Provider}] Respuesta recibida en {Ms}ms. Tokens: {Tokens}. Op: {Op}",
                ProviderName, stopwatch.ElapsedMilliseconds, tokensUsed, request.OperationType);

            return new LLMResponseDto
            {
                GeneratedText = generatedText,
                ProviderName = ProviderName,
                ModelName = Model,
                TokensUsed = tokensUsed,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }

        /// <summary>
        /// Hook para que las subclases agreguen headers HTTP adicionales si son necesarios.
        /// Ejemplo: OpenRouter requiere HTTP-Referer y X-Title.
        /// </summary>
        protected virtual void ConfigureAdditionalHeaders(HttpClient client) { }

        // ============================================================================
        // MODELOS INTERNOS PARA DESERIALIZAR LA RESPUESTA DE OPENAI CHAT COMPLETIONS
        // ============================================================================

        private class OpenAIChatResponse
        {
            public List<Choice>? Choices { get; set; }
            public Usage? Usage { get; set; }
        }

        private class Choice
        {
            public Message? Message { get; set; }
        }

        private class Message
        {
            public string? Content { get; set; }
        }

        private class Usage
        {
            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }
    }
}
