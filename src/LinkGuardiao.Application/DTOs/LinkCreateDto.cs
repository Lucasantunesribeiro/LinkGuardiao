using System.ComponentModel.DataAnnotations;

namespace LinkGuardiao.Application.DTOs
{
    public class LinkCreateDto
    {
        [Required]
        [Url]
        [MaxLength(2048)]
        public string OriginalUrl { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? Title { get; set; }
        
        [MinLength(4)]
        [MaxLength(128)]
        public string? Password { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
    }
}
