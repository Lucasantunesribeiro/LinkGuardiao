using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkGuardiao.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ShortenedLink> ShortenedLinks { get; set; } = null!;
        public DbSet<LinkAccess> LinkAccesses { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<ShortenedLink>()
                .HasOne(l => l.User)
                .WithMany(u => u.ShortenedLinks)
                .HasForeignKey(l => l.UserId);
                
            modelBuilder.Entity<LinkAccess>()
                .HasOne(a => a.ShortenedLink)
                .WithMany(l => l.Accesses)
                .HasForeignKey(a => a.ShortenedLinkId);
                
            // Add indexes
            modelBuilder.Entity<ShortenedLink>()
                .HasIndex(l => l.ShortCode)
                .IsUnique();

            modelBuilder.Entity<ShortenedLink>()
                .HasIndex(l => l.UserId);

            modelBuilder.Entity<LinkAccess>()
                .HasIndex(a => a.ShortenedLinkId);
        }
    }
}
