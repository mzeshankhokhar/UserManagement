using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    public class UserRoles
    {
        public int UserId { get; set; } // Foreign key
        [ForeignKey("UserId")]
        public User User { get; set; } //Navigation Property

        public int RoleId { get; set; } // Foreign key
       
        [ForeignKey("RoleId")]
        public Role Role { get; set; } //Navigation Property

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
