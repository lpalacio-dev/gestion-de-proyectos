using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Services
{
    public interface ITaskService
    {

        // Recupera tareas.Lo ideal es filtrar por proyecto (ej. /api/projects/{ projectId}/tasks)
        Task<IEnumerable<TaskDto>> GetAllTasksAsync(Guid projectId);

        // Crea una tarea. Necesita el projectId para asegurar el contexto y la ruta del API
        Task<TaskDto> CreateTaskAsync(Guid projectId, CreateTaskDto dto);

        // Obtiene una tarea, verificando que el usuario tenga acceso al ProjectId asociado
        Task<TaskDto> GetTaskByIdAsync(Guid taskId);

        // Actualiza una tarea.
        Task UpdateTaskAsync(Guid taskId, UpdateTaskDto dto);

        // Elimina una tarea.
        Task DeleteTaskAsync(Guid taskId);
    }
}
