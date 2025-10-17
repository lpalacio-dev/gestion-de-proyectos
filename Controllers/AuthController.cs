using gestion_de_proyectos.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel;
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
        // ¡CAMBIO! Usar ApplicationUser en lugar de IdentityUser
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            // ¡CAMBIO! Usar ApplicationUser en lugar de IdentityUser
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // MÉTODO PRIVADO: Generador de JWT (Lógica Pendiente)
        // Lo implementamos aquí para tener todo en un solo lugar.
        private AuthResponseDto GetAuthToken(ApplicationUser user, IList<string> roles)
        {
            var authClaims = new List<Claim>
            {
                // Mantenemos UserName para el ClaimTypes.Name, que es estándar
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            // Agregar los roles como Claims
            foreach (var userRole in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3), // El token expira en 3 horas
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
             );

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo
            };
        }

        // ------------------------------------------------------------------

        // 1. ENDPOINT DE REGISTRO: /api/auth/register

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto model)
        {
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
            {
                return StatusCode(StatusCodes.Status409Conflict, new { Status = "Error", Message = "El usuario ya existe." });
            }

            // ¡CAMBIO! Instanciar ApplicationUser y mapear la propiedad Name
            ApplicationUser user = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username,
                RegistrationDate = DateTime.UtcNow // Configurar la propiedad migrada
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }

            // Asignar un rol por defecto
            if (await _roleManager.RoleExistsAsync("Member"))
            {
                await _userManager.AddToRoleAsync(user, "Member");
            }

            return Ok(new { Status = "Success", Message = "Usuario registrado exitosamente!" });
        }

        // 2. ENDPOINT DE LOGIN: /api/auth/login
        [HttpPost]
        [Route("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            // 1. Buscar y validar credenciales
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // 2. Obtener roles
                var userRoles = await _userManager.GetRolesAsync(user);

                // 3. GENERAR EL JWT (Lógica PENDIENTE completada)
                var tokenResponse = GetAuthToken(user, userRoles);

                // 4. Devolver la respuesta con el token
                return Ok(tokenResponse);
            }

            return Unauthorized(new { Status = "Error", Message = "Nombre de usuario o contraseña inválidos." });
        }

    }

}
