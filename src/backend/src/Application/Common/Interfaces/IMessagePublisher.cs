namespace RealTimeAuction.Application.Common.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string queueName)
        where T : class;
}
