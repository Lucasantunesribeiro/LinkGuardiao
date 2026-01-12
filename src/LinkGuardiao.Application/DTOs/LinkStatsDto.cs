namespace LinkGuardiao.Application.DTOs
{
    public class LinkStatsDto
    {
        public int LinkId { get; set; }
        public string ShortCode { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public int TotalClicks { get; set; }
        public List<BrowserStatsDto> BrowserStats { get; set; } = new();
        public List<IpStatsDto> TopIpAddresses { get; set; } = new();
        public List<DateStatsDto> ClicksByDate { get; set; } = new();
    }

    public class BrowserStatsDto
    {
        public string Browser { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class IpStatsDto
    {
        public string IpAddress { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class DateStatsDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
