using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Core.Model
{
    internal class Group: BaseEntity
    {
        [ForeignKey("User")]
        public int GroupOwnerId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public GroupType Type { get; set; } = GroupType.Friends;

        public virtual User GroupOwner { get; set; }
    }

    public enum GroupType
    {
        Personal = 0,
        Family = 1,
        Friends = 2,
        Work = 3,
        Travel = 4,
        Custom = 5
    }
}
