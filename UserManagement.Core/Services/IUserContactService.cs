using UserManagement.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Claim = System.Security.Claims.Claim;

namespace UserManagement.Core.Services
{
    public interface IUserContactService : IService<UserContact>
    {
        Task AddOrUpdateContactsAsync(List<UserContact> contacts, int userId);
        Task<UserContact> GetUserContactById(int contactId, int userId);
        Task<IEnumerable<UserContact>> GetUserContacts(int userId);
        Task AddUserContact(UserContact contact, int userId);
        Task AddUserContacts(List<UserContact> contacts, int userId);
        Task RemoveUserContact(int id);
    }
}
