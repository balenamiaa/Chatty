using Chatty.Shared.Models.Messages;

using FluentValidation;

namespace Chatty.Shared.Models.Validation;

public sealed class CreateMessageRequestValidator : AbstractValidator<CreateMessageRequest>
{
    public CreateMessageRequestValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(x => x.Length <= 1024 * 1024) // 1MB max
            .WithMessage("Message content cannot exceed 1MB");

        RuleFor(x => x.MessageNonce)
            .NotEmpty()
            .Must(x => x.Length == 24)
            .WithMessage("Message nonce must be 24 bytes");

        RuleFor(x => x.KeyVersion)
            .GreaterThan(0);
    }
}
