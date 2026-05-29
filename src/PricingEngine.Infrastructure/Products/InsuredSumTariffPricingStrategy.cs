using PricingEngine.Application.Products;
using PricingEngine.Application.Quotes;
using PricingEngine.Domain;
using PricingEngine.Domain.Products;
using PricingEngine.Domain.Quotes;

namespace PricingEngine.Infrastructure.Products;

public class InsuredSumTariffPricingStrategy : IProductPricingStrategy
{
    private const string PricingModelCode = "insured_sum_tariff";

    public bool CanPrice(ProductDefinition product)
    {
        return product.PricingConfig.RootElement.TryGetProperty("pricingModel", out var pricingModel)
            && string.Equals(pricingModel.GetString(), PricingModelCode, StringComparison.OrdinalIgnoreCase);
    }

    public Task<QuoteCalculationResult> CalculateAsync(
        ProductDefinition product,
        QuoteCalculationRequest request,
        CancellationToken cancellationToken)
    {
        var config = product.PricingConfig.RootElement;
        var insuredSumInput = config.GetProperty("insuredSumInput").GetString() ?? "insuredSum";
        var insuredSum = ReadRequiredDecimal(request, insuredSumInput);
        var tariff = config.GetProperty("tariff").GetDecimal();
        var fixedFee = config.GetProperty("fixedFee").GetDecimal();
        var taxRate = config.GetProperty("taxRate").GetDecimal();
        var policyFee = config.GetProperty("policyFee").GetDecimal();

        var netPremium = new Money(insuredSum * tariff, request.Currency);
        var taxes = new Money(netPremium.Amount * taxRate, request.Currency);
        var fees = new Money(fixedFee + policyFee, request.Currency);

        return Task.FromResult(new QuoteCalculationResult(new QuoteBreakdown(netPremium, taxes, fees)));
    }

    private static decimal ReadRequiredDecimal(QuoteCalculationRequest request, string propertyName)
    {
        if (!request.Inputs.RootElement.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"Required input '{propertyName}' was not provided.");
        }

        return property.ValueKind switch
        {
            System.Text.Json.JsonValueKind.Number => property.GetDecimal(),
            _ => throw new InvalidOperationException($"Input '{propertyName}' must be numeric.")
        };
    }
}
