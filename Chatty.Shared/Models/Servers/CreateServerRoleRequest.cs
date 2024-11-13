namespace Chatty.Shared.Models.Servers;

public sealed record CreateServerRoleRequest(
    string Name,
    string? Color,
    int Position);