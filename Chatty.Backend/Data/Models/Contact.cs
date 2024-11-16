using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;

public sealed class Contact
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ContactUserId { get; set; }
    public required ContactStatus Status { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public User ContactUser { get; set; } = null!;
}
