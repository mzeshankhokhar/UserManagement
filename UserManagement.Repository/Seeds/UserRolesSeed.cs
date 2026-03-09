using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagement.Core.Model;

namespace UserManagement.Repository.Seeds
{
    public class UserRolesSeed : IEntityTypeConfiguration<UserRoles>
    {
        public void Configure(EntityTypeBuilder<UserRoles> builder)
        {
            builder.HasData(
                // Admin user gets SITEADMIN role (Id = 1)
                new UserRoles()
                {
                    RoleId = 1, // SITEADMIN
                    UserId = 1, // admin user
                    UpdatedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                },
                // Admin user also gets ADMIN role (Id = 2)
                new UserRoles()
                {
                    RoleId = 2, // ADMIN
                    UserId = 1, // admin user
                    UpdatedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                }
            );
        }
    }
}