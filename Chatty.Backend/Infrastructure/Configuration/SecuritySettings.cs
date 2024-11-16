namespace Chatty.Backend.Infrastructure.Configuration;

public sealed class SecuritySettings
{
    public int PasswordHashingIterations { get; init; } = 10000;
    public int KeyRotationDays { get; init; } = 30;
    public int MaxDevicesPerUser { get; init; } = 10;
    public int MaxLoginAttempts { get; init; } = 5;
    public int LockoutMinutes { get; init; } = 15;
    public int MinPasswordLength { get; init; } = 8;
    public bool RequireUppercase { get; init; } = true;
    public bool RequireLowercase { get; init; } = true;
    public bool RequireDigit { get; init; } = true;
    public bool RequireSpecialChar { get; init; } = true;
}
