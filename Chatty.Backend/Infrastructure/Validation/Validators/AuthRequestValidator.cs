using Chatty.Shared.Models.Auth;

using FluentValidation;

namespace Chatty.Shared.Models.Validation;

public sealed class AuthRequestValidator : AbstractValidator<AuthRequest>
{
    public AuthRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        When(x => x.DeviceId is not null, () =>
        {
            RuleFor(x => x.DeviceId)
                .Must(x => Guid.TryParse(x, out _))
                .WithMessage("Device ID must be a valid GUID");

            RuleFor(x => x.DeviceName)
                .MaximumLength(100);
        });
    }
}
