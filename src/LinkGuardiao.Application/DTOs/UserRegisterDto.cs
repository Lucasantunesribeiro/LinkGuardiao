using System.ComponentModel.DataAnnotations;

namespace LinkGuardiao.Application.DTOs
{
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "O nome é obrigatório")]
        [MaxLength(100, ErrorMessage = "O nome deve ter até 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        [MaxLength(100, ErrorMessage = "O e-mail deve ter até 100 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
        [MaxLength(128, ErrorMessage = "A senha deve ter até 128 caracteres")]
        public string Password { get; set; } = string.Empty;
    }
}
