using System.Text.Json;

namespace PricingEngine.Api.Contracts;

public record CreateQuoteRequest(
    string ProductCode,
    string Channel,
    string Currency,
    JsonDocument Inputs);
