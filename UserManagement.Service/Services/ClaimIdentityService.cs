using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;
using UserManagement.Core.Services;
using UserManagement.Core.UnitOfWorks;

namespace UserManagement.Service.Services
{
    public class ClaimIdentityService : Service<Claim>, IClaimIdentityService
    {
        private readonly IClaimRepository _claimRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository;

        public ClaimIdentityService(
            IGenericRepository<Claim> repository,
            IUnitOfWork unitOfWork,
            IClaimRepository claimRepository,
            IUserRepository userRepository,
            IMapper mapper)
            : base(repository, unitOfWork)
        {
            _claimRepository = claimRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<IdentityResult> CreateAsync(Claim claim, CancellationToken cancellationToken)
        {
            try
            {
                // Add new claim to the repository and commit the transaction
                await _claimRepository.AddAsync(claim);
                await _unitOfWork.CommitAsync();
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                // Log the exception (if logging is set up)
                return IdentityResult.Failed(new IdentityError { Description = ex.Message });
            }
        }

        public async Task<IdentityResult> DeleteAsync(Claim claim, CancellationToken cancellationToken)
        {
            try
            {
                // Remove claim from the repository and commit the transaction
                _claimRepository.Remove(claim);
                await _unitOfWork.CommitAsync();
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError { Description = ex.Message });
            }
        }

        public Task<Claim> FindByIdAsync(string claimId, CancellationToken cancellationToken)
        {
            if (int.TryParse(claimId, out var id))
            {
                // Retrieve the claim by ID
                return _claimRepository.GetByIdAsync(id);
            }
            return Task.FromResult<Claim>(null);
        }

        public Task<Claim> FindByNameAsync(string normalizedClaimName, CancellationToken cancellationToken)
        {
            // Retrieve the claim by its normalized name (case-insensitive search)
            return _claimRepository.Where(c => c.Value.ToUpper() == normalizedClaimName).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<IList<Claim>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            // Placeholder method for retrieving users associated with a claim (not implemented)
            throw new NotImplementedException();
        }

        public Task<IList<System.Security.Claims.Claim>> GetClaimsAsync(Claim claim, CancellationToken cancellationToken)
        {
            // Placeholder method for retrieving claims related to the specified Claim entity (not implemented)
            throw new NotImplementedException();
        }

        public Task AddClaimsAsync(Claim claim, IEnumerable<System.Security.Claims.Claim> claims, CancellationToken cancellationToken)
        {
            // Placeholder method for adding multiple claims to the Claim entity (not implemented)
            throw new NotImplementedException();
        }

        public Task RemoveClaimsAsync(Claim claim, IEnumerable<System.Security.Claims.Claim> claims, CancellationToken cancellationToken)
        {
            // Placeholder method for removing multiple claims from the Claim entity (not implemented)
            throw new NotImplementedException();
        }

        public Task ReplaceClaimAsync(Claim claim, System.Security.Claims.Claim oldClaim, System.Security.Claims.Claim newClaim, CancellationToken cancellationToken)
        {
            // Placeholder method for replacing a claim with a new one (not implemented)
            throw new NotImplementedException();
        }

        public Task<string> GetUserIdAsync(Claim claim, CancellationToken cancellationToken)
        {
            // Retrieve the claim's ID as string
            return Task.FromResult(claim.Id.ToString());
        }

        public Task<string> GetUserNameAsync(Claim claim, CancellationToken cancellationToken)
        {
            // Retrieve the claim's name as string
            return Task.FromResult(claim.Value);
        }

        public Task<string> GetNormalizedUserNameAsync(Claim claim, CancellationToken cancellationToken)
        {
            // Retrieve the claim's normalized name (uppercased)
            return Task.FromResult(claim.Value.ToUpper());
        }

        public Task SetUserNameAsync(Claim claim, string userName, CancellationToken cancellationToken)
        {
            // Set the claim's name
            claim.Value = userName;
            return Task.CompletedTask;
        }

        public Task SetNormalizedUserNameAsync(Claim claim, string normalizedName, CancellationToken cancellationToken)
        {
            // Set the claim's normalized name (uppercased)
            claim.Value = normalizedName.ToUpper();
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(Claim claim, CancellationToken cancellationToken)
        {
            try
            {
                // Update the claim in the repository and commit the transaction
                _claimRepository.Update(claim);
                await _unitOfWork.CommitAsync();
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError { Description = ex.Message });
            }
        }

        public void Dispose()
        {
            // Dispose of any resources if needed (currently empty, can be enhanced if necessary)
        }

        public async Task<IList<Claim>> GetUsersForClaimAsync(System.Security.Claims.Claim claim, CancellationToken cancellationToken)
        {
            // Find the claim entity in the repository by its name (claim.Type)
            var claimEntity = await _claimRepository
                .Where(c => c.Value == claim.Type)
                .FirstOrDefaultAsync(cancellationToken);

            if (claimEntity == null)
            {
                // If the claim does not exist in the repository, return an empty list
                return new List<Claim>();
            }

            // Query for users who have this claim associated with their roles
            var usersWithClaim = await _userRepository
                .Where(u => u.UserRoles
                    .Any(ur => ur.Role.RoleClaims
                        .Any(rc => rc.ClaimId == claimEntity.Id)))  // Ensures the user has the specific claim
                .Include(u => u.UserRoles)  // Include the UserRoles for proper mapping
                .ThenInclude(ur => ur.Role)  // Include the Role to access its RoleClaims
                .ThenInclude(r => r.RoleClaims)  // Include RoleClaims to check for the claim
                .ToListAsync(cancellationToken);

            // Now, return the list of claims for each user who has the specified claim
            var userClaims = new List<Claim>();
            foreach (var user in usersWithClaim)
            {
                // Add the user's claim (you may modify how you add the claims here based on your requirements)
                // For now, just adding the claim type and value
                userClaims.AddRange(user.UserRoles.SelectMany(x => x.Role.RoleClaims.Select(x => x.Claim)));
            }

            return userClaims;
        }


    }
}
