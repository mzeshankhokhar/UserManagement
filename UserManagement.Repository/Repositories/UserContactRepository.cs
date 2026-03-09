using Microsoft.EntityFrameworkCore;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Repository.Repositories
{
    public class UserInvitationRepository : GenericRepository<UserInvitation>, IUserInvitationRepository
    {
        public UserInvitationRepository(AppDbContext context) : base(context)
        {
        }

        public Task<UserInvitation> AddUserInvitationsAsync(int userId, UserInvitation userInvitation)
        {
            throw new NotImplementedException();
        }

        public Task<List<UserInvitation>> GetUserInvitationsAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<List<UserInvitation>> GetUserInvitationsByPhoneAsync(int userId, string phoneNumber)
        {
            throw new NotImplementedException();
        }
    }
}
