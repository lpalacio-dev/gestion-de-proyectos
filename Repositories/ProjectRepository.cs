using Microsoft.EntityFrameworkCore;
using gestion_de_proyectos.Models;

namespace gestion_de_proyectos.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly ApplicationDbContext _context;

        // Constructor: Inyección de dependencia del ApplicationDbContext
        public ProjectRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ------------------------------------------------------------------
        // READ: Obtener un Proyecto por ID (incluyendo el Propietario)
        // ------------------------------------------------------------------
        public async Task<Project?> GetByIdAsync(Guid id)
        {
            // Usamos Include() para cargar la información del Owner (User) junto con el Project.
            // Esto es crucial para poder mapear el OwnerName en el DTO de respuesta.
            return await _context.Projects
                                 .Include(p => p.Owner)
                                 .FirstOrDefaultAsync(p => p.Id == id);

        }

        // ------------------------------------------------------------------
        // READ: Obtener todos los Proyectos (incluyendo Propietarios y Tareas)
        // ------------------------------------------------------------------

        public async Task<IEnumerable<Project>> GetAllAsync()
        {
            // Incluimos tanto el Owner como la colección de Tasks.
            // Se usa .AsNoTracking() porque solo estamos leyendo, mejorando el rendimiento.
            return await _context.Projects
                                 .Include(p => p.Owner)
                                 .Include(p => p.Tasks)
                                 .AsNoTracking()
                                 .ToListAsync();
        }

        // ------------------------------------------------------------------
        // CREATE: Agregar un nuevo Proyecto
        // ------------------------------------------------------------------
        public async Task<Project?> AddAsync(Project project)
        {
            await _context.Projects.AddAsync(project);
            return project; // Devolvemos el objeto, que tendrá su estado modificado por EF
        }

        // ------------------------------------------------------------------
        // UPDATE: Marcar el objeto como modificado
        // ------------------------------------------------------------------
        public void Update(Project project)
        {
            // EF Core rastrea la entidad, simplemente marcamos su estado como modificado
            _context.Projects.Update(project);
        }

        // ------------------------------------------------------------------
        // DELETE: Eliminar un Proyecto
        // ------------------------------------------------------------------
        public void Delete(Project project)
        {
            // EF Core elimina el proyecto (y potencialmente sus tareas si está configurado en cascada)
            _context.Projects.Remove(project);
        }

        // ------------------------------------------------------------------
        // Commit: Guardar todos los cambios pendientes en la base de datos
        // ------------------------------------------------------------------
        public async Task<bool> SaveChangesAsync()
        {
            // Devuelve true si se guardó al menos 1 cambio
            return await _context.SaveChangesAsync() >= 1;
        }


    }
}
