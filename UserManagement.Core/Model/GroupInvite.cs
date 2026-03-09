using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    internal class GroupInvite : BaseEntity
    {
        [ForeignKey("Group")]
        public int GroupId { get; set; }               // Group being invited to
        [ForeignKey("User")]
        public int InviterId { get; set; }             // Registered user sending the invite

        public string InviteeEmail { get; set; }       // Optional
        public string InviteePhone { get; set; }       // Optional
        public string Channel { get; set; }            // "Email", "SMS", "WhatsApp"
        public string Message { get; set; }            // Optional custom note

        public string InviteCode { get; set; } = Guid.NewGuid().ToString(); // Unique code
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedDate { get; set; }
        public bool IsAccepted { get; set; } = false;

        // Once user registers or accepts
        [ForeignKey("User")]
        public int? RegisteredUserId { get; set; }

        // Navigation properties
        public virtual Group Group { get; set; }
        public virtual User Inviter { get; set; }
        public virtual User RegisteredUser { get; set; }
    }
}
