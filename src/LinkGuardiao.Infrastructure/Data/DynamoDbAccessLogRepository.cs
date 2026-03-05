using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace LinkGuardiao.Infrastructure.Data
{
    public class DynamoDbAccessLogRepository : IAccessLogRepository
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly DynamoDbOptions _options;

        public DynamoDbAccessLogRepository(IAmazonDynamoDB dynamoDb, IOptions<DynamoDbOptions> options)
        {
            _dynamoDb = dynamoDb;
            _options = options.Value;
        }

        public Task RecordAccessAsync(LinkAccess access, CancellationToken cancellationToken = default)
        {
            var expiresAt = access.AccessTime.AddDays(_options.AccessRetentionDays);
            var request = new PutItemRequest
            {
                TableName = _options.AccessTableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    ["shortCode"] = new AttributeValue { S = access.ShortCode },
                    ["accessTime"] = new AttributeValue { S = access.AccessTime.ToString("O", CultureInfo.InvariantCulture) },
                    ["ipAddress"] = new AttributeValue { S = access.IpAddress },
                    ["userAgent"] = new AttributeValue { S = access.UserAgent ?? string.Empty },
                    ["referrerUrl"] = new AttributeValue { S = access.ReferrerUrl ?? string.Empty },
                    ["browser"] = new AttributeValue { S = access.Browser ?? string.Empty },
                    ["operatingSystem"] = new AttributeValue { S = access.OperatingSystem ?? string.Empty },
                    ["deviceType"] = new AttributeValue { S = access.DeviceType ?? string.Empty },
                    ["expiresAtEpoch"] = new AttributeValue { N = ToEpochSeconds(expiresAt).ToString(CultureInfo.InvariantCulture) }
                }
            };

            return _dynamoDb.PutItemAsync(request, cancellationToken);
        }

        public async Task<IReadOnlyList<LinkAccess>> ListAccessesAsync(string shortCode, int limit, CancellationToken cancellationToken = default)
        {
            var request = new QueryRequest
            {
                TableName = _options.AccessTableName,
                KeyConditionExpression = "shortCode = :shortCode",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":shortCode"] = new AttributeValue { S = shortCode }
                },
                ScanIndexForward = false,
                Limit = limit
            };

            var response = await _dynamoDb.QueryAsync(request, cancellationToken);
            return response.Items.Select(MapAccess).ToList();
        }

        private static LinkAccess MapAccess(Dictionary<string, AttributeValue> item)
        {
            return new LinkAccess
            {
                Id = Guid.NewGuid().ToString("N"),
                ShortCode = item["shortCode"].S,
                AccessTime = DateTime.Parse(item["accessTime"].S, null, DateTimeStyles.RoundtripKind),
                IpAddress = item.TryGetValue("ipAddress", out var ip) ? ip.S : string.Empty,
                UserAgent = item.TryGetValue("userAgent", out var ua) ? ua.S : null,
                ReferrerUrl = item.TryGetValue("referrerUrl", out var referrer) ? referrer.S : null,
                Browser = item.TryGetValue("browser", out var browser) && !string.IsNullOrWhiteSpace(browser.S) ? browser.S : null,
                OperatingSystem = item.TryGetValue("operatingSystem", out var os) && !string.IsNullOrWhiteSpace(os.S) ? os.S : null,
                DeviceType = item.TryGetValue("deviceType", out var device) && !string.IsNullOrWhiteSpace(device.S) ? device.S : null
            };
        }

        private static long ToEpochSeconds(DateTime value)
        {
            return new DateTimeOffset(value).ToUnixTimeSeconds();
        }
    }
}
