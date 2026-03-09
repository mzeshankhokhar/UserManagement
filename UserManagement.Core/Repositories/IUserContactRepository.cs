using UserManagement.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Repositories
{
    public interface IUserContactRepository : IGenericRepository<UserContact>
    {
        Task<List<UserContact>> GetContactsByUserAsync(int userId);
        Task<UserContact> GetContactByIdAsync(int contactId, int userId);
        Task<List<User>> GetRegisteredUsersAsync(List<string> phoneNumbers, List<string> emails);
        Task<IEnumerable<UserContact>> GetContactsAsync(int userId);
        Task AddContactAsync(UserContact contact, int userId);
        Task AddContactsAsync(List<UserContact> contact, int userId);
        Task DeleteContactAsync(int id);
    }
}
