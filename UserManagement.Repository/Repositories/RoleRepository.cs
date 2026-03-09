using Microsoft.EntityFrameworkCore;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;

namespace UserManagement.Repository.Repositories
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Role> GetByNameAsync(string normalizedRoleName)
        {
            return await _context.Roles
                .SingleOrDefaultAsync(r => r.Name.ToUpper() == normalizedRoleName.ToUpper());
        }

        public async Task<bool> RoleExistsAsync(string normalizedRoleName)
        {
            return await _context.Roles
                .AnyAsync(r => r.Name.ToUpper() == normalizedRoleName.ToUpper());
        }

        public async Task<Role> GetRoleWithClaimsAsync(int roleId)
        {
            return await _context.Roles
                .Include(r => r.RoleClaims)
                .ThenInclude(rc => rc.Claim)
                .FirstOrDefaultAsync(r => r.Id == roleId);
        }

        public async Task AddClaimToRoleAsync(int roleId, int claimId)
        {
            var exists = await _context.RoleClaims
                .AnyAsync(rc => rc.RoleId == roleId && rc.ClaimId == claimId);

            if (!exists)
            {
                var roleClaim = new RoleClaims
                {
                    RoleId = roleId,
                    ClaimId = claimId,
                    CreatedDate = DateTime.UtcNow
                };
                await _context.RoleClaims.AddAsync(roleClaim);
            }
        }

        public async Task RemoveClaimFromRoleAsync(int roleId, int claimId)
        {
            var roleClaim = await _context.RoleClaims
                .FirstOrDefaultAsync(rc => rc.RoleId == roleId && rc.ClaimId == claimId);

            if (roleClaim != null)
            {
                _context.RoleClaims.Remove(roleClaim);
            }
        }
    }
}
