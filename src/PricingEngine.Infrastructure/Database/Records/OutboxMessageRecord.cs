namespace PricingEngine.Infrastructure.Database.Records;

public class OutboxMessageRecord
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public required string PayloadJson { get; set; }
    public required string Status { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
}
