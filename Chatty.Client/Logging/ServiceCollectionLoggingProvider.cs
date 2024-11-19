using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chatty.Client.Logging;

internal class ServiceCollectionLoggingProvider<T>(IServiceCollection services) : ILoggerProvider
    where T : class
{
    public ILogger CreateLogger(string categoryName)
    {
        var serviceProvider = services.BuildServiceProvider();
        return (ILogger)serviceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        // No-op
    }
}
