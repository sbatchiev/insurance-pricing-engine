using System.Text.Json;
using PricingEngine.Application.Quotes;
using PricingEngine.Domain.Products;
using PricingEngine.Infrastructure.Products;

namespace PricingEngine.Tests;

public sealed class InsuredSumTariffPricingStrategyTests
{
    [Fact]
    public void CanPrice_matches_pricing_model_code()
    {
        var product = CreateProduct("""
            {
              "pricingModel": "insured_sum_tariff",
              "insuredSumInput": "insuredSum",
              "tariff": 0.015,
              "fixedFee": 10.00,
              "taxRate": 0.10,
              "policyFee": 2.50
            }
            """);

        var strategy = new InsuredSumTariffPricingStrategy();

        Assert.True(strategy.CanPrice(product));
    }

    [Fact]
    public void CanPrice_rejects_different_pricing_model_code()
    {
        var product = CreateProduct("""{ "pricingModel": "tiered_tariff" }""");

        var strategy = new InsuredSumTariffPricingStrategy();

        Assert.False(strategy.CanPrice(product));
    }

    [Fact]
    public async Task CalculateAsync_uses_product_configuration_for_breakdown()
    {
        var product = CreateProduct("""
            {
              "pricingModel": "insured_sum_tariff",
              "insuredSumInput": "insuredSum",
              "tariff": 0.015,
              "fixedFee": 10.00,
              "taxRate": 0.10,
              "policyFee": 2.50
            }
            """);
        using var inputs = JsonDocument.Parse("""{ "insuredSum": 10000 }""");

        var strategy = new InsuredSumTariffPricingStrategy();

        var result = await strategy.CalculateAsync(
            product,
            new QuoteCalculationRequest("travel-basic", "web", "EUR", inputs, DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal(150.00m, result.Breakdown.NetPremium.Amount);
        Assert.Equal(15.00m, result.Breakdown.Taxes.Amount);
        Assert.Equal(12.50m, result.Breakdown.Fees.Amount);
        Assert.Equal(177.50m, result.Breakdown.Total.Amount);
    }

    [Fact]
    public async Task CalculateAsync_rejects_missing_required_input()
    {
        var product = CreateProduct("""
            {
              "pricingModel": "insured_sum_tariff",
              "insuredSumInput": "insuredSum",
              "tariff": 0.015,
              "fixedFee": 10.00,
              "taxRate": 0.10,
              "policyFee": 2.50
            }
            """);
        using var inputs = JsonDocument.Parse("""{ "durationDays": 7 }""");

        var strategy = new InsuredSumTariffPricingStrategy();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            strategy.CalculateAsync(
                product,
                new QuoteCalculationRequest("travel-basic", "web", "EUR", inputs, DateTimeOffset.UtcNow),
                CancellationToken.None));
    }

    private static ProductDefinition CreateProduct(string pricingConfigJson)
    {
        using var inputSchema = JsonDocument.Parse("{}");
        var pricingConfig = JsonDocument.Parse(pricingConfigJson);

        return new ProductDefinition(
            Guid.NewGuid(),
            "travel-basic",
            "Travel Basic",
            1,
            true,
            JsonDocument.Parse(inputSchema.RootElement.GetRawText()),
            pricingConfig);
    }
}
