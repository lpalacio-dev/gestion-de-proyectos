using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using gestion_de_proyectos.Repositories;
using BCrypt.Net; // Importamos el paquete BCrypt

namespace gestion_de_proyectos.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        // El servicio depende del repositorio
        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserResponseDto> CreateUserAsync (UserCreationDto userDto)
        {
            // 1. Lógica de Negocio: Verificar si el correo ya existe
            // (Para simplicidad, omitimos la verificación aquí, pero iría en esta capa).

            // 2. Seguridad: Hashing de la Contraseña
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            // 3. Mapeo DTO a Entidad
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Name = userDto.Name,
                Email = userDto.Email,
                // Almacenamos el hash, NUNCA la contraseña plana
                PasswordHash = hashedPassword,
                RegistrationDate = DateTime.UtcNow
            };

            // 4. Persistencia (Delegar al Repositorio)
            var createdUser = await _userRepository.AddUserAsync(newUser);

            // 5. Mapeo Entidad a DTO de Respuesta
            return MapToResponseDto(createdUser);
        }

        // --- OTRAS OPERACIONES CRUD (Ejemplo: Get) ---

        public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);

            // Lógica de validación: Si el usuario no existe, retornar null
            if (user == null) return null;

            // Mapeo
            return MapToResponseDto(user);
        }

        // --- 1. Implementación de Leer Todos (GET ALL) ---
        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            // 1. Llamar al Repositorio para obtener la lista de entidades
            var users = await _userRepository.GetAllUsersAsync();

            // 2. Mapear la lista de entidades (User) a la lista de DTOs (UserResponseDto)
            // Se utiliza LINQ's Select para mapear cada elemento
            var userDtos = users.Select(u => MapToResponseDto(u)).ToList();

            // 3. Devolver la lista de DTOs
            return userDtos;
        }

        // --- 2. Implementación de Actualizar (UPDATE) ---
        public async Task<UserResponseDto?> UpdateUserAsync(Guid id, UserUpdateDto userDto)
        {
            // 1. Lógica de Negocio: Buscar si el usuario existe antes de actualizar
            var existingUser = await _userRepository.GetUserByIdAsync(id);

            if (existingUser == null)
            {
                return null; // El servicio indica al controlador que la entidad no existe
            }

            // 2. Lógica de Mapeo y Actualización de Propiedades
            // El servicio decide qué propiedades se pueden actualizar
            existingUser.Name = userDto.Name;

            // Solo actualiza el email si se proporciona un valor en el DTO
            if (!string.IsNullOrWhiteSpace(userDto.Email))
            {
                // [Opcional: Agregar lógica de negocio aquí para verificar unicidad del email]
                existingUser.Email = userDto.Email;
            }

            // 3. Persistencia (Delegar al Repositorio)
            var updatedUser = await _userRepository.UpdateUserAsync(existingUser);

            // 4. Mapeo Entidad a DTO de Respuesta
            return MapToResponseDto(updatedUser);
        }

        // --- 3. Implementación de Borrar (DELETE) ---
        public async Task<bool> DeleteUserAsync(Guid id)
        {
            // 1. Lógica de Negocio: Delegar la operación de borrado al Repositorio
            // El repositorio se encarga de verificar si existe y realizar la eliminación.
            var isDeleted = await _userRepository.DeleteUserAsync(id);

            // 2. Devolver el resultado de la operación
            return isDeleted;
        }

        // --- FUNCIÓN DE UTILIDAD: Mapeo ---
        private static UserResponseDto MapToResponseDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                RegistrationDate = user.RegistrationDate
            };
        }
    }
}
