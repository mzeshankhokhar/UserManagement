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
    public class UserContactRepository : GenericRepository<UserContact>, IUserContactRepository
    {
        public UserContactRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<UserContact>> GetContactsByUserAsync(int userId)
        {
            return await _context.UserContact.Where(x => x.UserId == userId).ToListAsync();
        }

        public async Task<List<User>> GetRegisteredUsersAsync(List<string> phoneNumbers, List<string> emails)
        {
            return await _context.Users
                    .Where(u => phoneNumbers.Contains(u.PhoneNumber) || emails.Contains(u.Email))
                    .ToListAsync();
        }

        public async Task<IEnumerable<UserContact>> GetContactsAsync(int userId) 
        {
            return await _context.UserContact
                        .AsNoTracking()
                        .Include(x => x.User)
                        .Include(x => x.RegisteredUser)
                        .Where(c => c.UserId == userId && c.RegisteredUserId != userId)
                        .OrderByDescending(c => c.IsFavorite)
                        .ThenByDescending(c => c.UploadedDate)
                        .ToListAsync();
        }

        public async Task<UserContact> GetContactByIdAsync(int contactId, int userId)
        {
            return await _context.UserContact
                        .AsNoTracking()
                        .Include(x => x.User)
                        .Include(x => x.RegisteredUser)
                        .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId && c.RegisteredUserId != contactId);
        }

        public async Task AddContactAsync(UserContact contact, int userId)
        {
            _context.UserContact.Add(contact);
            await _context.SaveChangesAsync();
        }

        public async Task AddContactsAsync(List<UserContact> contacts, int userId)
        {
            // assign userId to all
            contacts.ForEach(x => x.UserId = userId);

            // fetch all existing phone + email combinations for this user
            var existingContacts = await _context.UserContact
                .Where(x => x.UserId == userId)
                .Select(x => new { x.PhoneNumber, x.Email })
                .ToListAsync();

            // filter only new contacts (no duplicate phone or email)
            var newContacts = contacts
                .Where(c =>
                    !existingContacts.Any(e =>
                        (e.PhoneNumber != null && e.PhoneNumber == c.PhoneNumber) ||
                        (e.Email != null && e.Email == c.Email)))
                .ToList();

            if (newContacts.Count == 0)
                return; // nothing new to add

            _context.UserContact.AddRange(newContacts);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteContactAsync(int id)
        {
            var contact = await _context.UserContact.FindAsync(id);
            if (contact != null)
            {
                _context.UserContact.Remove(contact);
                await _context.SaveChangesAsync();
            }
        }
    }
}
