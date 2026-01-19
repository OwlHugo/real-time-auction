using MassTransit;
using RealTimeAuction.Application.Common.Interfaces;

namespace RealTimeAuction.Infrastructure.Messaging;

public class RabbitMQMessagePublisher : IMessagePublisher
{
    private readonly IBus _bus;

    public RabbitMQMessagePublisher(IBus bus)
    {
        _bus = bus;
    }

    public async Task PublishAsync<T>(T message, string queueName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{queueName}"));
        await endpoint.Send(message);
    }
}
