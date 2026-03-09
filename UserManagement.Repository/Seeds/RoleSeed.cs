using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagement.Core.Model;

namespace UserManagement.Repository.Seeds
{
    public class RoleSeed : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasData(
                new Role()
                {
                    Id = 1,
                    UpdatedDate = DateTime.UtcNow,
                    Name = "SITEADMIN",
                    Description = "Site Administrator with full system access including impersonation.",
                    CreatedDate = DateTime.UtcNow,
                },
                new Role()
                {
                    Id = 2,
                    UpdatedDate = DateTime.UtcNow,
                    Name = "ADMIN",
                    Description = "Admin with access to manage users and impersonation.",
                    CreatedDate = DateTime.UtcNow,
                },
                new Role()
                {
                    Id = 3,
                    UpdatedDate = DateTime.UtcNow,
                    Name = "NORMALUSER",
                    Description = "Normal user with access to user-specific pages only.",
                    CreatedDate = DateTime.UtcNow,
                }
            );
        }
    }
}
