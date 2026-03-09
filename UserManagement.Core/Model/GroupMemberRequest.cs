using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    internal class GroupMemberRequest
    {
        [ForeignKey("Group")]
        public int GroupId { get; set; }               // Group where member is being requested
        [ForeignKey("User")]
        public int RequestedById { get; set; }         // Existing member making the request
        [ForeignKey("User")]
        public int? TargetUserId { get; set; }         // Existing user (if registered)
        public string TargetEmail { get; set; }        // Email (if not registered)
        public string TargetPhone { get; set; }        // Phone (if not registered)
        public string InviteCode { get; set; } = Guid.NewGuid().ToString(); // Optional unique link

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string Message { get; set; }             // Optional reason/note
        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedDate { get; set; }
        [ForeignKey("User")]
        public int? RespondedById { get; set; }         // Admin who approved/rejected

        // Navigation properties
        public virtual Group Group { get; set; }
        public virtual User RequestedBy { get; set; }
        public virtual User TargetUser { get; set; }
        public virtual User RespondedBy { get; set; }
    }
}
