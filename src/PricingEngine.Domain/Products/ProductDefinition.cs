using System.Text.Json;

namespace PricingEngine.Domain.Products;

public record ProductDefinition(
    Guid Id,
    string Code,
    string Name,
    int Version,
    bool IsActive,
    JsonDocument InputSchema,
    JsonDocument PricingConfig);
