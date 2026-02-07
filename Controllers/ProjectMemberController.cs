using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestion_de_proyectos.Controllers
{
    /// <summary>
    /// Controlador para gestionar los miembros de proyectos.
    /// Permite agregar, listar, actualizar roles y eliminar miembros.
    /// </summary>
    [Authorize(Roles = "User")]
    [Route("api/projects/{projectId:guid}/members")]
    [ApiController]
    public class ProjectMemberController : ControllerBase
    {
        private readonly IProjectMemberService _projectMemberService;

        public ProjectMemberController(IProjectMemberService projectMemberService)
        {
            _projectMemberService = projectMemberService;
        }

        /// <summary>
        /// Obtiene todos los miembros de un proyecto.
        /// Requiere acceso al proyecto (Owner, Miembro o Admin global).
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        /// <returns>Lista de miembros del proyecto</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProjectMemberDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<ProjectMemberDto>>> GetProjectMembers(Guid projectId)
        {
            try
            {
                var members = await _projectMemberService.GetProjectMembersAsync(projectId);
                return Ok(members);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Obtiene un miembro específico del proyecto.
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        /// <param name="userId">ID del usuario miembro</param>
        /// <returns>Información del miembro</returns>
        [HttpGet("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProjectMemberDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProjectMemberDto>> GetProjectMember(Guid projectId, string userId)
        {
            try
            {
                var member = await _projectMemberService.GetProjectMemberAsync(projectId, userId);
                return Ok(member);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Agrega un nuevo miembro al proyecto.
        /// Solo el Owner o Admins del proyecto pueden agregar miembros.
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        /// <param name="dto">Información del miembro a agregar</param>
        /// <returns>Información del miembro agregado</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ProjectMemberDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ProjectMemberDto>> AddProjectMember(
            Guid projectId,
            [FromBody] AddProjectMemberDto dto)
        {
            try
            {
                var member = await _projectMemberService.AddProjectMemberAsync(projectId, dto);
                return CreatedAtAction(
                    nameof(GetProjectMember),
                    new { projectId = projectId, userId = member.UserId },
                    member);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza el rol de un miembro existente.
        /// Solo el Owner o Admins del proyecto pueden actualizar roles.
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        /// <param name="userId">ID del usuario miembro</param>
        /// <param name="dto">Nuevo rol para el miembro</param>
        [HttpPut("{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateProjectMemberRole(
            Guid projectId,
            string userId,
            [FromBody] UpdateProjectMemberRoleDto dto)
        {
            try
            {
                await _projectMemberService.UpdateProjectMemberRoleAsync(projectId, userId, dto);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un miembro del proyecto.
        /// Solo el Owner o Admins del proyecto pueden eliminar miembros.
        /// El Owner no puede ser eliminado.
        /// </summary>
        /// <param name="projectId">ID del proyecto</param>
        /// <param name="userId">ID del usuario a eliminar</param>
        [HttpDelete("{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveProjectMember(Guid projectId, string userId)
        {
            try
            {
                await _projectMemberService.RemoveProjectMemberAsync(projectId, userId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Permite que el usuario actual abandone un proyecto.
        /// El Owner no puede abandonar su propio proyecto.
        /// Endpoint: POST /api/projects/{projectId}/members/leave
        /// </summary>
        /// <param name="projectId">ID del proyecto a abandonar</param>
        [HttpPost("leave")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LeaveProject(Guid projectId)
        {
            try
            {
                await _projectMemberService.LeaveProjectAsync(projectId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}