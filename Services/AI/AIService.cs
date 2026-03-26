using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using gestion_de_proyectos.Configuration;
using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.DTOs.AI;
using gestion_de_proyectos.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace gestion_de_proyectos.Services.AI
{
    public class AIService : IAIService
    {
        private readonly IFallbackLLMService _llmService;
        private readonly IProjectService _projectService;
        private readonly ITaskService _taskService;
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly AIOptions _options;
        private readonly ILogger<AIService> _logger;

        // Prompts cargados desde archivos .txt en el directorio Prompts/
        private static string? _projectGenerationPrompt;
        private static string? _taskSuggestionPrompt;
        private static readonly object _promptLock = new();

        public AIService(
            IFallbackLLMService llmService,
            IProjectService projectService,
            ITaskService taskService,
            IProjectRepository projectRepository,
            IProjectAuthorizationService authorizationService,
            IUserContextAccessor userContextAccessor,
            IOptions<AIOptions> aiOptions,
            ILogger<AIService> logger)
        {
            _llmService = llmService;
            _projectService = projectService;
            _taskService = taskService;
            _projectRepository = projectRepository;
            _authorizationService = authorizationService;
            _userContextAccessor = userContextAccessor;
            _options = aiOptions.Value;
            _logger = logger;
        }

        // ============================================================================
        // GENERATE PROJECT
        // ============================================================================

        public async Task<AIGeneratedProjectDto> GenerateProjectAsync(
            GenerateProjectRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                throw new LLMUnavailableException("El módulo de IA está desactivado.", Array.Empty<string>());

            _logger.LogGenerateProjectStarted(
                _userContextAccessor.GetCurrentUserId(),
                request.Description.Length,
                request.MaxTasks);

            // Construir el mensaje del usuario con parámetros adicionales
            var userMessage = BuildProjectGenerationMessage(request);

            var llmRequest = new LLMRequestDto
            {
                SystemPrompt  = GetProjectGenerationPrompt(),
                UserMessage   = userMessage,
                Temperature   = 0.5f,
                MaxTokens     = 2000,
                OperationType = "GenerateProject"
            };

            var llmResponse = await _llmService.CompleteWithFallbackAsync(llmRequest, cancellationToken);

            // Parsear la respuesta JSON del LLM
            var parsed = ParseProjectJson(llmResponse.GeneratedText, llmResponse.ProviderName);

            // Aplicar límite de tareas configurado
            int maxTasks = Math.Min(request.MaxTasks, _options.MaxTasksPerProject);
            if (parsed.Tasks.Count > maxTasks)
            {
                parsed.Tasks = parsed.Tasks.Take(maxTasks).ToList();
                _logger.LogInformation("[AIService] Lista de tareas truncada a {Max}.", maxTasks);
            }

            // Reasignar orderIndex tras el posible truncado
            for (int i = 0; i < parsed.Tasks.Count; i++)
                parsed.Tasks[i].OrderIndex = i + 1;

            parsed.GeneratedByProvider = llmResponse.ProviderName;
            parsed.UsedFallback        = llmResponse.UsedFallback;
            parsed.GeneratedAt         = DateTime.UtcNow;

            _logger.LogGenerateProjectCompleted(
                parsed.Name,
                parsed.Tasks.Count,
                llmResponse.ProviderName,
                llmResponse.UsedFallback);

            return parsed;
        }

        // ============================================================================
        // CONFIRM AND PERSIST
        // ============================================================================

        public async Task<ProjectDto> ConfirmAndPersistProjectAsync(
            AIConfirmProjectDto dto,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            _logger.LogConfirmProjectStarted(currentUserId, dto.Name, dto.SelectedTasks.Count);

            // 1. Crear el proyecto usando el servicio existente
            //    ProjectService asigna OwnerId, CreationDate y auto-agrega al owner como miembro.
            var createProjectDto = new CreateProjectDto
            {
                Name        = dto.Name,
                Description = dto.Description
            };

            // Necesitamos el status también — creamos el proyecto y luego actualizamos el estado
            // si difiere del default (OnHold), para no duplicar lógica de ProjectService.
            var projectDto = await _projectService.CreateProjectAsync(createProjectDto);

            if (!string.IsNullOrWhiteSpace(dto.Status) && dto.Status != "OnHold")
            {
                await _projectService.UpdateProjectAsync(projectDto.Id, new UpdateProjectDto
                {
                    Name        = dto.Name,
                    Description = dto.Description,
                    Status      = dto.Status
                });
            }

            // 2. Crear las tareas seleccionadas
            int createdTasks = 0;
            int failedTasks  = 0;

            foreach (var taskDto in dto.SelectedTasks)
            {
                try
                {
                    var createTaskDto = new CreateTaskDto
                    {
                        Title       = taskDto.Title,
                        Description = taskDto.Description,
                        Priority    = taskDto.Priority,
                        DueDate     = taskDto.DueDateOffsetDays.HasValue
                            ? DateTime.UtcNow.AddDays(taskDto.DueDateOffsetDays.Value)
                            : null
                    };

                    await _taskService.CreateTaskAsync(projectDto.Id, createTaskDto);
                    createdTasks++;
                }
                catch (Exception ex)
                {
                    // Un fallo individual en una tarea no revierte el proyecto ni las demás tareas.
                    failedTasks++;
                    _logger.LogTaskCreationError(ex, taskDto.Title, projectDto.Id);
                }
            }

            _logger.LogConfirmProjectCompleted(projectDto.Id, createdTasks, failedTasks);

            // Retornar el proyecto recién creado (con sus relaciones)
            return await _projectService.GetProjectByIdAsync(projectDto.Id);
        }

        // ============================================================================
        // SUGGEST TASKS
        // ============================================================================

        public async Task<IEnumerable<AIGeneratedTaskDto>> SuggestTasksForProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // Validar acceso al proyecto (lanza NotFoundException o UnauthorizedAccessException)
            await _authorizationService.ValidateProjectAccessAsync(currentUserId, projectId);

            // Cargar el proyecto con sus tareas actuales
            var project = await _projectRepository.GetByIdAsync(projectId)
                ?? throw new NotFoundException($"Proyecto {projectId} no encontrado.");

            _logger.LogSuggestTasksStarted(
                currentUserId,
                projectId,
                project.Name,
                project.Tasks.Count);

            var userMessage = BuildTaskSuggestionMessage(project);

            var llmRequest = new LLMRequestDto
            {
                SystemPrompt  = GetTaskSuggestionPrompt(),
                UserMessage   = userMessage,
                Temperature   = 0.5f,
                MaxTokens     = 1500,
                OperationType = "SuggestTasks"
            };

            var llmResponse = await _llmService.CompleteWithFallbackAsync(llmRequest, cancellationToken);

            var suggestions = ParseTaskListJson(llmResponse.GeneratedText, llmResponse.ProviderName);

            // Ajustar orderIndex para continuar desde las tareas existentes
            int baseIndex = project.Tasks.Count;
            for (int i = 0; i < suggestions.Count; i++)
                suggestions[i].OrderIndex = baseIndex + i + 1;

            _logger.LogSuggestTasksCompleted(projectId, suggestions.Count, llmResponse.ProviderName);

            return suggestions;
        }

        // ============================================================================
        // CONSTRUCCIÓN DE MENSAJES (prompt de usuario)
        // ============================================================================

        private static string BuildProjectGenerationMessage(GenerateProjectRequestDto request)
        {
            var sb = new StringBuilder();

            sb.AppendLine("PROJECT DESCRIPTION:");
            sb.AppendLine(request.Description);
            sb.AppendLine();
            sb.AppendLine($"CONSTRAINTS:");
            sb.AppendLine($"- Maximum tasks: {request.MaxTasks}");
            sb.AppendLine($"- Detail level: {request.DetailLevel}");

            if (!string.IsNullOrWhiteSpace(request.Language))
                sb.AppendLine($"- Response language: {request.Language}");

            if (request.DetailLevel == "detailed")
                sb.AppendLine("- For each task, include acceptance criteria in the description field.");

            return sb.ToString();
        }

        private static string BuildTaskSuggestionMessage(Models.Project project)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"PROJECT NAME: {project.Name}");
            sb.AppendLine($"PROJECT DESCRIPTION: {project.Description ?? "(no description)"}");
            sb.AppendLine($"PROJECT STATUS: {project.Status}");
            sb.AppendLine();
            sb.AppendLine("EXISTING TASKS (DO NOT DUPLICATE THESE):");

            if (!project.Tasks.Any())
            {
                sb.AppendLine("(no tasks yet — suggest a complete initial set)");
            }
            else
            {
                foreach (var task in project.Tasks.OrderBy(t => t.Title))
                {
                    sb.AppendLine($"- [{task.Priority ?? "Medium"}] {task.Title}");
                    if (!string.IsNullOrWhiteSpace(task.Description))
                        sb.AppendLine($"  {task.Description}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Suggest only tasks that are MISSING from this project. Maximum 10 suggestions.");

            return sb.ToString();
        }

        // ============================================================================
        // PARSEO Y SANITIZACIÓN DE JSON
        // ============================================================================

        /// <summary>
        /// Extrae y sanitiza el JSON de la respuesta del LLM.
        /// Los LLMs a veces envuelven el JSON en backticks o añaden texto antes/después.
        /// </summary>
        private static string SanitizeJson(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return rawText;

            var text = rawText.Trim();

            // Eliminar bloques de código markdown (```json ... ``` o ``` ... ```)
            if (text.StartsWith("```"))
            {
                var firstNewline = text.IndexOf('\n');
                if (firstNewline > 0)
                    text = text[(firstNewline + 1)..];

                var lastFence = text.LastIndexOf("```");
                if (lastFence > 0)
                    text = text[..lastFence];

                text = text.Trim();
            }

            // Buscar el primer { o [ y el último } o ] para extraer solo el JSON
            int jsonStart = text.IndexOfAny(new[] { '{', '[' });
            int jsonEnd   = Math.Max(text.LastIndexOf('}'), text.LastIndexOf(']'));

            if (jsonStart >= 0 && jsonEnd > jsonStart)
                text = text[jsonStart..(jsonEnd + 1)];

            return text.Trim();
        }

        private AIGeneratedProjectDto ParseProjectJson(string rawText, string providerName)
        {
            var sanitized = SanitizeJson(rawText);

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
                };

                var raw = JsonSerializer.Deserialize<RawLLMProject>(sanitized, options)
                    ?? throw new JsonException("La deserialización retornó null.");

                // Mapear al DTO de salida
                return new AIGeneratedProjectDto
                {
                    Name        = (raw.Name ?? string.Empty).Trim(),
                    Description = raw.Description?.Trim(),
                    Status      = NormalizeProjectStatus(raw.Status),
                    Tasks       = (raw.Tasks ?? new List<RawLLMTask>())
                        .Select((t, i) => new AIGeneratedTaskDto
                        {
                            Title              = (t.Title ?? string.Empty).Trim(),
                            Description        = t.Description?.Trim(),
                            Priority           = NormalizeTaskPriority(t.Priority),
                            DueDateOffsetDays  = t.DueDateOffsetDays,
                            OrderIndex         = t.OrderIndex > 0 ? t.OrderIndex : i + 1
                        })
                        .ToList()
                };
            }
            catch (JsonException ex)
            {
                _logger.LogParseError(providerName, "GenerateProject", sanitized);

                throw new LLMParseException(providerName, sanitized, ex);
            }
        }

        private List<AIGeneratedTaskDto> ParseTaskListJson(string rawText, string providerName)
        {
            var sanitized = SanitizeJson(rawText);

            // Manejar el caso donde el LLM devuelve un objeto vacío {} en lugar de []
            if (sanitized == "{}" || sanitized == "[]")
                return new List<AIGeneratedTaskDto>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // La respuesta puede ser un array directo o un objeto con una propiedad "tasks"
                if (sanitized.TrimStart().StartsWith('['))
                {
                    var rawList = JsonSerializer.Deserialize<List<RawLLMTask>>(sanitized, options)
                        ?? new List<RawLLMTask>();

                    return rawList.Select((t, i) => new AIGeneratedTaskDto
                    {
                        Title             = (t.Title ?? string.Empty).Trim(),
                        Description       = t.Description?.Trim(),
                        Priority          = NormalizeTaskPriority(t.Priority),
                        DueDateOffsetDays = t.DueDateOffsetDays,
                        OrderIndex        = t.OrderIndex > 0 ? t.OrderIndex : i + 1
                    }).ToList();
                }
                else
                {
                    // Intentar deserializar como objeto con propiedad "tasks"
                    var wrapper = JsonSerializer.Deserialize<RawLLMProject>(sanitized, options);
                    var tasks   = wrapper?.Tasks ?? new List<RawLLMTask>();

                    return tasks.Select((t, i) => new AIGeneratedTaskDto
                    {
                        Title             = (t.Title ?? string.Empty).Trim(),
                        Description       = t.Description?.Trim(),
                        Priority          = NormalizeTaskPriority(t.Priority),
                        DueDateOffsetDays = t.DueDateOffsetDays,
                        OrderIndex        = t.OrderIndex > 0 ? t.OrderIndex : i + 1
                    }).ToList();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogParseError(providerName, "SuggestTasks", sanitized);

                throw new LLMParseException(providerName, sanitized, ex);
            }
        }

        // ============================================================================
        // NORMALIZACIÓN DE VALORES
        // ============================================================================

        private static string NormalizeProjectStatus(string? status) =>
            status?.Trim() switch
            {
                "InProgress" or "in_progress" or "In Progress" => "InProgress",
                "Completed"  or "completed"                     => "Completed",
                "Archived"   or "archived"                      => "Archived",
                _                                               => "OnHold"
            };

        private static string NormalizeTaskPriority(string? priority) =>
            priority?.Trim().ToLower() switch
            {
                "high"   or "alta"   or "alto"   => "High",
                "low"    or "baja"   or "bajo"   => "Low",
                _                                 => "Medium"
            };

        // ============================================================================
        // CARGA DE PROMPTS (lazy, thread-safe, cacheado en memoria)
        // ============================================================================

        private static string GetProjectGenerationPrompt()
        {
            if (_projectGenerationPrompt != null) return _projectGenerationPrompt;
            lock (_promptLock)
            {
                _projectGenerationPrompt ??= LoadPrompt("ProjectGenerationPrompt.txt");
                return _projectGenerationPrompt;
            }
        }

        private static string GetTaskSuggestionPrompt()
        {
            if (_taskSuggestionPrompt != null) return _taskSuggestionPrompt;
            lock (_promptLock)
            {
                _taskSuggestionPrompt ??= LoadPrompt("TaskSuggestionPrompt.txt");
                return _taskSuggestionPrompt;
            }
        }

        private static string LoadPrompt(string fileName)
        {
            // Busca el archivo en Prompts/ relativo al directorio de la aplicación
            var baseDir = AppContext.BaseDirectory;
            var path    = Path.Combine(baseDir, "Prompts", fileName);

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"Prompt file not found: {path}. " +
                    "Asegúrate de que el archivo está en la carpeta 'Prompts/' y tiene Build Action = Content / Copy Always.");

            return File.ReadAllText(path, System.Text.Encoding.UTF8);
        }

        // ============================================================================
        // MODELOS INTERNOS PARA DESERIALIZAR LA RESPUESTA DEL LLM
        // ============================================================================

        private class RawLLMProject
        {
            public string?          Name        { get; set; }
            public string?          Description { get; set; }
            public string?          Status      { get; set; }
            public List<RawLLMTask>? Tasks       { get; set; }
        }

        private class RawLLMTask
        {
            public string? Title             { get; set; }
            public string? Description       { get; set; }
            public string? Priority          { get; set; }
            public int?    DueDateOffsetDays { get; set; }
            public int     OrderIndex        { get; set; }
        }
    }
}
