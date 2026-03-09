using AutoMapper;
using Microsoft.AspNetCore.Identity;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;
using UserManagement.Core.Services;
using UserManagement.Core.UnitOfWorks;

namespace UserManagement.Service.Services
{
    public class UserIdentityService : Service<User>, IUserIdentityService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public UserIdentityService(IGenericRepository<User> repository,
                                   IUnitOfWork unitOfWork,
                                   IUserRepository userRepository,
                                   IMapper mapper) :
            base(repository, unitOfWork)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            try
            {
                // Ensure SecurityStamp is set (required for token generation)
                if (string.IsNullOrEmpty(user.SecurityStamp))
                {
                    user.SecurityStamp = Guid.NewGuid().ToString();
                }

                // Set CreatedDate if not set
                if (user.CreatedDate == default)
                {
                    user.CreatedDate = DateTime.UtcNow;
                }

                await _userRepository.AddAsync(user);
                await _unitOfWork.CommitAsync();
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                // Get the innermost exception for better error details
                var innerException = ex;
                while (innerException.InnerException != null)
                {
                    innerException = innerException.InnerException;
                }

                return IdentityResult.Failed(new IdentityError 
                { 
                    Code = "CreateUserFailed",
                    Description = $"Could not create user: {innerException.Message}" 
                });
            }
        }

        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            try
            {
                _userRepository.Remove(user); // Remove user via repository
                await _unitOfWork.CommitAsync(); // Commit transaction
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                // Log the exception if logging is set up
                return IdentityResult.Failed(new IdentityError { Description = $"Could not delete user: {ex.Message}" });
            }
        }

        public async Task<User> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("Invalid user ID", nameof(userId));

            return await _userRepository.GetByIdAsync(int.Parse(userId));
        }

        public async Task<User> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return await _userRepository.GetByUsernameAsync(normalizedUserName);
        }

        public async Task<User> FindByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
        {
            user.UserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id.ToString());
        }

        public Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            try
            {
                _userRepository.Update(user); // Update user via repository
                await _unitOfWork.CommitAsync(); // Commit transaction
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                // Log the exception if logging is set up
                return IdentityResult.Failed(new IdentityError { Description = $"Could not update user: {ex.Message}" });
            }
        }

        public void Dispose()
        {
            // Implement if any resources need to be disposed
        }

        public Task SetEmailAsync(User user, string email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<string> GetEmailAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.IsEmailConfirmed == true);
        }

        public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        {
            user.IsEmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task<string> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult((user.Email ?? string.Empty).ToUpperInvariant());
        }

        public Task SetNormalizedEmailAsync(User user, string normalizedEmail, CancellationToken cancellationToken)
        {
            user.Email = normalizedEmail;
            return Task.CompletedTask;
        }

        // Phone number store
        public Task SetPhoneNumberAsync(User user, string phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber = phoneNumber;
            return Task.CompletedTask;
        }

        public Task<string> GetPhoneNumberAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.IsPhoneNumberConfirmed == true);
        }

        public Task SetPhoneNumberConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        {
            user.IsPhoneNumberConfirmed = confirmed;
            return Task.CompletedTask;
        }

        // Security stamp store (required for token providers)
        public Task<string> GetSecurityStampAsync(User user, CancellationToken cancellationToken)
        {
            // Ensure we never return null - generate if missing
            if (string.IsNullOrEmpty(user.SecurityStamp))
            {
                user.SecurityStamp = Guid.NewGuid().ToString();
            }
            return Task.FromResult(user.SecurityStamp);
        }

        public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
        {
            user.SecurityStamp = stamp;
            return Task.CompletedTask;
        }

        public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            await _userRepository.AssignRoleToUserAsync(user.Id, roleName);
            await _unitOfWork.CommitAsync(); // Commit transaction
        }

        public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            await _userRepository.RemoveRoleFromUserAsync(user.Id, roleName);
            await _unitOfWork.CommitAsync();
        }

        public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
        {
            IList<string> rolesName = await _userRepository.GetRoleOfUserAsync(user.Id);
            return rolesName;
        }

        public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            return await _userRepository.IsInRoleAsync(user.Id, roleName);
        }

        public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            return await _userRepository.GetUsersInRoleAsync(roleName);
        }
    }
}
