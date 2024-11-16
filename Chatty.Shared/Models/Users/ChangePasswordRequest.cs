namespace Chatty.Shared.Models.Users;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
