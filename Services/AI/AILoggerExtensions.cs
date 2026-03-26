using gestion_de_proyectos.DTOs.AI;

namespace gestion_de_proyectos.Services.AI
{
    /// <summary>
    /// Extension methods de logging estructurado para el módulo de IA.
    /// Centraliza todos los mensajes de log con propiedades nombradas consistentes,
    /// lo que permite filtrarlos y consultarlos fácilmente en CloudWatch Logs Insights.
    ///
    /// Convención de nombres de propiedades:
    ///   AIOperation   → nombre de la operación (GenerateProject, ConfirmProject, SuggestTasks)
    ///   AIProvider    → nombre del proveedor LLM que respondió
    ///   AITokens      → tokens consumidos en la llamada
    ///   AIResponseMs  → tiempo de respuesta del proveedor en milisegundos
    ///   AIFallback    → true si se activó el mecanismo de fallback
    ///   AIProjectId   → ID del proyecto involucrado
    ///   AIUserId      → ID del usuario que hizo la solicitud
    /// </summary>
    public static class AILoggerExtensions
    {
        // ============================================================================
        // FallbackLLMService — eventos de la cascada
        // ============================================================================

        public static void LogCascadeStarted(
            this ILogger logger,
            string operationType,
            IEnumerable<string> providerNames)
        {
            logger.LogInformation(
                "[AI:Cascade] Operación={AIOperation} Iniciando cascada. Providers={AIProviders}",
                operationType,
                string.Join(" → ", providerNames));
        }

        public static void LogProviderAttempt(
            this ILogger logger,
            string operationType,
            string providerName,
            int attemptNumber)
        {
            logger.LogInformation(
                "[AI:Cascade] Operación={AIOperation} Intento={AIAttempt} Provider={AIProvider}",
                operationType,
                attemptNumber,
                providerName);
        }

        public static void LogProviderSuccess(
            this ILogger logger,
            string operationType,
            string providerName,
            int tokensUsed,
            long responseMs,
            bool usedFallback)
        {
            logger.LogInformation(
                "[AI:Provider] Operación={AIOperation} Provider={AIProvider} " +
                "Tokens={AITokens} TiempoMs={AIResponseMs} Fallback={AIFallback}",
                operationType,
                providerName,
                tokensUsed,
                responseMs,
                usedFallback);
        }

        public static void LogProviderRateLimit(
            this ILogger logger,
            string operationType,
            string providerName,
            int? retryAfterSeconds)
        {
            logger.LogWarning(
                "[AI:RateLimit] Operación={AIOperation} Provider={AIProvider} " +
                "RateLimitado=true RetryAfterSegundos={RetryAfter}",
                operationType,
                providerName,
                retryAfterSeconds);
        }

        public static void LogProviderTimeout(
            this ILogger logger,
            string operationType,
            string providerName)
        {
            logger.LogWarning(
                "[AI:Timeout] Operación={AIOperation} Provider={AIProvider} Timeout=true",
                operationType,
                providerName);
        }

        public static void LogProviderHttpError(
            this ILogger logger,
            string operationType,
            string providerName,
            string errorMessage)
        {
            logger.LogWarning(
                "[AI:HttpError] Operación={AIOperation} Provider={AIProvider} Error={AIError}",
                operationType,
                providerName,
                errorMessage);
        }

        public static void LogAllProvidersFailed(
            this ILogger logger,
            string operationType,
            IEnumerable<string> failedProviders)
        {
            logger.LogError(
                "[AI:AllFailed] Operación={AIOperation} TodosFallaron=true Providers={AIFailedProviders}",
                operationType,
                string.Join(", ", failedProviders));
        }

        // ============================================================================
        // AIService — eventos de dominio
        // ============================================================================

        public static void LogGenerateProjectStarted(
            this ILogger logger,
            string userId,
            int descriptionLength,
            int maxTasks)
        {
            logger.LogInformation(
                "[AI:Generate] AIOperacion=GenerateProject AIUserId={AIUserId} " +
                "DescripcionChars={AIDescLen} MaxTareas={AIMaxTasks}",
                userId,
                descriptionLength,
                maxTasks);
        }

