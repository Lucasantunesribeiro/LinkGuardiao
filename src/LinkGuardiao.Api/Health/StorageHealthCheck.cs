using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LinkGuardiao.Infrastructure.Options;
using LinkGuardiao.Infrastructure.PostgreSQL.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LinkGuardiao.Api.Health
{
    public sealed class StorageHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly DynamoDbOptions _dynamoDbOptions;

        public StorageHealthCheck(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IOptions<DynamoDbOptions> dynamoDbOptions)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _dynamoDbOptions = dynamoDbOptions.Value;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var storageProvider = _configuration["STORAGE_PROVIDER"] ?? "dynamodb";

            using var scope = _serviceProvider.CreateScope();
            if (storageProvider.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
            {
                var dbContext = scope.ServiceProvider.GetService<LinkGuardiaoDbContext>();
                if (dbContext == null)
                {
                    return HealthCheckResult.Unhealthy("PostgreSQL provider is selected but DbContext is not registered.");
                }

                return await dbContext.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy("PostgreSQL connection is healthy.")
                    : HealthCheckResult.Unhealthy("PostgreSQL connection failed.");
            }

            var dynamoDb = scope.ServiceProvider.GetService<IAmazonDynamoDB>();
            if (dynamoDb == null)
            {
                return HealthCheckResult.Unhealthy("DynamoDB provider is selected but the client is not registered.");
            }

            if (string.IsNullOrWhiteSpace(_dynamoDbOptions.LinksTableName))
            {
                return HealthCheckResult.Unhealthy("DynamoDB links table is not configured.");
            }

            await dynamoDb.DescribeTableAsync(new DescribeTableRequest
            {
                TableName = _dynamoDbOptions.LinksTableName
            }, cancellationToken);

            return HealthCheckResult.Healthy("DynamoDB connection is healthy.");
        }
    }
}
