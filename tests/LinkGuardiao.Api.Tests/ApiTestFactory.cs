using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LinkGuardiao.Infrastructure.Data;
using Xunit;

namespace LinkGuardiao.Api.Tests
{
    public class ApiTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();
        }

        public new async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = _connection.ConnectionString,
                    ["Database:Provider"] = "Sqlite",
                    ["Jwt:Secret"] = "dev-secret-change-me-please-32chars",
                    ["Jwt:Issuer"] = "LinkGuardiao",
                    ["Jwt:Audience"] = "LinkGuardiao",
                    ["Seed:Enable"] = "false"
                };

                config.AddInMemoryCollection(settings);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                services.RemoveAll(typeof(ApplicationDbContext));
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
            });
        }
    }
}
