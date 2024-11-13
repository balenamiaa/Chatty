using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Users;

public sealed record UpdateStatusRequest(
    UserStatus Status,
    string? StatusMessage);