using gestion_de_proyectos.DTOs;
using System.Threading.Tasks;
namespace gestion_de_proyectos.Services
{
    public interface IProjectService
    {
        // Recupera todos los proyectos (filtrado de autorización debería aplicarse aquí)
        Task<IEnumerable<ProjectDto>> GetAllProjectsAsync();

        // Crea un proyecto, asignando el OwnerId y la CreationDate internamente
        Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto);

        // Obtiene un proyecto, aplicando autorización a nivel de recurso (miembro o propietario)
        Task<ProjectDto> GetProjectByIdAsync(Guid id);

        // Actualiza un proyecto, aplicando chequeos de autorización
        Task UpdateProjectAsync(Guid id, UpdateProjectDto dto);

        // Elimina un proyecto, aplicando chequeos de autorización (propietario o admin)
        Task DeleteProjectAsync(Guid id);
    }
}
