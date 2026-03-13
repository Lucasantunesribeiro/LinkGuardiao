namespace LinkGuardiao.Infrastructure.Options
{
    public class DynamoDbOptions
    {
        public const string SectionName = "DynamoDb";

        public string LinksTableName { get; set; } = string.Empty;
        public string UsersTableName { get; set; } = string.Empty;
        public string AccessTableName { get; set; } = string.Empty;
        public string DailyLimitsTableName { get; set; } = string.Empty;
        public string RefreshTokensTableName { get; set; } = string.Empty;
        public string EmailLocksTableName { get; set; } = string.Empty;
        public string ServiceUrl { get; set; } = string.Empty;
        public int AccessRetentionDays { get; set; } = 30;
    }
}
