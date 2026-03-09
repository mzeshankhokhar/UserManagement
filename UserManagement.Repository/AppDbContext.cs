using System.Reflection;
using Microsoft.EntityFrameworkCore;
using UserManagement.Core;
using UserManagement.Core.Model;

namespace UserManagement.Repository
{
    public class AppDbContext : DbContext
    {
        // DbContextOptions dememizin sebebi yolu startup tarafında vereceğim
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<UserRoles> UserRoles { get; set; }
        public DbSet<RoleClaims> RoleClaims { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<UserContact> UserContact { get; set; }
        public DbSet<UserInvitation> UserInvitation { get; set; }
        public DbSet<DatabaseVersion> DatabaseVersions { get; set; }
        public DbSet<VerificationCode> VerificationCodes { get; set; }

        public override int SaveChanges()
        {
            foreach (var item in ChangeTracker.Entries())
            {
                if (item.Entity is BaseEntity entityReference)
                {
                    switch (item.Entity)
                    {
                        case EntityState.Added:
                        {
                            entityReference.CreatedDate = DateTime.Now;
                            break;
                        }
                        case EntityState.Modified:
                        {
                            entityReference.UpdatedDate = DateTime.Now;
                            break;
                        }
                    }
                }
            }
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {

            foreach (var item in ChangeTracker.Entries())
            {
                if (item.Entity is BaseEntity entityReference)
                {
                    switch (item.State)
                    {
                        case EntityState.Added:
                        {
                            entityReference.CreatedDate = DateTime.Now;
                            break;
                        }
                        case EntityState.Modified:
                        {
                            Entry(entityReference).Property(x => x.CreatedDate).IsModified = false;

                            entityReference.UpdatedDate = DateTime.Now;
                            break;
                        }
                    }
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Configuring UserRole many-to-many relationship
            modelBuilder.Entity<UserRoles>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Configuring RoleClaim many-to-many relationship
            modelBuilder.Entity<RoleClaims>()
                .HasKey(rc => new { rc.RoleId, rc.ClaimId });

            modelBuilder.Entity<RoleClaims>()
                .HasOne(rc => rc.Role)
                .WithMany(r => r.RoleClaims)
                .HasForeignKey(rc => rc.RoleId);

            modelBuilder.Entity<RoleClaims>()
                .HasOne(rc => rc.Claim)
                .WithMany(c => c.RoleClaims)
                .HasForeignKey(rc => rc.ClaimId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
