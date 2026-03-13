using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using LinkGuardiao.AnalyticsConsumer;
using LinkGuardiao.Application.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LinkGuardiao.Api.Tests
{
    public sealed class AnalyticsConsumerTests
    {
        [Fact]
        public async Task FunctionHandler_DuplicateRedelivery_IncrementsClickCountOnce()
        {
            const string shortCode = "dup123";
            var links = new InMemoryLinkRepository();
            await links.TryCreateAsync(new ShortenedLink
            {
                Id = shortCode,
                ShortCode = shortCode,
                OriginalUrl = "https://example.com",
                UserId = "user-1",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

            var accessLogs = new InMemoryAccessLogRepository();
            var function = new Function(accessLogs, links, NullLogger<Function>.Instance);

            var payload = JsonSerializer.Serialize(new AccessLogMessage
            {
                Id = "evt-123",
                ShortCode = shortCode,
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0",
                AccessTime = new DateTime(2026, 3, 13, 12, 0, 0, DateTimeKind.Utc)
            });

            var sqsEvent = new SQSEvent
            {
                Records =
                [
                    new SQSEvent.SQSMessage { MessageId = "msg-1", Body = payload },
                    new SQSEvent.SQSMessage { MessageId = "msg-2", Body = payload }
                ]
            };

            await function.FunctionHandler(sqsEvent, new StubLambdaContext());

            var link = await links.GetByShortCodeAsync(shortCode);
            Assert.NotNull(link);
            Assert.Equal(1, link!.ClickCount);

            var accesses = await accessLogs.ListAccessesAsync(shortCode, 10);
            Assert.Single(accesses);
        }

        private sealed class StubLambdaContext : ILambdaContext
        {
            public string AwsRequestId => "test-request";
            public IClientContext ClientContext => throw new NotSupportedException();
            public string FunctionName => "analytics-consumer-tests";
            public string FunctionVersion => "1";
            public ICognitoIdentity Identity => throw new NotSupportedException();
            public string InvokedFunctionArn => "arn:aws:lambda:sa-east-1:123456789012:function:test";
            public ILambdaLogger Logger => new StubLambdaLogger();
            public string LogGroupName => "/aws/lambda/test";
            public string LogStreamName => "tests";
            public int MemoryLimitInMB => 256;
            public TimeSpan RemainingTime => TimeSpan.FromMinutes(5);
        }

        private sealed class StubLambdaLogger : ILambdaLogger
        {
            public void Log(string message) { }
            public void LogLine(string message) { }
        }
    }
}
