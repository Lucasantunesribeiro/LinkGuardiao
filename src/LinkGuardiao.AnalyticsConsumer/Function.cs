using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.Data;
using LinkGuardiao.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LinkGuardiao.AnalyticsConsumer
{
    public class Function
    {
        private readonly IAccessLogRepository _accessLogs;
        private readonly ILinkRepository _links;
        private readonly ILogger<Function> _logger;

        public Function()
        {
            var services = new ServiceCollection();

            var dynamoOptions = new DynamoDbOptions
            {
                LinksTableName = Environment.GetEnvironmentVariable("DDB_TABLE_LINKS") ?? string.Empty,
                AccessTableName = Environment.GetEnvironmentVariable("DDB_TABLE_ACCESS") ?? string.Empty,
                UsersTableName = Environment.GetEnvironmentVariable("DDB_TABLE_USERS") ?? string.Empty,
                DailyLimitsTableName = Environment.GetEnvironmentVariable("DDB_TABLE_DAILY_LIMITS") ?? string.Empty
            };

            services.AddSingleton(Options.Create(dynamoOptions));
            services.AddDefaultAWSOptions(new Amazon.Extensions.NETCore.Setup.AWSOptions());
            services.AddAWSService<IAmazonDynamoDB>();
            services.AddSingleton<ILinkRepository, DynamoDbLinkRepository>();
            services.AddSingleton<IAccessLogRepository, DynamoDbAccessLogRepository>();
            services.AddLogging(b => b.AddConsole());

            var provider = services.BuildServiceProvider();
            _accessLogs = provider.GetRequiredService<IAccessLogRepository>();
            _links = provider.GetRequiredService<ILinkRepository>();
            _logger = provider.GetRequiredService<ILogger<Function>>();
        }

        // Constructor for unit tests
        public Function(IAccessLogRepository accessLogs, ILinkRepository links, ILogger<Function> logger)
        {
            _accessLogs = accessLogs;
            _links = links;
            _logger = logger;
        }

        public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            foreach (var record in sqsEvent.Records)
            {
                try
                {
                    var message = JsonSerializer.Deserialize<AccessLogMessage>(record.Body);
                    if (message == null)
                    {
                        _logger.LogWarning("Null message body in SQS record {MessageId}", record.MessageId);
                        continue;
                    }

                    var access = new LinkAccess
                    {
                        Id = message.Id,
                        ShortCode = message.ShortCode,
                        IpAddress = message.IpAddress,
                        UserAgent = message.UserAgent,
                        ReferrerUrl = message.ReferrerUrl,
                        Browser = message.Browser,
                        OperatingSystem = message.OperatingSystem,
                        DeviceType = message.DeviceType,
                        AccessTime = message.AccessTime
                    };

                    await _accessLogs.RecordAccessAsync(access);
                    await _links.IncrementClickCountAsync(message.ShortCode);

                    _logger.LogInformation("Processed access log for {ShortCode}", message.ShortCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process SQS record {MessageId}", record.MessageId);
                    throw; // Let SQS retry; DLQ handles repeated failures
                }
            }
        }
    }
}
