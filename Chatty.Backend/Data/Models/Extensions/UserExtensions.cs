using Chatty.Shared.Models.Users;

namespace Chatty.Backend.Data.Models;

public static class UserExtensions
{
    public static UserDto ToDto(this User user) => new(
        Id: user.Id,
        Username: user.Username,
        Email: user.Email,
        FirstName: user.FirstName,
        LastName: user.LastName,
        ProfilePictureUrl: user.ProfilePictureUrl,
        StatusMessage: user.StatusMessage,
        LastOnlineAt: user.LastOnlineAt,
        Locale: user.Locale,
        CreatedAt: user.CreatedAt
    );
}