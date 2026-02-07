using System.Threading.Tasks;
using EntityTask = gestion_de_proyectos.Models.Task;
namespace gestion_de_proyectos.Repositories
{
    public interface ITaskRepository
    {
        // Obtener una tarea por su Id
        Task<EntityTask?> GetByIdAsync(Guid id);

        // Obtener todas las tareas (o scoped by ProjectId, pero mantendremos general)
        Task<IQueryable<EntityTask>> GetAllAsync();

        // CRUD esencial
        Task AddAsync(EntityTask task);
        void Update(EntityTask task);
        void Delete(EntityTask task);

        // Método para guardar los cambios en la base de datos
        Task<bool> SaveChangesAsync();

        // Verificar la existencia de una tarea (útil para el servicio)
        Task<bool> ExistsAsync(Guid id);
    }
}
