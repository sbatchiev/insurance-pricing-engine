using System.Text.Json;
using PricingEngine.Application.Products;
using PricingEngine.Application.Quotes;
using PricingEngine.Domain;
using PricingEngine.Domain.Products;
using PricingEngine.Domain.Quotes;

namespace PricingEngine.Tests;

public sealed class QuoteServiceTests
{
    [Fact]
    public async Task CreateQuoteAsync_persists_quote_with_requested_timestamp()
    {
        using var inputs = JsonDocument.Parse("""{ "insuredSum": 10000 }""");
        var requestedAt = new DateTimeOffset(2026, 5, 29, 10, 30, 0, TimeSpan.Zero);
        var repository = new InMemoryQuoteRepository();
        var productRepository = new SingleProductRepository(CreateProduct());
        var strategy = new FixedPricingStrategy();
        var service = new QuoteService(
            productRepository,
            new ProductPricingStrategyResolver([strategy]),
            new InstallmentOptionCalculator([1, 2]),
            repository);

        var quote = await service.CreateQuoteAsync(
            new QuoteCalculationRequest("travel-basic", "web", "EUR", inputs, requestedAt),
            CancellationToken.None);

        Assert.Same(quote, repository.SavedQuote);
        Assert.Equal(requestedAt, quote.CreatedAt);
        Assert.Equal("travel-basic", quote.ProductCode);
        Assert.Equal([1, 2], quote.InstallmentOptions.Select(option => option.InstallmentCount));
    }

    [Fact]
    public async Task CreateQuoteAsync_rejects_unknown_product()
    {
        using var inputs = JsonDocument.Parse("""{}""");
        var service = new QuoteService(
            new SingleProductRepository(null),
            new ProductPricingStrategyResolver([new FixedPricingStrategy()]),
            new InstallmentOptionCalculator([1]),
            new InMemoryQuoteRepository());

        await Assert.ThrowsAsync<UnknownProductException>(() =>
            service.CreateQuoteAsync(
                new QuoteCalculationRequest("unknown", "web", "EUR", inputs, DateTimeOffset.UtcNow),
                CancellationToken.None));
    }

    [Fact]
    public async Task GetQuoteAsync_returns_persisted_quote()
    {
        var quote = CreateQuote();
        var repository = new InMemoryQuoteRepository { SavedQuote = quote };
        var service = new QuoteService(
            new SingleProductRepository(CreateProduct()),
            new ProductPricingStrategyResolver([new FixedPricingStrategy()]),
            new InstallmentOptionCalculator([1]),
            repository);

        var result = await service.GetQuoteAsync(quote.Id, CancellationToken.None);

        Assert.Same(quote, result);
    }

    [Fact]
    public async Task GetQuoteAsync_returns_null_when_quote_does_not_exist()
    {
        var service = new QuoteService(
            new SingleProductRepository(CreateProduct()),
            new ProductPricingStrategyResolver([new FixedPricingStrategy()]),
            new InstallmentOptionCalculator([1]),
            new InMemoryQuoteRepository());

        var result = await service.GetQuoteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    private static ProductDefinition CreateProduct()
    {
        return new ProductDefinition(
            Guid.NewGuid(),
            "travel-basic",
            "Travel Basic",
            1,
            true,
            JsonDocument.Parse("{}"),
            JsonDocument.Parse("""{ "pricingModel": "fixed_test" }"""));
    }

    private static Quote CreateQuote()
    {
        return new Quote(
            Guid.NewGuid(),
            "travel-basic",
            1,
            "web",
            "EUR",
            JsonDocument.Parse("""{ "insuredSum": 10000 }"""),
            new QuoteBreakdown(
                new Money(100m, "EUR"),
                new Money(10m, "EUR"),
                new Money(5m, "EUR")),
            [new InstallmentOption(1, new Money(115m, "EUR"), [new InstallmentPayment(1, new DateOnly(2026, 5, 29), new Money(115m, "EUR"))])],
            new DateTimeOffset(2026, 5, 29, 10, 30, 0, TimeSpan.Zero));
    }

    private sealed class SingleProductRepository(ProductDefinition? product) : IProductDefinitionRepository
    {
        public Task<ProductDefinition?> GetActiveByCodeAsync(string productCode, CancellationToken cancellationToken) =>
            Task.FromResult(product?.Code == productCode ? product : null);
    }

    private sealed class InMemoryQuoteRepository : IQuoteRepository
    {
        public Quote? SavedQuote { get; set; }

        public Task<Quote?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken)
        {
            return Task.FromResult(SavedQuote?.Id == quoteId ? SavedQuote : null);
        }

        public Task SaveAsync(Quote quote, CancellationToken cancellationToken)
        {
            SavedQuote = quote;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedPricingStrategy : IProductPricingStrategy
    {
        public bool CanPrice(ProductDefinition product) => true;

        public Task<QuoteCalculationResult> CalculateAsync(
            ProductDefinition product,
            QuoteCalculationRequest request,
            CancellationToken cancellationToken)
        {
            var breakdown = new QuoteBreakdown(
                new Money(100m, request.Currency),
                new Money(10m, request.Currency),
                new Money(5m, request.Currency));

            return Task.FromResult(new QuoteCalculationResult(breakdown));
        }
    }
}
