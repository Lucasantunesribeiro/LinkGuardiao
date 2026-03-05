using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace LinkGuardiao.Infrastructure.Data
{
    public class DynamoDbLinkRepository : ILinkRepository
    {
        private const string UserIndexName = "gsi1";
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly DynamoDbOptions _options;

        public DynamoDbLinkRepository(IAmazonDynamoDB dynamoDb, IOptions<DynamoDbOptions> options)
        {
            _dynamoDb = dynamoDb;
            _options = options.Value;
        }

        public async Task<ShortenedLink?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            var response = await _dynamoDb.GetItemAsync(new GetItemRequest
            {
                TableName = _options.LinksTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["shortCode"] = new AttributeValue { S = shortCode }
                }
            }, cancellationToken);

            return response.Item == null || response.Item.Count == 0 ? null : MapLink(response.Item);
        }

        public async Task<ShortenedLink?> GetByShortCodeForUserAsync(string shortCode, string userId, CancellationToken cancellationToken = default)
        {
            var link = await GetByShortCodeAsync(shortCode, cancellationToken);
            return link != null && link.UserId == userId ? link : null;
        }

        public async Task<IReadOnlyList<ShortenedLink>> ListByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var links = new List<ShortenedLink>();
            var request = new QueryRequest
            {
                TableName = _options.LinksTableName,
                IndexName = UserIndexName,
                KeyConditionExpression = "userId = :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":userId"] = new AttributeValue { S = userId }
                },
                ScanIndexForward = false
            };

            do
            {
                var response = await _dynamoDb.QueryAsync(request, cancellationToken);
                foreach (var item in response.Items)
                {
                    links.Add(MapLink(item));
                }

                request.ExclusiveStartKey = response.LastEvaluatedKey;
            } while (request.ExclusiveStartKey != null && request.ExclusiveStartKey.Count > 0);

            return links;
        }

        public async Task<bool> TryCreateAsync(ShortenedLink link, CancellationToken cancellationToken = default)
        {
            var request = new PutItemRequest
            {
                TableName = _options.LinksTableName,
                Item = ToItem(link),
                ConditionExpression = "attribute_not_exists(shortCode)"
            };

            try
            {
                await _dynamoDb.PutItemAsync(request, cancellationToken);
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        public async Task UpdateAsync(ShortenedLink link, CancellationToken cancellationToken = default)
        {
            var updateExpression = "SET originalUrl = :originalUrl, isActive = :isActive";
            var values = new Dictionary<string, AttributeValue>
            {
                [":originalUrl"] = new AttributeValue { S = link.OriginalUrl },
                [":isActive"] = new AttributeValue { BOOL = link.IsActive }
            };
            var removeAttributes = new List<string>();

            if (!string.IsNullOrWhiteSpace(link.Title))
            {
                updateExpression += ", title = :title";
                values[":title"] = new AttributeValue { S = link.Title };
            }
            else
            {
                removeAttributes.Add("title");
            }

            if (!string.IsNullOrWhiteSpace(link.PasswordHash))
            {
                updateExpression += ", passwordHash = :passwordHash";
                values[":passwordHash"] = new AttributeValue { S = link.PasswordHash };
            }
            else
            {
                removeAttributes.Add("passwordHash");
            }

            if (link.ExpiresAt.HasValue)
            {
                updateExpression += ", expiresAt = :expiresAt, expiresAtEpoch = :expiresAtEpoch";
                values[":expiresAt"] = new AttributeValue { S = link.ExpiresAt.Value.ToString("O") };
                values[":expiresAtEpoch"] = new AttributeValue { N = ToEpochSeconds(link.ExpiresAt.Value).ToString(CultureInfo.InvariantCulture) };
            }
            else
            {
                removeAttributes.Add("expiresAt");
                removeAttributes.Add("expiresAtEpoch");
            }

            if (removeAttributes.Count > 0)
            {
                updateExpression += " REMOVE " + string.Join(", ", removeAttributes);
            }

            var request = new UpdateItemRequest
            {
                TableName = _options.LinksTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["shortCode"] = new AttributeValue { S = link.ShortCode }
                },
                UpdateExpression = updateExpression,
                ExpressionAttributeValues = values,
                ConditionExpression = "attribute_exists(shortCode)"
            };

            await _dynamoDb.UpdateItemAsync(request, cancellationToken);
        }

        public async Task<bool> DeleteAsync(string shortCode, string userId, CancellationToken cancellationToken = default)
        {
            var request = new DeleteItemRequest
            {
                TableName = _options.LinksTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["shortCode"] = new AttributeValue { S = shortCode }
                },
                ConditionExpression = "userId = :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":userId"] = new AttributeValue { S = userId }
                }
            };

            try
            {
                await _dynamoDb.DeleteItemAsync(request, cancellationToken);
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        public async Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            var response = await _dynamoDb.GetItemAsync(new GetItemRequest
            {
                TableName = _options.LinksTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["shortCode"] = new AttributeValue { S = shortCode }
                },
                ProjectionExpression = "shortCode"
            }, cancellationToken);

            return response.Item != null && response.Item.Count > 0;
        }

        public Task IncrementClickCountAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            return _dynamoDb.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _options.LinksTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["shortCode"] = new AttributeValue { S = shortCode }
                },
                UpdateExpression = "ADD clickCount :inc",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":inc"] = new AttributeValue { N = "1" }
                }
            }, cancellationToken);
        }

        private static Dictionary<string, AttributeValue> ToItem(ShortenedLink link)
        {
            var createdAt = link.CreatedAt == default ? DateTime.UtcNow : link.CreatedAt;
            var item = new Dictionary<string, AttributeValue>
            {
                ["shortCode"] = new AttributeValue { S = link.ShortCode },
                ["userId"] = new AttributeValue { S = link.UserId },
                ["originalUrl"] = new AttributeValue { S = link.OriginalUrl },
                ["createdAt"] = new AttributeValue { S = createdAt.ToString("O") },
                ["isActive"] = new AttributeValue { BOOL = link.IsActive },
                ["clickCount"] = new AttributeValue { N = link.ClickCount.ToString(CultureInfo.InvariantCulture) }
            };

            if (string.IsNullOrWhiteSpace(link.Id))
            {
                link.Id = link.ShortCode;
            }

            if (!string.IsNullOrWhiteSpace(link.Title))
            {
                item["title"] = new AttributeValue { S = link.Title };
            }

            if (!string.IsNullOrWhiteSpace(link.PasswordHash))
            {
                item["passwordHash"] = new AttributeValue { S = link.PasswordHash };
            }

            if (link.ExpiresAt.HasValue)
            {
                item["expiresAt"] = new AttributeValue { S = link.ExpiresAt.Value.ToString("O") };
                item["expiresAtEpoch"] = new AttributeValue { N = ToEpochSeconds(link.ExpiresAt.Value).ToString(CultureInfo.InvariantCulture) };
            }

            return item;
        }

        private static ShortenedLink MapLink(Dictionary<string, AttributeValue> item)
        {
            var shortCode = item["shortCode"].S;
            return new ShortenedLink
            {
                Id = shortCode,
                ShortCode = shortCode,
                UserId = item["userId"].S,
                OriginalUrl = item["originalUrl"].S,
                Title = item.TryGetValue("title", out var title) ? title.S : null,
                PasswordHash = item.TryGetValue("passwordHash", out var passwordHash) ? passwordHash.S : null,
                CreatedAt = DateTime.Parse(item["createdAt"].S, null, DateTimeStyles.RoundtripKind),
                ExpiresAt = item.TryGetValue("expiresAt", out var expiresAt)
                    ? DateTime.Parse(expiresAt.S, null, DateTimeStyles.RoundtripKind)
                    : null,
                IsActive = !item.TryGetValue("isActive", out var isActive) || isActive.BOOL,
                ClickCount = item.TryGetValue("clickCount", out var clickCount)
                    ? int.Parse(clickCount.N, CultureInfo.InvariantCulture)
                    : 0
            };
        }

        private static long ToEpochSeconds(DateTime value)
        {
            return new DateTimeOffset(value).ToUnixTimeSeconds();
        }
    }
}
