using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using gestion_de_proyectos.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Services
{
    public class ProjectMemberService : IProjectMemberService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public ProjectMemberService(
            ApplicationDbContext dbContext,
            IProjectRepository projectRepository,
            IUserContextAccessor userContextAccessor,
            IProjectAuthorizationService authorizationService,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _projectRepository = projectRepository;
            _userContextAccessor = userContextAccessor;
            _authorizationService = authorizationService;
            _userManager = userManager;
            _mapper = mapper;
        }

        // ============================================================================
        // IMPLEMENTACIÓN DE MÉTODOS DEL SERVICIO
        // ============================================================================

        public async Task<IEnumerable<ProjectMemberDto>> GetProjectMembersAsync(Guid projectId)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // FASE 4: Validar acceso al proyecto
            await _authorizationService.ValidateProjectAccessAsync(currentUserId, projectId);

            var members = await _dbContext.ProjectMembers
                .Include(pm => pm.User)
                .Include(pm => pm.Project)
                .Where(pm => pm.ProjectId == projectId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProjectMemberDto>>(members);
        }

        public async Task<ProjectMemberDto> GetProjectMemberAsync(Guid projectId, string userId)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // FASE 4: Validar acceso al proyecto
            await _authorizationService.ValidateProjectAccessAsync(currentUserId, projectId);

            var member = await _dbContext.ProjectMembers
                .Include(pm => pm.User)
                .Include(pm => pm.Project)
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

            if (member == null)
            {
                throw new NotFoundException($"Usuario {userId} no es miembro del proyecto {projectId}.");
            }

            return _mapper.Map<ProjectMemberDto>(member);
        }

        public async Task<ProjectMemberDto> AddProjectMemberAsync(Guid projectId, AddProjectMemberDto dto)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // FASE 4: Validar que el usuario puede gestionar miembros
            await _authorizationService.ValidateCanManageMembersAsync(currentUserId, projectId);

            // Buscar el usuario a agregar (por UserId o Username)
            ApplicationUser? userToAdd = await _userManager.FindByIdAsync(dto.UserIdentifier);

            if (userToAdd == null)
            {
                // Intentar buscar por Username
                userToAdd = await _userManager.FindByNameAsync(dto.UserIdentifier);
            }

            if (userToAdd == null)
            {
                throw new NotFoundException($"Usuario '{dto.UserIdentifier}' no encontrado.");
            }

            // Verificar que el usuario no sea ya el Owner
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project!.OwnerId == userToAdd.Id)
            {
                throw new InvalidOperationException("El Owner del proyecto ya es miembro automáticamente.");
            }

            // Verificar que el usuario no sea ya miembro
            var existingMember = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userToAdd.Id);

            if (existingMember != null)
            {
                throw new InvalidOperationException($"El usuario '{userToAdd.UserName}' ya es miembro de este proyecto.");
            }

            // Validar el rol
            var validRoles = new[] { "Member", "Admin" };
            if (!validRoles.Contains(dto.Role))
            {
                throw new InvalidOperationException($"Rol '{dto.Role}' no válido. Roles permitidos: {string.Join(", ", validRoles)}");
            }

            // Crear el nuevo miembro
            var newMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userToAdd.Id,
                Role = dto.Role,
                JoinedDate = DateTime.UtcNow
            };

            _dbContext.ProjectMembers.Add(newMember);
            await _dbContext.SaveChangesAsync();

            // Recargar con las relaciones para el DTO
            var addedMember = await _dbContext.ProjectMembers
                .Include(pm => pm.User)
                .Include(pm => pm.Project)
                .FirstAsync(pm => pm.ProjectId == projectId && pm.UserId == userToAdd.Id);

            return _mapper.Map<ProjectMemberDto>(addedMember);
        }

        public async Task UpdateProjectMemberRoleAsync(Guid projectId, string userId, UpdateProjectMemberRoleDto dto)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // FASE 4: Validar que el usuario puede gestionar miembros
            await _authorizationService.ValidateCanManageMembersAsync(currentUserId, projectId);

            // Buscar el miembro
            var member = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

            if (member == null)
            {
                throw new NotFoundException($"Usuario {userId} no es miembro del proyecto.");
            }

            // Validar el nuevo rol
            var validRoles = new[] { "Member", "Admin" };
            if (!validRoles.Contains(dto.Role))
            {
                throw new InvalidOperationException($"Rol '{dto.Role}' no válido. Roles permitidos: {string.Join(", ", validRoles)}");
            }

            // No permitir que el Owner sea degradado
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project!.OwnerId == userId)
            {
                throw new InvalidOperationException("No se puede cambiar el rol del Owner del proyecto.");
            }

            // Actualizar el rol
            member.Role = dto.Role;
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveProjectMemberAsync(Guid projectId, string userId)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // FASE 4: Validar que el usuario puede gestionar miembros
            await _authorizationService.ValidateCanManageMembersAsync(currentUserId, projectId);

            // Buscar el miembro
            var member = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

            if (member == null)
            {
                // Idempotencia: no hacer nada si no existe
                return;
            }

            // Verificar que no sea el Owner
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project!.OwnerId == userId)
            {
                throw new InvalidOperationException("No se puede eliminar al Owner del proyecto.");
            }

            // Eliminar el miembro
            _dbContext.ProjectMembers.Remove(member);
            await _dbContext.SaveChangesAsync();
        }

        public async Task LeaveProjectAsync(Guid projectId)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();

            // Validar que el proyecto existe
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
            {
                throw new NotFoundException($"Proyecto con ID {projectId} no encontrado.");
            }

            // El Owner no puede abandonar su propio proyecto
            if (project.OwnerId == currentUserId)
            {
                throw new InvalidOperationException("El Owner no puede abandonar su propio proyecto. Debe transferir la propiedad o eliminar el proyecto.");
            }

            // Buscar la membresía
            var member = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId);

            if (member == null)
            {
                throw new NotFoundException("No eres miembro de este proyecto.");
            }

            // Eliminar la membresía
            _dbContext.ProjectMembers.Remove(member);
            await _dbContext.SaveChangesAsync();
        }
    }
}