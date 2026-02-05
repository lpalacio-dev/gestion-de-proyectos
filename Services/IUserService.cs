using gestion_de_proyectos.DTOs;
using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Services
{
    /// <summary>
    /// Servicio para gestionar usuarios del sistema.
    /// Maneja búsqueda, perfiles, actualización y gestión de roles.
    /// </summary>
    public interface IUserService
    {
        // ============================================================================
        // BÚSQUEDA Y LISTADO DE USUARIOS
        // ============================================================================

        /// <summary>
        /// Busca usuarios por nombre de usuario o email.
        /// Disponible para todos los usuarios autenticados.
        /// </summary>
        /// <param name="searchTerm">Término de búsqueda</param>
        /// <returns>Lista de usuarios que coinciden con la búsqueda</returns>
        Task<IEnumerable<UserSearchResultDto>> SearchUsersAsync(string searchTerm);

        /// <summary>
        /// Obtiene todos los usuarios del sistema.
        /// Solo disponible para Admin.
        /// </summary>
        /// <returns>Lista completa de usuarios</returns>
        Task<IEnumerable<UserDto>> GetAllUsersAsync();

        /// <summary>
        /// Obtiene información pública de un usuario específico.
        /// Disponible para todos los usuarios autenticados.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Información pública del usuario</returns>
        Task<UserDto> GetUserByIdAsync(string userId);

        // ============================================================================
        // PERFIL PROPIO
        // ============================================================================

        /// <summary>
        /// Obtiene el perfil completo del usuario actual.
        /// Incluye información privada y estadísticas.
        /// </summary>
        /// <returns>Perfil completo del usuario actual</returns>
        Task<UserProfileDto> GetMyProfileAsync();

        /// <summary>
        /// Actualiza el perfil del usuario actual.
        /// Solo puede actualizar su propia información.
        /// </summary>
        /// <param name="dto">Datos a actualizar</param>
        Task UpdateMyProfileAsync(UpdateUserProfileDto dto);

        /// <summary>
        /// Cambia la contraseña del usuario actual.
        /// Requiere la contraseña actual para validación.
        /// </summary>
        /// <param name="dto">Contraseñas actual y nueva</param>
        Task ChangePasswordAsync(ChangePasswordDto dto);

        // ============================================================================
        // GESTIÓN DE ROLES (SOLO ADMIN)
        // ============================================================================

        /// <summary>
        /// Obtiene los roles de un usuario específico.
        /// Solo disponible para Admin.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Lista de roles del usuario</returns>
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);

        /// <summary>
        /// Actualiza los roles de un usuario.
        /// Solo disponible para Admin.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="dto">Nuevos roles</param>
        Task UpdateUserRolesAsync(string userId, ManageUserRolesDto dto);

        /// <summary>
        /// Elimina un usuario del sistema.
        /// Solo disponible para Admin.
        /// </summary>
        /// <param name="userId">ID del usuario a eliminar</param>
        Task DeleteUserAsync(string userId);
        Task UpdateProfileImageAsync(string imageKey);
        Task DeleteProfileImageAsync();
    }
}