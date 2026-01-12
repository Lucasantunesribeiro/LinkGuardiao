using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkGuardiao.Application.Entities
{
    public class ShortenedLink
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [JsonIgnore]
        public User User { get; set; } = null!;
        
        [Required]
        [MaxLength(2000)]
        public string OriginalUrl { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(10)]
        public string ShortCode { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? Title { get; set; }
        
        public string? PasswordHash { get; set; }
        
        public bool IsPasswordProtected => !string.IsNullOrEmpty(PasswordHash);
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiresAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public int ClickCount { get; set; } = 0;
        
        // Navigation property
        [JsonIgnore]
        public ICollection<LinkAccess> Accesses { get; set; } = new List<LinkAccess>();
    }
}
