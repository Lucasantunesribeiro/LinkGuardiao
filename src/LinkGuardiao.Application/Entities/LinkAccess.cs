using System.ComponentModel.DataAnnotations;
namespace LinkGuardiao.Application.Entities
{
    public class LinkAccess
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public string ShortCode { get; set; } = string.Empty;
        
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
