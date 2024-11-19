using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;

public sealed class Channel
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public required string Name { get; set; }
    public string? Topic { get; set; }
    public bool IsPrivate { get; set; }
    public required ChannelType ChannelType { get; set; } = ChannelType.Text;
    public int Position { get; set; }
    public int RateLimitPerUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Server? Server { get; set; }
    public ICollection<ChannelMember> Members { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}
