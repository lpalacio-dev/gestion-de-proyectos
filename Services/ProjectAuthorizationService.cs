using gestion_de_proyectos.Repositories;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Services
{
    public class ProjectAuthorizationService : IProjectAuthorizationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserContextAccessor _userContextAccessor;

        public ProjectAuthorizationService(
            ApplicationDbContext dbContext,
            IProjectRepository projectRepository,
            IUserContextAccessor userContextAccessor)
        {
            _dbContext = dbContext;
            _projectRepository = projectRepository;
            _userContextAccessor = userContextAccessor;
        }

        // ============================================================================
        // MÉTODOS DE VERIFICACIÓN DE ROLES
        // ============================================================================

        public async Task<string?> GetUserRoleInProjectAsync(string userId, Guid projectId)
        {
            // Primero verificar si es el Owner del proyecto
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project?.OwnerId == userId)
            {
                return "Owner";
            }

            // Buscar en ProjectMembers
            var member = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

            return member?.Role;
        }

        public async Task<bool> IsProjectOwnerAsync(string userId, Guid projectId)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            return project?.OwnerId == userId;
        }

        public async Task<bool> IsProjectAdminAsync(string userId, Guid projectId)
        {
            return await _dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId &&
                               pm.UserId == userId &&
                               pm.Role == "Admin");
        }

        public async Task<bool> IsProjectMemberAsync(string userId, Guid projectId)
        {
            return await _dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        }

        public bool IsGlobalAdmin(string userId)
        {
            return _userContextAccessor.IsUserInRole("Admin");
        }

        // ============================================================================
        // MÉTODOS DE VALIDACIÓN DE PERMISOS
        // ============================================================================

        public async Task<bool> CanViewProjectAsync(string userId, Guid projectId)
        {
            // Admin global puede ver todo
            if (IsGlobalAdmin(userId))
            {
                return true;
            }

            // Owner puede ver su proyecto
            if (await IsProjectOwnerAsync(userId, projectId))
            {
                return true;
            }

            // Cualquier miembro puede ver el proyecto
            return await IsProjectMemberAsync(userId, projectId);
        }

        public async Task<bool> CanModifyProjectAsync(string userId, Guid projectId)
        {
            // Solo Owner o Admin global pueden modificar detalles del proyecto
            if (IsGlobalAdmin(userId))
            {
                return true;
            }

            return await IsProjectOwnerAsync(userId, projectId);
        }

        public async Task<bool> CanDeleteProjectAsync(string userId, Guid projectId)
        {
            // Solo Owner o Admin global pueden eliminar el proyecto
            if (IsGlobalAdmin(userId))
            {
                return true;
            }

            return await IsProjectOwnerAsync(userId, projectId);
        }

        public async Task<bool> CanManageMembersAsync(string userId, Guid projectId)
        {
            // Admin global puede gestionar miembros
            if (IsGlobalAdmin(userId))
            {
                return true;
            }

            // Owner puede gestionar miembros
            if (await IsProjectOwnerAsync(userId, projectId))
            {
                return true;
            }

            // Admin del proyecto puede gestionar miembros
            return await IsProjectAdminAsync(userId, projectId);
        }

        public async Task<bool> CanManageTasksAsync(string userId, Guid projectId)
        {
            // Admin global puede gestionar tareas
            if (IsGlobalAdmin(userId))
            {
                return true;
            }

            // Owner puede gestionar tareas
            if (await IsProjectOwnerAsync(userId, projectId))
            {
                return true;
            }

            // Admin del proyecto puede gestionar tareas
            if (await IsProjectAdminAsync(userId, projectId))
            {
                return true;
            }

            // Member puede gestionar tareas
            return await IsProjectMemberAsync(userId, projectId);
        }

        // ============================================================================
        // MÉTODOS DE VALIDACIÓN CON EXCEPCIONES
        // ============================================================================

        public async Task ValidateProjectAccessAsync(string userId, Guid projectId)
        {
            // Verificar que el proyecto existe
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
            {
                throw new NotFoundException($"Proyecto con ID {projectId} no encontrado.");
            }

            // Verificar acceso
            if (!await CanViewProjectAsync(userId, projectId))
            {
                throw new UnauthorizedAccessException(
                    "No tienes permiso para acceder a este proyecto. " +
                    "Debes ser Owner, Miembro o Administrador global.");
            }
        }

        public async Task ValidateCanModifyProjectAsync(string userId, Guid projectId)
        {
            // Verificar que el proyecto existe
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
            {
                throw new NotFoundException($"Proyecto con ID {projectId} no encontrado.");
            }

            // Verificar permiso
            if (!await CanModifyProjectAsync(userId, projectId))
            {
                throw new UnauthorizedAccessException(
                    "No tienes permiso para modificar este proyecto. " +
                    "Solo el Owner o un Administrador global pueden modificar el proyecto.");
            }
        }

        public async Task ValidateCanManageMembersAsync(string userId, Guid projectId)
        {
            // Verificar que el proyecto existe
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
            {
                throw new NotFoundException($"Proyecto con ID {projectId} no encontrado.");
            }

            // Verificar permiso
            if (!await CanManageMembersAsync(userId, projectId))
            {
                throw new UnauthorizedAccessException(
                    "No tienes permiso para gestionar miembros de este proyecto. " +
                    "Solo el Owner, Admins del proyecto o Administradores globales pueden gestionar miembros.");
            }
        }

        public async Task ValidateCanManageTasksAsync(string userId, Guid projectId)
        {
            // Verificar que el proyecto existe
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
            {
                throw new NotFoundException($"Proyecto con ID {projectId} no encontrado.");
            }

            // Verificar permiso
            if (!await CanManageTasksAsync(userId, projectId))
            {
                throw new UnauthorizedAccessException(
                    "No tienes permiso para gestionar tareas en este proyecto. " +
                    "Debes ser Owner, Admin, Miembro o Administrador global.");
            }
        }
    }
}