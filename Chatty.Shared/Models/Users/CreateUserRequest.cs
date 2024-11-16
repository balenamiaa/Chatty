namespace Chatty.Shared.Models.Users;

public sealed record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? ProfilePictureUrl,
    string Locale);
