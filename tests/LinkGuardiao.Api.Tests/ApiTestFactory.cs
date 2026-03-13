using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;
using Amazon.DynamoDBv2;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.Caching;

namespace LinkGuardiao.Api.Tests
{
    public class ApiTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public new async Task DisposeAsync()
        {
            await Task.CompletedTask;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "dev-secret-change-me-please-32chars",
                    ["Jwt:Issuer"] = "LinkGuardiao",
                    ["Jwt:Audience"] = "LinkGuardiao",
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                    ["DynamoDb:LinksTableName"] = "test-links",
                    ["DynamoDb:UsersTableName"] = "test-users",
                    ["DynamoDb:AccessTableName"] = "test-access",
                    ["DynamoDb:DailyLimitsTableName"] = "test-limits",
                    ["DynamoDb:RefreshTokensTableName"] = "test-refresh",
                    ["DynamoDb:EmailLocksTableName"] = "test-email-locks",
                    ["DynamoDb:ServiceUrl"] = string.Empty,
                    ["ConnectionStrings:Redis"] = string.Empty
                };

                config.AddInMemoryCollection(settings);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IAmazonDynamoDB));
                services.RemoveAll(typeof(ILinkRepository));
                services.RemoveAll(typeof(IUserRepository));
                services.RemoveAll(typeof(IAccessLogRepository));
                services.RemoveAll(typeof(IDailyLimitStore));
                services.RemoveAll(typeof(IRefreshTokenRepository));
                services.RemoveAll(typeof(IAnalyticsQueue));
                services.RemoveAll(typeof(Amazon.SQS.IAmazonSQS));
                services.RemoveAll(typeof(ILinkReadCache));
                services.RemoveAll(typeof(IDistributedCache));

                services.AddSingleton<ILinkRepository, InMemoryLinkRepository>();
                services.AddSingleton<IUserRepository, InMemoryUserRepository>();
                services.AddSingleton<IAccessLogRepository, InMemoryAccessLogRepository>();
                services.AddSingleton<IDailyLimitStore, AllowAllDailyLimitStore>();
                services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();
                services.AddDistributedMemoryCache();
                services.AddSingleton<ILinkReadCache, NoOpLinkReadCache>();
                // InMemoryAnalyticsQueue auto-injects IAccessLogRepository and ILinkRepository
                services.AddSingleton<InMemoryAnalyticsQueue>();
                services.AddSingleton<IAnalyticsQueue>(sp => sp.GetRequiredService<InMemoryAnalyticsQueue>());

            });
        }
    }
}
