using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PricingEngine.Infrastructure.Messaging;
using PricingEngine.Infrastructure.Database;

namespace PricingEngine.Infrastructure.Outbox;

public class OutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublisherBackgroundService> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unexpected error while publishing outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollingIntervalSeconds)), stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var now = DateTimeOffset.UtcNow;

        var messages = await dbContext.OutboxMessages
            .Where(message =>
                message.Status == OutboxMessageStatus.Pending &&
                (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.CreatedAt)
            .Take(Math.Max(1, _options.BatchSize))
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(message.Type, message.PayloadJson, cancellationToken);
                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.LastError = null;
                logger.LogInformation("Published outbox message {MessageId} of type {MessageType}.", message.Id, message.Type);
            }
            catch (Exception exception)
            {
                message.RetryCount++;
                message.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, message.RetryCount)));
                message.LastError = exception.Message;
                logger.LogWarning(exception, "Failed publishing outbox message {MessageId}; retry {RetryCount}.", message.Id, message.RetryCount);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
