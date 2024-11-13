using Microsoft.EntityFrameworkCore;
using Chatty.Backend.Data.Models;
using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data;

public sealed class ChattyDbContext : DbContext
{
    public ChattyDbContext(DbContextOptions<ChattyDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<PreKey> PreKeys => Set<PreKey>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<ServerRole> ServerRoles => Set<ServerRole>();
    public DbSet<ServerMember> ServerMembers => Set<ServerMember>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Call> Calls => Set<Call>();
    public DbSet<CallParticipant> CallParticipants => Set<CallParticipant>();
    public DbSet<Sticker> Stickers => Set<Sticker>();
    public DbSet<StickerPack> StickerPacks => Set<StickerPack>();
    public DbSet<ServerRolePermission> ServerRolePermissions => Set<ServerRolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.LastOnlineAt);

            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Locale).HasMaxLength(10);
        });

        // UserDevice configuration
        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.DeviceId }).IsUnique();
            entity.HasIndex(e => e.DeviceToken);

            entity.Property(e => e.DeviceName).HasMaxLength(100);
            entity.Property(e => e.DeviceType)
                .HasMaxLength(20)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<DeviceType>(v));

            entity.HasOne(d => d.User)
                .WithMany(u => u.Devices)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PreKey configuration
        modelBuilder.Entity<PreKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.DeviceId, e.PreKeyId }).IsUnique();

            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Server configuration
        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(s => s.Owner)
                .WithMany()
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ServerRole configuration
        modelBuilder.Entity<ServerRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ServerId, e.Name }).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(7);

            entity.HasOne(r => r.Server)
                .WithMany(s => s.Roles)
                .HasForeignKey(r => r.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ServerRolePermission configuration
        modelBuilder.Entity<ServerRolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.Permission });

            entity.Property(e => e.Permission)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<PermissionType>(v));

            entity.HasOne(p => p.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(p => p.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ServerMember configuration
        modelBuilder.Entity<ServerMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ServerId, e.UserId }).IsUnique();

            entity.Property(e => e.Nickname).HasMaxLength(50);

            entity.HasOne(m => m.Server)
                .WithMany(s => s.Members)
                .HasForeignKey(m => m.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Role)
                .WithMany(r => r.Members)
                .HasForeignKey(m => m.RoleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Channel configuration
        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.ChannelType)
                .HasMaxLength(20)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ChannelType>(v));

            entity.HasOne(c => c.Server)
                .WithMany(s => s.Channels)
                .HasForeignKey(c => c.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChannelMember configuration
        modelBuilder.Entity<ChannelMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ChannelId, e.UserId }).IsUnique();

            entity.HasOne(m => m.Channel)
                .WithMany(c => c.Members)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ChannelId, e.SentAt });
            entity.HasIndex(e => e.SenderId);
            entity.HasIndex(e => e.ParentMessageId);

            entity.Property(e => e.ContentType)
                .HasMaxLength(20)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ContentType>(v));

            entity.HasOne(m => m.Channel)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ParentMessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DirectMessage configuration
        modelBuilder.Entity<DirectMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SenderId, e.RecipientId, e.SentAt });
            entity.HasIndex(e => e.ParentMessageId);

            entity.Property(e => e.ContentType)
                .HasMaxLength(20)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ContentType>(v));

            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ParentMessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Attachment configuration
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId);
            entity.HasIndex(e => e.DirectMessageId);

            entity.Property(e => e.ContentType)
                .HasMaxLength(20)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ContentType>(v));

            entity.HasOne(a => a.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(a => a.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.DirectMessage)
                .WithMany(m => m.Attachments)
                .HasForeignKey(a => a.DirectMessageId)
                .OnDelete(DeleteBehavior.Cascade);


            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Attachment_MessageType",
                "(message_id IS NULL AND direct_message_id IS NOT NULL) OR (message_id IS NOT NULL AND direct_message_id IS NULL)"));
        });

        // Contact configuration
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ContactUserId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Status });

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ContactStatus>(v));

            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.ContactUser)
                .WithMany()
                .HasForeignKey(c => c.ContactUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Contact_Status",
                "status IN ('Pending', 'Accepted', 'Blocked')"));
        });

        // Call configuration
        modelBuilder.Entity<Call>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChannelId);
            entity.HasIndex(e => e.InitiatorId);
            entity.HasIndex(e => e.Status).HasFilter($"status != '{CallStatus.Ended}'");

            entity.Property(e => e.CallType)
                .HasMaxLength(10)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<CallType>(v));

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<CallStatus>(v));

            entity.HasOne(c => c.Channel)
                .WithMany()
                .HasForeignKey(c => c.ChannelId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(c => c.Initiator)
                .WithMany()
                .HasForeignKey(c => c.InitiatorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Call_CallType", $"call_type IN ('Voice', 'Video')");
                t.HasCheckConstraint("CK_Call_Status", $"status IN ('Initiated', 'Ringing', 'Connected', 'Ended')");
            });
        });

        // CallParticipant configuration
        modelBuilder.Entity<CallParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CallId, e.UserId }).IsUnique();

            entity.HasOne(p => p.Call)
                .WithMany(c => c.Participants)
                .HasForeignKey(p => p.CallId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StickerPack configuration
        modelBuilder.Entity<StickerPack>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(s => s.Creator)
                .WithMany()
                .HasForeignKey(s => s.CreatorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(s => s.EnabledServers)
                .WithMany()
                .UsingEntity(
                    "ServerStickerPacks",
                    l => l.HasOne(typeof(Server)).WithMany().HasForeignKey("ServerId"),
                    r => r.HasOne(typeof(StickerPack)).WithMany().HasForeignKey("StickerPackId"));
        });

        // Sticker configuration
        modelBuilder.Entity<Sticker>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Tags).HasColumnType("text[]");

            entity.HasOne(s => s.Pack)
                .WithMany(p => p.Stickers)
                .HasForeignKey(s => s.PackId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Creator)
                .WithMany()
                .HasForeignKey(s => s.CreatorId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}