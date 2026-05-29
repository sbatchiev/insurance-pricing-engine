using System.Text.Json;

namespace PricingEngine.Domain.Quotes;

public record Quote(
    Guid Id,
    string ProductCode,
    int ProductVersion,
    string Channel,
    string Currency,
    JsonDocument Inputs,
    QuoteBreakdown Breakdown,
    IReadOnlyCollection<InstallmentOption> InstallmentOptions,
    DateTimeOffset CreatedAt);
