using Chatty.Shared.Models.Devices;

using FluentValidation;

namespace Chatty.Shared.Models.Validation;

public sealed class RegisterDeviceRequestValidator : AbstractValidator<RegisterDeviceRequest>
{
    public RegisterDeviceRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty();

        RuleFor(x => x.DeviceName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.DeviceName));

        RuleFor(x => x.PublicKey)
            .NotEmpty()
            .Must(x => x.Length >= 32) // Minimum key size
            .WithMessage("Public key must be at least 32 bytes");

        RuleFor(x => x.DeviceToken)
            .MaximumLength(512)
            .When(x => !string.IsNullOrEmpty(x.DeviceToken));
    }
}
