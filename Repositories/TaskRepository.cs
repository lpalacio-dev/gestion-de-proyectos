
using Microsoft.EntityFrameworkCore;
using TaskModelo = gestion_de_proyectos.Models.Task;

namespace gestion_de_proyectos.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<TaskModelo> GetTasksWithIncludes()
        {
            // Cláusula CRÍTICA: Carga eager de las entidades relacionadas
            return _context.Tasks
                .Include(t => t.Project)       // Carga el proyecto al que pertenece
                .Include(t => t.AssignedUser); // Carga el usuario asignado
        }

        public async Task<IEnumerable<TaskModelo>> GetAllAsync()
        {
            // Usamos la función auxiliar para asegurar las inclusiones
            return await GetTasksWithIncludes().ToListAsync();
        }

        public async Task<TaskModelo?> GetByIdAsync(Guid id)
        {
            // Usamos la función auxiliar para asegurar las inclusiones
            return await GetTasksWithIncludes()
                         .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(TaskModelo task)
        {
            await _context.Tasks.AddAsync(task);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TaskModelo task)
        {
            // En EF Core, adjuntar y marcar como modificado es común para updates completos
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Tasks.AnyAsync(t => t.Id == id);
        }


    }
}
