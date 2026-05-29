using PricingEngine.Application.Quotes;
using PricingEngine.Domain.Products;

namespace PricingEngine.Application.Products;

public interface IProductPricingStrategy
{
    bool CanPrice(ProductDefinition product);

    Task<QuoteCalculationResult> CalculateAsync(
        ProductDefinition product,
        QuoteCalculationRequest request,
        CancellationToken cancellationToken);
}
