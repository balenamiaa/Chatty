using Chatty.Shared.Models.Users;

namespace Chatty.Backend.Data.Models;

public static class UserExtensions
{
    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.FirstName,
        user.LastName,
        user.ProfilePictureUrl,
        user.StatusMessage,
        LastOnlineAt: user.LastOnlineAt,
        Locale: user.Locale,
        CreatedAt: user.CreatedAt,
        Status: user.Status
    );
}
