namespace LinkGuardiao.Application.Entities
{
    public class AccessLogMessage
    {
        public string Id { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public string? ReferrerUrl { get; set; }
        public string? Browser { get; set; }
        public string? OperatingSystem { get; set; }
        public string? DeviceType { get; set; }
        public DateTime AccessTime { get; set; }
        public string MessageVersion { get; set; } = "1.0";
    }
}
