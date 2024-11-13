using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;
public sealed class UserDevice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required Guid DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public required byte[] PublicKey { get; set; }
    public string? DeviceToken { get; set; }
    public required DeviceType DeviceType { get; set; }
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}