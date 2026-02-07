using gestion_de_proyectos.Models;
using Task = System.Threading.Tasks.Task;
namespace gestion_de_proyectos.Repositories
{
    public interface IProjectRepository
    {
        // Obtiene un proyecto por su ID. Retorna null si no existe.
        Task<Project?> GetByIdAsync(Guid id);

        // Obtiene una colección consultable de todos los proyectos (útil para filtrado posterior).
        // NOTA: Se debe tener cuidado al usar IQueryable; si es demasiado grande, se debe limitar.
        Task<IQueryable<Project>> GetAllAsync();

        // Agrega un nuevo proyecto.
        Task AddAsync(Project project);

        // Actualiza un proyecto existente.
        Task UpdateAsync(Project project);

        // Elimina un proyecto por su ID (o pasando el objeto Project).
        Task DeleteAsync(Guid id);

        // Guarda los cambios en la base de datos (patrón Unit of Work simplificado).
        Task<int> SaveChangesAsync();
        Task<bool> ExistsAsync(Guid id);

    }
}
