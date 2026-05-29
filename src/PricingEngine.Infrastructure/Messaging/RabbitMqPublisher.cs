using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PricingEngine.Infrastructure.Messaging;

public sealed class RabbitMqPublisher(IOptions<RabbitMqOptions> options) : IMessagePublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync(string messageType, string payloadJson, CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            Type = messageType,
            Persistent = true
        };

        var body = Encoding.UTF8.GetBytes(payloadJson);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
