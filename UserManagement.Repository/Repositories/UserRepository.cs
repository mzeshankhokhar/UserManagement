using Microsoft.EntityFrameworkCore;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;
using System.Threading.Tasks;

namespace UserManagement.Repository.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                                 .SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetByUsernameAsync(string normalizedUserName)
        {
            return await _context.Users
                                 .SingleOrDefaultAsync(u => u.UserName == normalizedUserName);
        }

        public async Task<bool> IsEmailRegisteredAsync(string email)
        {
            return await _context.Users
                                 .AnyAsync(u => u.Email == email);
        }

        public async Task<bool> AssignRoleToUserAsync(int userId, string roleName)
        {
            bool isUserExists = await _context.Users.AnyAsync(x => x.Id == userId);
            if (isUserExists)
            {
                bool isSameRoleAlreadyAssigned = await _context.Users.Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                    .AnyAsync(x => x.UserRoles.Any(y => y.Role.Name == roleName) && x.Id == userId);
                if (!isSameRoleAlreadyAssigned)
                {
                    Role role = await _context.Roles.SingleOrDefaultAsync(x => x.Name == roleName);
                    await _context.UserRoles.AddAsync(new UserRoles
                    {
                        UserId = userId,
                        RoleId = role.Id,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    });
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> RemoveRoleFromUserAsync(int userId, string roleName)
        {
            bool isUserExists = await _context.Users.AnyAsync(x => x.Id == userId);
            if (isUserExists)
            {
                bool isSameRoleAssigned = await _context.Users.AnyAsync(x => x.UserRoles.Any(y => y.Role.Name == roleName) && x.Id == userId);
                if (isSameRoleAssigned)
                {
                    Role role = await _context.Roles.SingleOrDefaultAsync(x => x.Name == roleName);
                    UserRoles userRoles = await _context.UserRoles.SingleOrDefaultAsync(y => y.RoleId == role.Id && y.UserId == userId);
                    _context.UserRoles.Remove(userRoles);
                    return true;
                }
            }
            return false;
        }

        public async Task<List<string>> GetRoleOfUserAsync(int userId)
        {
            bool isUserExists = await _context.Users.AnyAsync(x => x.Id == userId);
            var rolesName = new List<string>();
            if (isUserExists)
            {
                User userWithRoles = await _context.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).SingleOrDefaultAsync(x => x.Id == userId);
                rolesName.AddRange(userWithRoles.UserRoles.Select(x => x.Role.Name).ToList());
            }
            return rolesName;
        }

        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            Role role = await _context.Roles.SingleOrDefaultAsync(x => x.Name == roleName);
            if (role != null)
            {
                bool isInRole = _context.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).SingleOrDefault(x => x.Id == userId).UserRoles.Any(x => x.RoleId == role.Id);
                return isInRole;
            }
            return false;
        }

        public async Task<List<User>> GetUsersInRoleAsync(string roleName)
        {
            var users = new List<User>();
            Role role = await _context.Roles.SingleOrDefaultAsync(x => x.Name == roleName);
            if (role != null)
            {
                users = await _context.Users.Where(x => x.UserRoles.Any(x => x.RoleId == role.Id)).ToListAsync();
                return users;
            }

            return new List<User>();
        }

    }
}
