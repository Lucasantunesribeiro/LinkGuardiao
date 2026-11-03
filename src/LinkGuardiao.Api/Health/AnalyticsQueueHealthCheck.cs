using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LinkGuardiao.Api.Health
{
    public sealed class AnalyticsQueueHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;

        // O cliente SQS so e resolvido quando a fila esta configurada: resolve-lo no construtor exigia
        // AWS_REGION mesmo sem SQS e derrubava /health com 500 no modo PostgreSQL sem AWS.
        public AnalyticsQueueHealthCheck(IServiceProvider services, IConfiguration configuration)
        {
            _services = services;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var queueUrl = _configuration["SQS_ANALYTICS_QUEUE_URL"];
            if (string.IsNullOrWhiteSpace(queueUrl))
            {
                return HealthCheckResult.Healthy("Analytics queue is disabled.");
            }

            var sqs = _services.GetService<IAmazonSQS>();
            if (sqs == null)
            {
                return HealthCheckResult.Unhealthy("Analytics queue is configured but SQS client is unavailable.");
            }

            await sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                AttributeNames = new List<string> { QueueAttributeName.QueueArn }
            }, cancellationToken);

            return HealthCheckResult.Healthy("Analytics queue is healthy.");
        }
    }
}
