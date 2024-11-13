using FluentValidation;
using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Models.Validation;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches(@"^[\w-]+$")
            .WithMessage("Username can only contain letters, numbers, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.Username));

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

        RuleFor(x => x.StatusMessage)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.StatusMessage));

        RuleFor(x => x.Locale)
            .Matches(@"^[a-z]{2}-[A-Z]{2}$")
            .WithMessage("Locale must be in format 'xx-XX' (e.g., en-US)")
            .When(x => !string.IsNullOrEmpty(x.Locale));
    }

    private static bool BeAValidUrl(string? url)
        => url is null || Uri.TryCreate(url, UriKind.Absolute, out _);
}