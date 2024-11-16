using FluentValidation;
using Chatty.Shared.Models.Notifications;

namespace Chatty.Shared.Models.Validation;

public sealed class NotificationSettingsValidator : AbstractValidator<NotificationPreferences>
{

    public NotificationSettingsValidator()
    {
        RuleFor(x => x.QuietHoursStart)
            .Must((settings, start) => !start.HasValue || !settings.QuietHoursEnd.HasValue || start.Value < settings.QuietHoursEnd.Value)
            .WithMessage("Quiet hours start time must be before end time");

        RuleFor(x => x.QuietHoursEnd)
            .Must((settings, end) => !end.HasValue || !settings.QuietHoursStart.HasValue || end.Value > settings.QuietHoursStart.Value)
            .WithMessage("Quiet hours end time must be after start time");

        RuleFor(x => x.MutedChannels)
            .Must(x => x == null || x.Count <= 100)
            .WithMessage("Cannot mute more than 100 channels");

        RuleFor(x => x.MutedUsers)
            .Must(x => x == null || x.Count <= 100)
            .WithMessage("Cannot mute more than 100 users");
    }
}
