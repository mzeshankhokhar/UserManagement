using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagement.Core.Model;

namespace UserManagement.Repository.Seeds
{
    public class RoleClaimsSeed : IEntityTypeConfiguration<RoleClaims>
    {
        public void Configure(EntityTypeBuilder<RoleClaims> builder)
        {
            builder.HasData(
                new RoleClaims()
                {
                    RoleId = 1,
                    ClaimId = 1,
                    UpdatedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                },
                new RoleClaims()
                {
                    RoleId = 1,
                    ClaimId = 2,
                    UpdatedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                }
            );
        }
    }
}