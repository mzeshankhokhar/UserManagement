using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UserManagement.Core.DTOs
{
    public class UserContactDto : BaseDto
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string NormalizePhoneNumber => PhoneNumber?.Trim().Replace(" ", "").Replace("-", "");
        public string Email { get; set; }
        public bool IsValidPhoneNumber { get; set; }

        // ✅ Computed based on RegisteredUserId
        public bool IsRegisteredUser => RegisteredUserId != null;
        public int? RegisteredUserId { get; set; }
        public bool IsFavorite { get; set; } = false;
        public bool IsInvited { get; set; } = false;
    }

}
