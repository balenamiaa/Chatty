using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;

public sealed class ServerRole
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public required string Name { get; set; }
    public string? Color { get; set; }
    public bool IsDefault { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Server Server { get; set; } = null!;
    public ICollection<ServerMember> Members { get; set; } = [];
    public ICollection<ServerRolePermission> Permissions { get; set; } = [];
}
