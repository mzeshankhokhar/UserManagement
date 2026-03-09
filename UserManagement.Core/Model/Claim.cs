using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    public class Claim : BaseEntity
    {
        public string Type { get; set; } // Corresponds to Claim.Type, e.g., "Role" or "Permission"
        public string Value { get; set; } // Corresponds to Claim.Value, e.g., "Admin" or "ReadOnly"
        public string Issuer { get; set; } // The issuer of the claim, e.g., "LocalAuthority"
        public string OriginalIssuer { get; set; } // The original issuer, if different from Issuer
        public ICollection<RoleClaims> RoleClaims { get; set; } // Many-to-many with Role
    }
}
