using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestion_de_proyectos.Controllers
{
    /// <summary>
    /// Controlador para gestionar usuarios del sistema.
    /// Maneja búsqueda, perfiles, actualización y gestión de roles.
    /// </summary>
    [Authorize(Roles = "User")]
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // ============================================================================
        // BÚSQUEDA Y LISTADO DE USUARIOS
        // ============================================================================

        /// <summary>
        /// Busca usuarios por nombre o email.
        /// Útil para agregar miembros a proyectos.
        /// </summary>
        /// <param name="q">Término de búsqueda</param>
        /// <returns>Lista de usuarios que coinciden</returns>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserSearchResultDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<UserSearchResultDto>>> SearchUsers([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return BadRequest(new { Message = "El término de búsqueda debe tener al menos 2 caracteres." });
            }

            var users = await _userService.SearchUsersAsync(q);
            return Ok(users);
        }

        /// <summary>
        /// Obtiene todos los usuarios del sistema.
        /// Solo disponible para administradores.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserDto>))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Obtiene información pública de un usuario específico.
        /// </summary>
        /// <param name="id">ID del usuario</param>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return Ok(user);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        // ============================================================================
        // PERFIL PROPIO
        // ============================================================================

        /// <summary>
        /// Obtiene el perfil completo del usuario actual.
        /// Incluye información privada y estadísticas.
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserProfileDto))]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            try
            {
                var profile = await _userService.GetMyProfileAsync();
                return Ok(profile);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza el perfil del usuario actual.
        /// Permite cambiar email y teléfono.
        /// </summary>
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto dto)
        {
            try
            {
                await _userService.UpdateMyProfileAsync(dto);
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

        /// <summary>
        /// Cambia la contraseña del usuario actual.
        /// Requiere la contraseña actual para validación.
        /// </summary>
        [HttpPost("me/change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                await _userService.ChangePasswordAsync(dto);
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

        // ============================================================================
        // GESTIÓN DE ROLES (SOLO ADMIN)
        // ============================================================================

        /// <summary>
        /// Obtiene los roles de un usuario específico.
        /// Solo disponible para administradores.
        /// </summary>
        /// <param name="id">ID del usuario</param>
        [HttpGet("{id}/roles")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<string>>> GetUserRoles(string id)
        {
            try
            {
                var roles = await _userService.GetUserRolesAsync(id);
                return Ok(roles);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Actualiza los roles de un usuario.
        /// Solo disponible para administradores.
        /// </summary>
        /// <param name="id">ID del usuario</param>
        /// <param name="dto">Nuevos roles</param>
        [HttpPut("{id}/roles")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] ManageUserRolesDto dto)
        {
            try
            {
                await _userService.UpdateUserRolesAsync(id, dto);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un usuario del sistema.
        /// Solo disponible para administradores.
        /// No se puede eliminar si es propietario de proyectos.
        /// </summary>
        /// <param name="id">ID del usuario a eliminar</param>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}