using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.Core.Model
{
    public class FriendRequest : BaseEntity
    {
        [ForeignKey("User")]
        public int SenderId { get; set; }      // User who sent the request
        [ForeignKey("User")]
        public int ReceiverId { get; set; }    // User who receives the request
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedDate { get; set; }

        // Pending, Accepted, Rejected, Cancelled
        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

        // Optional message/note with request
        public string Message { get; set; }

        // Navigation properties
        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
    }

    public enum FriendRequestStatus { 
        Pending, 
        Accepted, 
        Rejected, 
        Cancelled 
    }

}
