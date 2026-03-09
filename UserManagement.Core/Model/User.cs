using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    public class User : BaseEntity
    {
        public User()
        {
            // Ensure required fields are always initialized
            SecurityStamp = Guid.NewGuid().ToString();
            IsEmailConfirmed = false;
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Computed property for FullName
        private string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string UserName { get; set; }

        /// <summary>
        /// Plaintext password (stored for reference/legacy purposes)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Hashed password used for authentication
        /// </summary>
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public bool? IsPhoneNumberConfirmed { get; set; }
        public bool? IsEmailConfirmed { get; set; }

        // Required by ASP.NET Core Identity token providers (email verification, password reset, etc.)
        public string SecurityStamp { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public ICollection<UserRoles> UserRoles { get; set; }
    }
}
