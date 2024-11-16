using Chatty.Shared.Models.Channels;

using FluentValidation;

namespace Chatty.Shared.Models.Validation;

public sealed class CreateChannelRequestValidator : AbstractValidator<CreateChannelRequest>
{
    public CreateChannelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100)
            .Matches(@"^[\w\s-]+$")
            .WithMessage("Channel name can only contain letters, numbers, spaces, and hyphens");

        RuleFor(x => x.Topic)
            .MaximumLength(1024)
            .When(x => !string.IsNullOrEmpty(x.Topic));

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.RateLimitPerUser)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(21600); // 6 hours
    }
}
