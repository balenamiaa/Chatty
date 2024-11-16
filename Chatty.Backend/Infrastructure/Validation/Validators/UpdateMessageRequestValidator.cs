using Chatty.Shared.Models.Messages;

using FluentValidation;

namespace Chatty.Shared.Models.Validation;

public class UpdateMessageRequestValidator : AbstractValidator<UpdateMessageRequest>
{
    public UpdateMessageRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Message content cannot be empty");

        RuleFor(x => x.MessageNonce)
            .NotEmpty()
            .Must(x => x.Length == 24)
            .WithMessage("Message nonce must be 24 bytes");

        RuleFor(x => x.KeyVersion)
            .GreaterThan(0);
    }
}
