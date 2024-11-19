using Chatty.Shared.Models.Calls;

using FluentValidation;

namespace Chatty.Shared.Models.Validation;

public sealed class CreateCallRequestValidator : AbstractValidator<CreateCallRequest>
{
    public CreateCallRequestValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty()
            .When(x => x.RecipientId is null)
            .WithMessage("Either ChannelId or RecipientId must be provided");

        RuleFor(x => x.RecipientId)
            .NotEmpty()
            .When(x => x.ChannelId is null)
            .WithMessage("Either ChannelId or RecipientId must be provided");

        RuleFor(x => x)
            .Must(x => x.ChannelId is null != x.RecipientId is null)
            .WithMessage("Exactly one of ChannelId or RecipientId must be provided");
    }
}
