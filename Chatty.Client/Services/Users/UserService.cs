using System.Net;
using System.Net.Http.Json;

using Chatty.Client.Cache;
using Chatty.Client.Crypto;
using Chatty.Client.Exceptions;
using Chatty.Client.Http;
using Chatty.Client.Logging;
using Chatty.Client.Realtime;
using Chatty.Client.State;
using Chatty.Client.Storage;
using Chatty.Shared.Models.Devices;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Users;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Services.Users;

/// <summary>
///     Service for managing user profiles and settings
/// </summary>
public sealed class UserService(
    IHttpClientFactory httpClientFactory,
    ICacheService cache,
    IStateManager state,
    IDeviceManager deviceManager,
    ICryptoService cryptoService,
    ILogger<UserService> logger,
    IChattyRealtimeClient realtimeClient)
    : BaseService(httpClientFactory, logger, "UserService"), IUserService
{
    public async Task<UserDto> GetCurrentUserAsync(CancellationToken ct = default) => await GetMeAsync(ct);

    public async Task<UserDto> GetAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try cache first
        var user = await cache.GetAsync<UserDto>(
            CacheKeys.User(userId),
            ct);

        if (user is not null)
        {
            logger.LogCacheHit(CacheKeys.User(userId));
            logger.LogMethodExit();
            return user;
        }

        logger.LogCacheMiss(CacheKeys.User(userId));
        var endpoint = ApiEndpoints.Users.Profile(userId);
        logger.LogHttpRequest("GET", endpoint);

        // Get from server
        user = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<UserDto>(ct);
            });

        if (user is null)
        {
            throw new ApiException(
                "Failed to parse user response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.User(userId),
            user,
            ct: ct);

        logger.LogMethodExit();
        return user;
    }

    public async Task<UserDto> UpdateProfileAsync(UpdateUserRequest request, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("PUT", ApiEndpoints.Users.Me);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsJsonAsync(ApiEndpoints.Users.Me, request, ct);
                logger.LogHttpResponse("PUT", ApiEndpoints.Users.Me, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<UserDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse user response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.UserMe(),
            response,
            ct: ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<UserSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try state first
        var settings = await state.GetStateAsync<UserSettingsDto>(
            StateKeys.UserSettings(),
            ct);

        if (settings is not null)
        {
            logger.LogStateChange(StateKeys.UserSettings(), "Retrieved");
            logger.LogMethodExit();
            return settings;
        }

        logger.LogHttpRequest("GET", ApiEndpoints.Users.Settings);

        // Get from server
        settings = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(ApiEndpoints.Users.Settings, ct);
                logger.LogHttpResponse("GET", ApiEndpoints.Users.Settings, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<UserSettingsDto>(ct);
            });

        if (settings is null)
        {
            throw new ApiException(
                "Failed to parse settings response",
                HttpStatusCode.InternalServerError);
        }

        // Update state
        await state.SetStateAsync(
            StateKeys.UserSettings(),
            settings,
            ct);

        logger.LogMethodExit();
        return settings;
    }

    public async Task<UserSettingsDto> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("PUT", ApiEndpoints.Users.Settings);

        var settings = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsJsonAsync(ApiEndpoints.Users.Settings, request, ct);
                logger.LogHttpResponse("PUT", ApiEndpoints.Users.Settings, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<UserSettingsDto>(ct);
            });

        if (settings is null)
        {
            throw new ApiException(
                "Failed to parse settings response",
                HttpStatusCode.InternalServerError);
        }

        // Update state
        await state.SetStateAsync(
            StateKeys.UserSettings(),
            settings,
            ct);

        logger.LogMethodExit();
        return settings;
    }

    public async Task<IReadOnlyList<UserDeviceDto>> GetDevicesAsync(CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try state first
        var devices = await state.GetStateAsync<List<UserDeviceDto>>(
            StateKeys.UserDevices(),
            ct);

        if (devices is not null)
        {
            logger.LogStateChange(StateKeys.UserDevices(), $"Retrieved {devices.Count} devices");
            logger.LogMethodExit();
            return devices;
        }

        logger.LogHttpRequest("GET", ApiEndpoints.Users.Devices);

        // Get from server
        devices = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(ApiEndpoints.Users.Devices, ct);
                logger.LogHttpResponse("GET", ApiEndpoints.Users.Devices, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<List<UserDeviceDto>>(ct);
            });

        if (devices is null)
        {
            throw new ApiException(
                "Failed to parse devices response",
                HttpStatusCode.InternalServerError);
        }

        // Update state
        await state.SetStateAsync(
            StateKeys.UserDevices(),
            devices,
            ct);

        logger.LogMethodExit();
        return devices;
    }

    public async Task<UserDeviceDto> RegisterDeviceAsync(CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Generate device keys
        var (publicKey, privateKey) = await cryptoService.GenerateKeyPairAsync();
        var (preKeyPublic, preKeyPrivate) = await cryptoService.GenerateKeyPairAsync();

        // Store private keys
        await state.SetStateAsync(
            StateKeys.DeviceKeys(),
            new DeviceKeys
            {
                PrivateKey = privateKey,
                PreKeyPrivate = preKeyPrivate
            },
            ct);

        // Create device ID
        var deviceId = Guid.NewGuid();

        // Create request
        var request = new RegisterDeviceRequest(
            deviceId,
            Environment.MachineName,
            DeviceType.Desktop,
            null,
            publicKey);

        logger.LogHttpRequest("POST", ApiEndpoints.Users.Devices);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(ApiEndpoints.Users.Devices, request, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Users.Devices, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<UserDeviceDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse device response",
                HttpStatusCode.InternalServerError);
        }

        // Store device ID
        await deviceManager.SetDeviceIdAsync(deviceId);

        logger.LogMethodExit();
        return response;
    }

    public async Task<UserDeviceDto> UpdateDeviceAsync(
        UpdateDeviceRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        var deviceId = await deviceManager.GetOrCreateDeviceIdAsync();
        var endpoint = ApiEndpoints.Users.Device(deviceId);
        logger.LogHttpRequest("PUT", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsJsonAsync(endpoint, request, ct);
                logger.LogHttpResponse("PUT", endpoint, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<UserDeviceDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse device response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task DeleteDeviceAsync(Guid deviceId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        var endpoint = ApiEndpoints.Users.Device(deviceId);
        logger.LogHttpRequest("DELETE", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.DeleteAsync(endpoint, ct);
                logger.LogHttpResponse("DELETE", endpoint, (int)response.StatusCode);
                return response;
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to delete device",
                response.StatusCode);
        }

        // If this is our device, clear keys
        var ourDeviceId = await deviceManager.GetOrCreateDeviceIdAsync();
        if (deviceId == ourDeviceId)
        {
            await deviceManager.ClearKeysAsync();
        }

        logger.LogMethodExit();
    }

    public async Task SendFriendRequestAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        var endpoint = ApiEndpoints.Users.FriendRequest(userId);
        logger.LogHttpRequest("POST", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsync(endpoint, null, ct);
                logger.LogHttpResponse("POST", endpoint, (int)response.StatusCode);
                return response;
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to send friend request",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task AcceptFriendRequestAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        var endpoint = ApiEndpoints.Users.AcceptFriendRequest(userId);
        logger.LogHttpRequest("POST", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsync(endpoint, null, ct);
                logger.LogHttpResponse("POST", endpoint, (int)response.StatusCode);
                return response;
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to accept friend request",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task RejectFriendRequestAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        var endpoint = ApiEndpoints.Users.RejectFriendRequest(userId);
        logger.LogHttpRequest("POST", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsync(endpoint, null, ct);
                logger.LogHttpResponse("POST", endpoint, (int)response.StatusCode);
                return response;
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to reject friend request",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task RemoveFriendAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        var endpoint = ApiEndpoints.Users.RemoveFriend(userId);
        logger.LogHttpRequest("DELETE", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.DeleteAsync(endpoint, ct);
                logger.LogHttpResponse("DELETE", endpoint, (int)response.StatusCode);
                return response;
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to remove friend",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task BlockUserAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        var endpoint = ApiEndpoints.Users.BlockUser(userId);
        logger.LogHttpRequest("POST", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsync(endpoint, null, ct);
                logger.LogHttpResponse("POST", endpoint, (int)response.StatusCode);
                return response;
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to block user",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task UnblockUserAsync(Guid userId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        var endpoint = ApiEndpoints.Users.UnblockUser(userId);
        logger.LogHttpRequest("DELETE", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.DeleteAsync(endpoint, ct);
                logger.LogHttpResponse("DELETE", endpoint, (int)response.StatusCode);
                return response;
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to unblock user",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task UpdateStatusAsync(UserStatus status, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Use SignalR for real-time status updates
        await realtimeClient.UpdateStatusAsync(status);

        logger.LogMethodExit();
    }

    public async Task<IReadOnlyList<UserDto>> GetFriendsAsync(CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try cache first
        var friends = await cache.GetAsync<List<UserDto>>(
            CacheKeys.UserFriends(),
            ct);

        if (friends is not null)
        {
            logger.LogCacheHit(CacheKeys.UserFriends());
            logger.LogMethodExit();
            return friends;
        }

        logger.LogCacheMiss(CacheKeys.UserFriends());
        logger.LogHttpRequest("GET", ApiEndpoints.Users.Friends);

        // Get from server
        friends = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(ApiEndpoints.Users.Friends, ct);
                logger.LogHttpResponse("GET", ApiEndpoints.Users.Friends, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<List<UserDto>>(ct);
            });

        if (friends is null)
        {
            throw new ApiException(
                "Failed to parse friends response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.UserFriends(),
            friends,
            ct: ct);

        foreach (var friend in friends)
        {
            await cache.SetAsync(
                CacheKeys.User(friend.Id),
                friend,
                ct: ct);
        }

        logger.LogMethodExit();
        return friends;
    }

    public async Task<IReadOnlyList<UserDto>> GetBlockedUsersAsync(CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try cache first
        var blockedUsers = await cache.GetAsync<List<UserDto>>(
            CacheKeys.UserBlockedUsers(),
            ct);

        if (blockedUsers is not null)
        {
            logger.LogCacheHit(CacheKeys.UserBlockedUsers());
            logger.LogMethodExit();
            return blockedUsers;
        }

        logger.LogCacheMiss(CacheKeys.UserBlockedUsers());
        logger.LogHttpRequest("GET", ApiEndpoints.Users.BlockedUsers);

        // Get from server
        blockedUsers = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(ApiEndpoints.Users.BlockedUsers, ct);
                logger.LogHttpResponse("GET", ApiEndpoints.Users.BlockedUsers, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<List<UserDto>>(ct);
            });

        if (blockedUsers is null)
        {
            throw new ApiException(
                "Failed to parse blocked users response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.UserBlockedUsers(),
            blockedUsers,
            ct: ct);

        foreach (var user in blockedUsers)
        {
            await cache.SetAsync(
                CacheKeys.User(user.Id),
                user,
                ct: ct);
        }

        logger.LogMethodExit();
        return blockedUsers;
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Users.Password);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(ApiEndpoints.Users.Password, request, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Users.Password, (int)response.StatusCode);
                return response;
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to change password",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task RequestPasswordResetAsync(RequestPasswordResetRequest request, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Users.PasswordReset);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(ApiEndpoints.Users.PasswordReset, request, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Users.PasswordReset, (int)response.StatusCode);
                return response;
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to request password reset",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Users.PasswordResetConfirm);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(ApiEndpoints.Users.PasswordResetConfirm, request, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Users.PasswordResetConfirm, (int)response.StatusCode);
                return response;
            });

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to reset password",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task<UserDto> GetMeAsync(CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try cache first
        var user = await cache.GetAsync<UserDto>(
            CacheKeys.UserMe(),
            ct);

        if (user is not null)
        {
            logger.LogCacheHit(CacheKeys.UserMe());
            logger.LogMethodExit();
            return user;
        }

        logger.LogCacheMiss(CacheKeys.UserMe());
        logger.LogHttpRequest("GET", ApiEndpoints.Users.Me);

        // Get from server
        user = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(ApiEndpoints.Users.Me, ct);
                logger.LogHttpResponse("GET", ApiEndpoints.Users.Me, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<UserDto>(ct);
            });

        if (user is null)
        {
            throw new ApiException(
                "Failed to parse user response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.UserMe(),
            user,
            ct: ct);

        logger.LogMethodExit();
        return user;
    }
}
