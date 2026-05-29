namespace PricingEngine.Infrastructure.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync(string messageType, string payloadJson, CancellationToken cancellationToken);
}
