using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    internal class GroupMembers
    {
        [ForeignKey("Group")]
        public int GroupId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
        public bool IsAdmin { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Optional custom name or role in group
        public string DisplayName { get; set; }

        // Navigation properties
        public virtual Group Group { get; set; }
        public virtual User User { get; set; }
    }
}
