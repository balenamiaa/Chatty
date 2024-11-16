namespace Chatty.Backend.Data.Models;

public sealed class ChannelMember
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Channel Channel { get; set; } = null!;
    public User User { get; set; } = null!;
}
