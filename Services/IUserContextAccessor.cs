using System.Security.Claims;

namespace gestion_de_proyectos.Services
{
    public interface IUserContextAccessor
    {
        /// <summary>
        /// Obtiene el ID del usuario autenticado (ClaimTypes.NameIdentifier).
        /// </summary>
        /// <returns>El ID del usuario como string.</returns>
        string GetCurrentUserId();

        /// <summary>
        /// Verifica si el usuario autenticado tiene un rol específico.
        /// </summary>
        /// <param name="role">El nombre del rol a verificar.</param>
        /// <returns>True si el usuario está en el rol, de lo contrario False.</returns>
        bool IsUserInRole(string role);

        // Opcional: Para una mayor flexibilidad
        ClaimsPrincipal? GetUser();
    }
}
