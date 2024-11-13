namespace Chatty.Backend.Data.Models;

public sealed class CallParticipant
{
    public Guid Id { get; set; }
    public Guid CallId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool Muted { get; set; }
    public bool VideoEnabled { get; set; } = true;

    // Navigation properties
    public Call Call { get; set; } = null!;
    public User User { get; set; } = null!;
}