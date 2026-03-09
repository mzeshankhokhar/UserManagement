using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    internal class Friend : BaseEntity
    {
        [ForeignKey("User")]
        public int UserId { get; set; }           // The user who owns this friendship

        [ForeignKey("User")]
        public int FriendUserId { get; set; }     // The friend's user ID

        public bool IsFavorite { get; set; } = false;
        public string NickName { get; set; }

        // Navigation properties
        public virtual User User { get; set; }             // Main user reference
        public virtual User FriendUser { get; set; }       // Friend user reference

        // Optional — friend group associations
        public virtual ICollection<Group> Groups { get; set; }
    }
}
