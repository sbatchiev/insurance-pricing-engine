using PricingEngine.Domain.Products;

namespace PricingEngine.Application.Products;

public interface IProductDefinitionRepository
{
    Task<ProductDefinition?> GetActiveByCodeAsync(string productCode, CancellationToken cancellationToken);
}
