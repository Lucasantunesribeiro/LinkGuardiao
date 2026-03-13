using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LinkGuardiao.Api.Health
{
    public sealed class AnalyticsQueueHealthCheck : IHealthCheck
    {
        private readonly IAmazonSQS? _sqs;
        private readonly IConfiguration _configuration;

        public AnalyticsQueueHealthCheck(IAmazonSQS? sqs, IConfiguration configuration)
        {
            _sqs = sqs;
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

            if (_sqs == null)
            {
                return HealthCheckResult.Unhealthy("Analytics queue is configured but SQS client is unavailable.");
            }

            await _sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                AttributeNames = new List<string> { QueueAttributeName.QueueArn }
            }, cancellationToken);

            return HealthCheckResult.Healthy("Analytics queue is healthy.");
        }
    }
}
