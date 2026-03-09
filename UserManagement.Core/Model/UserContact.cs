using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    public class UserContact : BaseEntity
    {
        
        public int UserId { get; set; }                  // The owner of this contact list
        public string Name { get; set; }                 // Contact name from phone
        public string PhoneNumber { get; set; }          // Normalized phone number (E.164 format)
        public string NormalizePhoneNumber => PhoneNumber?.Trim().Replace(" ", "").Replace("-", "");
        public bool IsValidPhoneNumber => IsValidPhone(PhoneNumber);
        public string Email { get; set; }
        public bool IsRegistered { get; set; } = false;  // True if this contact is already a UserManagement user
        public int? RegisteredUserId { get; set; }       // Link to registered user if exists
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        // Optional flags
        public bool IsFavorite { get; set; } = false;
        public bool IsInvited { get; set; } = false;     // True if invitation sent

        // Navigation
        [ForeignKey("UserId")]
        public virtual User User { get; set; }           // The user who uploaded
        [ForeignKey("RegisteredUserId")]
        public virtual User RegisteredUser { get; set; } // Linked app user (if any)

        /// <summary>
        /// Basic regex validation for international or local numbers
        /// </summary>
        private bool IsValidPhone(string phoneNumber)
        {
            // Accepts +923001234567 or 03001234567
            var pattern = @"^(?:\+?92|0)?3\d{9}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }
    }
}
