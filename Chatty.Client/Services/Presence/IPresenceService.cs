using Chatty.Shared.Models.Enums;

namespace Chatty.Client.Services;

/// <summary>
///     Service for managing user presence and status
/// </summary>
public interface IPresenceService
{
    /// <summary>
    ///     Observable that emits when a user's status changes
    /// </summary>
    IObservable<(Guid UserId, UserStatus Status, string? StatusMessage)> OnStatusChanged { get; }

    /// <summary>
    ///     Observable that emits when a user's online state changes
    /// </summary>
    IObservable<(Guid UserId, bool IsOnline)> OnOnlineStateChanged { get; }

    /// <summary>
    ///     Update the current user's status
    /// </summary>
    Task UpdateStatusAsync(
        UserStatus status,
        string? statusMessage = null,
        CancellationToken ct = default);

    /// <summary>
    ///     Get a user's current status
    /// </summary>
    Task<UserStatus> GetUserStatusAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    ///     Get status for multiple users
    /// </summary>
    Task<IReadOnlyDictionary<Guid, UserStatus>> GetUsersStatusAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct = default);
}
