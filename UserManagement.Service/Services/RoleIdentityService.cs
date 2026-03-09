using AutoMapper;
using Microsoft.AspNetCore.Identity;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;
using UserManagement.Core.Services;
using UserManagement.Core.UnitOfWorks;

namespace UserManagement.Service.Services
{
    public class RoleIdentityService : Service<Role>, IRoleIdentityService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public RoleIdentityService(
            IGenericRepository<Role> repository,
            IUnitOfWork unitOfWork,
            IRoleRepository roleRepository,
            IClaimRepository claimRepository,
            IMapper mapper) : base(repository, unitOfWork)
        {
            _roleRepository = roleRepository;
            _claimRepository = claimRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));

            try
            {
                await _roleRepository.AddAsync(role);
                await _unitOfWork.CommitAsync();
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError { Description = $"Could not create role: {ex.Message}" });
            }
        }

        public async Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));

            try
            {
                _roleRepository.Remove(role);
                await _unitOfWork.CommitAsync();
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError { Description = $"Could not delete role: {ex.Message}" });
            }
        }

        public async Task<Role> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(roleId)) 
                throw new ArgumentException("Invalid role ID", nameof(roleId));

            return await _roleRepository.GetByIdAsync(int.Parse(roleId));
        }

        public async Task<Role> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            return await _roleRepository.GetByNameAsync(normalizedRoleName);
        }

        public Task<string> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Name?.ToUpperInvariant());
        }

        public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Id.ToString());
        }

        public Task<string> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Name);
        }

        public Task SetNormalizedRoleNameAsync(Role role, string normalizedName, CancellationToken cancellationToken)
        {
            // We store the original name, normalization is handled in queries
            return Task.CompletedTask;
        }

        public Task SetRoleNameAsync(Role role, string roleName, CancellationToken cancellationToken)
        {
            role.Name = roleName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));

            try
            {
                _roleRepository.Update(role);
                await _unitOfWork.CommitAsync();
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError { Description = $"Could not update role: {ex.Message}" });
            }
        }

        public async Task<IList<System.Security.Claims.Claim>> GetClaimsAsync(Role role, CancellationToken cancellationToken = default)
        {
            var roleWithClaims = await _roleRepository.GetRoleWithClaimsAsync(role.Id);

            if (roleWithClaims?.RoleClaims == null)
            {
                return new List<System.Security.Claims.Claim>();
            }

            return roleWithClaims.RoleClaims
                .Where(rc => rc.Claim != null)
                .Select(rc => new System.Security.Claims.Claim(rc.Claim.Type, rc.Claim.Value))
                .ToList();
        }

        public async Task AddClaimAsync(Role role, System.Security.Claims.Claim claim, CancellationToken cancellationToken = default)
        {
            // Find or create the claim entity
            var claimEntity = await _claimRepository.GetByTypeAndValueAsync(claim.Type, claim.Value);

            if (claimEntity == null)
            {
                claimEntity = new Core.Model.Claim
                {
                    Type = claim.Type,
                    Value = claim.Value,
                    Issuer = claim.Issuer ?? "LOCAL",
                    OriginalIssuer = claim.OriginalIssuer ?? "LOCAL",
                    CreatedDate = DateTime.UtcNow
                };
                await _claimRepository.AddAsync(claimEntity);
                await _unitOfWork.CommitAsync();
            }

            // Add the role-claim relationship
            await _roleRepository.AddClaimToRoleAsync(role.Id, claimEntity.Id);
            await _unitOfWork.CommitAsync();
        }

        public async Task RemoveClaimAsync(Role role, System.Security.Claims.Claim claim, CancellationToken cancellationToken = default)
        {
            var claimEntity = await _claimRepository.GetByTypeAndValueAsync(claim.Type, claim.Value);

            if (claimEntity != null)
            {
                await _roleRepository.RemoveClaimFromRoleAsync(role.Id, claimEntity.Id);
                await _unitOfWork.CommitAsync();
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
