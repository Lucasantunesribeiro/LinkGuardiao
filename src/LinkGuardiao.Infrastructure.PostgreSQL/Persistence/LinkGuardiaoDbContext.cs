using LinkGuardiao.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkGuardiao.Infrastructure.PostgreSQL.Persistence
{
    public class LinkGuardiaoDbContext : DbContext
    {
        public LinkGuardiaoDbContext(DbContextOptions<LinkGuardiaoDbContext> options) : base(options) { }

        public DbSet<ShortenedLink> Links { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<LinkAccess> AccessLogs { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ShortenedLink>(e =>
            {
                e.ToTable("links");
                e.HasKey(l => l.Id);
                e.HasIndex(l => l.ShortCode).IsUnique();
                e.HasIndex(l => new { l.UserId, l.CreatedAt });
                e.Property(l => l.CreatedAt).HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                e.Property(l => l.ExpiresAt).HasConversion(
                    v => v.HasValue ? v.Value.ToUniversalTime() : (DateTime?)null,
                    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);
            });

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");
                e.HasKey(u => u.Id);
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.CreatedAt).HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            });

            modelBuilder.Entity<LinkAccess>(e =>
            {
                e.ToTable("access_logs");
                e.HasKey(a => a.Id);
                e.HasIndex(a => new { a.ShortCode, a.AccessTime });
                e.Property(a => a.AccessTime).HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            });

            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.ToTable("refresh_tokens");
                e.HasKey(r => r.TokenHash);
                e.HasIndex(r => r.UserId);
                e.Property(r => r.CreatedAt).HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                e.Property(r => r.ExpiresAt).HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            });
        }
    }
}
