using Microsoft.EntityFrameworkCore;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;

namespace UserManagement.Repository.Repositories
{
    public class ClaimRepository : GenericRepository<Claim>, IClaimRepository
    {
        public ClaimRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Claim>> GetClaimsByRoleIdAsync(int roleId)
        {
            return await _context.RoleClaims
                .Where(rc => rc.RoleId == roleId)
                .Select(rc => rc.Claim)
                .ToListAsync();
        }

        public async Task<IEnumerable<Claim>> GetClaimsByUserIdAsync(int userId)
        {
            var claims = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .ThenInclude(r => r.RoleClaims)
                .ThenInclude(rc => rc.Claim)
                .SelectMany(ur => ur.Role.RoleClaims.Select(rc => rc.Claim))
                .ToListAsync();

            return claims;
        }

        public async Task<Claim> GetByTypeAndValueAsync(string type, string value)
        {
            return await _context.Claims
                .FirstOrDefaultAsync(c => c.Type == type && c.Value == value);
        }
    }
}
