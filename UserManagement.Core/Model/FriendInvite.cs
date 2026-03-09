using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    internal class FriendInvite
    {
        [ForeignKey("User")]
        public int InviterId { get; set; }          // Registered user sending the invite
        public string InviteeEmail { get; set; }    // Optional: email of invitee
        public string InviteePhone { get; set; }    // Optional: phone number
        public string InviteCode { get; set; } = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                                            .Replace("=", "")
                                            .Replace("+", "")
                                            .Replace("/", "")
                                            .Substring(0, 10);    // Unique code or token for joining
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedDate { get; set; }
        public bool IsAccepted { get; set; } = false;
        public string Channel { get; set; }         // "Email", "SMS", "WhatsApp"
        public string Message { get; set; }         // Optional message

        // Link once user joins
        [ForeignKey("User")]
        public int? RegisteredUserId { get; set; }

        // Navigation properties
        public virtual User Inviter { get; set; }
        public virtual User RegisteredUser { get; set; }
    }
}
