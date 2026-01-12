using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkGuardiao.Application.Entities
{
    public class LinkAccess
    {
        [Key]
        public int Id { get; set; }
        
        public int ShortenedLinkId { get; set; }
        
        [JsonIgnore]
        public ShortenedLink ShortenedLink { get; set; } = null!;
        
        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        [MaxLength(2000)]
        public string? ReferrerUrl { get; set; }
        
        public string? Browser { get; set; }
        
        public string? OperatingSystem { get; set; }
        
        public string? DeviceType { get; set; }
        
        public DateTime AccessTime { get; set; } = DateTime.UtcNow;
    }
}
