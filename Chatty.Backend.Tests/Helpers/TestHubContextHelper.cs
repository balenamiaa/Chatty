using Microsoft.AspNetCore.SignalR;

using Moq;

namespace Chatty.Backend.Tests.Helpers;

public static class TestHubContextHelper
{
    public static Mock<IHubContext<THub, TClient>> CreateHubContext<THub, TClient>()
        where THub : Hub<TClient>
        where TClient : class
    {
        var mockClients = new Mock<IHubClients<TClient>>();
        var mockClientProxy = new Mock<TClient>();

        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        mockClients.Setup(c => c.Client(It.IsAny<string>()))
            .Returns(mockClientProxy.Object);

        mockClients.Setup(c => c.Clients(It.IsAny<IReadOnlyList<string>>()))
            .Returns(mockClientProxy.Object);

        mockClients.Setup(c => c.Group(It.IsAny<string>()))
            .Returns(mockClientProxy.Object);

        mockClients.Setup(c => c.Groups(It.IsAny<IReadOnlyList<string>>()))
            .Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<THub, TClient>>();
        mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

        return mockHubContext;
    }
}
