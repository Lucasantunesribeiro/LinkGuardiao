using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkGuardiao.Application.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsAdmin { get; set; } = false;
        
        // Navigation property
        [JsonIgnore]
        public ICollection<ShortenedLink> ShortenedLinks { get; set; } = new List<ShortenedLink>();
    }
}
