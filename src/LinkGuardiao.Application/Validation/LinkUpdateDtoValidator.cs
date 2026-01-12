using FluentValidation;
using LinkGuardiao.Application.DTOs;

namespace LinkGuardiao.Application.Validation
{
    public class LinkUpdateDtoValidator : AbstractValidator<LinkUpdateDto>
    {
        public LinkUpdateDtoValidator()
        {
            RuleFor(x => x.OriginalUrl)
                .NotEmpty()
                .MaximumLength(2000)
                .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute));

            RuleFor(x => x.Password)
                .MinimumLength(4)
                .When(x => !string.IsNullOrWhiteSpace(x.Password));

            RuleFor(x => x.ExpiresAt)
                .Must(date => date == null || date > DateTime.UtcNow);
        }
    }
}
