using PricingEngine.Domain.Products;

namespace PricingEngine.Application.Products;

public class ProductPricingStrategyResolver(IEnumerable<IProductPricingStrategy> strategies)
{
    public IProductPricingStrategy Resolve(ProductDefinition product)
    {
        return strategies.FirstOrDefault(strategy => strategy.CanPrice(product))
            ?? throw new InvalidOperationException($"No pricing strategy registered for product '{product.Code}'.");
    }
}
