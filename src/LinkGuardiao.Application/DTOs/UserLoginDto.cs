using System.ComponentModel.DataAnnotations;

namespace LinkGuardiao.Application.DTOs
{
    public class UserLoginDto
    {
        [Required(ErrorMessage = "O e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        [MaxLength(100, ErrorMessage = "O e-mail deve ter até 100 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória")]
        [MaxLength(128, ErrorMessage = "A senha deve ter até 128 caracteres")]
        public string Password { get; set; } = string.Empty;
    }
}
