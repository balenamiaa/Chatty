using FluentValidation;
using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Models.Validation;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches(@"^[\w-]+$")
            .WithMessage("Username can only contain letters, numbers, and hyphens");

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

        RuleFor(x => x.FirstName)
            .MaximumLength(50)
            .Matches(@"^[\p{L}\s-]+$")
            .WithMessage("First name can only contain letters, spaces, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(50)
            .Matches(@"^[\p{L}\s-]+$")
            .WithMessage("Last name can only contain letters, spaces, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.ProfilePictureUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrEmpty(x.ProfilePictureUrl))
            .WithMessage("Must be a valid URL");

        RuleFor(x => x.Locale)
            .NotEmpty()
            .Matches(@"^[a-z]{2}-[A-Z]{2}$")
            .WithMessage("Locale must be in format 'xx-XX' (e.g., en-US)");
    }

    private static bool BeAValidUrl(string? url)
        => url is null || Uri.TryCreate(url, UriKind.Absolute, out _);
}