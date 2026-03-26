using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.DTOs.AI;
using gestion_de_proyectos.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestion_de_proyectos.Controllers
{
    /// <summary>
    /// Controlador del módulo de IA.
    /// Expone tres endpoints:
    ///   POST /api/ai/generate-project  → genera una sugerencia (no persiste)
    ///   POST /api/ai/confirm-project   → persiste la sugerencia confirmada por el usuario
    ///   GET  /api/ai/suggest-tasks/{projectId} → sugiere tareas faltantes para un proyecto existente
    /// </summary>
    [Authorize(Roles = "User")]
    [Route("api/ai")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIController> _logger;

        public AIController(IAIService aiService, ILogger<AIController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        // ============================================================================
        // POST /api/ai/generate-project
        // ============================================================================

        /// <summary>
        /// Genera una sugerencia de proyecto completo a partir de una descripción en lenguaje natural.
        /// NO persiste nada en la base de datos. El usuario debe revisar y confirmar con /confirm-project.
        /// </summary>
        /// <param name="dto">Descripción del proyecto y parámetros opcionales de generación.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Proyecto sugerido con nombre, descripción, estado y lista de tareas ordenadas.</returns>
        [HttpPost("generate-project")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AIGeneratedProjectDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<AIGeneratedProjectDto>> GenerateProject(
            [FromBody] GenerateProjectRequestDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _aiService.GenerateProjectAsync(dto, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (LLMParseException ex)
            {
                _logger.LogError(ex, "[AIController] Error de parseo en generate-project.");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    Message = "El modelo de IA no respondió en el formato esperado. Intenta de nuevo.",
                    Provider = ex.ProviderName
                });
            }
            catch (LLMUnavailableException ex)
            {
                _logger.LogError(ex, "[AIController] Todos los proveedores LLM fallaron en generate-project.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    Message        = "El servicio de IA no está disponible en este momento. Intenta en unos minutos.",
                    FailedProviders = ex.FailedProviders
                });
            }
        }

        // ============================================================================
        // POST /api/ai/confirm-project
        // ============================================================================

        /// <summary>
        /// Persiste el proyecto y las tareas confirmadas (y posiblemente editadas) por el usuario.
        /// El usuario actual se convierte automáticamente en Owner del proyecto.
        /// </summary>
        /// <param name="dto">Proyecto y tareas confirmados por el usuario.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>ProjectDto completo del proyecto recién creado.</returns>
        [HttpPost("confirm-project")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ProjectDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProjectDto>> ConfirmProject(
            [FromBody] AIConfirmProjectDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var projectDto = await _aiService.ConfirmAndPersistProjectAsync(dto, cancellationToken);

                return CreatedAtAction(
                    actionName:      nameof(ProjectController.GetProject),
                    controllerName:  "Project",
                    routeValues:     new { id = projectDto.Id },
                    value:           projectDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ============================================================================
        // GET /api/ai/suggest-tasks/{projectId}
        // ============================================================================

        /// <summary>
        /// Sugiere tareas faltantes para un proyecto existente.
        /// Analiza las tareas actuales y propone únicamente tareas complementarias, sin duplicar.
        /// NO persiste nada: el usuario decide qué sugerencias acepta desde el frontend.
        /// </summary>
        /// <param name="projectId">ID del proyecto al que se quieren agregar tareas.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Lista de tareas sugeridas ordenadas por dependencia lógica.</returns>
        [HttpGet("suggest-tasks/{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AIGeneratedTaskDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<IEnumerable<AIGeneratedTaskDto>>> SuggestTasks(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            try
            {
                var suggestions = await _aiService.SuggestTasksForProjectAsync(projectId, cancellationToken);
                return Ok(suggestions);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (LLMParseException ex)
            {
                _logger.LogError(ex, "[AIController] Error de parseo en suggest-tasks para proyecto {Id}.", projectId);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    Message  = "El modelo de IA no respondió en el formato esperado. Intenta de nuevo.",
                    Provider = ex.ProviderName
                });
            }
            catch (LLMUnavailableException ex)
            {
                _logger.LogError(ex, "[AIController] Todos los proveedores LLM fallaron en suggest-tasks.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    Message         = "El servicio de IA no está disponible en este momento. Intenta en unos minutos.",
                    FailedProviders = ex.FailedProviders
                });
            }
        }
    }
}
