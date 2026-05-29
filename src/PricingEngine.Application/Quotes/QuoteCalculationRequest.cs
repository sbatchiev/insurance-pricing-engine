using System.Text.Json;

namespace PricingEngine.Application.Quotes;

public record QuoteCalculationRequest(
    string ProductCode,
    string Channel,
    string Currency,
    JsonDocument Inputs,
    DateTimeOffset RequestedAt);
