// Services/ProjectService.cs - FASE 4: Autorización Mejorada
using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using gestion_de_proyectos.Repositories;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _dbContext;

        public ProjectService(
            IProjectRepository projectRepository,
            IUserContextAccessor userContextAccessor,
            IProjectAuthorizationService authorizationService,
            IMapper mapper,
            ApplicationDbContext dbContext)
        {
            _projectRepository = projectRepository;
            _userContextAccessor = userContextAccessor;
            _authorizationService = authorizationService;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        // ============================================================================
        // IMPLEMENTACIÓN DE MÉTODOS
        // ============================================================================

        public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();
            var isAdmin = _authorizationService.IsGlobalAdmin(currentUserId);

            var queryable = await _projectRepository.GetAllAsync();

            if (!isAdmin)
            {
                // Filtra para mostrar solo los proyectos donde el usuario es Owner o Miembro
                queryable = queryable.Where(p =>
                    p.OwnerId == currentUserId ||
                    p.ProjectMembers.Any(pm => pm.UserId == currentUserId));
            }

            var projects = await queryable
                .Include(p => p.ProjectMembers)
                .Include(p => p.Owner)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProjectDto>>(projects);
        }

        public async Task<ProjectDto> GetProjectByIdAsync(Guid id)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // FASE 4: Usar servicio de autorización centralizado
            await _authorizationService.ValidateProjectAccessAsync(currentUserId, id);

            var project = await _projectRepository.GetByIdAsync(id);

            // Si llegamos aquí, el proyecto existe y el usuario tiene acceso
            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto)
        {
            var project = _mapper.Map<Project>(dto);

            // Lógica de Negocio: Asignar OwnerId y CreationDate
            var currentUserId = _userContextAccessor.GetCurrentUserId();
            project.OwnerId = currentUserId;
            project.CreationDate = DateTime.UtcNow;

            await _projectRepository.AddAsync(project);
            await _projectRepository.SaveChangesAsync();

            // Auto-agregar al Owner como miembro del proyecto con rol "Owner"
            var ownerMembership = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = currentUserId,
                Role = "Owner",
                JoinedDate = DateTime.UtcNow
            };

            _dbContext.ProjectMembers.Add(ownerMembership);
            await _dbContext.SaveChangesAsync();

            // Recargar el proyecto con todas las relaciones para el DTO
            var createdProject = await _projectRepository.GetByIdAsync(project.Id);
            return _mapper.Map<ProjectDto>(createdProject);
        }

        public async Task UpdateProjectAsync(Guid id, UpdateProjectDto dto)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // FASE 4: Validar permiso para modificar
            await _authorizationService.ValidateCanModifyProjectAsync(currentUserId, id);

            var project = await _projectRepository.GetByIdAsync(id);

            // Si llegamos aquí, el usuario tiene permiso
            _mapper.Map(dto, project);

            await _projectRepository.UpdateAsync(project);
            await _projectRepository.SaveChangesAsync();
        }

        public async Task DeleteProjectAsync(Guid id)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // Verificar que el proyecto existe
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                // Idempotencia: no hacer nada si no existe
                return;
            }

            // FASE 4: Validar permiso para eliminar
            // ValidateCanModifyProjectAsync verifica Owner o Admin global
            await _authorizationService.ValidateCanModifyProjectAsync(currentUserId, id);

            await _projectRepository.DeleteAsync(id);
            await _projectRepository.SaveChangesAsync();
        }
    }
}