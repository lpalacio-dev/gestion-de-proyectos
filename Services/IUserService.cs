using gestion_de_proyectos.DTOs;

namespace gestion_de_proyectos.Services
{
    public interface IUserService
    {
        Task<UserResponseDto> CreateUserAsync(UserCreationDto UserDto);
        Task<UserResponseDto> GetUserByIdAsync(Guid id);
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto> UpdateUserAsync(Guid id, UserUpdateDto userDto);
        Task<bool> DeleteUserAsync(Guid id);
    }
}
