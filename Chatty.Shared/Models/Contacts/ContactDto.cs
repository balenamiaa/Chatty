using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Models.Contacts;

public sealed record ContactDto(
    Guid Id,
    UserDto User,
    UserDto ContactUser,
    ContactStatus Status,
    DateTime AddedAt);
