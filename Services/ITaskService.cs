using gestion_de_proyectos.DTOs;

namespace gestion_de_proyectos.Services
{
    public interface ITaskService
    {
        // CRUD estándar
        Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync();
        Task<TaskResponseDto?> GetTaskByIdAsync(Guid id);

        // Operaciones que incluyen validación de relaciones
        Task<TaskResponseDto> CreateTaskAsync(TaskCreationDto dto);
        Task<TaskResponseDto?> UpdateTaskAsync(Guid id, TaskUpdateDto dto);

        Task<bool> DeleteTaskAsync(Guid id);
    }
}
