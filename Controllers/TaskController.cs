// Controllers/TaskController.cs
using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gestion_de_proyectos.Controllers
{
    // Seguridad de Roles: Autoriza a usuarios con roles de Admin, ProjectManager o Member
    [Authorize(Roles = "Admin, ProjectManager, Member")]
    [Route("api/projects/{projectId:guid}/tasks")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        // GET: api/projects/{projectId}/tasks
        // La autorización de contexto (acceso al proyecto) se hace en el Servicio.
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TaskDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks(Guid projectId)
        {
            try
            {
                var tasks = await _taskService.GetAllTasksAsync(projectId);
                return Ok(tasks);
            }
            catch (NotFoundException)
            {
                return NotFound(); // Proyecto no encontrado
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid(); // 403 Forbidden: No es miembro del proyecto
            }
        }

        // GET: api/projects/{projectId}/tasks/{taskId}
        // El {projectId} en la ruta es redundante aquí, pero ayuda a la estructura RESTful.
        // Usaremos el taskId para la operación principal.
        [HttpGet("{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TaskDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TaskDto>> GetTask(Guid taskId)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(taskId);
                return Ok(task);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid(); // 403 Forbidden: No es miembro del proyecto padre
            }
        }

        // POST: api/projects/{projectId}/tasks
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TaskDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TaskDto>> CreateTask(Guid projectId, CreateTaskDto dto)
        {
            try
            {
                var taskDto = await _taskService.CreateTaskAsync(projectId, dto);

                // Retorna la URI del recurso recién creado. Se asume que la ruta es simple.
                return CreatedAtAction(nameof(GetTask),
                    new { projectId = projectId, taskId = taskDto.Id },
                    taskDto);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid(); // 403 Forbidden: No tiene permiso para crear en este proyecto
            }
        }

        // PUT: api/projects/{projectId}/tasks/{taskId}
        [HttpPut("{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateTask(Guid taskId, UpdateTaskDto dto)
        {
            try
            {
                await _taskService.UpdateTaskAsync(taskId, dto);
                return NoContent();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // DELETE: api/projects/{projectId}/tasks/{taskId}
        [HttpDelete("{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteTask(Guid taskId)
        {
            try
            {
                await _taskService.DeleteTaskAsync(taskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}