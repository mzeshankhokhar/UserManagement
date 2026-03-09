using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagement.Core.Model;

namespace UserManagement.Repository.Seeds
{
    public class UserSeed : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasData(
                new User()
                {
                    Id = 1,
                    Email = "admin@gmail.com",
                    DateOfBirth = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    FirstName = "Site",
                    LastName = "Admin",
                    IsEmailConfirmed = true,
                    Password = "admin",
                    UpdatedDate = DateTime.UtcNow,
                    UserName = "admin",
                    SecurityStamp = "STATIC-ADMIN-STAMP-001",
                    // Password: admin (hashed)
                    PasswordHash = "AQAAAAIAAYagAAAAEESEQTeNi2oFYvss9iXiFnzd7ReYP+R1lcbVZkC00En1dw1xeEGftkgBFQ4KZBaBHg==",
                }
            );
        }
    }
}