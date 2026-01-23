using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Services
{
    /// <summary>
    /// Servicio centralizado para manejar la lógica de autorización de proyectos.
    /// Proporciona métodos helper para verificar permisos basados en roles de proyecto.
    /// </summary>
    public interface IProjectAuthorizationService
    {
        // ============================================================================
        // MÉTODOS DE VERIFICACIÓN DE ROLES
        // ============================================================================

        /// <summary>
        /// Obtiene el rol del usuario en un proyecto específico.
        /// Retorna null si el usuario no es miembro del proyecto.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="projectId">ID del proyecto</param>
        /// <returns>Rol del usuario ("Owner", "Admin", "Member") o null</returns>
        Task<string?> GetUserRoleInProjectAsync(string userId, Guid projectId);

        /// <summary>
        /// Verifica si el usuario es Owner del proyecto.
        /// </summary>
        Task<bool> IsProjectOwnerAsync(string userId, Guid projectId);

        /// <summary>
        /// Verifica si el usuario es Admin del proyecto (role "Admin" en ProjectMembers).
        /// </summary>
        Task<bool> IsProjectAdminAsync(string userId, Guid projectId);

        /// <summary>
        /// Verifica si el usuario es miembro del proyecto (cualquier rol).
        /// </summary>
        Task<bool> IsProjectMemberAsync(string userId, Guid projectId);

        /// <summary>
        /// Verifica si el usuario es Admin global del sistema.
        /// </summary>
        bool IsGlobalAdmin(string userId);

        // ============================================================================
        // MÉTODOS DE VALIDACIÓN DE PERMISOS
        // ============================================================================

        /// <summary>
        /// Verifica si el usuario tiene acceso de lectura al proyecto.
        /// Acceso: Owner, Admin, Member o Admin Global.
        /// </summary>
        Task<bool> CanViewProjectAsync(string userId, Guid projectId);

        /// <summary>
        /// Verifica si el usuario puede modificar el proyecto (actualizar detalles).
        /// Acceso: Owner o Admin Global.
        /// </summary>
        Task<bool> CanModifyProjectAsync(string userId, Guid projectId);

        /// <summary>
        /// Verifica si el usuario puede eliminar el proyecto.
        /// Acceso: Owner o Admin Global.
        /// </summary>
        Task<bool> CanDeleteProjectAsync(string userId, Guid projectId);

        /// <summary>
        /// Verifica si el usuario puede gestionar miembros del proyecto.
        /// Acceso: Owner, Admin del proyecto, o Admin Global.
        /// </summary>
        Task<bool> CanManageMembersAsync(string userId, Guid projectId);

        /// <summary>
        /// Verifica si el usuario puede crear/editar/eliminar tareas en el proyecto.
        /// Acceso: Owner, Admin, Member o Admin Global.
        /// </summary>
        Task<bool> CanManageTasksAsync(string userId, Guid projectId);

        // ============================================================================
        // MÉTODOS DE VALIDACIÓN CON EXCEPCIONES
        // ============================================================================

        /// <summary>
        /// Valida acceso al proyecto o lanza UnauthorizedAccessException.
        /// Uso: await ValidateProjectAccessAsync(userId, projectId);
        /// </summary>
        Task ValidateProjectAccessAsync(string userId, Guid projectId);

        /// <summary>
        /// Valida permiso para modificar proyecto o lanza UnauthorizedAccessException.
        /// </summary>
        Task ValidateCanModifyProjectAsync(string userId, Guid projectId);

        /// <summary>
        /// Valida permiso para gestionar miembros o lanza UnauthorizedAccessException.
        /// </summary>
        Task ValidateCanManageMembersAsync(string userId, Guid projectId);

        /// <summary>
        /// Valida permiso para gestionar tareas o lanza UnauthorizedAccessException.
        /// </summary>
        Task ValidateCanManageTasksAsync(string userId, Guid projectId);
    }
}