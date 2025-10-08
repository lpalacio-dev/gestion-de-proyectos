using gestion_de_proyectos.Models;

namespace gestion_de_proyectos.Repositories
{
    public interface IProjectRepository
    {
        Task<Project?> GetByIdAsync(Guid id);
        Task<IEnumerable<Project>> GetAllAsync();
        Task<Project?> AddAsync(Project project);
        void Update(Project project);
        void Delete(Project project);
        Task<bool> SaveChangesAsync();

    }
}
