using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkGuardiao.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            await context.Database.MigrateAsync(cancellationToken);

            var seedEnabled = configuration.GetValue<bool>("Seed:Enable");
            if (!seedEnabled)
            {
                return;
            }

            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var demoEmail = configuration.GetValue<string>("Seed:DemoUserEmail") ?? "demo@linkguardiao.local";
            var demoPassword = configuration.GetValue<string>("Seed:DemoUserPassword") ?? "ChangeMe123!";

            var hasUsers = await context.Users.AnyAsync(cancellationToken);
            if (!hasUsers)
            {
                var demoUser = new User
                {
                    Username = "Demo",
                    Email = demoEmail,
                    PasswordHash = passwordHasher.Hash(demoPassword),
                    CreatedAt = DateTime.UtcNow,
                    IsAdmin = false
                };

                context.Users.Add(demoUser);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
