using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly ApplicationDbContext _dbContext;
        private readonly IS3Service _s3Service; // NUEVO

        public UserService(
            UserManager<ApplicationUser> userManager,
            IUserContextAccessor userContextAccessor,
            ApplicationDbContext dbContext,
            IS3Service s3Service)
        {
            _userManager = userManager;
            _userContextAccessor = userContextAccessor;
            _dbContext = dbContext;
            _s3Service = s3Service;
        }

        // ============================================================================
        // BÚSQUEDA Y LISTADO DE USUARIOS
        // ============================================================================

        public async Task<IEnumerable<UserSearchResultDto>> SearchUsersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<UserSearchResultDto>();
            }

            var normalizedSearchTerm = searchTerm.ToUpper();

            var users = await _userManager.Users
                .Where(u => u.NormalizedUserName!.Contains(normalizedSearchTerm) ||
                           (u.NormalizedEmail != null && u.NormalizedEmail.Contains(normalizedSearchTerm)))
                .Take(20) // Limitar resultados
                .Select(u => new UserSearchResultDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email,
                    RegistrationDate = u.RegistrationDate
                })
                .ToListAsync();

            return users;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            // Verificar que el usuario actual es Admin
            if (!_userContextAccessor.IsUserInRole("Admin"))
            {
                throw new UnauthorizedAccessException("Solo los administradores pueden ver todos los usuarios.");
            }

            var users = await _userManager.Users.ToListAsync();

            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email,
                    RegistrationDate = user.RegistrationDate,
                    Roles = roles
                });
            }

            return userDtos;
        }

        public async Task<UserDto> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException($"Usuario con ID {userId} no encontrado.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email,
                RegistrationDate = user.RegistrationDate,
                Roles = roles
            };
        }

        // ============================================================================
        // PERFIL PROPIO
        // ============================================================================

        public async Task<UserProfileDto> GetMyProfileAsync()
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user == null)
            {
                throw new NotFoundException("Usuario actual no encontrado.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Obtener estadísticas
            var ownedProjectsCount = await _dbContext.Projects
                .CountAsync(p => p.OwnerId == currentUserId);

            var memberProjectsCount = await _dbContext.ProjectMembers
                .CountAsync(pm => pm.UserId == currentUserId);

            var assignedTasksCount = await _dbContext.Tasks
                .CountAsync(t => t.AssignedToId == currentUserId);

            // NUEVO: Generar URL firmada si tiene imagen
            string? profileImageUrl = null;
            if (!string.IsNullOrEmpty(user.ProfileImageKey))
            {
                // Asumiendo que tienes IS3Service inyectado
                profileImageUrl = await _s3Service.GetPresignedUrlAsync(user.ProfileImageKey);
            }

            return new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                RegistrationDate = user.RegistrationDate,
                Roles = roles,
                ProfileImageUrl = profileImageUrl, // NUEVO
                OwnedProjectsCount = ownedProjectsCount,
                MemberProjectsCount = memberProjectsCount,
                AssignedTasksCount = assignedTasksCount
            };
        }

        public async Task UpdateMyProfileAsync(UpdateUserProfileDto dto)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user == null)
            {
                throw new NotFoundException("Usuario actual no encontrado.");
            }

            // Actualizar email si se proporciona
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                // Verificar que el email no esté en uso
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null && existingUser.Id != currentUserId)
                {
                    throw new InvalidOperationException("El email ya está en uso por otro usuario.");
                }

                user.Email = dto.Email;
                user.EmailConfirmed = false; // Requerir re-confirmación
            }

            // Actualizar teléfono si se proporciona
            if (dto.PhoneNumber != null) // Permite null para limpiar el teléfono
            {
                user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber;
                user.PhoneNumberConfirmed = false; // Requerir re-confirmación
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Error al actualizar perfil: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user == null)
            {
                throw new NotFoundException("Usuario actual no encontrado.");
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Error al cambiar contraseña: {errors}");
            }
        }

        // ============================================================================
        // GESTIÓN DE ROLES (SOLO ADMIN)
        // ============================================================================

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            // Verificar que el usuario actual es Admin
            if (!_userContextAccessor.IsUserInRole("Admin"))
            {
                throw new UnauthorizedAccessException("Solo los administradores pueden ver roles de usuarios.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException($"Usuario con ID {userId} no encontrado.");
            }

            return await _userManager.GetRolesAsync(user);
        }

        public async Task UpdateUserRolesAsync(string userId, ManageUserRolesDto dto)
        {
            // Verificar que el usuario actual es Admin
            if (!_userContextAccessor.IsUserInRole("Admin"))
            {
                throw new UnauthorizedAccessException("Solo los administradores pueden gestionar roles.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException($"Usuario con ID {userId} no encontrado.");
            }

            // Validar que los roles proporcionados existen
            var validRoles = new[] { "Admin", "User" };
            var invalidRoles = dto.Roles.Where(r => !validRoles.Contains(r)).ToList();

            if (invalidRoles.Any())
            {
                throw new InvalidOperationException($"Roles inválidos: {string.Join(", ", invalidRoles)}. Roles válidos: {string.Join(", ", validRoles)}");
            }

            // Obtener roles actuales
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remover roles actuales
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    throw new InvalidOperationException("Error al remover roles actuales.");
                }
            }

            // Agregar nuevos roles
            if (dto.Roles.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, dto.Roles);
                if (!addResult.Succeeded)
                {
                    throw new InvalidOperationException($"Error al agregar roles: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        public async Task DeleteUserAsync(string userId)
        {
            // Verificar que el usuario actual es Admin
            if (!_userContextAccessor.IsUserInRole("Admin"))
            {
                throw new UnauthorizedAccessException("Solo los administradores pueden eliminar usuarios.");
            }

            var currentUserId = _userContextAccessor.GetCurrentUserId();
            if (userId == currentUserId)
            {
                throw new InvalidOperationException("No puedes eliminar tu propia cuenta.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                // Idempotencia
                return;
            }

            // Verificar si el usuario es propietario de proyectos
            var ownedProjects = await _dbContext.Projects
                .Where(p => p.OwnerId == userId)
                .CountAsync();

            if (ownedProjects > 0)
            {
                throw new InvalidOperationException($"No se puede eliminar el usuario porque es propietario de {ownedProjects} proyecto(s). Transfiere la propiedad primero.");
            }

            // Eliminar membresías del usuario
            var memberships = await _dbContext.ProjectMembers
                .Where(pm => pm.UserId == userId)
                .ToListAsync();

            _dbContext.ProjectMembers.RemoveRange(memberships);

            // Desasignar tareas
            var assignedTasks = await _dbContext.Tasks
                .Where(t => t.AssignedToId == userId)
                .ToListAsync();

            foreach (var task in assignedTasks)
            {
                task.AssignedToId = null;
            }

            await _dbContext.SaveChangesAsync();

            // Eliminar usuario
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Error al eliminar usuario: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }


        public async Task UpdateProfileImageAsync(string imageKey)
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user == null)
            {
                throw new NotFoundException("Usuario actual no encontrado.");
            }

            // Si ya tenía una imagen, eliminarla de S3
            if (!string.IsNullOrEmpty(user.ProfileImageKey))
            {
                await _s3Service.DeleteFileAsync(user.ProfileImageKey);
            }

            // Actualizar la clave de la nueva imagen
            user.ProfileImageKey = imageKey;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Error al actualizar imagen de perfil.");
            }
        }

        public async Task DeleteProfileImageAsync()
        {
            var currentUserId = _userContextAccessor.GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user == null)
            {
                throw new NotFoundException("Usuario actual no encontrado.");
            }

            if (string.IsNullOrEmpty(user.ProfileImageKey))
            {
                throw new NotFoundException("El usuario no tiene imagen de perfil.");
            }

            // Eliminar de S3
            await _s3Service.DeleteFileAsync(user.ProfileImageKey);

            // Limpiar la clave en la BD
            user.ProfileImageKey = null;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Error al eliminar imagen de perfil.");
            }
        }
    }
}