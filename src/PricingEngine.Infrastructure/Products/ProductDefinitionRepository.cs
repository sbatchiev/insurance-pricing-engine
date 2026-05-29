using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PricingEngine.Application.Products;
using PricingEngine.Domain.Products;
using PricingEngine.Infrastructure.Database;

namespace PricingEngine.Infrastructure.Products;

public class ProductDefinitionRepository(PricingDbContext dbContext) : IProductDefinitionRepository
{
    public async Task<ProductDefinition?> GetActiveByCodeAsync(string productCode, CancellationToken cancellationToken)
    {
        var record = await dbContext.ProductDefinitions
            .AsNoTracking()
            .Where(product => product.Code == productCode && product.IsActive)
            .OrderByDescending(product => product.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return record is null
            ? null
            : new ProductDefinition(
                record.Id,
                record.Code,
                record.Name,
                record.Version,
                record.IsActive,
                JsonDocument.Parse(record.InputSchemaJson),
                JsonDocument.Parse(record.PricingConfigJson));
    }
}
