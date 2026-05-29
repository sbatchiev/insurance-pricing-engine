using System.Text.Json;
using PricingEngine.Application.Quotes;
using PricingEngine.Domain;
using PricingEngine.Domain.Quotes;
using PricingEngine.Infrastructure.Database.Records;
using PricingEngine.Infrastructure.Outbox;

namespace PricingEngine.Infrastructure.Database;

public class QuoteRepository(PricingDbContext dbContext) : IQuoteRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Quote?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken)
    {
        var record = await dbContext.Quotes.FindAsync([quoteId], cancellationToken);
        if (record is null)
        {
            return null;
        }

        var storedQuote = JsonSerializer.Deserialize<StoredQuoteDocument>(record.ResponseJson, JsonOptions)
            ?? throw new InvalidOperationException($"Quote '{quoteId}' has invalid response JSON.");

        return new Quote(
            record.Id,
            record.ProductCode,
            record.ProductVersion,
            record.Channel,
            record.Currency,
            JsonDocument.Parse(record.RequestJson),
            new QuoteBreakdown(
                new Money(record.NetPremium, record.Currency),
                new Money(record.Taxes, record.Currency),
                new Money(record.Fees, record.Currency)),
            storedQuote.InstallmentOptions,
            record.CreatedAt);
    }

    public async Task SaveAsync(Quote quote, CancellationToken cancellationToken)
    {
        dbContext.Quotes.Add(new QuoteRecord
        {
            Id = quote.Id,
            ProductCode = quote.ProductCode,
            ProductVersion = quote.ProductVersion,
            Channel = quote.Channel,
            Currency = quote.Currency,
            NetPremium = quote.Breakdown.NetPremium.Amount,
            Taxes = quote.Breakdown.Taxes.Amount,
            Fees = quote.Breakdown.Fees.Amount,
            Total = quote.Breakdown.Total.Amount,
            RequestJson = quote.Inputs.RootElement.GetRawText(),
            ResponseJson = JsonSerializer.Serialize(quote, JsonOptions),
            CreatedAt = quote.CreatedAt
        });

        dbContext.OutboxMessages.Add(new OutboxMessageRecord
        {
            Id = Guid.NewGuid(),
            Type = "QuoteGenerated",
            PayloadJson = JsonSerializer.Serialize(new
            {
                quote.Id,
                quote.ProductCode,
                quote.ProductVersion,
                quote.Channel,
                quote.Currency,
                quote.Breakdown,
                quote.InstallmentOptions,
                quote.CreatedAt
            }, JsonOptions),
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private record StoredQuoteDocument(IReadOnlyCollection<InstallmentOption> InstallmentOptions);
}
