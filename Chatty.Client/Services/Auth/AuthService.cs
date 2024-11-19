using System.Net;
using System.Net.Http.Json;

using Chatty.Client.Cache;
using Chatty.Client.Exceptions;
using Chatty.Client.Http;
using Chatty.Client.Logging;
using Chatty.Client.Models.Auth;
using Chatty.Client.State;
using Chatty.Shared.Models.Auth;
using Chatty.Shared.Models.Users;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Services.Auth;

/// <summary>
///     Service for managing authentication and authorization
/// </summary>
public class AuthService(
    IHttpClientFactory httpClientFactory,
    ICacheService cache,
    IStateManager state,
    ILogger<AuthService> logger)
    : BaseService(httpClientFactory, logger, "AuthService"), IAuthService
{
    public bool IsAuthenticated => !string.IsNullOrEmpty(GetAuthToken().Result);

    public string? CurrentToken => GetAuthToken().Result;

    public async Task<UserDto> RegisterAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Auth.Register);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(ApiEndpoints.Auth.Register, request, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Auth.Register, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<UserDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse user response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task<AuthResponse> LoginAsync(AuthRequest request, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Auth.Login);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(ApiEndpoints.Auth.Login, request, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Auth.Login, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<AuthResponse>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse login response",
                HttpStatusCode.InternalServerError);
        }

        // Store tokens
        await state.SetStateAsync(
            StateKeys.AuthToken(),
            new AuthState
            {
                Token = response.AccessToken,
                ExpiresAt = response.ExpiresAt
            },
            ct);

        await state.SetStateAsync(
            StateKeys.RefreshToken(),
            response.RefreshToken,
            ct);

        // Store user
        await cache.SetAsync(
            CacheKeys.UserMe(),
            response.User,
            ct: ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        var token = await GetAuthToken(ct);
        if (token == null)
        {
            return;
        }

        try
        {
            var response = await ExecuteWithPoliciesAsync(
                async client =>
                {
                    var response = await client.PostAsJsonAsync(ApiEndpoints.Auth.Logout, new { Token = token }, ct);
                    logger.LogHttpResponse("POST", ApiEndpoints.Auth.Logout, (int)response.StatusCode);
                    return await response.Content.ReadFromJsonAsync<bool>(ct);
                },
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to logout on server");
        }

        await state.RemoveStateAsync(StateKeys.AuthToken(), ct);
        await state.RemoveStateAsync(StateKeys.RefreshToken(), ct);
        await cache.ClearAsync(ct);

        // Clear device keys
        await state.RemoveStateAsync(StateKeys.DeviceKeys(), ct);
        await state.RemoveStateAsync(StateKeys.DevicePreKey(), ct);

        logger.LogMethodExit();
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Auth.Refresh);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response =
                    await client.PostAsJsonAsync(ApiEndpoints.Auth.Refresh, new { RefreshToken = refreshToken }, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Auth.Refresh, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<AuthResponse>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse refresh response",
                HttpStatusCode.InternalServerError);
        }

        // Store tokens
        await state.SetStateAsync(
            StateKeys.AuthToken(),
            new AuthState
            {
                Token = response.AccessToken,
                ExpiresAt = response.ExpiresAt
            },
            ct);

        await state.SetStateAsync(
            StateKeys.RefreshToken(),
            response.RefreshToken,
            ct);

        // Store user
        await cache.SetAsync(
            CacheKeys.UserMe(),
            response.User,
            ct: ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Auth.Validate);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(ApiEndpoints.Auth.Validate, new { Token = token }, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Auth.Validate, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<bool>(ct);
            });

        logger.LogMethodExit();
        return response;
    }

    public async Task<string?> GetAuthToken(CancellationToken ct = default)
    {
        var authState = await state.GetStateAsync<AuthState>(StateKeys.AuthToken(), ct);
        if (authState == null)
        {
            return null;
        }

        // Check if token is expired or about to expire (within 5 minutes)
        if (authState.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
        {
            // Try to refresh the token
            var refreshToken = await state.GetStateAsync<string>(StateKeys.RefreshToken(), ct);
            if (refreshToken == null)
            {
                return null;
            }

            try
            {
                await RefreshTokenAsync(refreshToken, ct);
                authState = await state.GetStateAsync<AuthState>(StateKeys.AuthToken(), ct);
            }
            catch
            {
                // If refresh fails, return null to indicate no valid token
                return null;
            }
        }

        return authState?.Token;
    }
}
