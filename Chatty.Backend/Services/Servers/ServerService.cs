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

public sealed class ServerService : IServerService
{
    private readonly ChattyDbContext _context;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ServerService> _logger;
    private readonly LimitSettings _limitSettings;

    public ServerService(
        ChattyDbContext context,
        IEventBus eventBus,
        ILogger<ServerService> logger,
        IOptions<LimitSettings> limitSettings)
    {
        _context = context;
        _eventBus = eventBus;
        _logger = logger;
        _limitSettings = limitSettings.Value;
    }

    public async Task<Result<ServerDto>> CreateAsync(
        Guid userId,
        CreateServerRequest request,
        CancellationToken ct = default)
    {
        // Check server limit for user
        var userServerCount = await _context.ServerMembers
            .CountAsync(m => m.UserId == userId, ct);

        if (userServerCount >= _limitSettings.MaxServersPerUser)
            return Result<ServerDto>.Failure(Error.Validation($"User cannot join more than {_limitSettings.MaxServersPerUser} servers"));

        try
        {
            var server = new Server
            {
                Name = request.Name,
                OwnerId = userId,
                IconUrl = request.IconUrl
            };

            _context.Servers.Add(server);
            await _context.SaveChangesAsync(ct);

            // Create default role
            var defaultRole = new ServerRole
            {
                ServerId = server.Id,
                Name = "@everyone",
                IsDefault = true,
                Position = 0,
                Permissions =
                [
                    new() { Permission = PermissionType.ViewChannels },
                    new() { Permission = PermissionType.SendMessages },
                    new() { Permission = PermissionType.ReadMessageHistory },
                    new() { Permission = PermissionType.Connect },
                    new() { Permission = PermissionType.Speak }
                ]
            };

            _context.ServerRoles.Add(defaultRole);
            await _context.SaveChangesAsync(ct);

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

            _context.ServerMembers.Add(ownerMember);
            _context.Channels.Add(generalChannel);
            _context.Channels.Add(voiceChannel);

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

            _context.ChannelMembers.Add(generalMember);
            _context.ChannelMembers.Add(voiceMember);

            await _context.SaveChangesAsync(ct);

            // Load relationships for DTO
            await LoadServerRelationships(server, ct);

            var serverDto = server.ToDto();

            // Publish server created event
            await _eventBus.PublishAsync(new ServerCreatedEvent(serverDto), ct);

            return Result<ServerDto>.Success(serverDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create server");
            return Result<ServerDto>.Failure(Error.Internal("Failed to create server"));
        }
    }

    public async Task<Result<ServerDto>> UpdateAsync(
        Guid serverId,
        UpdateServerRequest request,
        CancellationToken ct = default)
    {
        var server = await _context.Servers
            .Include(s => s.Owner)
            .Include(s => s.Roles)
            .Include(s => s.Members)
                .ThenInclude(m => m.User)
            .Include(s => s.Members)
                .ThenInclude(m => m.Role)
            .FirstOrDefaultAsync(s => s.Id == serverId, ct);

        if (server is null)
            return Result<ServerDto>.Failure(Error.NotFound("Server not found"));

        try
        {
            if (request.Name is not null)
                server.Name = request.Name;

            if (request.IconUrl is not null)
                server.IconUrl = request.IconUrl;

            server.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            var serverDto = server.ToDto();

            // Publish server updated event
            await _eventBus.PublishAsync(new ServerUpdatedEvent(serverDto), ct);

            return Result<ServerDto>.Success(serverDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update server {ServerId}", serverId);
            return Result<ServerDto>.Failure(Error.Internal("Failed to update server"));
        }
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        var server = await _context.Servers.FindAsync([serverId], ct);
        if (server is null)
            return Result<bool>.Success(true);

        try
        {
            _context.Servers.Remove(server);
            await _context.SaveChangesAsync(ct);

            // Publish server deleted event
            await _eventBus.PublishAsync(new ServerDeletedEvent(serverId), ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete server {ServerId}", serverId);
            return Result<bool>.Failure(Error.Internal("Failed to delete server"));
        }
    }

    public async Task<Result<ServerDto>> GetAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        var server = await _context.Servers
            .Include(s => s.Owner)
            .Include(s => s.Roles)
            .Include(s => s.Members)
                .ThenInclude(m => m.User)
            .Include(s => s.Members)
                .ThenInclude(m => m.Role)
            .FirstOrDefaultAsync(s => s.Id == serverId, ct);

        if (server is null)
            return Result<ServerDto>.Failure(Error.NotFound("Server not found"));

        return Result<ServerDto>.Success(server.ToDto());
    }

    public async Task<Result<IReadOnlyList<ServerDto>>> GetUserServersAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var servers = await _context.Servers
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
        var server = await _context.Servers.FindAsync([serverId], ct);
        if (server is null)
            return Result<ServerRoleDto>.Failure(Error.NotFound("Server not found"));

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

            _context.ServerRoles.Add(role);
            await _context.SaveChangesAsync(ct);

            var roleDto = role.ToDto();

            // Publish role created event
            await _eventBus.PublishAsync(new ServerRoleCreatedEvent(serverId, roleDto), ct);

            return Result<ServerRoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create role for server {ServerId}", serverId);
            return Result<ServerRoleDto>.Failure(Error.Internal("Failed to create role"));
        }
    }

