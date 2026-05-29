namespace PricingEngine.Infrastructure.Outbox;

public class OutboxOptions
{
    public int BatchSize { get; set; } = 20;
    public int PollingIntervalSeconds { get; set; } = 5;
}
