using System.Net;
using System.Net.Http.Json;

using Chatty.Client.Cache;
using Chatty.Client.Exceptions;
using Chatty.Client.Http;
using Chatty.Client.Logging;
using Chatty.Shared.Models.Channels;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Services.Channels;

/// <summary>
///     Service for managing channels
/// </summary>
public sealed class ChannelService(
    IHttpClientFactory httpClientFactory,
    ICacheService cache,
    ILogger<ChannelService> logger)
    : BaseService(httpClientFactory, logger, "ChannelService"), IChannelService
{
    public async Task<ChannelDto> GetAsync(Guid channelId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try cache first
        var channel = await cache.GetAsync<ChannelDto>(
            CacheKeys.Channel(channelId),
            ct);

        if (channel is not null)
        {
            logger.LogCacheHit(CacheKeys.Channel(channelId));
            logger.LogMethodExit();
            return channel;
        }

        logger.LogCacheMiss(CacheKeys.Channel(channelId));
        var endpoint = ApiEndpoints.Channels.Channel(channelId);
        logger.LogHttpRequest("GET", endpoint);

        // Get from server
        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get channel",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ChannelDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse channel response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.Channel(channelId),
            response,
            ct: ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<IReadOnlyList<ChannelDto>> GetForServerAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try cache first
        var channels = await cache.GetAsync<List<ChannelDto>>(
            CacheKeys.ServerChannels(serverId),
            ct);

        if (channels is not null)
        {
            logger.LogCacheHit(CacheKeys.ServerChannels(serverId));
            logger.LogMethodExit();
            return channels;
        }

        logger.LogCacheMiss(CacheKeys.ServerChannels(serverId));
        var endpoint = ApiEndpoints.Channels.ServerChannels(serverId);
        logger.LogHttpRequest("GET", endpoint);

        // Get from server
        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get channels",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<ChannelDto>>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse channels response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.ServerChannels(serverId),
            response,
            ct: ct);

        foreach (var channel in response)
        {
            await cache.SetAsync(
                CacheKeys.Channel(channel.Id),
                channel,
                ct: ct);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task<ChannelDto> CreateAsync(
        Guid serverId,
        CreateChannelRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Channels.ServerChannels(serverId));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response =
                    await client.PostAsJsonAsync(ApiEndpoints.Channels.ServerChannels(serverId), request, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Channels.ServerChannels(serverId),
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to create channel",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ChannelDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse channel response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.Channel(response.Id),
            response,
            ct: ct);

        // Invalidate server channels cache
        await cache.RemoveAsync(
            CacheKeys.ServerChannels(serverId),
            ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<ChannelDto> UpdateAsync(
        Guid channelId,
        UpdateChannelRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("PUT", ApiEndpoints.Channels.Channel(channelId));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsJsonAsync(ApiEndpoints.Channels.Channel(channelId), request, ct);
                logger.LogHttpResponse("PUT", ApiEndpoints.Channels.Channel(channelId), (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to update channel",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ChannelDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse channel response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.Channel(channelId),
            response,
            ct: ct);

        // Invalidate server channels cache
        await cache.RemoveAsync(
            CacheKeys.ServerChannels(response.ServerId),
            ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task DeleteAsync(Guid channelId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Get channel first for server ID
        var channel = await GetAsync(channelId, ct);

        logger.LogHttpRequest("DELETE", ApiEndpoints.Channels.Channel(channelId));

        var response = await ExecuteWithPoliciesAsync(
            client => client.DeleteAsync(ApiEndpoints.Channels.Channel(channelId), ct));

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to delete channel",
                response.StatusCode);
        }

        // Remove from cache
        await cache.RemoveAsync(
            CacheKeys.Channel(channelId),
            ct);

        // Invalidate server channels cache
        await cache.RemoveAsync(
            CacheKeys.ServerChannels(channel.ServerId),
            ct);

        logger.LogMethodExit();
    }

    public async Task<IReadOnlyList<ChannelMemberDto>> GetMembersAsync(
        Guid channelId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("GET", ApiEndpoints.Channels.ChannelMembers(channelId));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(ApiEndpoints.Channels.ChannelMembers(channelId), ct);
                logger.LogHttpResponse("GET", ApiEndpoints.Channels.ChannelMembers(channelId),
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get members",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<ChannelMemberDto>>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse members response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task AddMemberAsync(
        Guid channelId,
        Guid userId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Channels.Member(channelId, userId);
        logger.LogHttpRequest("PUT", endpoint);

        await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsync(endpoint, null, ct);
                logger.LogHttpResponse("PUT", endpoint, (int)response.StatusCode);
                return response;
            });

        logger.LogMethodExit();
    }

    public async Task RemoveMemberAsync(
        Guid channelId,
        Guid userId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("DELETE", ApiEndpoints.Channels.ChannelMember(channelId, userId));

        var response = await ExecuteWithPoliciesAsync(
            client => client.DeleteAsync(ApiEndpoints.Channels.ChannelMember(channelId, userId), ct));

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to remove member",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task<ChannelPermissionsDto> GetPermissionsAsync(
        Guid channelId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("GET", ApiEndpoints.Channels.ChannelPermissions(channelId));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(ApiEndpoints.Channels.ChannelPermissions(channelId), ct);
                logger.LogHttpResponse("GET", ApiEndpoints.Channels.ChannelPermissions(channelId),
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get permissions",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ChannelPermissionsDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse permissions response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task<ChannelPermissionsDto> UpdatePermissionsAsync(
        Guid channelId,
        UpdateChannelPermissionsRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("PUT", ApiEndpoints.Channels.ChannelPermissions(channelId));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response =
                    await client.PutAsJsonAsync(ApiEndpoints.Channels.ChannelPermissions(channelId), request, ct);
                logger.LogHttpResponse("PUT", ApiEndpoints.Channels.ChannelPermissions(channelId),
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to update permissions",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ChannelPermissionsDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse permissions response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task<ChannelInviteDto> CreateInviteAsync(
        Guid channelId,
        CreateChannelInviteRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Channels.ChannelInvites(channelId));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response =
                    await client.PostAsJsonAsync(ApiEndpoints.Channels.ChannelInvites(channelId), request, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Channels.ChannelInvites(channelId),
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to create invite",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ChannelInviteDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse invite response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task<IReadOnlyList<ChannelInviteDto>> GetInvitesAsync(
        Guid channelId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("GET", ApiEndpoints.Channels.ChannelInvites(channelId));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(ApiEndpoints.Channels.ChannelInvites(channelId), ct);
                logger.LogHttpResponse("GET", ApiEndpoints.Channels.ChannelInvites(channelId),
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get invites",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<ChannelInviteDto>>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse invites response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task DeleteInviteAsync(
        Guid channelId,
        string inviteCode,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("DELETE", ApiEndpoints.Channels.ChannelInvite(channelId, inviteCode));

        var response = await ExecuteWithPoliciesAsync(
            client => client.DeleteAsync(ApiEndpoints.Channels.ChannelInvite(channelId, inviteCode), ct));

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to delete invite",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task<ChannelDto> JoinAsync(
        string inviteCode,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Channels.Join(inviteCode));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(ApiEndpoints.Channels.Join(inviteCode), new { }, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Channels.Join(inviteCode), (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to join channel",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ChannelDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse channel response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.Channel(response.Id),
            response,
            ct: ct);

        // Invalidate server channels cache
        await cache.RemoveAsync(
            CacheKeys.ServerChannels(response.ServerId),
            ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<IReadOnlyList<ChannelCategoryDto>> GetCategoriesAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("GET", ApiEndpoints.Channels.ServerCategories(serverId));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(ApiEndpoints.Channels.ServerCategories(serverId), ct);
                logger.LogHttpResponse("GET", ApiEndpoints.Channels.ServerCategories(serverId),
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get categories",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<ChannelCategoryDto>>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse categories response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task<ChannelCategoryDto> CreateCategoryAsync(
        Guid serverId,
        CreateChannelCategoryRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Channels.ServerCategories(serverId));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response =
                    await client.PostAsJsonAsync(ApiEndpoints.Channels.ServerCategories(serverId), request, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Channels.ServerCategories(serverId),
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to create category",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ChannelCategoryDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse category response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task<ChannelCategoryDto> UpdateCategoryAsync(
        Guid categoryId,
        UpdateChannelCategoryRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("PUT", ApiEndpoints.Channels.Category(categoryId));

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsJsonAsync(ApiEndpoints.Channels.Category(categoryId), request, ct);
                logger.LogHttpResponse("PUT", ApiEndpoints.Channels.Category(categoryId), (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to update category",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ChannelCategoryDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse category response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task DeleteCategoryAsync(
        Guid categoryId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("DELETE", ApiEndpoints.Channels.Category(categoryId));

        var response = await ExecuteWithPoliciesAsync(
            client => client.DeleteAsync(ApiEndpoints.Channels.Category(categoryId), ct));

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to delete category",
                response.StatusCode);
        }

        logger.LogMethodExit();
    }

    public async Task UpdateMemberAsync(
        Guid channelId,
        Guid userId,
        UpdateChannelMemberRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Channels.MemberRole(channelId, userId);
        logger.LogHttpRequest("PUT", endpoint);

        await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsJsonAsync(endpoint, request, ct);
                logger.LogHttpResponse("PUT", endpoint, (int)response.StatusCode);
                return response;
            });

        logger.LogMethodExit();
    }
}
