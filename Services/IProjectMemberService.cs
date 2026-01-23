using gestion_de_proyectos.DTOs;
using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Services
{
    /// <summary>
    /// Servicio para gestionar miembros de proyectos.
    /// Maneja la adición, eliminación y actualización de roles de miembros.
    /// </summary>
    public interface IProjectMemberService
    {
        /// <summary>
        /// Obtiene todos los miembros de un proyecto específico.
        /// Requiere que el usuario actual tenga acceso al proyecto.
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        /// <returns>Lista de miembros del proyecto</returns>
        Task<IEnumerable<ProjectMemberDto>> GetProjectMembersAsync(Guid projectId);

        /// <summary>
        /// Obtiene un miembro específico de un proyecto.
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        /// <param name="userId">ID del usuario miembro</param>
        /// <returns>Información del miembro</returns>
        Task<ProjectMemberDto> GetProjectMemberAsync(Guid projectId, string userId);

        /// <summary>
        /// Agrega un nuevo miembro al proyecto.
        /// Solo el Owner o Admin del proyecto puede agregar miembros.
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        /// <param name="dto">Información del miembro a agregar</param>
        /// <returns>Información del miembro agregado</returns>
        Task<ProjectMemberDto> AddProjectMemberAsync(Guid projectId, AddProjectMemberDto dto);

        /// <summary>
        /// Actualiza el rol de un miembro existente en el proyecto.
        /// Solo el Owner o Admin del proyecto puede actualizar roles.
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        /// <param name="userId">ID del usuario miembro</param>
        /// <param name="dto">Nuevo rol</param>
        Task UpdateProjectMemberRoleAsync(Guid projectId, string userId, UpdateProjectMemberRoleDto dto);

        /// <summary>
        /// Elimina un miembro del proyecto.
        /// Solo el Owner o Admin del proyecto puede eliminar miembros.
        /// El Owner no puede ser eliminado.
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        /// <param name="userId">ID del usuario a eliminar</param>
        Task RemoveProjectMemberAsync(Guid projectId, string userId);

        /// <summary>
        /// Permite que el usuario actual abandone un proyecto.
        /// El Owner no puede abandonar su propio proyecto.
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        Task LeaveProjectAsync(Guid projectId);
    }
}