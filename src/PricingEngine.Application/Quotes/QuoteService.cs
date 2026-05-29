using PricingEngine.Application.Products;
using PricingEngine.Domain.Quotes;

namespace PricingEngine.Application.Quotes;

public class QuoteService(
    IProductDefinitionRepository products,
    ProductPricingStrategyResolver strategyResolver,
    InstallmentOptionCalculator installmentOptionCalculator,
    IQuoteRepository quotes)
{
    public Task<Quote?> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken)
    {
        return quotes.GetByIdAsync(quoteId, cancellationToken);
    }

    public async Task<Quote> CreateQuoteAsync(QuoteCalculationRequest request, CancellationToken cancellationToken)
    {
        var product = await products.GetActiveByCodeAsync(request.ProductCode, cancellationToken)
            ?? throw new UnknownProductException(request.ProductCode);

        var strategy = strategyResolver.Resolve(product);
        var calculation = await strategy.CalculateAsync(product, request, cancellationToken);
        var installments = installmentOptionCalculator.Calculate(calculation.Breakdown.Total, DateOnly.FromDateTime(request.RequestedAt.UtcDateTime));

        var quote = new Quote(
            Guid.NewGuid(),
            product.Code,
            product.Version,
            request.Channel,
            request.Currency,
            request.Inputs,
            calculation.Breakdown,
            installments,
            request.RequestedAt);

        await quotes.SaveAsync(quote, cancellationToken);
        return quote;
    }
}

public sealed class UnknownProductException(string productCode)
    : Exception($"Product '{productCode}' does not exist or is not active.");
