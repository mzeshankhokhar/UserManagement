using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagement.Core.Model;

namespace UserManagement.Repository.Seeds
{
    public class ClaimSeed : IEntityTypeConfiguration<Claim>
    {
        public void Configure(EntityTypeBuilder<Claim> builder)
        {
            builder.HasData(
                new Claim()
                {
                    Id = 1,
                    Value = "WEBACCESS",
                    Type = "Admin",
                    Issuer = "UserManagementAPI",
                    OriginalIssuer = "www.UserManagement.com",
                    UpdatedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                },
                new Claim()
                {
                    Id = 2,
                    Value = "APIACCESS",
                    Type = "Admin",
                    Issuer = "UserManagementAPI",
                    OriginalIssuer = "www.UserManagement.com",
                    UpdatedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                }
                ,
                new Claim()
                {
                    Id = 3,
                    Value = "APPACCESS",
                    Type = "Admin",
                    Issuer = "UserManagementAPI",
                    OriginalIssuer = "www.UserManagement.com",
                    UpdatedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                }
            );
        }
    }
}