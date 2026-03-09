using UserManagement.Core.Model;

namespace UserManagement.Core.Repositories
{
    public interface IClaimRepository : IGenericRepository<Claim>
    {
        Task<IEnumerable<Claim>> GetClaimsByRoleIdAsync(int roleId);
        Task<IEnumerable<Claim>> GetClaimsByUserIdAsync(int userId);
        Task<Claim> GetByTypeAndValueAsync(string type, string value);
    }
}
