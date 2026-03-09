using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    public class UserInvitation : BaseEntity
    {
        
        public int InviterId { get; set; }
        public string InviteCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool InvitationSentByEmail { get; set; }
        public bool InvitationSentByPhone { get; set; }
        public bool IsJoined { get; set; } = false;
        public bool IsExpired { get; set; } = false;

        // Navigation
        [ForeignKey("InviterId")]
        public virtual User Inviter { get; set; }
    }
}
