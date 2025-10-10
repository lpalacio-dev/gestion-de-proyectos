using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace gestion_de_proyectos.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        // ----------------------------------------------------------------
        // POST: api/Tasks
        // ----------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CreateTask(TaskCreationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdTask = await _taskService.CreateTaskAsync(dto);

                // 201 Created: Devuelve la tarea recién creada y la ubicación del recurso
                return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
            }
            // Captura las excepciones de lógica de negocio (por IDs de Project/User no válidos)
            catch (InvalidOperationException ex)
            {
                // Traduce el error de negocio a 400 Bad Request
                return BadRequest(new { error = ex.Message });
            }
        }

        // ----------------------------------------------------------------
        // GET: api/Tasks
        // ----------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetAllTasks()
        {
            var tasks = await _taskService.GetAllTasksAsync();
            return Ok(tasks); // 200 OK
        }

        // ----------------------------------------------------------------
        // GET: api/Tasks/{id}
        // ----------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskResponseDto>> GetTaskById(Guid id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);

            if (task == null)
            {
                return NotFound(); // 404 Not Found
            }

            return Ok(task); // 200 OK
        }

        // ----------------------------------------------------------------
        // PUT: api/Tasks/{id}
        // ----------------------------------------------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(Guid id, TaskUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedTask = await _taskService.UpdateTaskAsync(id, dto);

                if (updatedTask == null)
                {
                    return NotFound(); // 404 Not Found (la tarea con 'id' no existe)
                }

                return Ok(updatedTask); // 200 OK
            }
            catch (InvalidOperationException ex)
            {
                // Captura IDs de Project/User no válidos en la actualización
                return BadRequest(new { error = ex.Message }); // 400 Bad Request
            }
        }

        // ----------------------------------------------------------------
        // DELETE: api/Tasks/{id}
        // ----------------------------------------------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var success = await _taskService.DeleteTaskAsync(id);

            if (!success)
            {
                return NotFound(); // 404 Not Found
            }

            return NoContent(); // 204 No Content (eliminación exitosa sin cuerpo de respuesta)
        }
    }
}
