using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    public class RoleClaims
    {
        [ForeignKey("Role")]
        public int RoleId { get; set; } // Foreign key
        public Role Role { get; set; } //Navigation Property

        [ForeignKey("Claim")]
        public int ClaimId { get; set; } // Foreign key
        public Claim Claim { get; set; } // Navigation Property

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
