using FluentValidation;
using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Security;

namespace LinkGuardiao.Application.Validation
{
    public class LinkCreateDtoValidator : AbstractValidator<LinkCreateDto>
    {
        public LinkCreateDtoValidator()
        {
            RuleFor(x => x.OriginalUrl)
                .NotEmpty()
                .MaximumLength(2048)
                .Must(UrlSafety.IsSafeHttpUrl)
                .WithMessage("URL must be a valid http/https address.");

            RuleFor(x => x.Title)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Title));

            RuleFor(x => x.Password)
                .MinimumLength(4)
                .MaximumLength(128)
                .When(x => !string.IsNullOrWhiteSpace(x.Password));

            RuleFor(x => x.ExpiresAt)
                .Must(date => date == null || date > DateTime.UtcNow)
                .WithMessage("A data de expiração deve ser futura.");
        }
    }
}
