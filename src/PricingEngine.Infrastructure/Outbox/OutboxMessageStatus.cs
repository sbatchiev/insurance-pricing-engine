namespace PricingEngine.Infrastructure.Outbox;

public static class OutboxMessageStatus
{
    public const string Pending = "Pending";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
}
