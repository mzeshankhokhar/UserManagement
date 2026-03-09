using UserManagement.Core.Model;
using Microsoft.AspNetCore.Identity;

namespace UserManagement.Core.Services
{
    public interface IUserIdentityService :
        IUserStore<User>,
        IUserPasswordStore<User>,
        IUserEmailStore<User>,
        IUserPhoneNumberStore<User>,
        IUserSecurityStampStore<User>,
        IUserRoleStore<User>
    {
        
    }
}
