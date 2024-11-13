namespace Chatty.Shared.Models.Users;

public sealed record ResetPasswordRequest(
    string Email,
    string ResetToken,
    string NewPassword);