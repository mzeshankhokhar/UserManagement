using UserManagement.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Repositories
{
    public interface IUserInvitationRepository : IGenericRepository<UserInvitation>
    {
        Task<List<UserInvitation>> GetUserInvitationsAsync(int userId);
        Task<List<UserInvitation>> GetUserInvitationsByPhoneAsync(int userId, string phoneNumber);
        Task<UserInvitation> AddUserInvitationsAsync(int userId, UserInvitation userInvitation);

    }
}
