namespace LinkGuardiao.Application.DTOs
{
    public sealed class LinkAccessGrantResponse
    {
        public string AccessGrant { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
    }
}
