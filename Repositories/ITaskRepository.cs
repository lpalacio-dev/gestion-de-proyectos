// ¡Nuevo alias de using!
using TaskModelo = gestion_de_proyectos.Models.Task;


namespace gestion_de_proyectos.Repositories
{
    public interface ITaskRepository
    {
        // Obtener todas las tareas, incluyendo Project y AssignedUser para el DTO de respuesta
        Task<IEnumerable<TaskModelo>> GetAllAsync();

        // Obtener una tarea por ID, incluyendo Project y AssignedUser
        Task<TaskModelo?> GetByIdAsync(Guid id);

        // Agregar una nueva tarea
        Task AddAsync(TaskModelo task);

        // Actualizar una tarea existente
        Task UpdateAsync(TaskModelo task);

        // Eliminar una tarea por ID
        Task DeleteAsync(Guid id);

        // Verificar la existencia de una tarea (útil para el servicio)
        Task<bool> ExistsAsync(Guid id);
    }
}
