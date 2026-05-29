namespace PricingEngine.Infrastructure.Database.Records;

public class QuoteRecord
{
    public Guid Id { get; set; }
    public required string ProductCode { get; set; }
    public int ProductVersion { get; set; }
    public required string Channel { get; set; }
    public required string Currency { get; set; }
    public decimal NetPremium { get; set; }
    public decimal Taxes { get; set; }
    public decimal Fees { get; set; }
    public decimal Total { get; set; }
    public required string RequestJson { get; set; }
    public required string ResponseJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
