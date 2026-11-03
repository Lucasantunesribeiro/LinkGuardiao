using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LinkGuardiao.Api.Tests
{
    /// <summary>
    /// Configuracao da demo gratuita (Render + Neon): Production, STORAGE_PROVIDER=postgresql, sem AWS.
    /// Antes a API exigia nomes de tabelas DynamoDB em producao e nao subia nesse modo.
    /// </summary>
    public class PostgreSqlProductionStartupTests
    {
        private sealed class ProductionPostgreSqlFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("STORAGE_PROVIDER", "postgresql");
                // Nao conecta: o host so e construido, nenhuma requisicao toca o banco.
                builder.UseSetting("ConnectionStrings:PostgreSQL", "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x");
                builder.UseSetting("Jwt:Secret", "test-secret-with-at-least-32-characters!!");
                builder.UseSetting("Jwt:Issuer", "LinkGuardiao");
                builder.UseSetting("Jwt:Audience", "LinkGuardiao");
                builder.UseSetting("CORS_ALLOWED_ORIGIN", "https://linkguardiao.pages.dev");
            }
        }

        [Fact]
        public void Host_StartsWithoutDynamoDbTables_AndRecordsClicksDirectly()
        {
            using var factory = new ProductionPostgreSqlFactory();
            using var scope = factory.Services.CreateScope();

            var queue = scope.ServiceProvider.GetRequiredService<IAnalyticsQueue>();

            Assert.IsType<DirectAnalyticsQueue>(queue);
        }

        [Fact]
        public async Task Health_WithoutAwsRegion_ReportsAnalyticsQueueDisabledInsteadOf500()
        {
            using var factory = new ProductionPostgreSqlFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/health");
            var body = await response.Content.ReadAsStringAsync();

            // O banco de teste nao existe (storage Unhealthy => 503), mas nada pode estourar em 500.
            Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
            using var json = System.Text.Json.JsonDocument.Parse(body);
            var analytics = json.RootElement.GetProperty("checks").EnumerateArray()
                .Single(check => check.GetProperty("name").GetString() == "analytics_queue");
            Assert.Equal("Healthy", analytics.GetProperty("status").GetString());
        }
    }
}
