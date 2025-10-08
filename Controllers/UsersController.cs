using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using gestion_de_proyectos.Services;
using Microsoft.AspNetCore.Mvc;

namespace gestion_de_proyectos.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // La ruta será /api/users
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        // Inyección de Dependencias: ASP.NET Core pasa la implementación de IUserRepository
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
        { 
            // El servicio se encarga de obtener los datos y mapearlos a DTOs.
            var userDtos = await _userService.GetAllUsersAsync();

            // Siempre se devuelve 200 OK, incluso si la lista está vacía.
            return Ok(userDtos);

        }

        // GET: api/users/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(Guid id)
        {
            var userDto = await _userService.GetUserByIdAsync(id);

            if (userDto == null)
            {
                return NotFound(); // HTTP 404
            }

            return Ok(userDto); // HTTP 200
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> PostUser([FromBody] UserCreationDto userDto)
        {
            // La validación de atributos ([Required], [EmailAddress]) ocurre automáticamente.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // HTTP 400
            }

            // El servicio maneja el hashing, la creación y el mapeo de respuesta.
            var createdUserDto = await _userService.CreateUserAsync(userDto);

            // Responde con HTTP 201 Created y la URL del nuevo recurso.
            return CreatedAtAction(nameof(GetUser), new { id = createdUserDto.Id }, createdUserDto);
        }

        // PUT: api/users/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> PutUser(Guid id, [FromBody] UserUpdateDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // HTTP 400
            }

            // El servicio intenta actualizar el usuario.
            var updatedUserDto = await _userService.UpdateUserAsync(id, userDto);

            if (updatedUserDto == null)
            {
                return NotFound(); // Usuario no encontrado, HTTP 404
            }

            // Si la actualización fue exitosa, responde 204 No Content.
            return NoContent();
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            // El servicio maneja la lógica de borrado.
            var isDeleted = await _userService.DeleteUserAsync(id);

            if (!isDeleted)
            {
                return NotFound(); // Usuario no encontrado, HTTP 404
            }

            // Si se borra exitosamente, responde 204 No Content.
            return NoContent();
        }
    }
}
