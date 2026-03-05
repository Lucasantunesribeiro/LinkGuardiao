using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace LinkGuardiao.Application.Entities
{
    public class ShortenedLink
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string OriginalUrl { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(10)]
        public string ShortCode { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? Title { get; set; }
        
        [JsonIgnore]
        public string? PasswordHash { get; set; }
        
        public bool IsPasswordProtected => !string.IsNullOrEmpty(PasswordHash);
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiresAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public int ClickCount { get; set; } = 0;
    }
}
