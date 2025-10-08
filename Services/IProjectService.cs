using gestion_de_proyectos.DTOs;

namespace gestion_de_proyectos.Services
{
    public interface IProjectService
    {
        Task<ProjectResponseDto?> CreateProjectAsync(ProjectCreationDto projectDto);
        Task<ProjectResponseDto?> GetProjectByIdAsync(Guid id);
        Task<IEnumerable<ProjectResponseDto>> GetAllProjectsAsync();

        Task<bool> UpdateProjectAsync(Guid id, ProjectUpdateDto projectDto);

        Task<bool> DeleteProjectAsync(Guid id);
    }
}
