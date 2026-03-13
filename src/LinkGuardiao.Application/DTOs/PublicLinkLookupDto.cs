namespace LinkGuardiao.Application.DTOs
{
    public sealed class PublicLinkLookupDto
    {
        public string ShortCode { get; set; } = string.Empty;
        public string? Title { get; set; }
        public bool IsPasswordProtected { get; set; }
    }
}
