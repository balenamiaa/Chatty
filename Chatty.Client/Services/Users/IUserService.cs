using Chatty.Shared.Models.Devices;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Users;

namespace Chatty.Client.Services;

/// <summary>
///     Service for managing user profiles and settings
/// </summary>
public interface IUserService
{
    /// <summary>
    ///     Gets the current user's profile
    /// </summary>
    Task<UserDto> GetCurrentUserAsync(CancellationToken ct = default);

    /// <summary>
    ///     Gets a user by ID
    /// </summary>
    Task<UserDto> GetAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Updates the current user's profile
    /// </summary>
    Task<UserDto> UpdateProfileAsync(
        UpdateUserRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Updates the current user's status
    /// </summary>
    Task UpdateStatusAsync(UserStatus status, CancellationToken ct = default);

    /// <summary>
    ///     Changes the current user's password
    /// </summary>
    Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Requests a password reset for a user
    /// </summary>
    Task RequestPasswordResetAsync(
        RequestPasswordResetRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Resets a user's password using a reset token
    /// </summary>
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);

    /// <summary>
    ///     Gets the current user's friends
    /// </summary>
    Task<IReadOnlyList<UserDto>> GetFriendsAsync(CancellationToken ct = default);

    /// <summary>
    ///     Sends a friend request to a user
    /// </summary>
    Task SendFriendRequestAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Accepts a friend request from a user
    /// </summary>
    Task AcceptFriendRequestAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Rejects a friend request from a user
    /// </summary>
    Task RejectFriendRequestAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Removes a friend
    /// </summary>
    Task RemoveFriendAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Blocks a user
    /// </summary>
    Task BlockUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Unblocks a user
    /// </summary>
    Task UnblockUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Gets the current user's blocked users
    /// </summary>
    Task<IReadOnlyList<UserDto>> GetBlockedUsersAsync(CancellationToken ct = default);

    /// <summary>
    ///     Gets the current user's settings
    /// </summary>
    Task<UserSettingsDto> GetSettingsAsync(CancellationToken ct = default);

    /// <summary>
    ///     Updates the current user's settings
    /// </summary>
    Task<UserSettingsDto> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Gets all devices registered to the current user
    /// </summary>
    Task<IReadOnlyList<UserDeviceDto>> GetDevicesAsync(CancellationToken ct = default);

    /// <summary>
    ///     Registers a new device for the current user with E2E encryption keys
    /// </summary>
    Task<UserDeviceDto> RegisterDeviceAsync(CancellationToken ct = default);

    /// <summary>
    ///     Updates an existing device
    /// </summary>
    Task<UserDeviceDto> UpdateDeviceAsync(
        UpdateDeviceRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Deletes a device and its associated encryption keys
    /// </summary>
    Task DeleteDeviceAsync(Guid deviceId, CancellationToken ct = default);
}
