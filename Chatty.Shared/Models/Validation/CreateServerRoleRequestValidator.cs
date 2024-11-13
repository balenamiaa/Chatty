using FluentValidation;
using Chatty.Shared.Models.Servers;

namespace Chatty.Shared.Models.Validation;

public sealed class CreateServerRoleRequestValidator : AbstractValidator<CreateServerRoleRequest>
{
    public CreateServerRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(50)
            .Matches(@"^[\w\s-]+$")
            .WithMessage("Role name can only contain letters, numbers, spaces, and hyphens");

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("Color must be a valid hex color code (e.g., #FF0000)");

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo(0);
    }
}