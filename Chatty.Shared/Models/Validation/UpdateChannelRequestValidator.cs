using FluentValidation;
using Chatty.Shared.Models.Channels;

namespace Chatty.Shared.Models.Validation;

public sealed class UpdateChannelRequestValidator : AbstractValidator<UpdateChannelRequest>
{
    public UpdateChannelRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2)
            .MaximumLength(100)
            .Matches(@"^[\w\s-]+$")
            .WithMessage("Channel name can only contain letters, numbers, spaces, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Topic)
            .MaximumLength(1024)
            .When(x => !string.IsNullOrEmpty(x.Topic));

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Position.HasValue);

        RuleFor(x => x.RateLimitPerUser)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(21600) // 6 hours
            .When(x => x.RateLimitPerUser.HasValue);
    }
}