namespace PricingEngine.Infrastructure.Database.Records;

public class ProductDefinitionRecord
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public required string InputSchemaJson { get; set; }
    public required string PricingConfigJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