    private static ICollection<ServerRolePermission> ConvertToServerRolePermissions(ICollection<PermissionType> permissions)
    {
        return permissions.Select(p => new ServerRolePermission { Permission = p }).ToList();
    }

    public async Task<Result<bool>> AddMemberAsync(
        Guid serverId,
        Guid userId,
        Guid? roleId = null,
        CancellationToken ct = default)
    {
        var server = await _context.Servers
            .Include(s => s.Members)
            .Include(s => s.Roles)
            .Include(s => s.Channels)
            .FirstOrDefaultAsync(s => s.Id == serverId, ct);

        if (server is null)
            return Result<bool>.Failure(Error.NotFound("Server not found"));

        // Check member limit
        if (server.Members.Count >= _limitSettings.MaxMembersPerServer)
            return Result<bool>.Failure(Error.Validation($"Server cannot have more than {_limitSettings.MaxMembersPerServer} members"));

        if (server.Members.Any(m => m.UserId == userId))
            return Result<bool>.Success(true);

        try
        {
            // If no role specified, use the default role
            if (roleId == null)
            {
                var defaultRole = server.Roles.FirstOrDefault(r => r.IsDefault);
                if (defaultRole == null)
                {
                    return Result<bool>.Failure(Error.Internal("Server has no default role"));
                }
                roleId = defaultRole.Id;
            }

            var member = new ServerMember
            {
                ServerId = serverId,
                UserId = userId,
                RoleId = roleId
            };

            _context.ServerMembers.Add(member);

            // Add member to all default channels
            foreach (var channel in server.Channels.Where(c => !c.IsPrivate))
            {
                var channelMember = new ChannelMember
                {
                    ChannelId = channel.Id,
                    UserId = userId
                };
                _context.ChannelMembers.Add(channelMember);
            }

            await _context.SaveChangesAsync(ct);

            // Load relationships for event
            await _context.Entry(member)
                .Reference(m => m.User)
                .LoadAsync(ct);

            await _context.Entry(member)
                .Reference(m => m.Role)
                .LoadAsync(ct);

            // Publish member joined event
            await _eventBus.PublishAsync(
                new ServerMemberJoinedEvent(serverId, member.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add member {UserId} to server {ServerId}", userId, serverId);
            return Result<bool>.Failure(Error.Internal("Failed to add member"));
        }
    }

    public async Task<Result<bool>> RemoveMemberAsync(
        Guid serverId,
        Guid userId,
        CancellationToken ct = default)
    {
        var member = await _context.ServerMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId, ct);

        if (member is null)
            return Result<bool>.Success(true);

        try
        {
            _context.ServerMembers.Remove(member);
            await _context.SaveChangesAsync(ct);

            // Publish member left event
            await _eventBus.PublishAsync(
                new ServerMemberRemovedEvent(serverId, member.User.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove member {UserId} from server {ServerId}", userId, serverId);
            return Result<bool>.Failure(Error.Internal("Failed to remove member"));
        }
    }

    public async Task<Result<bool>> UpdateMemberRoleAsync(
        Guid serverId,
        Guid userId,
        Guid roleId,
        CancellationToken ct = default)
    {
        var member = await _context.ServerMembers
            .Include(m => m.User)
            .Include(m => m.Role)
            .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId, ct);

        if (member is null)
            return Result<bool>.Failure(Error.NotFound("Member not found"));

        try
        {
            member.RoleId = roleId;
            await _context.SaveChangesAsync(ct);

            // Publish member updated event
            await _eventBus.PublishAsync(
                new ServerMemberUpdatedEvent(serverId, member.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update role for member {UserId} in server {ServerId}", userId, serverId);
            return Result<bool>.Failure(Error.Internal("Failed to update member role"));
        }
    }

    private async Task LoadServerRelationships(Server server, CancellationToken ct)
    {
        await _context.Entry(server)
            .Reference(s => s.Owner)
            .LoadAsync(ct);

        await _context.Entry(server)
            .Collection(s => s.Roles)
            .LoadAsync(ct);

        await _context.Entry(server)
            .Collection(s => s.Members)
            .Query()
            .Include(m => m.User)
            .Include(m => m.Role)
            .LoadAsync(ct);
    }
}
