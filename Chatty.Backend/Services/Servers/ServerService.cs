using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Data.Models.Extensions;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Backend.Realtime.Events;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Servers;
using Chatty.Shared.Realtime.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Chatty.Backend.Services.Servers;

public sealed class ServerService(
    ChattyDbContext context,
    IEventBus eventBus,
    ILogger<ServerService> logger,
    IOptions<LimitSettings> limitSettings)
    : IServerService
{
    private readonly LimitSettings _limitSettings = limitSettings.Value;

    public async Task<Result<ServerDto>> CreateAsync(
        Guid userId,
        CreateServerRequest request,
        CancellationToken ct = default)
    {
        // Check server limit for user
        var userServerCount = await context.ServerMembers
            .CountAsync(m => m.UserId == userId, ct);

        if (userServerCount >= _limitSettings.MaxServersPerUser)
        {
            return Result<ServerDto>.Failure(
                Error.Validation($"User cannot join more than {_limitSettings.MaxServersPerUser} servers"));
        }

        try
        {
            var server = new Server
            {
                Name = request.Name,
                OwnerId = userId,
                IconUrl = request.IconUrl
            };

            context.Servers.Add(server);
            await context.SaveChangesAsync(ct);

            // Create default role
            var defaultRole = new ServerRole
            {
                ServerId = server.Id,
                Name = "@everyone",
                IsDefault = true,
                Position = 0,
                Permissions =
                [
                    new ServerRolePermission { Permission = PermissionType.ViewChannels },
                    new ServerRolePermission { Permission = PermissionType.SendMessages },
                    new ServerRolePermission { Permission = PermissionType.ReadMessageHistory },
                    new ServerRolePermission { Permission = PermissionType.Connect },
                    new ServerRolePermission { Permission = PermissionType.Speak }
                ]
            };

            context.ServerRoles.Add(defaultRole);
            await context.SaveChangesAsync(ct);

            // Add owner as member with default role
            var ownerMember = new ServerMember
            {
                ServerId = server.Id,
                UserId = userId,
                RoleId = defaultRole.Id
            };

            // Create default channels
            var generalChannel = new Channel
            {
                ServerId = server.Id,
                Name = "general",
                ChannelType = ChannelType.Text,
                Position = 0
            };

            var voiceChannel = new Channel
            {
                ServerId = server.Id,
                Name = "voice",
                ChannelType = ChannelType.Voice,
                Position = 1
            };

            context.ServerMembers.Add(ownerMember);
            context.Channels.Add(generalChannel);
            context.Channels.Add(voiceChannel);

            // Add owner to default channels
            var generalMember = new ChannelMember
            {
                ChannelId = generalChannel.Id,
                UserId = userId
            };

            var voiceMember = new ChannelMember
            {
                ChannelId = voiceChannel.Id,
                UserId = userId
            };

            context.ChannelMembers.Add(generalMember);
            context.ChannelMembers.Add(voiceMember);

            await context.SaveChangesAsync(ct);

            // Load relationships for DTO
            await LoadServerRelationships(server, ct);

            var serverDto = server.ToDto();

            // Publish server created event
            await eventBus.PublishAsync(new ServerCreatedEvent(serverDto), ct);

            return Result<ServerDto>.Success(serverDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create server");
            return Result<ServerDto>.Failure(Error.Internal("Failed to create server"));
        }
    }

    public async Task<Result<ServerDto>> UpdateAsync(
        Guid serverId,
        UpdateServerRequest request,
        CancellationToken ct = default)
    {
        var server = await context.Servers
            .Include(s => s.Owner)
            .Include(s => s.Roles)
            .Include(s => s.Members)
            .ThenInclude(m => m.User)
            .Include(s => s.Members)
            .ThenInclude(m => m.Role)
            .FirstOrDefaultAsync(s => s.Id == serverId, ct);

        if (server is null)
        {
            return Result<ServerDto>.Failure(Error.NotFound("Server not found"));
        }

        try
        {
            if (request.Name is not null)
            {
                server.Name = request.Name;
            }

            if (request.IconUrl is not null)
            {
                server.IconUrl = request.IconUrl;
            }

            server.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            var serverDto = server.ToDto();

            // Publish server updated event
            await eventBus.PublishAsync(new ServerUpdatedEvent(serverDto), ct);

            return Result<ServerDto>.Success(serverDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update server {ServerId}", serverId);
            return Result<ServerDto>.Failure(Error.Internal("Failed to update server"));
        }
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        var server = await context.Servers.FindAsync([serverId], ct);
        if (server is null)
        {
            return Result<bool>.Success(true);
        }

        try
        {
            context.Servers.Remove(server);
            await context.SaveChangesAsync(ct);

            // Publish server deleted event
            await eventBus.PublishAsync(new ServerDeletedEvent(serverId), ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete server {ServerId}", serverId);
            return Result<bool>.Failure(Error.Internal("Failed to delete server"));
        }
    }

    public async Task<Result<ServerDto>> GetAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        var server = await context.Servers
            .Include(s => s.Owner)
            .Include(s => s.Roles)
            .Include(s => s.Members)
            .ThenInclude(m => m.User)
            .Include(s => s.Members)
            .ThenInclude(m => m.Role)
            .FirstOrDefaultAsync(s => s.Id == serverId, ct);

        if (server is null)
        {
            return Result<ServerDto>.Failure(Error.NotFound("Server not found"));
        }

        return Result<ServerDto>.Success(server.ToDto());
    }

    public async Task<Result<IReadOnlyList<ServerDto>>> GetUserServersAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var servers = await context.Servers
            .Include(s => s.Owner)
            .Include(s => s.Roles)
            .Include(s => s.Members)
            .ThenInclude(m => m.User)
            .Include(s => s.Members)
            .ThenInclude(m => m.Role)
            .Where(s => s.Members.Any(m => m.UserId == userId))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ServerDto>>.Success(
            servers.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<ServerRoleDto>> CreateRoleAsync(
        Guid serverId,
        CreateServerRoleRequest request,
        CancellationToken ct = default)
    {
        var server = await context.Servers.FindAsync([serverId], ct);
        if (server is null)
        {
            return Result<ServerRoleDto>.Failure(Error.NotFound("Server not found"));
        }

        try
        {
            var role = new ServerRole
            {
                ServerId = serverId,
                Name = request.Name,
                Color = request.Color,
                Position = request.Position,
                Permissions = ConvertToServerRolePermissions(request.Permissions)
            };

            context.ServerRoles.Add(role);
            await context.SaveChangesAsync(ct);

            var roleDto = role.ToDto();

            // Publish role created event
            await eventBus.PublishAsync(new ServerRoleCreatedEvent(serverId, roleDto), ct);

            return Result<ServerRoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create role for server {ServerId}", serverId);
            return Result<ServerRoleDto>.Failure(Error.Internal("Failed to create role"));
        }
    }

    public async Task<Result<ServerMemberDto>> AddMemberAsync(
        Guid serverId,
        Guid userId,
        Guid? roleId = null,
        CancellationToken ct = default)
    {
        var server = await context.Servers
            .Include(s => s.Members)
            .Include(s => s.Roles)
            .Include(server => server.Channels)
            .FirstOrDefaultAsync(s => s.Id == serverId, ct);

        if (server is null)
        {
            return Result<ServerMemberDto>.Failure(Error.NotFound("Server not found"));
        }

        // Check member limit
        if (server.Members.Count >= _limitSettings.MaxMembersPerServer)
        {
            return Result<ServerMemberDto>.Failure(
                Error.Validation($"Server cannot have more than {_limitSettings.MaxMembersPerServer} members"));
        }

        if (server.Members.Any(m => m.UserId == userId))
        {
            return Result<ServerMemberDto>.Failure(Error.Validation("User is already a member of this server"));
        }

        try
        {
            // If no role specified, use the default role
            if (roleId == null)
            {
                var defaultRole = server.Roles.FirstOrDefault(r => r.IsDefault);
                if (defaultRole == null)
                {
                    return Result<ServerMemberDto>.Failure(Error.Internal("Server has no default role"));
                }

                roleId = defaultRole.Id;
            }

            var member = new ServerMember
            {
                ServerId = serverId,
                UserId = userId,
                RoleId = roleId
            };

            context.ServerMembers.Add(member);

            // Add member to all default channels
            foreach (var channel in server.Channels.Where(c => !c.IsPrivate))
            {
                var channelMember = new ChannelMember
                {
                    ChannelId = channel.Id,
                    UserId = userId
                };
                context.ChannelMembers.Add(channelMember);
            }

            await context.SaveChangesAsync(ct);

            // Load relationships for event
            await context.Entry(member)
                .Reference(m => m.User)
                .LoadAsync(ct);

            await context.Entry(member)
                .Reference(m => m.Role)
                .LoadAsync(ct);

            // Publish member joined event
            await eventBus.PublishAsync(
                new ServerMemberJoinedEvent(serverId, member.ToDto()),
                ct);

            return Result<ServerMemberDto>.Success(member.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add member {UserId} to server {ServerId}", userId, serverId);
            return Result<ServerMemberDto>.Failure(Error.Internal("Failed to add member"));
        }
    }

    public async Task<Result<bool>> RemoveMemberAsync(
        Guid serverId,
        Guid userId,
        CancellationToken ct = default)
    {
        var member = await context.ServerMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId, ct);

        if (member is null)
        {
            return Result<bool>.Success(true);
        }

        try
        {
            context.ServerMembers.Remove(member);
            await context.SaveChangesAsync(ct);

            // Publish member left event
            await eventBus.PublishAsync(
                new ServerMemberRemovedEvent(serverId, member.User.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove member {UserId} from server {ServerId}", userId, serverId);
            return Result<bool>.Failure(Error.Internal("Failed to remove member"));
        }
    }

    public async Task<Result<bool>> UpdateMemberRoleAsync(
        Guid serverId,
        Guid userId,
        Guid roleId,
        CancellationToken ct = default)
    {
        var member = await context.ServerMembers
            .Include(m => m.User)
            .Include(m => m.Role)
            .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId, ct);

        if (member is null)
        {
            return Result<bool>.Failure(Error.NotFound("Member not found"));
        }

        try
        {
            member.RoleId = roleId;
            await context.SaveChangesAsync(ct);

            // Publish member updated event
            await eventBus.PublishAsync(
                new ServerMemberUpdatedEvent(serverId, member.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update role for member {UserId} in server {ServerId}", userId, serverId);
            return Result<bool>.Failure(Error.Internal("Failed to update member role"));
        }
    }

    public async Task<Result<ServerMemberDto>> UpdateMemberAsync(
        Guid serverId,
        Guid userId,
        UpdateServerMemberRequest request,
        CancellationToken ct = default)
    {
        var member = await context.ServerMembers
            .Include(m => m.User)
            .Include(m => m.Role)
            .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId, ct);

        if (member is null)
        {
            return Result<ServerMemberDto>.Failure(Error.NotFound("Member not found"));
        }

        try
        {
            // Update member properties
            if (request.Nickname != null)
            {
                member.Nickname = request.Nickname;
            }

            if (request.RoleId.HasValue)
            {
                member.RoleId = request.RoleId.Value;
            }

            if (request.IsMuted.HasValue)
            {
                member.IsMuted = request.IsMuted.Value;
            }

            if (request.IsDeafened.HasValue)
            {
                member.IsDeafened = request.IsDeafened.Value;
            }

            await context.SaveChangesAsync(ct);

            // Publish member updated event
            await eventBus.PublishAsync(
                new ServerMemberUpdatedEvent(serverId, member.ToDto()),
                ct);

            return Result<ServerMemberDto>.Success(member.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update member {UserId} in server {ServerId}", userId, serverId);
            return Result<ServerMemberDto>.Failure(Error.Internal("Failed to update member"));
        }
    }

    public async Task<Result<bool>> KickMemberAsync(
        Guid serverId,
        Guid userId,
        CancellationToken ct = default)
    {
        var server = await context.Servers
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == serverId, ct);

        if (server is null)
        {
            return Result<bool>.Failure(Error.NotFound("Server not found"));
        }

        // Cannot kick the server owner
        if (server.OwnerId == userId)
        {
            return Result<bool>.Failure(Error.Validation("Cannot kick the server owner"));
        }

        var member = await context.ServerMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId, ct);

        if (member is null)
        {
            return Result<bool>.Success(true);
        }

        try
        {
            context.ServerMembers.Remove(member);
            await context.SaveChangesAsync(ct);

            // Publish member kicked event
            await eventBus.PublishAsync(
                new ServerMemberRemovedEvent(serverId, member.User.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to kick member {UserId} from server {ServerId}", userId, serverId);
            return Result<bool>.Failure(Error.Internal("Failed to kick member"));
        }
    }

    public async Task<Result<IReadOnlyList<ServerMemberDto>>> GetMembersAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        try
        {
            var members = await context.ServerMembers
                .Include(m => m.User)
                .Include(m => m.Role)
                .Where(m => m.ServerId == serverId)
                .ToListAsync(ct);

            return Result<IReadOnlyList<ServerMemberDto>>.Success(
                members.Select(m => m.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get members for server {ServerId}", serverId);
            return Result<IReadOnlyList<ServerMemberDto>>.Failure(Error.Internal("Failed to get server members"));
        }
    }

    public async Task<Result<IReadOnlyList<ServerRoleDto>>> GetRolesAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        try
        {
            var roles = await context.ServerRoles
                .Include(r => r.Permissions)
                .Where(r => r.ServerId == serverId)
                .OrderBy(r => r.Position)
                .ToListAsync(ct);

            return Result<IReadOnlyList<ServerRoleDto>>.Success(
                roles.Select(r => r.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get roles for server {ServerId}", serverId);
            return Result<IReadOnlyList<ServerRoleDto>>.Failure(Error.Internal("Failed to get server roles"));
        }
    }

    public async Task<Result<ServerRoleDto>> UpdateRoleAsync(
        Guid serverId,
        Guid roleId,
        UpdateServerRoleRequest request,
        CancellationToken ct = default)
    {
        var role = await context.ServerRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.ServerId == serverId && r.Id == roleId, ct);

        if (role is null)
        {
            return Result<ServerRoleDto>.Failure(Error.NotFound("Role not found"));
        }

        // Cannot modify default role permissions
        if (role.IsDefault && request.Permissions != null)
        {
            return Result<ServerRoleDto>.Failure(Error.Validation("Cannot modify default role permissions"));
        }

        try
        {
            if (request.Name != null)
            {
                role.Name = request.Name;
            }

            if (request.Color != null)
            {
                role.Color = request.Color;
            }

            if (request.Position != null)
            {
                role.Position = request.Position.Value;
            }

            if (request.Permissions != null)
            {
                // Remove existing permissions
                role.Permissions.Clear();


                // Add new permissions
                role.Permissions = ConvertToServerRolePermissions(request.Permissions);
            }

            await context.SaveChangesAsync(ct);

            // Publish role updated event
            await eventBus.PublishAsync(
                new ServerRoleUpdatedEvent(serverId, role.ToDto()),
                ct);

            return Result<ServerRoleDto>.Success(role.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update role {RoleId} in server {ServerId}", roleId, serverId);
            return Result<ServerRoleDto>.Failure(Error.Internal("Failed to update role"));
        }
    }

    public async Task<Result<bool>> DeleteRoleAsync(
        Guid serverId,
        Guid roleId,
        CancellationToken ct = default)
    {
        var role = await context.ServerRoles
            .FirstOrDefaultAsync(r => r.ServerId == serverId && r.Id == roleId, ct);

        if (role is null)
        {
            return Result<bool>.Success(true);
        }

        if (role.IsDefault)
        {
            return Result<bool>.Failure(Error.Validation("Cannot delete default role"));
        }

        try
        {
            context.ServerRoles.Remove(role);
            await context.SaveChangesAsync(ct);

            // Publish role deleted event
            await eventBus.PublishAsync(
                new ServerRoleDeletedEvent(serverId, roleId),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete role {RoleId} from server {ServerId}", roleId, serverId);
            return Result<bool>.Failure(Error.Internal("Failed to delete role"));
        }
    }

    private static ICollection<ServerRolePermission> ConvertToServerRolePermissions(
        ICollection<PermissionType> permissions) =>
        permissions.Select(p => new ServerRolePermission { Permission = p }).ToList();

    private async Task LoadServerRelationships(Server server, CancellationToken ct)
    {
        await context.Entry(server)
            .Reference(s => s.Owner)
            .LoadAsync(ct);

        await context.Entry(server)
            .Collection(s => s.Roles)
            .LoadAsync(ct);

        await context.Entry(server)
            .Collection(s => s.Members)
            .Query()
            .Include(m => m.User)
            .Include(m => m.Role)
            .LoadAsync(ct);
    }
}
