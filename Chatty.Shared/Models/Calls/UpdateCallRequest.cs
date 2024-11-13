using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Calls;

public sealed record UpdateCallRequest(
    CallStatus Status);