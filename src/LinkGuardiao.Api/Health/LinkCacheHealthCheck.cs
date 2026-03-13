using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LinkGuardiao.Api.Health
{
    public sealed class LinkCacheHealthCheck : IHealthCheck
    {
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;

        public LinkCacheHealthCheck(IDistributedCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var redisConnectionString = _configuration.GetConnectionString("Redis")
                ?? _configuration["REDIS_CONNECTION_STRING"];

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                return HealthCheckResult.Healthy("Using in-memory distributed cache.");
            }

            const string healthKey = "linkguardiao:health:cache";
            await _cache.SetStringAsync(
                healthKey,
                "ok",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                },
                cancellationToken);

            var value = await _cache.GetStringAsync(healthKey, cancellationToken);
            return value == "ok"
                ? HealthCheckResult.Healthy("Redis cache is healthy.")
                : HealthCheckResult.Unhealthy("Redis cache roundtrip failed.");
        }
    }
}
