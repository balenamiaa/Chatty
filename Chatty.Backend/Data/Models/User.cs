using Chatty.Shared.Models.Notifications;

namespace Chatty.Backend.Data.Models;

public sealed class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? StatusMessage { get; set; }
    public DateTime? LastOnlineAt { get; set; }
    public string Locale { get; set; } = "en-US";
    public NotificationPreferences? NotificationPreferences { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserDevice> Devices { get; set; } = [];
}
