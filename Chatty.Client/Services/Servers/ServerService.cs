using System.Net;
using System.Net.Http.Json;

using Chatty.Client.Cache;
using Chatty.Client.Exceptions;
using Chatty.Client.Http;
using Chatty.Client.Logging;
using Chatty.Client.Realtime;
using Chatty.Client.State;
using Chatty.Shared.Models.Servers;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Services.Servers;

/// <summary>
///     Service for managing servers
/// </summary>
public class ServerService(
    IHttpClientFactory httpClientFactory,
    ICacheService cache,
    IStateManager state,
    IChattyRealtimeClient realtimeClient,
    ILogger<ServerService> logger)
    : BaseService(httpClientFactory, logger, "ServerService"), IServerService
{
    private readonly IStateManager _state = state;

    public async Task<ServerDto> GetAsync(Guid serverId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try cache first
        var server = await cache.GetAsync<ServerDto>(
            CacheKeys.Server(serverId),
            ct);

        if (server is not null)
        {
            logger.LogCacheHit(CacheKeys.Server(serverId));
            logger.LogMethodExit();
            return server;
        }

        logger.LogCacheMiss(CacheKeys.Server(serverId));
        var endpoint = ApiEndpoints.Servers.Server(serverId);
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
                        "Failed to get server",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ServerDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse server response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.Server(serverId),
            response,
            ct: ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<IReadOnlyList<ServerDto>> GetJoinedServersAsync(CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try cache first
        var servers = await cache.GetAsync<List<ServerDto>>(
            CacheKeys.JoinedServers(),
            ct);

        if (servers is not null)
        {
            logger.LogCacheHit(CacheKeys.JoinedServers());
            logger.LogMethodExit();
            return servers;
        }

        logger.LogCacheMiss(CacheKeys.JoinedServers());

        // Get from realtime client
        servers = await realtimeClient.GetJoinedServersAsync(ct);

        // Update cache
        await cache.SetAsync(
            CacheKeys.JoinedServers(),
            servers,
            ct: ct);

        logger.LogMethodExit();
        return servers;
    }

    public async Task<ServerDto> CreateAsync(CreateServerRequest request, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", ApiEndpoints.Servers.List);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(ApiEndpoints.Servers.List, request, ct);
                logger.LogHttpResponse("POST", ApiEndpoints.Servers.List, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to create server",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ServerDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse server response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.Server(response.Id),
            response,
            ct: ct);

        // Invalidate server list
        await cache.RemoveAsync(
            CacheKeys.ServerList(),
            ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<ServerDto> UpdateAsync(
        Guid serverId,
        UpdateServerRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Server(serverId);
        logger.LogHttpRequest("PUT", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsJsonAsync(endpoint, request, ct);
                logger.LogHttpResponse("PUT", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to update server",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ServerDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse server response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.Server(serverId),
            response,
            ct: ct);

        // Invalidate server list
        await cache.RemoveAsync(
            CacheKeys.ServerList(),
            ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task DeleteAsync(Guid serverId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Server(serverId);
        logger.LogHttpRequest("DELETE", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.DeleteAsync(endpoint, ct);
                logger.LogHttpResponse("DELETE", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to delete server",
                        response.StatusCode);
                }

                return response;
            });

        // Remove from cache
        await cache.RemoveAsync(
            CacheKeys.Server(serverId),
            ct);

        // Invalidate server list
        await cache.RemoveAsync(
            CacheKeys.ServerList(),
            ct);

        logger.LogMethodExit();
    }

    public async Task<IReadOnlyList<ServerMemberDto>> GetMembersAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try cache first
        var members = await cache.GetAsync<List<ServerMemberDto>>(
            CacheKeys.ServerMembers(serverId),
            ct);

        if (members is not null)
        {
            logger.LogCacheHit(CacheKeys.ServerMembers(serverId));
            logger.LogMethodExit();
            return members;
        }

        logger.LogCacheMiss(CacheKeys.ServerMembers(serverId));
        var endpoint = ApiEndpoints.Servers.Members(serverId);
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
                        "Failed to get server members",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<ServerMemberDto>>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse members response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.ServerMembers(serverId),
            response,
            ct: ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<ServerMemberDto> AddMemberAsync(
        Guid serverId,
        Guid userId,
        Guid? roleId = null,
        CancellationToken ct = default)
    {
        var endpoint = ApiEndpoints.Servers.Members(serverId);
        logger.LogMethodEntry();
        logger.LogHttpRequest("POST", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(endpoint, new { UserId = userId, RoleId = roleId }, ct);
                logger.LogHttpResponse("POST", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to add server member",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ServerMemberDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse member response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task<ServerMemberDto> UpdateMemberAsync(
        Guid serverId,
        Guid userId,
        UpdateServerMemberRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Member(serverId, userId);
        logger.LogHttpRequest("PUT", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsJsonAsync(endpoint, request, ct);
                logger.LogHttpResponse("PUT", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to update server member",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ServerMemberDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse member response",
                HttpStatusCode.InternalServerError);
        }

        // Invalidate members cache
        await cache.RemoveAsync(
            CacheKeys.ServerMembers(serverId),
            ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task KickMemberAsync(
        Guid serverId,
        Guid userId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.MemberKick(serverId, userId);
        logger.LogHttpRequest("POST", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsync(endpoint, null, ct);
                logger.LogHttpResponse("POST", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to kick server member",
                        response.StatusCode);
                }

                return response;
            });

        // Invalidate members cache
        await cache.RemoveAsync(
            CacheKeys.ServerMembers(serverId),
            ct);

        logger.LogMethodExit();
    }

    public async Task RemoveMemberAsync(
        Guid serverId,
        Guid userId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Member(serverId, userId);
        logger.LogHttpRequest("DELETE", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.DeleteAsync(endpoint, ct);
                logger.LogHttpResponse("DELETE", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to remove server member",
                        response.StatusCode);
                }

                return response;
            });

        // Invalidate members cache
        await cache.RemoveAsync(
            CacheKeys.ServerMembers(serverId),
            ct);

        logger.LogMethodExit();
    }

    public async Task<IReadOnlyList<ServerRoleDto>> GetRolesAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();

        // Try cache first
        var roles = await cache.GetAsync<List<ServerRoleDto>>(
            CacheKeys.ServerRoles(serverId),
            ct);

        if (roles is not null)
        {
            logger.LogCacheHit(CacheKeys.ServerRoles(serverId));
            logger.LogMethodExit();
            return roles;
        }

        logger.LogCacheMiss(CacheKeys.ServerRoles(serverId));
        var endpoint = ApiEndpoints.Servers.Roles(serverId);
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
                        "Failed to get server roles",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<ServerRoleDto>>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse roles response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.ServerRoles(serverId),
            response,
            ct: ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<ServerRoleDto> CreateRoleAsync(
        Guid serverId,
        CreateServerRoleRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Roles(serverId);
        logger.LogHttpRequest("POST", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(endpoint, request, ct);
                logger.LogHttpResponse("POST", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to create server role",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ServerRoleDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse role response",
                HttpStatusCode.InternalServerError);
        }

        // Invalidate roles cache
        await cache.RemoveAsync(
            CacheKeys.ServerRoles(serverId),
            ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<ServerRoleDto> UpdateRoleAsync(
        Guid serverId,
        Guid roleId,
        UpdateServerRoleRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Role(serverId, roleId);
        logger.LogHttpRequest("PUT", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsJsonAsync(endpoint, request, ct);
                logger.LogHttpResponse("PUT", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to update server role",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ServerRoleDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse role response",
                HttpStatusCode.InternalServerError);
        }

        // Invalidate roles cache
        await cache.RemoveAsync(
            CacheKeys.ServerRoles(serverId),
            ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task DeleteRoleAsync(Guid serverId, Guid roleId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Role(serverId, roleId);
        logger.LogHttpRequest("DELETE", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.DeleteAsync(endpoint, ct);
                logger.LogHttpResponse("DELETE", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to delete server role",
                        response.StatusCode);
                }

                return response;
            });

        // Invalidate roles cache
        await cache.RemoveAsync(
            CacheKeys.ServerRoles(serverId),
            ct);

        logger.LogMethodExit();
    }

    public async Task<ServerMemberDto> UpdateMemberRoleAsync(
        Guid serverId,
        Guid userId,
        Guid roleId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Role(serverId, userId);
        logger.LogHttpRequest("PUT", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PutAsJsonAsync(endpoint, new { RoleId = roleId }, ct);
                logger.LogHttpResponse("PUT", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to update member role",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ServerMemberDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse member response",
                HttpStatusCode.InternalServerError);
        }

        // Invalidate members cache
        await cache.RemoveAsync(
            CacheKeys.ServerMembers(serverId),
            ct);

        logger.LogMethodExit();
        return response;
    }

    public async Task<ServerInviteDto> CreateInviteAsync(
        Guid serverId,
        CreateServerInviteRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Invites(serverId);
        logger.LogHttpRequest("POST", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsJsonAsync(endpoint, request, ct);
                logger.LogHttpResponse("POST", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to create server invite",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ServerInviteDto>(ct);
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

    public async Task<IReadOnlyList<ServerInviteDto>> GetInvitesAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Invites(serverId);
        logger.LogHttpRequest("GET", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get server invites",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<ServerInviteDto>>(ct);
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
        Guid serverId,
        string inviteCode,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Invite(serverId, Guid.Parse(inviteCode));
        logger.LogHttpRequest("DELETE", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.DeleteAsync(endpoint, ct);
                logger.LogHttpResponse("DELETE", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to delete server invite",
                        response.StatusCode);
                }

                return response;
            });

        logger.LogMethodExit();
    }

    public async Task<ServerDto> JoinAsync(string inviteCode, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Servers.Join(inviteCode);
        logger.LogHttpRequest("POST", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.PostAsync(endpoint, null, ct);
                logger.LogHttpResponse("POST", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to join server",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<ServerDto>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse server response",
                HttpStatusCode.InternalServerError);
        }

        // Update cache
        await cache.SetAsync(
            CacheKeys.Server(response.Id),
            response,
            ct: ct);

        // Invalidate joined servers cache
        await cache.RemoveAsync(
            CacheKeys.JoinedServers(),
            ct);

        // Join server's realtime group
        await realtimeClient.JoinServerAsync(response.Id);

        logger.LogMethodExit();
        return response;
    }

    public async Task LeaveAsync(Guid serverId, CancellationToken ct = default)
    {
        var response = await ExecuteWithPoliciesAsync(
            client => client.PostAsync($"api/servers/{serverId}/leave", null, ct));

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to leave server",
                response.StatusCode);
        }

        // Remove from cache
        await cache.RemoveAsync(
            CacheKeys.Server(serverId),
            ct);

        // Invalidate joined servers cache
        await cache.RemoveAsync(
            CacheKeys.JoinedServers(),
            ct);

        // Leave server's realtime group
        await realtimeClient.LeaveServerAsync(serverId);
    }

    public async Task AssignRoleAsync(
        Guid serverId,
        Guid userId,
        Guid roleId,
        CancellationToken ct = default)
    {
        var response = await ExecuteWithPoliciesAsync(
            client => client.PostAsync($"api/servers/{serverId}/members/{userId}/roles/{roleId}", null, ct));

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to assign role",
                response.StatusCode);
        }
    }

    public async Task UnassignRoleAsync(
        Guid serverId,
        Guid userId,
        Guid roleId,
        CancellationToken ct = default)
    {
        var response = await ExecuteWithPoliciesAsync(
            client => client.DeleteAsync($"api/servers/{serverId}/members/{userId}/roles/{roleId}", ct));

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to unassign role",
                response.StatusCode);
        }
    }
}
