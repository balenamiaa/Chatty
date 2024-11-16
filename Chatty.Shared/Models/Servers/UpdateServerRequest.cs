namespace Chatty.Shared.Models.Servers;

public sealed record UpdateServerRequest(
    string? Name,
    string? IconUrl);
