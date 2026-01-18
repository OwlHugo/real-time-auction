using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using RealTimeAuction.Application.Auctions.Commands.CreateAuction;
using RealTimeAuction.Application.Common.Behaviours;
using RealTimeAuction.Application.Common.Interfaces;

namespace RealTimeAuction.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<CreateAuctionCommand>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<CreateAuctionCommand>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _user.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());

        var requestLogger = new LoggingBehaviour<CreateAuctionCommand>(
            _logger.Object,
            _user.Object,
            _identityService.Object
        );

        await requestLogger.Process(
            new CreateAuctionCommand { Title = "title", StartingPrice = 10 },
            new CancellationToken()
        );

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<CreateAuctionCommand>(
            _logger.Object,
            _user.Object,
            _identityService.Object
        );

        await requestLogger.Process(
            new CreateAuctionCommand { Title = "title", StartingPrice = 10 },
            new CancellationToken()
        );

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Never);
    }
}