        public static void LogGenerateProjectCompleted(
            this ILogger logger,
            string projectName,
            int taskCount,
            string providerName,
            bool usedFallback)
        {
            logger.LogInformation(
                "[AI:Generate] AIOperacion=GenerateProject Completado=true " +
                "NombreProyecto={AIProjectName} Tareas={AITaskCount} " +
                "Provider={AIProvider} Fallback={AIFallback}",
                projectName,
                taskCount,
                providerName,
                usedFallback);
        }

        public static void LogConfirmProjectStarted(
            this ILogger logger,
            string userId,
            string projectName,
            int selectedTaskCount)
        {
            logger.LogInformation(
                "[AI:Confirm] AIOperacion=ConfirmProject AIUserId={AIUserId} " +
                "NombreProyecto={AIProjectName} TareasSeleccionadas={AITaskCount}",
                userId,
                projectName,
                selectedTaskCount);
        }

        public static void LogConfirmProjectCompleted(
            this ILogger logger,
            Guid projectId,
            int createdTasks,
            int failedTasks)
        {
            var level = failedTasks > 0 ? LogLevel.Warning : LogLevel.Information;

            logger.Log(level,
                "[AI:Confirm] AIOperacion=ConfirmProject Completado=true " +
                "AIProjectId={AIProjectId} TareasCreadas={AICreatedTasks} TareasFallidas={AIFailedTasks}",
                projectId,
                createdTasks,
                failedTasks);
        }

        public static void LogTaskCreationError(
            this ILogger logger,
            Exception ex,
            string taskTitle,
            Guid projectId)
        {
            logger.LogError(ex,
                "[AI:Confirm] Error al crear tarea. AIProjectId={AIProjectId} Tarea={AITaskTitle}",
                projectId,
                taskTitle);
        }

        public static void LogSuggestTasksStarted(
            this ILogger logger,
            string userId,
            Guid projectId,
            string projectName,
            int existingTaskCount)
        {
            logger.LogInformation(
                "[AI:Suggest] AIOperacion=SuggestTasks AIUserId={AIUserId} " +
                "AIProjectId={AIProjectId} NombreProyecto={AIProjectName} TareasExistentes={AIExistingTasks}",
                userId,
                projectId,
                projectName,
                existingTaskCount);
        }

        public static void LogSuggestTasksCompleted(
            this ILogger logger,
            Guid projectId,
            int suggestionCount,
            string providerName)
        {
            logger.LogInformation(
                "[AI:Suggest] AIOperacion=SuggestTasks Completado=true " +
                "AIProjectId={AIProjectId} Sugerencias={AISuggestionCount} Provider={AIProvider}",
                projectId,
                suggestionCount,
                providerName);
        }

        public static void LogParseError(
            this ILogger logger,
            string providerName,
            string operationType,
            string rawResponsePreview)
        {
            logger.LogError(
                "[AI:Parse] ErrorParseo=true Provider={AIProvider} Operacion={AIOperation} " +
                "RawPreview={AIRawPreview}",
                providerName,
                operationType,
                rawResponsePreview.Length > 300
                    ? rawResponsePreview[..300] + "…"
                    : rawResponsePreview);
        }

        // ============================================================================
        // Rate limit — eventos del middleware
        // ============================================================================

        public static void LogRateLimitExceeded(
            this ILogger logger,
            string userId,
            int limit,
            int windowMinutes,
            int secondsUntilReset)
        {
            logger.LogWarning(
                "[AI:RateLimit] LimiteAlcanzado=true AIUserId={AIUserId} " +
                "Limite={AILimit} VentanaMinutos={AIWindow} ResetEnSegundos={AIResetSeconds}",
                userId,
                limit,
                windowMinutes,
                secondsUntilReset);
        }
    }
}
