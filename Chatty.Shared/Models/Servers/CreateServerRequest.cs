namespace Chatty.Shared.Models.Servers;

public sealed record CreateServerRequest(
    string Name,
    string? IconUrl);