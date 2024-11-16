namespace Chatty.Backend.Data.Models;

public sealed class PreKey
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required Guid DeviceId { get; set; }
    public required int PreKeyId { get; set; }
    public required byte[] PreKeyPublic { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}
