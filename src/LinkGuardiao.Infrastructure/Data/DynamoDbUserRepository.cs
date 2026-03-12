using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Exceptions;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace LinkGuardiao.Infrastructure.Data
{
    public class DynamoDbUserRepository : IUserRepository
    {
        private const string EmailIndexName = "gsi1";
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly DynamoDbOptions _options;

        public DynamoDbUserRepository(IAmazonDynamoDB dynamoDb, IOptions<DynamoDbOptions> options)
        {
            _dynamoDb = dynamoDb;
            _options = options.Value;
        }

        public async Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var response = await _dynamoDb.GetItemAsync(new GetItemRequest
            {
                TableName = _options.UsersTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["userId"] = new AttributeValue { S = userId }
                }
            }, cancellationToken);

            return response.Item == null || response.Item.Count == 0 ? null : MapUser(response.Item);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var response = await _dynamoDb.QueryAsync(new QueryRequest
            {
                TableName = _options.UsersTableName,
                IndexName = EmailIndexName,
                KeyConditionExpression = "email = :email",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":email"] = new AttributeValue { S = email }
                },
                Limit = 1
            }, cancellationToken);

            var item = response.Items.FirstOrDefault();
            return item == null ? null : MapUser(item);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            var response = await _dynamoDb.QueryAsync(new QueryRequest
            {
                TableName = _options.UsersTableName,
                IndexName = EmailIndexName,
                KeyConditionExpression = "email = :email",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":email"] = new AttributeValue { S = email }
                },
                Select = Select.COUNT,
                Limit = 1
            }, cancellationToken);

            return response.Count > 0;
        }

        public async Task CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            var transactItems = new List<TransactWriteItem>
            {
                // Write user record — fails if userId already exists
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _options.UsersTableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            ["userId"] = new AttributeValue { S = user.Id },
                            ["email"] = new AttributeValue { S = user.Email },
                            ["username"] = new AttributeValue { S = user.Username },
                            ["passwordHash"] = new AttributeValue { S = user.PasswordHash },
                            ["createdAt"] = new AttributeValue { S = user.CreatedAt.ToString("O", CultureInfo.InvariantCulture) },
                            ["isAdmin"] = new AttributeValue { BOOL = user.IsAdmin }
                        },
                        ConditionExpression = "attribute_not_exists(userId)"
                    }
                }
            };

            // If EmailLocksTableName is configured, add an email-lock item to enforce
            // uniqueness atomically (prevents TOCTOU between EmailExistsAsync + CreateAsync)
            if (!string.IsNullOrWhiteSpace(_options.EmailLocksTableName))
            {
                transactItems.Add(new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _options.EmailLocksTableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            ["email"] = new AttributeValue { S = user.Email }
                        },
                        ConditionExpression = "attribute_not_exists(email)"
                    }
                });
            }

            try
            {
                await _dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
                {
                    TransactItems = transactItems
                }, cancellationToken);
            }
            catch (TransactionCanceledException ex)
                when (ex.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
            {
                throw new UserExistsException();
            }
        }

        private static User MapUser(Dictionary<string, AttributeValue> item)
        {
            return new User
            {
                Id = item["userId"].S,
                Email = item["email"].S,
                Username = item["username"].S,
                PasswordHash = item["passwordHash"].S,
                CreatedAt = DateTime.Parse(item["createdAt"].S, null, DateTimeStyles.RoundtripKind),
                IsAdmin = item.TryGetValue("isAdmin", out var isAdmin) && isAdmin.BOOL
            };
        }
    }
}
