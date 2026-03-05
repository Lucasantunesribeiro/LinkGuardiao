using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace LinkGuardiao.Infrastructure.Data
{
    public class DynamoDbDailyLimitStore : IDailyLimitStore
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly DynamoDbOptions _options;

        public DynamoDbDailyLimitStore(IAmazonDynamoDB dynamoDb, IOptions<DynamoDbOptions> options)
        {
            _dynamoDb = dynamoDb;
            _options = options.Value;
        }

        public async Task<bool> TryConsumeAsync(string userId, int limit, CancellationToken cancellationToken = default)
        {
            if (limit <= 0)
            {
                return true;
            }

            var now = DateTime.UtcNow;
            var dayKey = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var key = $"USER#{userId}#DATE#{dayKey}";
            var expiresAt = new DateTimeOffset(now.Date.AddDays(2)).ToUnixTimeSeconds();

            var request = new UpdateItemRequest
            {
                TableName = _options.DailyLimitsTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["key"] = new AttributeValue { S = key }
                },
                UpdateExpression = "SET #count = if_not_exists(#count, :zero) + :inc, #expiresAtEpoch = :ttl",
                ConditionExpression = "attribute_not_exists(#count) OR #count < :limit",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#count"] = "count",
                    ["#expiresAtEpoch"] = "expiresAtEpoch"
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":zero"] = new AttributeValue { N = "0" },
                    [":inc"] = new AttributeValue { N = "1" },
                    [":limit"] = new AttributeValue { N = limit.ToString(CultureInfo.InvariantCulture) },
                    [":ttl"] = new AttributeValue { N = expiresAt.ToString(CultureInfo.InvariantCulture) }
                }
            };

            try
            {
                await _dynamoDb.UpdateItemAsync(request, cancellationToken);
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }
    }
}
