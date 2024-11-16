using Chatty.Shared.Models.Attachments;

using FluentValidation;

namespace Chatty.Shared.Models.Validation;

public sealed class CreateAttachmentRequestValidator : AbstractValidator<CreateAttachmentRequest>
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "video/mp4", "video/webm",
        "audio/mp3", "audio/ogg", "audio/wav",
        "application/pdf",
        "text/plain"
    ];

    public CreateAttachmentRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Matches(@"^[\w\-. ]+$")
            .WithMessage("Filename can only contain letters, numbers, spaces, dots, and hyphens");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100 * 1024 * 1024) // 100MB max
            .WithMessage("File size must be between 1 byte and 100MB");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(x => AllowedContentTypes.Contains(x.ToLowerInvariant()))
            .WithMessage($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}");

        RuleFor(x => x.EncryptionKey)
            .NotEmpty()
            .Must(x => x.Length == 32)
            .WithMessage("Encryption key must be 32 bytes");

        RuleFor(x => x.EncryptionIv)
            .NotEmpty()
            .Must(x => x.Length == 16)
            .WithMessage("Encryption IV must be 16 bytes");
    }
}
