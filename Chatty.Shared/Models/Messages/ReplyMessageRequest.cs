using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Messages;

public sealed class ReplyMessageRequest
{
    public required byte[] Content { get; set; }
    public required byte[] MessageNonce { get; set; }
    public ContentType ContentType { get; set; } = ContentType.Text;
    public int KeyVersion { get; set; } = 1;
    public string? Metadata { get; set; }
}
