
using Microsoft.EntityFrameworkCore;
using EntityTask = gestion_de_proyectos.Models.Task;

namespace gestion_de_proyectos.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Obtiene una tarea por ID, incluyendo las relaciones necesarias
        public async Task<EntityTask?> GetByIdAsync(Guid id)
        {
            return await _context.Tasks
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        // Devuelve una IQueryable para que el Service pueda filtrar por ProjectId
        public Task<IQueryable<EntityTask>> GetAllAsync()
        {
            return Task.FromResult(_context.Tasks.AsQueryable());
        }

        // Agrega una tarea
        public async Task AddAsync(EntityTask task)
        {
            await _context.Tasks.AddAsync(task);
        }

        // Marca el objeto como modificado
        public void Update(EntityTask task)
        {
            // Se asume que el objeto ya está siendo rastreado o se usa el Attach/Modified
        }

        // Elimina una tarea
        public void Delete(EntityTask task)
        {
            _context.Tasks.Remove(task);
        }

        // Guarda los cambios pendientes en la base de datos
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        // Verifica la existencia de una tarea
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Tasks.AnyAsync(t => t.Id == id);
        }
    }

}
