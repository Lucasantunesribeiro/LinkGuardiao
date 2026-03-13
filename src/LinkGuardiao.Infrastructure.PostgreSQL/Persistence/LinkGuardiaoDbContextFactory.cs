using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LinkGuardiao.Infrastructure.PostgreSQL.Persistence
{
    public class LinkGuardiaoDbContextFactory : IDesignTimeDbContextFactory<LinkGuardiaoDbContext>
    {
        public LinkGuardiaoDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING")
                ?? "Host=localhost;Port=54329;Database=linkguardiao;Username=linkguardiao;Password=linkguardiao";

            var builder = new DbContextOptionsBuilder<LinkGuardiaoDbContext>();
            builder.UseNpgsql(connectionString);
            return new LinkGuardiaoDbContext(builder.Options);
        }
    }
}
