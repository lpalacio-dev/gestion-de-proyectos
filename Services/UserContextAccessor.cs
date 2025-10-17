using gestion_de_proyectos.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace gestion_de_proyectos.Services
{
    public class UserContextAccessor : IUserContextAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserContextAccessor(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public string GetCurrentUserId()
        {
            // Lee el ClaimTypes.NameIdentifier (el ID de usuario en ASP.NET Identity)
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User is not authenticated.");
        }

        public bool IsUserInRole(string role)
        {
            var userPrincipal = _httpContextAccessor.HttpContext?.User;
            if (userPrincipal == null)
            {
                return false;
            }
            // Utiliza el método de ClaimsPrincipal para verificar roles
            return userPrincipal.IsInRole(role);
        }

        public ClaimsPrincipal? GetUser()
        {
            return _httpContextAccessor.HttpContext?.User;
        }
    }
}
