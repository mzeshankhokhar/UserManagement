using AutoMapper;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;
using UserManagement.Core.Services;
using UserManagement.Core.UnitOfWorks;

namespace UserManagement.Service.Services
{
    public class UserContactService : Service<UserContact>, IUserContactService
    {
        private readonly IUserContactRepository _userContactRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public UserContactService(IGenericRepository<UserContact> repository,
            IUnitOfWork unitOfWork,
            IUserContactRepository userContactRepository,
            IMapper mapper) :
            base(repository, unitOfWork)
        {
            _userContactRepository = userContactRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public Task<IEnumerable<UserContact>> GetUserContacts(int userId)
        {
            return _userContactRepository.GetContactsAsync(userId);
        }

        public Task<UserContact> GetUserContactById(int contactId, int userId)
        {
            return _userContactRepository.GetContactByIdAsync(contactId, userId);
        }

        public Task AddUserContact(UserContact contact, int userId)
        {
            return _userContactRepository.AddContactAsync(contact, userId);
        }

        public Task AddUserContacts(List<UserContact> contacts, int userId)
        {
            return _userContactRepository.AddContactsAsync(contacts, userId);
        }

        public async Task AddOrUpdateContactsAsync(List<UserContact> contacts, int userId)
        {
            // Normalize userId
            contacts.ForEach(c => c.UserId = userId);

            // Get existing contacts
            var existingContacts = await _userContactRepository.GetContactsByUserAsync(userId);
            var phoneNumbers = contacts.Select(c => c.PhoneNumber).Where(p => !string.IsNullOrEmpty(p)).ToList();
            var emails = contacts.Select(c => c.Email).Where(e => !string.IsNullOrEmpty(e)).ToList();

            // Get registered users matching contact info
            var registeredUsers = await _userContactRepository.GetRegisteredUsersAsync(phoneNumbers, emails);

            var newContacts = new List<UserContact>();

            foreach (var contact in contacts)
            {
                // Find if contact already exists
                var existing = existingContacts.FirstOrDefault(e =>
                    (e.PhoneNumber != null && e.PhoneNumber == contact.PhoneNumber) ||
                    (e.Email != null && e.Email == contact.Email));

                // Try find matching registered user
                var matchedUser = registeredUsers.FirstOrDefault(u =>
                    (!string.IsNullOrEmpty(contact.PhoneNumber) && u.PhoneNumber == contact.PhoneNumber) ||
                    (!string.IsNullOrEmpty(contact.Email) && u.Email == contact.Email));

                // If contact already exists, update its registered info if changed
                if (existing != null)
                {
                    if (matchedUser != null && !existing.IsRegistered)
                    {
                        existing.IsRegistered = true;
                        existing.RegisteredUserId = matchedUser.Id;
                    }
                    continue;
                }

                // New contact — enrich and add
                if (matchedUser != null)
                {
                    contact.IsRegistered = true;
                    contact.RegisteredUserId = matchedUser.Id;

                    //if (string.IsNullOrEmpty(contact.Name))
                    //    contact.Name = $"{matchedUser.FirstName} {matchedUser.LastName}".Trim();

                    if (string.IsNullOrEmpty(contact.Email))
                        contact.Email = matchedUser.Email;
                }

                newContacts.Add(contact);
            }

            if (newContacts.Count > 0)
                await _userContactRepository.AddRangeAsync(newContacts);

            await _unitOfWork.CommitAsync();
        }


        public Task RemoveUserContact(int id) 
        {
            return _userContactRepository.DeleteContactAsync(id);
        }
    }
}
