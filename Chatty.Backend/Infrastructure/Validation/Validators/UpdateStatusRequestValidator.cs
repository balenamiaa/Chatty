using Chatty.Shared.Models.Users;

using FluentValidation;

namespace Chatty.Shared.Models.Validation;

public sealed class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid status value");

        RuleFor(x => x.StatusMessage)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.StatusMessage));
    }
}
