using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Telemetry;
using LinkGuardiao.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace LinkGuardiao.Infrastructure.Messaging
{
    public class SqsAnalyticsQueue : IAnalyticsQueue
    {
        private readonly IAmazonSQS _sqs;
        private readonly SqsOptions _options;

        public SqsAnalyticsQueue(IAmazonSQS sqs, IOptions<SqsOptions> options)
        {
            _sqs = sqs;
            _options = options.Value;
        }

        public Task EnqueueAsync(AccessLogMessage message, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(message);
            LinkGuardiaoMetrics.RecordAnalyticsMessageEnqueued();
            return _sqs.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _options.AnalyticsQueueUrl,
                MessageBody = json
            }, ct);
        }
    }
}
