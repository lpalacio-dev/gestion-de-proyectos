using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using gestion_de_proyectos.Configuration;
using System.Security.Claims;

namespace gestion_de_proyectos.Middleware
{
    /// <summary>
    /// Middleware de rate limiting para los endpoints del módulo de IA (/api/ai/*).
    /// Limita a 10 solicitudes por usuario por hora usando una ventana deslizante en memoria.
    ///
    /// Diseño deliberadamente simple (in-process, sin Redis) porque:
    ///   - El límite de cuota de los LLMs gratuitos es por cuenta, no por instancia.
    ///   - Si el sistema escala a múltiples instancias ECS, cada una tendrá su propia ventana,
    ///     lo que efectivamente multiplica el límite. Para un límite estricto entre instancias
    ///     se necesitaría Redis — agregar cuando el uso justifique la complejidad.
    ///
    /// Ruta de activación: solo requests a /api/ai/
    /// </summary>
    public class AIRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AIRateLimitMiddleware> _logger;
        private readonly AIRateLimitOptions _options;

        // userId → lista de timestamps de solicitudes en la ventana actual
        private static readonly ConcurrentDictionary<string, Queue<DateTime>> _windows = new();

        public AIRateLimitMiddleware(
            RequestDelegate next,
            ILogger<AIRateLimitMiddleware> logger,
            IOptions<AIRateLimitOptions> options)
        {
            _next    = next;
            _logger  = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Solo aplicar a rutas del módulo de IA
            if (!context.Request.Path.StartsWithSegments("/api/ai"))
            {
                await _next(context);
                return;
            }

            // Obtener el ID del usuario autenticado
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Si no hay usuario, dejar pasar (el [Authorize] del controlador lo rechazará)
                await _next(context);
                return;
            }

            var now      = DateTime.UtcNow;
            var window   = _options.WindowMinutes;
            var limit    = _options.MaxRequestsPerWindow;
            var cutoff   = now.AddMinutes(-window);

            var queue = _windows.GetOrAdd(userId, _ => new Queue<DateTime>());

            lock (queue)
            {
                // Limpiar timestamps fuera de la ventana
                while (queue.Count > 0 && queue.Peek() < cutoff)
                    queue.Dequeue();

                if (queue.Count >= limit)
                {
                    var oldestInWindow = queue.Peek();
                    var resetAt        = oldestInWindow.AddMinutes(window);
                    var secondsLeft    = (int)(resetAt - now).TotalSeconds;

                    _logger.LogWarning(
                        "[AIRateLimit] Usuario {UserId} ha superado el límite de {Limit} req/{Window}min. " +
                        "Reset en {Seconds}s.",
                        userId, limit, window, secondsLeft);

                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.Headers["Retry-After"]   = secondsLeft.ToString();
                    context.Response.Headers["X-RateLimit-Limit"]     = limit.ToString();
                    context.Response.Headers["X-RateLimit-Remaining"] = "0";
                    context.Response.Headers["X-RateLimit-Reset"]     = resetAt.ToString("o");
                    context.Response.ContentType = "application/json";

                    context.Response.WriteAsync(
                        $"{{\"Message\":\"Has alcanzado el límite de {limit} solicitudes al módulo de IA " +
                        $"por {window} minutos. Intenta de nuevo en {secondsLeft} segundos.\"}}");
                    return;
                }

                queue.Enqueue(now);

                // Agregar headers informativos en respuestas exitosas
                context.Response.Headers["X-RateLimit-Limit"]     = limit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = (limit - queue.Count).ToString();
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Opciones de configuración del rate limit.
    /// Se registran en appsettings.json bajo la sección "AI:RateLimit".
    /// </summary>
    public class AIRateLimitOptions
    {
        public const string SectionName = "AI:RateLimit";

        /// <summary>Número máximo de solicitudes permitidas en la ventana de tiempo.</summary>
        public int MaxRequestsPerWindow { get; set; } = 10;

        /// <summary>Tamaño de la ventana de tiempo en minutos.</summary>
        public int WindowMinutes { get; set; } = 60;
    }

    /// <summary>
    /// Extension method para registrar el middleware limpiamente en Program.cs.
    /// Uso: app.UseAIRateLimit();
    /// </summary>
    public static class AIRateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseAIRateLimit(this IApplicationBuilder app)
            => app.UseMiddleware<AIRateLimitMiddleware>();
    }
}
