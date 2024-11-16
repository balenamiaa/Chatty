using Chatty.Shared.Models.Servers;

using FluentValidation;

namespace Chatty.Shared.Models.Validation;

public sealed class UpdateServerRequestValidator : AbstractValidator<UpdateServerRequest>
{
    public UpdateServerRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2)
            .MaximumLength(100)
            .Matches(@"^[\w\s-]+$")
            .WithMessage("Server name can only contain letters, numbers, spaces, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.IconUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrEmpty(x.IconUrl))
            .WithMessage("Must be a valid URL");
    }

    private static bool BeAValidUrl(string? url)
        => url is null || Uri.TryCreate(url, UriKind.Absolute, out _);
}
