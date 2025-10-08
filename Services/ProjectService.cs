using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using gestion_de_proyectos.Repositories;

namespace gestion_de_proyectos.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository; // Necesario para validar el OwnerId

        // Constructor: Inyección de dependencias
        public ProjectService(IProjectRepository projectRepository, IUserRepository userRepository)
        {
            _projectRepository = projectRepository;
            _userRepository = userRepository;
        }

        public async Task<ProjectResponseDto?> CreateProjectAsync(ProjectCreationDto projectDto)
        {
            // 1. Lógica de Negocio/Validación: Asegurar que el OwnerId existe.
            var owner = await _userRepository.GetUserByIdAsync(projectDto.OwnerId);
            if( owner == null)
            {
                return null;
            }

            // 2. Mapeo: DTO de Creación (Input) a Entidad (Project)
            var newProject = new Project
            {
                Id = Guid.NewGuid(),
                Name = projectDto.Name,
                Description = projectDto.Description,
                OwnerId = projectDto.OwnerId,
                // El estado y la fecha de creación se establecen automáticamente en el modelo
                // Project.cs (Status = "OnHold", CreationDate = DateTime.UtcNow)
            };

            // 3. Persistencia
            await _projectRepository.AddAsync(newProject);
            if(!await _projectRepository.SaveChangesAsync())
            {
                return null; // Fallo al guardar en la base de datos
            }

            // 4. Mapeo: Entidad (Project) a DTO de Respuesta (Output)
            // Nota: Para este mapeo, necesitamos el objeto 'Owner' que EF no carga automáticamente
            // después de un Add. Por simplicidad, lo cargamos aquí.
            newProject.Owner = owner;

            return MapToResponseDto(newProject);
        }

        public async Task<ProjectResponseDto?> GetProjectByIdAsync(Guid id)
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if(project == null)
            {
                return null;
            }

            // Mapeo: Entidad (Project) a DTO de Respuesta
            return MapToResponseDto(project);
        }

        public async Task<IEnumerable<ProjectResponseDto>> GetAllProjectsAsync()
        {
            var projects = await _projectRepository.GetAllAsync();

            // Mapeo: Colección de Entidades a Colección de DTOs
            return projects.Select(p => MapToResponseDto(p)).ToList();
        }

        public async Task<bool> UpdateProjectAsync(Guid id, ProjectUpdateDto projectDto)
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                return false;
            }

            // Mapeo: Aplicar cambios del DTO de Actualización al objeto Entity
            if (!string.IsNullOrEmpty(projectDto.Name))
                project.Name = projectDto.Name;

            if (!string.IsNullOrEmpty(projectDto.Description))
                project.Description = projectDto.Description;

            if (!string.IsNullOrEmpty(projectDto.Status))
            {
                // Lógica de validación del Status (Asegurarse que el string es un valor válido del Enum)
                if (Enum.TryParse<ProjectStatus>(projectDto.Status, true, out var newStatus))
                {
                    project.Status = newStatus.ToString();
                }
                else
                {
                    // Manejo de error: Estado inválido
                    // Podrías lanzar una excepción o devolver un error más específico.
                    return false;
                }
            }

            // Opcional: Reasignar propietario (Validar si el nuevo OwnerId existe)
            if (projectDto.OwnerId.HasValue && project.OwnerId != projectDto.OwnerId.Value)
            {
                var newOwner = await _userRepository.GetUserByIdAsync(projectDto.OwnerId.Value);
                if (newOwner != null)
                {
                    project.OwnerId = projectDto.OwnerId.Value;
                }
                else
                {
                    // Nuevo propietario no existe
                    return false;
                }
            }

            _projectRepository.Update(project);
            return await _projectRepository.SaveChangesAsync();

        }

        public async Task<bool> DeleteProjectAsync(Guid id)
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                return false;
            }

            _projectRepository.Delete(project);
            return await _projectRepository.SaveChangesAsync();
        }

        // ------------------------------------------------------------------
        // Helper: Mapeo de Entidad a DTO de Respuesta
        // ------------------------------------------------------------------
        private ProjectResponseDto MapToResponseDto(Project project)
        {
            return new ProjectResponseDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status,
                CreationDate = project.CreationDate,

                // Aseguramos que el objeto Owner esté cargado antes de acceder a Name
                OwnerId = project.OwnerId,
                OwnerName = project.Owner?.Name ?? "Unknown Owner",

                // Usamos la colección de navegación Tasks para contar las tareas
                TaskCount = project.Tasks?.Count ?? 0
            };
        }
    }
}
