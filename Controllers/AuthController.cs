using gestion_de_proyectos.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using gestion_de_proyectos.Models;

namespace gestion_de_proyectos.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // MÉTODO PRIVADO: Generador de JWT
        private AuthResponseDto GetAuthToken(ApplicationUser user, IList<string> roles)
        {
            var authClaims = new List<Claim>
            {
                // Claim estándar: UserName
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                // Claim estándar: UserId (IMPORTANTE para IUserContextAccessor)
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                // Claim JWT: Jti (JWT ID único)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            // Agregar los roles como Claims
            foreach (var userRole in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var authSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada"))
            );

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.UtcNow.AddHours(3), // Token válido por 3 horas
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo
            };
        }

        // ============================================================================
        // ENDPOINT DE REGISTRO: POST /api/auth/register
        // ============================================================================
        [HttpPost]
        [Route("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto model)
        {
            // Validar si el usuario ya existe
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    new { Status = "Error", Message = "El nombre de usuario ya está registrado." });
            }

            // Validar si el email ya existe
            var emailExists = await _userManager.FindByEmailAsync(model.Email);
            if (emailExists != null)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    new { Status = "Error", Message = "El correo electrónico ya está registrado." });
            }

            // Crear instancia de ApplicationUser
            ApplicationUser user = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username,
                RegistrationDate = DateTime.UtcNow
            };

            // Crear usuario en la base de datos
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new
                    {
                        Status = "Error",
                        Message = "Error al crear el usuario.",
                        Errors = result.Errors.Select(e => e.Description)
                    });
            }

            // ============================================================================
            // FASE 1: Asignar rol "User" por defecto a todos los nuevos usuarios
            // ============================================================================
            if (await _roleManager.RoleExistsAsync("User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
            }

            return Ok(new
            {
                Status = "Success",
                Message = "Usuario registrado exitosamente.",
                UserId = user.Id,
                Username = user.UserName
            });
        }

        // ============================================================================
        // ENDPOINT DE LOGIN: POST /api/auth/login
        // ============================================================================
        [HttpPost]
        [Route("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            // Buscar usuario por nombre de usuario
            var user = await _userManager.FindByNameAsync(model.Username);

            // Validar credenciales
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // Obtener roles del usuario
                var userRoles = await _userManager.GetRolesAsync(user);

                // Generar token JWT
                var tokenResponse = GetAuthToken(user, userRoles);

                return Ok(new
                {
                    Status = "Success",
                    Message = "Login exitoso.",
                    UserId = user.Id,
                    Username = user.UserName,
                    Roles = userRoles,
                    Token = tokenResponse.Token,
                    Expiration = tokenResponse.Expiration
                });
            }

            return Unauthorized(new
            {
                Status = "Error",
                Message = "Nombre de usuario o contraseña incorrectos."
            });
        }
    }
}