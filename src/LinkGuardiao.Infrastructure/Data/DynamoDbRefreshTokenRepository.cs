using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace LinkGuardiao.Infrastructure.Data
{
    public class DynamoDbRefreshTokenRepository : IRefreshTokenRepository
    {
        private const string UserIdIndexName = "gsi1";
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly DynamoDbOptions _options;

        public DynamoDbRefreshTokenRepository(IAmazonDynamoDB dynamoDb, IOptions<DynamoDbOptions> options)
        {
            _dynamoDb = dynamoDb;
            _options = options.Value;
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        {
            var response = await _dynamoDb.GetItemAsync(new GetItemRequest
            {
                TableName = _options.RefreshTokensTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["tokenHash"] = new AttributeValue { S = tokenHash }
                }
            }, ct);

            return response.Item == null || response.Item.Count == 0 ? null : MapToken(response.Item);
        }

        public Task CreateAsync(RefreshToken token, CancellationToken ct = default)
        {
            var expiresAtEpoch = new DateTimeOffset(token.ExpiresAt).ToUnixTimeSeconds();
            return _dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = _options.RefreshTokensTableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    ["tokenHash"] = new AttributeValue { S = token.TokenHash },
                    ["userId"] = new AttributeValue { S = token.UserId },
                    ["createdAt"] = new AttributeValue { S = token.CreatedAt.ToString("O", CultureInfo.InvariantCulture) },
                    ["expiresAt"] = new AttributeValue { S = token.ExpiresAt.ToString("O", CultureInfo.InvariantCulture) },
                    ["expiresAtEpoch"] = new AttributeValue { N = expiresAtEpoch.ToString() },
                    ["isRevoked"] = new AttributeValue { BOOL = false }
                },
                ConditionExpression = "attribute_not_exists(tokenHash)"
            }, ct);
        }

        public Task RevokeAsync(string tokenHash, CancellationToken ct = default)
        {
            return _dynamoDb.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _options.RefreshTokensTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["tokenHash"] = new AttributeValue { S = tokenHash }
                },
                UpdateExpression = "SET isRevoked = :true",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":true"] = new AttributeValue { BOOL = true }
                }
            }, ct);
        }

        public async Task RevokeAllForUserAsync(string userId, CancellationToken ct = default)
        {
            var response = await _dynamoDb.QueryAsync(new QueryRequest
            {
                TableName = _options.RefreshTokensTableName,
                IndexName = UserIdIndexName,
                KeyConditionExpression = "userId = :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":userId"] = new AttributeValue { S = userId }
                },
                ProjectionExpression = "tokenHash"
            }, ct);

            foreach (var item in response.Items)
            {
                await RevokeAsync(item["tokenHash"].S, ct);
            }
        }

        private static RefreshToken MapToken(Dictionary<string, AttributeValue> item)
        {
            return new RefreshToken
            {
                TokenHash = item["tokenHash"].S,
                UserId = item["userId"].S,
                CreatedAt = DateTime.Parse(item["createdAt"].S, null, DateTimeStyles.RoundtripKind),
                ExpiresAt = DateTime.Parse(item["expiresAt"].S, null, DateTimeStyles.RoundtripKind),
                IsRevoked = item.TryGetValue("isRevoked", out var revoked) && revoked.BOOL
            };
        }
    }
}
