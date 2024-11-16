namespace Chatty.Backend.Data.Models;

public sealed class Server
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid OwnerId { get; set; }
    public string? IconUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<ServerRole> Roles { get; set; } = [];
    public ICollection<ServerMember> Members { get; set; } = [];
    public ICollection<Channel> Channels { get; set; } = [];
}
