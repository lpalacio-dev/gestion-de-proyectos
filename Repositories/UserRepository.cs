using gestion_de_proyectos.Models;
using Microsoft.EntityFrameworkCore;

namespace gestion_de_proyectos.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. CREATE
        public async Task<User> AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // 2. READ (Todos)
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            // Usamos ToListAsync para ejecutar la consulta
            return await _context.Users.ToListAsync();
        }

        // 3. READ (Uno por ID)
        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        // 4. UPDATE
        public async Task<User?> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // 5. DELETE
        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var userToDelete = await _context.Users.FindAsync(id);
            if (userToDelete == null)
            {
                return false;
            }

            _context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
