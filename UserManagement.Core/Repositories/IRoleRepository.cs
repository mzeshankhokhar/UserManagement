using UserManagement.Core.Model;

namespace UserManagement.Core.Repositories
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role> GetByNameAsync(string normalizedRoleName);
        Task<bool> RoleExistsAsync(string normalizedRoleName);
        Task<Role> GetRoleWithClaimsAsync(int roleId);
        Task AddClaimToRoleAsync(int roleId, int claimId);
        Task RemoveClaimFromRoleAsync(int roleId, int claimId);
    }
}
