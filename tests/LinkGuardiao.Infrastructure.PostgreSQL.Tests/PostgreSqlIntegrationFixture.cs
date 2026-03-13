using LinkGuardiao.Infrastructure.PostgreSQL.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace LinkGuardiao.Infrastructure.PostgreSQL.Tests
{
    public sealed class PostgreSqlIntegrationFixture : IAsyncLifetime
    {
        private PostgreSqlContainer? _container;

        public bool IsAvailable { get; private set; }

        public string SkipReason { get; private set; } =
            "Set RUN_POSTGRESQL_TESTCONTAINERS=true to execute the PostgreSQL integration suite.";

        public async Task InitializeAsync()
        {
            if (!ShouldRun())
            {
                return;
            }

            try
            {
                _container = new PostgreSqlBuilder("postgres:16-alpine")
                    .WithDatabase("linkguardiao_tests")
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .Build();

                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _container.StartAsync(cancellationTokenSource.Token);
                await ResetDatabaseAsync();

                IsAvailable = true;
                SkipReason = string.Empty;
            }
            catch (Exception ex)
            {
                SkipReason = $"PostgreSQL Testcontainers unavailable: {ex.Message}";
                if (_container is not null)
                {
                    await _container.DisposeAsync().AsTask();
                    _container = null;
                }
            }
        }

        public async Task DisposeAsync()
        {
            if (_container is not null)
            {
                await _container.DisposeAsync().AsTask();
            }
        }

        public async Task ResetDatabaseAsync()
        {
            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        public LinkGuardiaoDbContext CreateDbContext()
        {
            if (_container is null)
            {
                throw new InvalidOperationException("PostgreSQL test container is not running.");
            }

            var options = new DbContextOptionsBuilder<LinkGuardiaoDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options;

            return new LinkGuardiaoDbContext(options);
        }

        private static bool ShouldRun()
        {
            return string.Equals(
                Environment.GetEnvironmentVariable("RUN_POSTGRESQL_TESTCONTAINERS"),
                "true",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
