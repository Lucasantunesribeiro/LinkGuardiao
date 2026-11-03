using FluentValidation;
using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Security;

namespace LinkGuardiao.Application.Validation
{
    public class LinkUpdateDtoValidator : AbstractValidator<LinkUpdateDto>
    {
        public LinkUpdateDtoValidator()
        {
            RuleFor(x => x.OriginalUrl)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MaximumLength(UrlSafety.MaxUrlLength)
                .Custom((url, context) =>
                {
                    if (!UrlSafety.TryNormalize(url, out _, out var reason))
                    {
                        context.AddFailure(nameof(LinkCreateDto.OriginalUrl), UrlSafety.Describe(reason));
                    }
                });

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
