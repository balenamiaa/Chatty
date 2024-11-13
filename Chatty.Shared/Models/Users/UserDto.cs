namespace Chatty.Shared.Models.Users;

public sealed record UserDto(
    Guid Id,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    string? ProfilePictureUrl,
    string? StatusMessage,
    DateTime? LastOnlineAt,
    string Locale,
    DateTime CreatedAt);