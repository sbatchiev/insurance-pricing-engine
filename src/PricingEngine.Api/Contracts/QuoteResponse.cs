using PricingEngine.Domain.Quotes;

namespace PricingEngine.Api.Contracts;

public record QuoteResponse(
    Guid QuoteId,
    string ProductCode,
    int ProductVersion,
    string Channel,
    string Currency,
    QuoteBreakdown Breakdown,
    IReadOnlyCollection<InstallmentOption> InstallmentOptions,
    DateTimeOffset CreatedAt)
{
    public static QuoteResponse FromQuote(Quote quote) =>
        new(
            quote.Id,
            quote.ProductCode,
            quote.ProductVersion,
            quote.Channel,
            quote.Currency,
            quote.Breakdown,
            quote.InstallmentOptions,
            quote.CreatedAt);
}
