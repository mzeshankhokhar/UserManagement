using Microsoft.AspNetCore.Identity;
using UserManagement.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Services
{
    public interface IClaimIdentityService : IUserClaimStore<Claim>
    {
    }
}
