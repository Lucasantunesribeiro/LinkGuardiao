using System.ComponentModel.DataAnnotations;

namespace LinkGuardiao.API.DTOs
{
    public class LinkUpdateDto
    {
        [Required]
        [Url]
        [MaxLength(2000)]
        public string OriginalUrl { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? Title { get; set; }
        
        [MinLength(4)]
        public string? Password { get; set; }
        
        public bool RemovePassword { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? ExpiresAt { get; set; }
    }
}