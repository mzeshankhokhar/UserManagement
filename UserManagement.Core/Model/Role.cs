using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    public class Role : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<UserRoles> UserRoles { get; set; } //// Many-to-many with User
        public ICollection<RoleClaims> RoleClaims { get; set; } // Many-to-many with Claim
    }
}
