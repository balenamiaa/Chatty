using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;

public sealed class Call
{
    public Guid Id { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid InitiatorId { get; set; }
    public required CallType CallType { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public CallStatus Status { get; set; } = CallStatus.Initiated;

    // Navigation properties
    public Channel? Channel { get; set; }
    public User Initiator { get; set; } = null!;
    public ICollection<CallParticipant> Participants { get; set; } = new List<CallParticipant>();
}