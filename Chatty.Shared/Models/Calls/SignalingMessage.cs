namespace Chatty.Shared.Models.Calls;

public sealed record SignalingMessage(
    string Type,
    string Data);