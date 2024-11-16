using Chatty.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Chatty.Backend.Tests.Helpers;

public static class TestDbContextFactory
{
    public static IDbContextFactory<ChattyDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<ChattyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PooledDbContextFactory<ChattyDbContext>(options);
    }

    public static ChattyDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ChattyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ChattyDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static void Destroy(ChattyDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    public static void Destroy(IDbContextFactory<ChattyDbContext> factory)
    {
        using var context = factory.CreateDbContext();
        context.Database.EnsureDeleted();
        context.Dispose();
    }
}
