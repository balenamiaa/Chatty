namespace Chatty.Shared.Models.Users;

public sealed record UpdateUserRequest(
    string? Username,
    string? FirstName,
    string? LastName,
    string? ProfilePictureUrl,
    string? StatusMessage,
    string? Locale);