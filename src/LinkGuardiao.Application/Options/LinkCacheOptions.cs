namespace LinkGuardiao.Application.Options
{
    public class LinkCacheOptions
    {
        public const string SectionName = "LinkCache";

        public int HotLinkTtlSeconds { get; set; } = 300;
    }
}
