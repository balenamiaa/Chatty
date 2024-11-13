using FluentValidation;
using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Validation;

public sealed class UpdateCallRequestValidator : AbstractValidator<UpdateCallRequest>
{
    public UpdateCallRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .NotEqual(CallStatus.Initiated)
            .WithMessage("Cannot manually set call status to Initiated");
    }
}