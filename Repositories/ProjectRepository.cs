using Microsoft.EntityFrameworkCore;
using gestion_de_proyectos.Models;
using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Obtiene un proyecto por ID, incluyendo relaciones
        public async Task<Project?> GetByIdAsync(Guid id)
        {
            return await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.ProjectMembers).ThenInclude(pm => pm.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // Devuelve una IQueryable para permitir filtrado en la capa de servicio
        public Task<IQueryable<Project>> GetAllAsync()
        {
            // Nota: No se llama a ToListAsync() aquí. 
            // Esto permite que el Service filtre y luego ejecute la consulta con ToListAsync.
            return Task.FromResult(_context.Projects.AsQueryable());
        }

        // Agrega un proyecto al contexto
        public async Task AddAsync(Project project)
        {
            await _context.Projects.AddAsync(project);
        }

        // Marca el objeto como modificado (si ya está en el contexto, no se necesita Attach)
        public Task UpdateAsync(Project project)
        {
            // EF Core rastrea automáticamente los cambios si el objeto fue recuperado del contexto
            // o se usa: _context.Entry(project).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        // Elimina un proyecto por ID
        public async Task DeleteAsync(Guid id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                _context.Projects.Remove(project);
            }
        }

        // Guarda los cambios pendientes en la base de datos
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Verifica si un proyecto existe
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Projects.AnyAsync(p => p.Id == id);
        }

    }
}
