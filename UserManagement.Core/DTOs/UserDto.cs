using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.DTOs
{
    public class UserDto : BaseDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Computed property for FullName
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public bool? IsPhoneNumberConfirmed { get; set; }
        public bool? IsEmailConfirmed { get; set; }
        public DateTime? DateOfBirth { get; set; }

    }
}
