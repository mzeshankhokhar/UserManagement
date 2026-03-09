using UserManagement.Core.Model;
using System.Threading.Tasks;

namespace UserManagement.Core.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByUsernameAsync(string normalizedUserName); // Find user by normalized username
        Task<User> GetByEmailAsync(string email); // Find user by email address
        Task<bool> IsEmailRegisteredAsync(string email); // Check if email is already registered
        Task<bool> AssignRoleToUserAsync(int userId, string roleName);
        Task<bool> RemoveRoleFromUserAsync(int userId, string roleName);
        Task<List<string>> GetRoleOfUserAsync(int userId);
        Task<bool> IsInRoleAsync(int userId, string roleName);
        Task<List<User>> GetUsersInRoleAsync(string roleName);

    }
}