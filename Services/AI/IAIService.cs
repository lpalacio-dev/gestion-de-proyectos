using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.DTOs.AI;

namespace gestion_de_proyectos.Services.AI
{
    /// <summary>
    /// Servicio de alto nivel que expone las capacidades de IA al dominio de la aplicación.
    /// Entiende los conceptos de Proyecto y Tarea, y orquesta el flujo completo:
    ///   1. Construir el prompt con el contexto del usuario
    ///   2. Llamar a FallbackLLMService para obtener la respuesta
    ///   3. Parsear y validar el JSON generado
    ///   4. (Opcional) Persistir a través de los servicios existentes
    ///
    /// Este servicio es el único punto de entrada del módulo de IA para el controlador.
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Genera una sugerencia de proyecto completo (con tareas) a partir de una descripción en lenguaje natural.
        ///
        /// IMPORTANTE: Este método NO persiste nada en la base de datos.
        /// Retorna la sugerencia para que el usuario la revise y edite en el frontend.
        /// La persistencia ocurre en ConfirmAndPersistProjectAsync.
        /// </summary>
        /// <param name="request">Descripción del usuario y parámetros de generación.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>
        /// Proyecto sugerido con nombre, descripción, estado y lista de tareas ordenadas.
        /// </returns>
        /// <exception cref="LLMUnavailableException">Si todos los proveedores LLM están caídos.</exception>
        /// <exception cref="LLMParseException">Si la respuesta del LLM no tiene el formato JSON esperado.</exception>
        /// <exception cref="ArgumentException">Si la descripción es demasiado corta o vacía.</exception>
        Task<AIGeneratedProjectDto> GenerateProjectAsync(GenerateProjectRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persiste el proyecto y las tareas confirmadas por el usuario.
        ///
        /// Flujo:
        ///   1. Crea el proyecto usando ProjectService (el usuario actual será Owner automáticamente).
        ///   2. Itera las tareas confirmadas y las crea con TaskService.
        ///   3. Retorna el ProjectDto completo del proyecto recién creado.
        ///
        /// NOTA: Si la creación del proyecto falla, no se intenta crear ninguna tarea.
        /// Si una tarea individual falla, se loggea el error pero el proyecto y las demás tareas se mantienen.
        /// </summary>
        /// <param name="dto">Proyecto y tareas confirmados (posiblemente editados) por el usuario.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>ProjectDto completo del proyecto recién creado en la base de datos.</returns>
        /// <exception cref="InvalidOperationException">Si el DTO tiene datos inválidos.</exception>
        Task<ProjectDto> ConfirmAndPersistProjectAsync(AIConfirmProjectDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sugiere tareas faltantes para un proyecto existente.
        ///
        /// La IA analiza el nombre, descripción y tareas ya existentes del proyecto,
        /// e identifica huecos lógicos en el plan para sugerir tareas complementarias.
        /// NO duplica tareas ya existentes.
        ///
        /// IMPORTANTE: Este método NO persiste nada. El usuario elige qué sugerencias aceptar.
        /// </summary>
        /// <param name="projectId">ID del proyecto existente al que se quieren agregar tareas.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Lista de tareas sugeridas, ordenadas por dependencia lógica.</returns>
        /// <exception cref="NotFoundException">Si el proyecto no existe.</exception>
        /// <exception cref="UnauthorizedAccessException">Si el usuario no tiene acceso al proyecto.</exception>
        /// <exception cref="LLMUnavailableException">Si todos los proveedores LLM están caídos.</exception>
        Task<IEnumerable<AIGeneratedTaskDto>> SuggestTasksForProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    }
}
