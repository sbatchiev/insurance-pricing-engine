using PricingEngine.Domain.Quotes;

namespace PricingEngine.Application.Quotes;

public interface IQuoteRepository
{
    Task<Quote?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken);

    Task SaveAsync(Quote quote, CancellationToken cancellationToken);
}
