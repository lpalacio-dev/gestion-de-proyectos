using gestion_de_proyectos.Models;

namespace gestion_de_proyectos.Repositories
{
    public interface IUserRepository
    {
        // CREATE
        Task<User> AddUserAsync(User user);

        // READ
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(Guid id);

        // UPDATE
        Task<User?> UpdateUserAsync(User user);

        // DELETE
        Task<bool> DeleteUserAsync(Guid id);
    }
}
