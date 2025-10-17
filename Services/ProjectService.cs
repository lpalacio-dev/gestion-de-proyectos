// Services/ProjectService.cs
using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using gestion_de_proyectos.Repositories;
using AutoMapper; // Asumimos AutoMapper para mapeo DTO <-> Modelo
using Microsoft.EntityFrameworkCore; // Necesario para consultas IQueryable
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Services
{
    // Excepciones personalizadas para manejar respuestas HTTP
    public class NotFoundException : Exception { public NotFoundException(string message) : base(message) { } }


    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly IMapper _mapper;

        // El plan menciona ApplicationDbContext para ProjectMember, lo inyectamos directamente:
        private readonly ApplicationDbContext _dbContext;

        public ProjectService(
            IProjectRepository projectRepository,
            IUserContextAccessor userContextAccessor,
            IMapper mapper,
            ApplicationDbContext dbContext)
        {
            _projectRepository = projectRepository;
            _userContextAccessor = userContextAccessor;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        // --- Lógica de Autorización ---
        private async Task<bool> IsUserOwnerOrAdmin(string userId, Guid projectId)
        {
            if (_userContextAccessor.IsUserInRole("Admin")) return true;

            var project = await _projectRepository.GetByIdAsync(projectId);
            return project?.OwnerId == userId;
        }

        private async Task<bool> IsUserProjectMember(string userId, Guid projectId)
        {
            // Se usa el DbSet directamente como sugirió el plan (si no hay ProjectMemberRepository)
            return await _dbContext.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        }

        // --- Implementación de IProjectService ---

        public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();
            var isAdmin = _userContextAccessor.IsUserInRole("Admin");

            var queryable = await _projectRepository.GetAllAsync();

            if (!isAdmin)
            {
                // Filtra para mostrar solo los proyectos donde el usuario es Owner o Miembro
                queryable = queryable.Where(p =>
                    p.OwnerId == currentUserId ||
                    p.ProjectMembers.Any(pm => pm.UserId == currentUserId));
            }

            var projects = await queryable
                .Include(p => p.ProjectMembers) // Aseguramos que se carguen los miembros para el DTO
                .Include(p => p.Owner) // Aseguramos el Owner para el DTO
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProjectDto>>(projects);
        }

        public async Task<ProjectDto> GetProjectByIdAsync(Guid id)
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                throw new NotFoundException($"Project with Id {id} not found.");
            }

            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // Autorización: Admin, Owner o ProjectMember
            if (!await IsUserOwnerOrAdmin(currentUserId, id) &&
                !await IsUserProjectMember(currentUserId, id))
            {
                throw new UnauthorizedAccessException("Access denied. User is not the owner, admin, or a member of this project.");
            }

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto)
        {
            var project = _mapper.Map<Project>(dto);

            // Lógica de Negocio: Asignar OwnerId y CreationDate
            project.OwnerId = _userContextAccessor.GetCurrentUserId();
            project.CreationDate = DateTime.UtcNow;

            await _projectRepository.AddAsync(project);
            await _projectRepository.SaveChangesAsync();

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task UpdateProjectAsync(Guid id, UpdateProjectDto dto)
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                throw new NotFoundException($"Project with Id {id} not found.");
            }

            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // Autorización: Solo el Owner o un Admin puede actualizar los detalles del proyecto
            if (!await IsUserOwnerOrAdmin(currentUserId, id))
            {
                throw new UnauthorizedAccessException("Access denied. Only the project owner or an admin can update project details.");
            }

            // Mapear los campos actualizables del DTO al modelo
            _mapper.Map(dto, project);

            await _projectRepository.UpdateAsync(project);
            await _projectRepository.SaveChangesAsync();
        }

        public async Task DeleteProjectAsync(Guid id)
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                // Si no se encuentra, retornamos sin error si el endpoint es DELETE (idempotencia)
                return;
            }

            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // Autorización: Solo el Owner o un Admin puede eliminar el proyecto
            if (!await IsUserOwnerOrAdmin(currentUserId, id))
            {
                throw new UnauthorizedAccessException("Access denied. Only the project owner or an admin can delete this project.");
            }

            await _projectRepository.DeleteAsync(id);
            await _projectRepository.SaveChangesAsync();
        }
    }
}